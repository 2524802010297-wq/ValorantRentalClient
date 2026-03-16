using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValorantRentalServer.Models;
using System.Security.Cryptography;
using System.Text;

namespace ValorantRentalServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminController> _logger;

        public AdminController(AppDbContext context, IConfiguration configuration, ILogger<AdminController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // ==================== STATISTICS ====================

        // GET: api/admin/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var today = DateTime.Today;
                
                var stats = new
                {
                    totalUsers = await _context.Users.CountAsync(),
                    activeSessions = await _context.ValorantSessions.CountAsync(s => s.Status == "Active"),
                    availableAccounts = await _context.RiotAccounts.CountAsync(a => a.IsAvailable && a.AccountStatus == "Good"),
                    totalAccounts = await _context.RiotAccounts.CountAsync(),
                    todayRevenue = await _context.Transactions
                        .Where(t => t.CreatedDate >= today && t.Status == "Success")
                        .SumAsync(t => (decimal?)t.Amount) ?? 0,
                    totalRevenue = await _context.Transactions
                        .Where(t => t.Status == "Success")
                        .SumAsync(t => (decimal?)t.Amount) ?? 0,
                    violations = await _context.Violations.CountAsync(v => v.DetectedTime >= today),
                    sessionsToday = await _context.ValorantSessions.CountAsync(s => s.StartTime >= today),
                    revenueByDay = await GetLast7DaysRevenue(),
                    recentActivities = await GetRecentActivities(),
                    valorantSessions = await _context.ValorantSessions.CountAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy statistics");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== KEY MANAGEMENT - ĐÃ SỬA LỖI KHÓA NGOẠI ====================

        // POST: api/admin/generate-valorant-keys
        [HttpPost("generate-valorant-keys")]
        public async Task<IActionResult> GenerateValorantKeys([FromBody] KeyGenerationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request không hợp lệ" });
                }

                if (request.Quantity <= 0 || request.Quantity > 100)
                {
                    return BadRequest(new { success = false, message = "Số lượng key phải từ 1-100" });
                }

                var keys = new List<ValorantKey>();
                
                for (int i = 0; i < request.Quantity; i++)
                {
                    var key = new ValorantKey
                    {
                        KeyCode = GenerateValorantKeyCode(),
                        PackageType = request.PackageType ?? "Day",
                        Duration = request.Duration,
                        Price = request.Price,
                        CreatedDate = DateTime.Now,
                        ExpiryDate = DateTime.Now.AddMonths(6),
                        Status = "Available"
                    };
                    
                    keys.Add(key);
                }

                await _context.ValorantKeys.AddRangeAsync(keys);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã tạo {request.Quantity} key thành công");

                return Ok(new { success = true, keys = keys.Select(k => k.KeyCode) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo key");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // GET: api/admin/keys
        [HttpGet("keys")]
        public async Task<IActionResult> GetKeys()
        {
            try
            {
                var keys = await _context.ValorantKeys
                    .OrderByDescending(k => k.CreatedDate)
                    .Take(100)
                    .Select(k => new
                    {
                        k.KeyId,
                        k.KeyCode,
                        k.PackageType,
                        k.Duration,
                        k.Price,
                        k.Status,
                        k.CreatedDate,
                        k.ExpiryDate,
                        k.IsSold,
                        Buyer = k.SoldToUserId != null ? 
                            _context.Users.Where(u => u.UserId == k.SoldToUserId).Select(u => u.Username).FirstOrDefault() : null,
                        ActivatedDate = k.ActivatedDate
                    })
                    .ToListAsync();

                return Ok(keys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách keys");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // DELETE: api/admin/keys/{keyCode} - XÓA THƯỜNG (CHỈ KHI KHÔNG CÓ LỊCH SỬ)
        [HttpDelete("keys/{keyCode}")]
        public async Task<IActionResult> DeleteKey(string keyCode)
        {
            try
            {
                _logger.LogInformation($"Yêu cầu xóa key: {keyCode}");
                
                if (string.IsNullOrEmpty(keyCode))
                {
                    return BadRequest(new { success = false, message = "Key code không hợp lệ" });
                }
                
                // Tìm key trong database
                var key = await _context.ValorantKeys
                    .FirstOrDefaultAsync(k => k.KeyCode == keyCode);
                    
                if (key == null)
                {
                    _logger.LogWarning($"Không tìm thấy key: {keyCode}");
                    return NotFound(new { success = false, message = "Key không tồn tại" });
                }

                // KIỂM TRA KEY CÓ ĐANG ĐƯỢC SỬ DỤNG KHÔNG
                var activeSession = await _context.ValorantSessions
                    .FirstOrDefaultAsync(s => s.KeyId == key.KeyId && s.Status == "Active");
                    
                if (activeSession != null)
                {
                    _logger.LogWarning($"Key {keyCode} đang được sử dụng, không thể xóa");
                    return BadRequest(new { success = false, message = "Key đang được sử dụng, không thể xóa" });
                }

                // KIỂM TRA KEY CÓ TRONG LỊCH SỬ KHÔNG
                var hasHistory = await _context.ValorantSessions
                    .AnyAsync(s => s.KeyId == key.KeyId);
                    
                if (hasHistory)
                {
                    _logger.LogWarning($"Key {keyCode} có lịch sử sử dụng, yêu cầu force delete");
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Key đã có lịch sử sử dụng, không thể xóa. Vui lòng dùng chức năng FORCE DELETE.",
                        requiresForce = true
                    });
                }

                // Xóa key
                _context.ValorantKeys.Remove(key);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa key thành công: {keyCode}");
                
                return Ok(new { success = true, message = "Đã xóa key thành công" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Lỗi database khi xóa key: {keyCode}");
                
                // Kiểm tra lỗi khóa ngoại
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Key đã có lịch sử sử dụng, không thể xóa. Vui lòng dùng chức năng FORCE DELETE.",
                        requiresForce = true,
                        error = dbEx.Message,
                        innerError = dbEx.InnerException.Message
                    });
                }
                
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Lỗi database khi xóa key",
                    error = dbEx.Message,
                    innerError = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa key: {keyCode}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // DELETE: api/admin/keys/force/{keyCode} - XÓA MẠNH (KỂ CẢ CÓ LỊCH SỬ)
        [HttpDelete("keys/force/{keyCode}")]
        public async Task<IActionResult> ForceDeleteKey(string keyCode)
        {
            try
            {
                _logger.LogInformation($"Yêu cầu FORCE xóa key: {keyCode}");
                
                if (string.IsNullOrEmpty(keyCode))
                {
                    return BadRequest(new { success = false, message = "Key code không hợp lệ" });
                }
                
                // Tìm key trong database (Include sessions để xóa)
                var key = await _context.ValorantKeys
                    .FirstOrDefaultAsync(k => k.KeyCode == keyCode);
                    
                if (key == null)
                {
                    _logger.LogWarning($"Không tìm thấy key: {keyCode}");
                    return NotFound(new { success = false, message = "Key không tồn tại" });
                }

                // BẮT ĐẦU TRANSACTION ĐỂ ĐẢM BẢO TÍNH TOÀN VẸN
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Xóa tất cả sessions liên quan đến key này
                        var sessions = await _context.ValorantSessions
                            .Where(s => s.KeyId == key.KeyId)
                            .ToListAsync();
                        
                        if (sessions.Any())
                        {
                            _context.ValorantSessions.RemoveRange(sessions);
                            _logger.LogInformation($"Đã xóa {sessions.Count} sessions liên quan đến key {keyCode}");
                            
                            // Cập nhật trạng thái tài khoản nếu có session active
                            foreach (var session in sessions.Where(s => s.Status == "Active"))
                            {
                                var account = await _context.RiotAccounts.FindAsync(session.AccountId);
                                if (account != null)
                                {
                                    account.IsAvailable = true;
                                    account.CurrentUserId = null;
                                    _logger.LogInformation($"Đã trả lại tài khoản {account.RiotUsername} về trạng thái available");
                                }
                            }
                        }

                        // Xóa violations liên quan (nếu có)
                        var sessionIds = sessions.Select(s => s.SessionId).ToList();
                        if (sessionIds.Any())
                        {
                            var violations = await _context.Violations
                                .Where(v => sessionIds.Contains(v.SessionId))
                                .ToListAsync();
                            
                            if (violations.Any())
                            {
                                _context.Violations.RemoveRange(violations);
                                _logger.LogInformation($"Đã xóa {violations.Count} violations liên quan");
                            }
                        }

                        // Xóa key
                        _context.ValorantKeys.Remove(key);
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Đã FORCE xóa key thành công: {keyCode}");
                        
                        return Ok(new { success = true, message = "Đã xóa key thành công (bao gồm cả lịch sử)" });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi force xóa key: {keyCode}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== RIOT ACCOUNTS MANAGEMENT - ĐÃ SỬA LỖI KHÓA NGOẠI ====================

        // GET: api/admin/riot-accounts
        [HttpGet("riot-accounts")]
        public async Task<IActionResult> GetRiotAccounts()
        {
            try
            {
                var accounts = await _context.RiotAccounts
                    .OrderByDescending(a => a.AccountId)
                    .Select(a => new
                    {
                        a.AccountId,
                        a.RiotUsername,
                        a.Region,
                        a.IsAvailable,
                        a.AccountStatus,
                        a.TotalUsed,
                        a.LastUsedDate,
                        a.GameName,
                        a.Notes,
                        Password = "********", // Ẩn mật khẩu thật
                        CurrentUser = a.CurrentUserId != null ? 
                            _context.Users.Where(u => u.UserId == a.CurrentUserId).Select(u => u.Username).FirstOrDefault() : null
                    })
                    .ToListAsync();

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách tài khoản");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/admin/add-riot-account
        [HttpPost("add-riot-account")]
        public async Task<IActionResult> AddRiotAccount([FromBody] AddRiotAccountRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request không hợp lệ" });
                }

                if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { success = false, message = "Username và password không được để trống" });
                }

                // Kiểm tra username đã tồn tại chưa
                var existingAccount = await _context.RiotAccounts
                    .FirstOrDefaultAsync(a => a.RiotUsername == request.Username);
                    
                if (existingAccount != null)
                {
                    return BadRequest(new { success = false, message = "Username đã tồn tại trong hệ thống" });
                }

                string encryptedPassword = EncryptPassword(request.Password);

                var account = new RiotAccount
                {
                    RiotUsername = request.Username,
                    RiotPassword = encryptedPassword,
                    Region = request.Region ?? "VN",
                    IsAvailable = true,
                    AccountStatus = "Good",
                    GameName = "Valorant",
                    TotalUsed = 0,
                    Notes = request.Notes
                };

                _context.RiotAccounts.Add(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã thêm tài khoản mới: {request.Username}");

                return Ok(new { success = true, accountId = account.AccountId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thêm tài khoản");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // DELETE: api/admin/riot-accounts/{accountId} - XÓA THƯỜNG (CHỈ KHI KHÔNG CÓ LỊCH SỬ)
        [HttpDelete("riot-accounts/{accountId}")]
        public async Task<IActionResult> DeleteRiotAccount(int accountId)
        {
            try
            {
                _logger.LogInformation($"Yêu cầu xóa tài khoản ID: {accountId}");
                
                if (accountId <= 0)
                {
                    return BadRequest(new { success = false, message = "ID tài khoản không hợp lệ" });
                }

                // Tìm tài khoản
                var account = await _context.RiotAccounts.FindAsync(accountId);
                    
                if (account == null)
                {
                    _logger.LogWarning($"Không tìm thấy tài khoản ID: {accountId}");
                    return NotFound(new { success = false, message = "Tài khoản không tồn tại" });
                }

                // Kiểm tra tài khoản có đang được sử dụng không
                var activeSession = await _context.ValorantSessions
                    .FirstOrDefaultAsync(s => s.AccountId == accountId && s.Status == "Active");
                    
                if (activeSession != null)
                {
                    _logger.LogWarning($"Tài khoản ID {accountId} đang được sử dụng, không thể xóa");
                    return BadRequest(new { success = false, message = "Tài khoản đang được sử dụng, không thể xóa" });
                }

                // Kiểm tra tài khoản có trong lịch sử không
                var hasHistory = await _context.ValorantSessions
                    .AnyAsync(s => s.AccountId == accountId);
                    
                if (hasHistory)
                {
                    _logger.LogWarning($"Tài khoản ID {accountId} có lịch sử sử dụng, yêu cầu force delete");
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Tài khoản đã có lịch sử sử dụng, không thể xóa. Vui lòng dùng chức năng FORCE DELETE.",
                        requiresForce = true
                    });
                }

                // Xóa tài khoản
                _context.RiotAccounts.Remove(account);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã xóa tài khoản thành công ID: {accountId}");
                
                return Ok(new { success = true, message = "Đã xóa tài khoản thành công" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Lỗi database khi xóa tài khoản ID: {accountId}");
                
                if (dbEx.InnerException != null && dbEx.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Tài khoản đã có lịch sử sử dụng, không thể xóa. Vui lòng dùng chức năng FORCE DELETE.",
                        requiresForce = true,
                        error = dbEx.Message,
                        innerError = dbEx.InnerException.Message
                    });
                }
                
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "Lỗi database khi xóa tài khoản",
                    error = dbEx.Message,
                    innerError = dbEx.InnerException?.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa tài khoản ID: {accountId}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // DELETE: api/admin/riot-accounts/force/{accountId} - XÓA MẠNH (KỂ CẢ CÓ LỊCH SỬ)
        [HttpDelete("riot-accounts/force/{accountId}")]
        public async Task<IActionResult> ForceDeleteRiotAccount(int accountId)
        {
            try
            {
                _logger.LogInformation($"Yêu cầu FORCE xóa tài khoản ID: {accountId}");
                
                if (accountId <= 0)
                {
                    return BadRequest(new { success = false, message = "ID tài khoản không hợp lệ" });
                }
                
                var account = await _context.RiotAccounts.FindAsync(accountId);
                    
                if (account == null)
                {
                    _logger.LogWarning($"Không tìm thấy tài khoản ID: {accountId}");
                    return NotFound(new { success = false, message = "Tài khoản không tồn tại" });
                }

                // BẮT ĐẦU TRANSACTION
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        // Xóa tất cả sessions liên quan đến tài khoản này
                        var sessions = await _context.ValorantSessions
                            .Where(s => s.AccountId == accountId)
                            .ToListAsync();
                        
                        if (sessions.Any())
                        {
                            _context.ValorantSessions.RemoveRange(sessions);
                            _logger.LogInformation($"Đã xóa {sessions.Count} sessions liên quan đến tài khoản ID {accountId}");
                            
                            // Cập nhật trạng thái keys nếu cần
                            foreach (var session in sessions)
                            {
                                var key = await _context.ValorantKeys.FindAsync(session.KeyId);
                                if (key != null && session.Status == "Active")
                                {
                                    key.Status = "Expired";
                                }
                            }
                        }

                        // Xóa violations liên quan
                        var sessionIds = sessions.Select(s => s.SessionId).ToList();
                        if (sessionIds.Any())
                        {
                            var violations = await _context.Violations
                                .Where(v => sessionIds.Contains(v.SessionId))
                                .ToListAsync();
                            
                            if (violations.Any())
                            {
                                _context.Violations.RemoveRange(violations);
                                _logger.LogInformation($"Đã xóa {violations.Count} violations liên quan");
                            }
                        }

                        // Xóa tài khoản
                        _context.RiotAccounts.Remove(account);
                        await _context.SaveChangesAsync();

                        // Commit transaction
                        await transaction.CommitAsync();

                        _logger.LogInformation($"Đã FORCE xóa tài khoản thành công ID: {accountId}");
                        
                        return Ok(new { success = true, message = "Đã xóa tài khoản thành công (bao gồm cả lịch sử)" });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi force xóa tài khoản ID: {accountId}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== SESSIONS MANAGEMENT ====================

        // GET: api/admin/sessions
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var sessions = await _context.ValorantSessions
                    .OrderByDescending(s => s.StartTime)
                    .Take(50)
                    .Select(s => new
                    {
                        s.SessionId,
                        s.UserId,
                        Username = _context.Users.Where(u => u.UserId == s.UserId).Select(u => u.Username).FirstOrDefault(),
                        AccountUsername = _context.RiotAccounts.Where(a => a.AccountId == s.AccountId).Select(a => a.RiotUsername).FirstOrDefault(),
                        s.StartTime,
                        s.EndTime,
                        s.Duration,
                        s.Status,
                        KeyCode = _context.ValorantKeys.Where(k => k.KeyId == s.KeyId).Select(k => k.KeyCode).FirstOrDefault(),
                        TimeLeft = s.Status == "Active" ? 
                            (int?)((s.StartTime.AddHours(
                                _context.ValorantKeys.Where(k => k.KeyId == s.KeyId).Select(k => k.Duration).FirstOrDefault()
                            ) - DateTime.Now).TotalMinutes) : null
                    })
                    .ToListAsync();

                return Ok(sessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách sessions");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/admin/sessions/{sessionId}/end
        [HttpPost("sessions/{sessionId}/end")]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            try
            {
                if (sessionId <= 0)
                {
                    return BadRequest(new { success = false, message = "Session ID không hợp lệ" });
                }

                var session = await _context.ValorantSessions
                    .Include(s => s.Key)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);
                    
                if (session == null)
                {
                    return NotFound(new { success = false, message = "Session không tồn tại" });
                }

                if (session.Status == "Active")
                {
                    session.EndTime = DateTime.Now;
                    session.Duration = (int)(DateTime.Now - session.StartTime).TotalMinutes;
                    session.Status = "Completed";

                    var account = await _context.RiotAccounts.FindAsync(session.AccountId);
                    if (account != null)
                    {
                        account.IsAvailable = true;
                        account.CurrentUserId = null;
                        account.TotalUsed++;
                    }

                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation($"Đã kết thúc session {sessionId}");
                }

                return Ok(new { success = true, message = "Đã kết thúc session" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi kết thúc session {sessionId}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== VIOLATIONS ====================

        // GET: api/admin/violations
        [HttpGet("violations")]
        public async Task<IActionResult> GetViolations()
        {
            try
            {
                var violations = await _context.Violations
                    .OrderByDescending(v => v.DetectedTime)
                    .Take(50)
                    .Select(v => new
                    {
                        v.ViolationId,
                        v.UserId,
                        Username = _context.Users.Where(u => u.UserId == v.UserId).Select(u => u.Username).FirstOrDefault(),
                        v.ViolationType,
                        v.Details,
                        v.DetectedTime,
                        v.Action,
                        SessionId = v.SessionId
                    })
                    .ToListAsync();

                return Ok(violations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách violations");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== REVENUE ====================

        // GET: api/admin/revenue
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenue()
        {
            try
            {
                var today = DateTime.Today;
                var weekStart = today.AddDays(-7);
                var monthStart = today.AddMonths(-1);
                var yearStart = today.AddYears(-1);

                var dailyData = await _context.Transactions
                    .Where(t => t.CreatedDate >= weekStart && t.Status == "Success")
                    .GroupBy(t => t.CreatedDate.Date)
                    .Select(g => new
                    {
                        date = g.Key.ToString("dd/MM"),
                        amount = g.Sum(t => t.Amount)
                    })
                    .OrderBy(g => g.date)
                    .ToListAsync();

                var monthlyData = await _context.Transactions
                    .Where(t => t.CreatedDate >= yearStart && t.Status == "Success")
                    .GroupBy(t => new { t.CreatedDate.Year, t.CreatedDate.Month })
                    .Select(g => new
                    {
                        month = $"{g.Key.Month}/{g.Key.Year}",
                        amount = g.Sum(t => t.Amount)
                    })
                    .OrderBy(g => g.month)
                    .ToListAsync();

                return Ok(new
                {
                    today = await _context.Transactions.Where(t => t.CreatedDate >= today && t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0,
                    week = await _context.Transactions.Where(t => t.CreatedDate >= weekStart && t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0,
                    month = await _context.Transactions.Where(t => t.CreatedDate >= monthStart && t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0,
                    total = await _context.Transactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0,
                    daily = dailyData,
                    monthly = monthlyData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy doanh thu");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== USERS ====================

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(50)
                    .Select(u => new
                    {
                        u.UserId,
                        u.Username,
                        u.Email,
                        u.PhoneNumber,
                        u.CreatedDate,
                        u.IsActive,
                        u.TotalSpent,
                        SessionCount = _context.ValorantSessions.Count(s => s.UserId == u.UserId),
                        TotalTime = _context.ValorantSessions.Where(s => s.UserId == u.UserId).Sum(s => (int?)s.Duration) ?? 0,
                        LastSession = _context.ValorantSessions
                            .Where(s => s.UserId == u.UserId)
                            .OrderByDescending(s => s.StartTime)
                            .Select(s => s.StartTime)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách users");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // POST: api/admin/users/{userId}/toggle
        [HttpPost("users/{userId}/toggle")]
        public async Task<IActionResult> ToggleUserStatus(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { success = false, message = "User ID không hợp lệ" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User không tồn tại" });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã {(user.IsActive ? "kích hoạt" : "vô hiệu hóa")} user {userId}");
                
                return Ok(new { success = true, isActive = user.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi toggle user {userId}");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ==================== HELPER METHODS ====================

        private string GenerateValorantKeyCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return "VAL" + new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string EncryptPassword(string password)
        {
            try
            {
                var keyString = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey32BytesLongHere!!!";
                // Đảm bảo key đúng 32 bytes
                var key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = new byte[16]; // Zero IV for simplicity

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);

                    return Convert.ToBase64String(encryptedBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi mã hóa password");
                return password; // Fallback to plain text nếu lỗi
            }
        }

        private async Task<List<int>> GetLast7DaysRevenue()
        {
            var result = new List<int>();
            var today = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var revenue = await _context.Transactions
                    .Where(t => t.CreatedDate.Date == date && t.Status == "Success")
                    .SumAsync(t => (int?)t.Amount) ?? 0;
                result.Add(revenue);
            }

            return result;
        }

        private async Task<List<object>> GetRecentActivities()
        {
            var activities = new List<object>();

            try
            {
                // Lấy 5 session gần nhất
                var recentSessions = await _context.ValorantSessions
                    .OrderByDescending(s => s.StartTime)
                    .Take(5)
                    .Select(s => new
                    {
                        time = s.StartTime,
                        user = _context.Users.Where(u => u.UserId == s.UserId).Select(u => u.Username).FirstOrDefault() ?? $"User {s.UserId}",
                        action = "Bắt đầu chơi",
                        details = $"Session {s.SessionId}"
                    })
                    .ToListAsync();

                activities.AddRange(recentSessions);

                // Lấy 5 transaction gần nhất
                var recentTransactions = await _context.Transactions
                    .OrderByDescending(t => t.CreatedDate)
                    .Take(5)
                    .Select(t => new
                    {
                        time = t.CreatedDate,
                        user = _context.Users.Where(u => u.UserId == t.UserId).Select(u => u.Username).FirstOrDefault() ?? $"User {t.UserId}",
                        action = "Thanh toán",
                        details = $"{t.Amount:N0}đ"
                    })
                    .ToListAsync();

                activities.AddRange(recentTransactions);

                // Lấy 5 violation gần nhất
                var recentViolations = await _context.Violations
                    .OrderByDescending(v => v.DetectedTime)
                    .Take(5)
                    .Select(v => new
                    {
                        time = v.DetectedTime,
                        user = _context.Users.Where(u => u.UserId == v.UserId).Select(u => u.Username).FirstOrDefault() ?? $"User {v.UserId}",
                        action = "Vi phạm",
                        details = v.ViolationType
                    })
                    .ToListAsync();

                activities.AddRange(recentViolations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy recent activities");
            }

            return activities.OrderByDescending(a => ((dynamic)a).time).Take(10).ToList<object>();
        }
    }

    // ==================== REQUEST MODELS ====================

    public class KeyGenerationRequest
    {
        public string PackageType { get; set; } = "Day";
        public int Quantity { get; set; } = 1;
        public decimal Price { get; set; } = 30000;
        public int Duration { get; set; } = 24;
    }

    public class AddRiotAccountRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Region { get; set; } = "VN";
        public string? Notes { get; set; }
    }
}