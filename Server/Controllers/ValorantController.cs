using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ValorantRentalServer.Models;
using System.Security.Cryptography;
using System.Text;

namespace ValorantRentalServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValorantController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ValorantController> _logger;
        private readonly IConfiguration _configuration;

        public ValorantController(AppDbContext context, ILogger<ValorantController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost("activate")]
        public async Task<IActionResult> ActivateKey([FromBody] ActivationRequest request)
        {
            try
            {
                _logger.LogInformation($"Activation request for key: {request.KeyCode}");

                var key = await _context.ValorantKeys
                    .FirstOrDefaultAsync(k => k.KeyCode == request.KeyCode);

                if (key == null)
                {
                    return NotFound(new { success = false, message = "Key không tồn tại" });
                }

                if (key.ExpiryDate < DateTime.Now)
                {
                    key.Status = "Expired";
                    await _context.SaveChangesAsync();
                    return BadRequest(new { success = false, message = "Key đã hết hạn" });
                }

                if (key.IsSold)
                {
                    var existingSession = await _context.ValorantSessions
                        .Where(s => s.KeyId == key.KeyId && s.Status == "Active")
                        .FirstOrDefaultAsync();

                    if (existingSession != null && existingSession.MachineId == request.MachineId)
                    {
                        var account = await _context.RiotAccounts.FindAsync(existingSession.AccountId);
                        if (account != null)
                        {
                            string decryptedPassword = DecryptPassword(account.RiotPassword);
                            
                            return Ok(new 
                            { 
                                success = true, 
                                message = "Tiếp tục phiên chơi",
                                userId = existingSession.UserId,
                                sessionId = existingSession.SessionId,
                                account = new
                                {
                                    accountId = account.AccountId,
                                    username = account.RiotUsername,
                                    password = decryptedPassword,
                                    region = account.Region
                                },
                                timeLeft = GetTimeLeft(existingSession.StartTime, key.Duration)
                            });
                        }
                    }
                    
                    return BadRequest(new { success = false, message = "Key đã được sử dụng ở máy khác" });
                }

                var user = new User
                {
                    Username = $"User_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    PasswordHash = HashString(request.MachineId),
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                key.IsSold = true;
                key.SoldToUserId = user.UserId;
                key.ActivatedDate = DateTime.Now;
                key.Status = "Active";

                var gameAccount = await GetAvailableRiotAccount();
                if (gameAccount == null)
                {
                    return StatusCode(503, new { success = false, message = "Hiện tại không có tài khoản trống, vui lòng thử lại sau" });
                }

                string decryptedPass = DecryptPassword(gameAccount.RiotPassword);

                gameAccount.IsAvailable = false;
                gameAccount.CurrentUserId = user.UserId;
                gameAccount.LastUsedDate = DateTime.Now;

                var session = new ValorantSession
                {
                    UserId = user.UserId,
                    KeyId = key.KeyId,
                    AccountId = gameAccount.AccountId,
                    StartTime = DateTime.Now,
                    Status = "Active",
                    MachineId = request.MachineId
                };

                _context.ValorantSessions.Add(session);
                await _context.SaveChangesAsync();

                var transaction = new Transaction
                {
                    UserId = user.UserId,
                    KeyId = key.KeyId,
                    Amount = key.Price,
                    PaymentMethod = "Key",
                    Status = "Success",
                    CreatedDate = DateTime.Now
                };
                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kích hoạt thành công!",
                    userId = user.UserId,
                    sessionId = session.SessionId,
                    account = new
                    {
                        accountId = gameAccount.AccountId,
                        username = gameAccount.RiotUsername,
                        password = decryptedPass,
                        region = gameAccount.Region
                    },
                    expiryDate = DateTime.Now.AddHours(key.Duration),
                    timeLeft = key.Duration * 60
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kích hoạt key Valorant");
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        [HttpGet("check-session/{userId}")]
        public async Task<IActionResult> CheckSession(int userId)
        {
            try
            {
                var session = await _context.ValorantSessions
                    .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Active");

                if (session == null)
                {
                    return Ok(new { active = false });
                }

                var key = await _context.ValorantKeys.FindAsync(session.KeyId);
                if (key == null)
                {
                    return Ok(new { active = false });
                }

                var timeLeft = GetTimeLeft(session.StartTime, key.Duration);

                return Ok(new
                {
                    active = true,
                    sessionId = session.SessionId,
                    accountId = session.AccountId,
                    timeLeft = timeLeft
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kiểm tra session");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        [HttpPost("end-session")]
        public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
        {
            try
            {
                var session = await _context.ValorantSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session != null && session.Status == "Active")
                {
                    session.EndTime = DateTime.Now;
                    session.Duration = (int)(DateTime.Now - session.StartTime).TotalMinutes;
                    session.Status = "Completed";

                    var account = await _context.RiotAccounts.FindAsync(request.AccountId);
                    if (account != null)
                    {
                        account.IsAvailable = true;
                        account.CurrentUserId = null;
                        account.TotalUsed++;
                    }

                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = true, message = "Đã kết thúc phiên chơi" });
                }

                return Ok(new { success = false, message = "Không tìm thấy phiên chơi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi kết thúc phiên");
                return StatusCode(500, new { success = false, message = "Lỗi server" });
            }
        }

        [HttpPost("report-violation")]
        public async Task<IActionResult> ReportViolation([FromBody] ViolationReport report)
        {
            try
            {
                var session = await _context.ValorantSessions
                    .FirstOrDefaultAsync(s => s.UserId == report.UserId && s.Status == "Active");

                if (session != null)
                {
                    var violation = new Violation
                    {
                        UserId = report.UserId,
                        SessionId = session.SessionId,
                        ViolationType = report.ViolationType,
                        Details = report.Details,
                        DetectedTime = DateTime.Now,
                        Action = "Warning"
                    };

                    _context.Violations.Add(violation);

                    if (report.ViolationType == "Cheat")
                    {
                        session.Status = "Violation";
                        
                        var user = await _context.Users.FindAsync(report.UserId);
                        if (user != null)
                        {
                            user.IsActive = false;
                        }
                        
                        var account = await _context.RiotAccounts.FindAsync(session.AccountId);
                        if (account != null)
                        {
                            account.AccountStatus = "Banned";
                            account.IsAvailable = false;
                        }
                        
                        violation.Action = "Ban";
                    }

                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = true, action = violation.Action });
                }

                return Ok(new { success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi báo cáo vi phạm");
                return StatusCode(500);
            }
        }

        private async Task<RiotAccount?> GetAvailableRiotAccount()
        {
            return await _context.RiotAccounts
                .Where(a => a.IsAvailable && a.AccountStatus == "Good")
                .FirstOrDefaultAsync();
        }

        private int GetTimeLeft(DateTime startTime, int durationHours)
        {
            var expiryTime = startTime.AddHours(durationHours);
            var timeLeft = (expiryTime - DateTime.Now).TotalMinutes;
            return timeLeft > 0 ? (int)timeLeft : 0;
        }

        private string HashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string EncryptPassword(string password)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Encryption:Key"]);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[16];

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);

                return Convert.ToBase64String(encryptedBytes);
            }
        }

        private string DecryptPassword(string encryptedPassword)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Encryption:Key"]);
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedPassword);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }

    public class ActivationRequest
    {
        public string KeyCode { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }

    public class EndSessionRequest
    {
        public int SessionId { get; set; }
        public int AccountId { get; set; }
    }

    public class ViolationReport
    {
        public int UserId { get; set; }
        public string ViolationType { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
    }
}