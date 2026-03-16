using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ValorantRentalClient
{
    public partial class MainForm : Form
    {
        private readonly HttpClient _httpClient;
        private string _machineId;
        private int _currentUserId;
        private int _currentSessionId;
        private int _currentAccountId;
        private Timer _sessionTimer;
        private NotifyIcon _trayIcon;
        private AntiCheatMonitor _antiCheat;
        private RiotAccountInfo _currentAccount;
        private int _timeLeft; // minutes

        // Import Windows API để tìm và điều khiển cửa sổ
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public MainForm()
        {
            InitializeComponent();
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000/api/") };
            _machineId = GetMachineId();
            _antiCheat = new AntiCheatMonitor();
            
            SetupValorantUI();
            CheckSavedSession();
        }

        private void SetupValorantUI()
        {
            // Form settings
            this.Text = "Valorant Rental - Thuê tài khoản Valorant";
            this.Size = new Size(1000, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 25, 35); // Màu tối theo phong cách Valorant
            this.Font = new Font("Segoe UI", 10);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Icon = SystemIcons.Application;

            // Header Panel với logo Valorant
            var headerPanel = new Panel
            {
                Size = new Size(1000, 100),
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(255, 70, 85) // Màu đỏ Valorant
            };

            var logoLabel = new Label
            {
                Text = "VALORANT RENTAL",
                Font = new Font("Arial Black", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(30, 25),
                AutoSize = true
            };
            headerPanel.Controls.Add(logoLabel);

            var subLabel = new Label
            {
                Text = "Cho thuê tài khoản Valorant uy tín - Bảo mật - 24/7",
                Font = new Font("Segoe UI", 12),
                ForeColor = Color.FromArgb(220, 220, 220),
                Location = new Point(30, 65),
                AutoSize = true
            };
            headerPanel.Controls.Add(subLabel);

            var statusLabel = new Label
            {
                Text = "🔴 Chưa kích hoạt",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.LightCoral,
                Location = new Point(750, 40),
                AutoSize = true,
                Name = "statusLabel"
            };
            headerPanel.Controls.Add(statusLabel);

            // Main content panel
            var contentPanel = new Panel
            {
                Size = new Size(980, 500),
                Location = new Point(10, 110),
                BackColor = Color.FromArgb(25, 35, 45),
                BorderStyle = BorderStyle.None
            };

            // Tab control
            var tabControl = new TabControl
            {
                Size = new Size(960, 480),
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(35, 45, 55),
                ForeColor = Color.White
            };

            // Tab 1: Kích hoạt
            var activationTab = new TabPage("🔑 KÍCH HOẠT KEY");
            activationTab.BackColor = Color.FromArgb(35, 45, 55);
            SetupActivationTab(activationTab);
            tabControl.TabPages.Add(activationTab);

            // Tab 2: Chơi Valorant
            var playTab = new TabPage("🎯 CHƠI VALORANT");
            playTab.BackColor = Color.FromArgb(35, 45, 55);
            SetupPlayTab(playTab);
            tabControl.TabPages.Add(playTab);

            // Tab 3: Hướng dẫn
            var guideTab = new TabPage("📘 HƯỚNG DẪN");
            guideTab.BackColor = Color.FromArgb(35, 45, 55);
            SetupGuideTab(guideTab);
            tabControl.TabPages.Add(guideTab);

            // Tab 4: Lịch sử
            var historyTab = new TabPage("📜 LỊCH SỬ");
            historyTab.BackColor = Color.FromArgb(35, 45, 55);
            SetupHistoryTab(historyTab);
            tabControl.TabPages.Add(historyTab);

            contentPanel.Controls.Add(tabControl);

            // Footer
            var footerPanel = new Panel
            {
                Size = new Size(1000, 30),
                Location = new Point(0, 620),
                BackColor = Color.FromArgb(255, 70, 85)
            };

            var supportLabel = new Label
            {
                Text = "📞 Hotline: 1900 6868 | 📧 Email: support@valorantrental.com | Fanpage: fb.com/valorantrental",
                ForeColor = Color.White,
                Location = new Point(20, 5),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            footerPanel.Controls.Add(supportLabel);

            // Add all panels
            this.Controls.Add(headerPanel);
            this.Controls.Add(contentPanel);
            this.Controls.Add(footerPanel);

            // System tray
            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "Valorant Rental",
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; };
        }

        private void SetupActivationTab(TabPage tab)
        {
            // Panel chứa form kích hoạt
            var panel = new Panel
            {
                Size = new Size(500, 300),
                Location = new Point(230, 70),
                BackColor = Color.FromArgb(45, 55, 65),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = "NHẬP KEY KÍCH HOẠT",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 70, 85),
                Location = new Point(150, 30),
                AutoSize = true
            };

            var lblKey = new Label
            {
                Text = "Mã key:",
                Location = new Point(80, 100),
                Size = new Size(100, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12)
            };

            var txtKey = new TextBox
            {
                Location = new Point(180, 100),
                Size = new Size(220, 35),
                Font = new Font("Segoe UI", 12),
                BackColor = Color.FromArgb(25, 35, 45),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "VAL"
            };

            var btnActivate = new Button
            {
                Text = "🚀 KÍCH HOẠT NGAY",
                Location = new Point(180, 160),
                Size = new Size(220, 50),
                BackColor = Color.FromArgb(255, 70, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnActivate.FlatAppearance.BorderSize = 0;

            var lblMessage = new Label
            {
                Location = new Point(80, 230),
                Size = new Size(340, 30),
                ForeColor = Color.FromArgb(0, 200, 0),
                Text = "Nhập key để bắt đầu!",
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Sự kiện click kích hoạt
            btnActivate.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtKey.Text) || !txtKey.Text.StartsWith("VAL"))
                {
                    MessageBox.Show("Vui lòng nhập key hợp lệ (bắt đầu bằng VAL)!", "Lỗi", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnActivate.Enabled = false;
                btnActivate.Text = "ĐANG XỬ LÝ...";
                lblMessage.Text = "⏳ Đang kết nối server...";

                var result = await ActivateValorantKey(txtKey.Text);

                if (result.success)
                {
                    lblMessage.Text = "✅ Kích hoạt thành công!";
                    lblMessage.ForeColor = Color.LightGreen;
                    
                    _currentUserId = result.userId;
                    _currentSessionId = result.sessionId;
                    _currentAccount = result.account;
                    _timeLeft = result.timeLeft;
                    
                    // Lưu session
                    Properties.Settings.Default.UserId = _currentUserId;
                    Properties.Settings.Default.Save();
                    
                    // Cập nhật UI
                    UpdateStatus(true, _timeLeft);
                    
                    // Bắt đầu timer
                    StartSessionTimer(_timeLeft);
                    
                    // Bắt đầu anti-cheat
                    _antiCheat.StartMonitoring(_currentUserId, _currentSessionId, OnCheatDetected);
                    
                    // Hiển thị thông báo thành công
                    MessageBox.Show($"Kích hoạt thành công!\n\nTài khoản: {_currentAccount.username}\nThời gian: {_timeLeft} phút", 
                        "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Chuyển sang tab chơi game
                    tab.Parent.SelectedIndex = 1;
                }
                else
                {
                    lblMessage.Text = "❌ " + result.message;
                    lblMessage.ForeColor = Color.Red;
                }

                btnActivate.Enabled = true;
                btnActivate.Text = "🚀 KÍCH HOẠT NGAY";
            };

            panel.Controls.AddRange(new Control[] { lblTitle, lblKey, txtKey, btnActivate, lblMessage });
            tab.Controls.Add(panel);

            // Thêm logo Valorant
            var pictureBox = new PictureBox
            {
                Size = new Size(150, 150),
                Location = new Point(400, 300),
                BackColor = Color.Transparent
            };
            // Vẽ logo đơn giản
            pictureBox.Paint += (s, e) =>
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(255, 70, 85)))
                {
                    e.Graphics.FillEllipse(brush, 25, 25, 100, 100);
                    using (Font font = new Font("Arial", 40, FontStyle.Bold))
                    {
                        e.Graphics.DrawString("V", font, Brushes.White, 45, 45);
                    }
                }
            };
            tab.Controls.Add(pictureBox);
        }

        private void SetupPlayTab(TabPage tab)
        {
            // Thông tin tài khoản
            var accountPanel = new Panel
            {
                Size = new Size(400, 200),
                Location = new Point(280, 50),
                BackColor = Color.FromArgb(45, 55, 65),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblAccountTitle = new Label
            {
                Text = "THÔNG TIN TÀI KHOẢN",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 70, 85),
                Location = new Point(100, 15),
                AutoSize = true
            };

            var lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new Point(30, 60),
                Size = new Size(120, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            var txtUsername = new TextBox
            {
                Location = new Point(160, 60),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(25, 35, 45),
                ForeColor = Color.Cyan,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Name = "txtUsername"
            };

            var lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new Point(30, 100),
                Size = new Size(120, 25),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            var txtPassword = new TextBox
            {
                Location = new Point(160, 100),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(25, 35, 45),
                ForeColor = Color.Yellow,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ReadOnly = true,
                PasswordChar = '•',
                BorderStyle = BorderStyle.FixedSingle,
                Name = "txtPassword"
            };

            var btnCopyUser = new Button
            {
                Text = "📋 Copy tên",
                Location = new Point(30, 140),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(60, 70, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnCopyPass = new Button
            {
                Text = "📋 Copy mk",
                Location = new Point(140, 140),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(60, 70, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnShowPass = new Button
            {
                Text = "👁️ Hiện",
                Location = new Point(250, 140),
                Size = new Size(60, 30),
                BackColor = Color.FromArgb(60, 70, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            accountPanel.Controls.AddRange(new Control[] { 
                lblAccountTitle, lblUsername, txtUsername, lblPassword, txtPassword,
                btnCopyUser, btnCopyPass, btnShowPass
            });

            // Panel nút chơi game
            var playPanel = new Panel
            {
                Size = new Size(400, 150),
                Location = new Point(280, 270),
                BackColor = Color.FromArgb(45, 55, 65),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTimeLeft = new Label
            {
                Text = "Thời gian còn lại: --:--:--",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(100, 20),
                AutoSize = true,
                Name = "lblTimeLeft"
            };

            var btnLaunchRiot = new Button
            {
                Text = "🚀 MỞ RIOT CLIENT",
                Location = new Point(100, 60),
                Size = new Size(200, 50),
                BackColor = Color.FromArgb(255, 70, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false,
                Name = "btnLaunchRiot"
            };

            var btnEndSession = new Button
            {
                Text = "⏹️ Kết thúc phiên",
                Location = new Point(120, 120),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(100, 100, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Enabled = false,
                Name = "btnEndSession"
            };

            playPanel.Controls.AddRange(new Control[] { lblTimeLeft, btnLaunchRiot, btnEndSession });

            tab.Controls.Add(accountPanel);
            tab.Controls.Add(playPanel);

            // Event handlers
            btnCopyUser.Click += (s, e) => 
            { 
                if (!string.IsNullOrEmpty(txtUsername.Text))
                {
                    Clipboard.SetText(txtUsername.Text);
                    MessageBox.Show("Đã copy tên đăng nhập!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnCopyPass.Click += (s, e) => 
            { 
                if (!string.IsNullOrEmpty(txtPassword.Text))
                {
                    Clipboard.SetText(txtPassword.Text);
                    MessageBox.Show("Đã copy mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            bool passVisible = false;
            btnShowPass.Click += (s, e) =>
            {
                passVisible = !passVisible;
                txtPassword.PasswordChar = passVisible ? '\0' : '•';
                btnShowPass.Text = passVisible ? "👁️ Ẩn" : "👁️ Hiện";
            };

            btnLaunchRiot.Click += async (s, e) => await LaunchRiotClient();
            btnEndSession.Click += async (s, e) => await EndValorantSession();
        }

        private void SetupGuideTab(TabPage tab)
        {
            var richTextBox = new RichTextBox
            {
                Location = new Point(50, 30),
                Size = new Size(860, 400),
                BackColor = Color.FromArgb(45, 55, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };

            richTextBox.Text = @"📘 HƯỚNG DẪN SỬ DỤNG DỊCH VỤ THUÊ TÀI KHOẢN VALORANT

1️⃣ BƯỚC 1: KÍCH HOẠT KEY
   - Nhập mã key bạn đã mua vào ô 'Mã key' ở tab KÍCH HOẠT
   - Nhấn nút 'KÍCH HOẠT NGAY'
   - Chờ hệ thống xác thực và cấp tài khoản

2️⃣ BƯỚC 2: ĐĂNG NHẬP RIOT CLIENT
   - Sau khi kích hoạt thành công, chuyển sang tab CHƠI VALORANT
   - Copy tên đăng nhập và mật khẩu (dùng nút Copy)
   - Nhấn 'MỞ RIOT CLIENT' để mở Riot Client
   - Dán thông tin đăng nhập vào Riot Client
   - Đăng nhập và bắt đầu chơi Valorant!

3️⃣ BƯỚC 3: TRONG KHI CHƠI
   - Không tắt phần mềm này khi đang chơi
   - Không sử dụng cheat, hack, tool gian lận
   - Không chia sẻ tài khoản cho người khác
   - Theo dõi thời gian còn lại trên màn hình

4️⃣ BƯỚC 4: KẾT THÚC PHIÊN
   - Sau khi chơi xong, nhấn 'Kết thúc phiên'
   - Hoặc phần mềm sẽ tự động kết thúc khi hết giờ

⚠️ LƯU Ý QUAN TRỌNG:
   - Vi phạm chính sách sẽ bị khóa key vĩnh viễn
   - Không hoàn tiền khi phát hiện cheat
   - Liên hệ support nếu gặp sự cố

📞 HOTLINE: 1900 6868
📧 EMAIL: support@valorantrental.com
💬 FANPAGE: fb.com/valorantrental";

            tab.Controls.Add(richTextBox);
        }

        private void SetupHistoryTab(TabPage tab)
        {
            var dataGridView = new DataGridView
            {
                Location = new Point(30, 30),
                Size = new Size(900, 400),
                BackgroundColor = Color.FromArgb(45, 55, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                Name = "historyGrid"
            };

            dataGridView.Columns.Add("Date", "Ngày giờ");
            dataGridView.Columns.Add("Account", "Tài khoản");
            dataGridView.Columns.Add("Duration", "Thời gian");
            dataGridView.Columns.Add("Status", "Trạng thái");

            // Style
            dataGridView.DefaultCellStyle.BackColor = Color.FromArgb(35, 45, 55);
            dataGridView.DefaultCellStyle.ForeColor = Color.White;
            dataGridView.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 70, 85);
            dataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(255, 70, 85);
            dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dataGridView.EnableHeadersVisualStyles = false;

            // Load history
            LoadHistory(dataGridView);

            tab.Controls.Add(dataGridView);
        }

        private async void LoadHistory(DataGridView grid)
        {
            // Demo data - thay bằng API call thực tế
            grid.Rows.Add("13/03/2026 20:30", "valorant_acc1", "2 giờ", "✅ Hoàn thành");
            grid.Rows.Add("12/03/2026 19:15", "valorant_acc2", "1 giờ", "✅ Hoàn thành");
            grid.Rows.Add("11/03/2026 22:00", "valorant_acc3", "3 giờ", "✅ Hoàn thành");
        }

        // API Methods
        private async Task<(bool success, int userId, int sessionId, string message, int timeLeft, RiotAccountInfo account)> ActivateValorantKey(string key)
        {
            try
            {
                var request = new
                {
                    KeyCode = key,
                    MachineId = _machineId,
                    IpAddress = GetLocalIPAddress()
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("valorant/activate", content);
                var json = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<ActivationResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var accountInfo = new RiotAccountInfo
                    {
                        accountId = result.account.accountId,
                        username = result.account.username,
                        password = result.account.password,
                        region = result.account.region
                    };
                    
                    return (true, result.userId, result.sessionId, "Thành công!", result.timeLeft, accountInfo);
                }
                else
                {
                    var error = JsonSerializer.Deserialize<ErrorResponse>(json);
                    return (false, 0, 0, error.message, 0, null);
                }
            }
            catch (Exception ex)
            {
                return (false, 0, 0, $"Lỗi: {ex.Message}", 0, null);
            }
        }

        private async Task<bool> GetAccountForLogin()
        {
            try
            {
                var request = new
                {
                    UserId = _currentUserId,
                    SessionId = _currentSessionId,
                    RiotClientPath = FindRiotClientPath()
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("valorant/get-account", content);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AccountResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (result.success)
                    {
                        _currentAccount = new RiotAccountInfo
                        {
                            accountId = result.account.accountId,
                            username = result.account.username,
                            password = result.account.password,
                            region = result.account.region
                        };
                        
                        _timeLeft = result.timeLeft;
                        
                        // Update UI
                        UpdateAccountDisplay();
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task LaunchRiotClient()
        {
            try
            {
                // Kiểm tra xem đã có tài khoản chưa
                if (_currentAccount == null)
                {
                    // Thử lấy lại từ server
                    bool success = await GetAccountForLogin();
                    if (!success)
                    {
                        MessageBox.Show("Không thể lấy thông tin tài khoản. Vui lòng thử lại!", "Lỗi", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // Tìm đường dẫn Riot Client
                string riotPath = FindRiotClientPath();
                
                if (string.IsNullOrEmpty(riotPath))
                {
                    // Hỏi người dùng tự chọn
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        ofd.Title = "Chọn Riot Client";
                        ofd.Filter = "RiotClientServices.exe|RiotClientServices.exe";
                        ofd.InitialDirectory = @"C:\Riot Games\Riot Client";
                        
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            riotPath = ofd.FileName;
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy Riot Client. Vui lòng tự mở và đăng nhập thủ công.", 
                                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                }

                // Mở Riot Client
                Process.Start(riotPath);
                
                // Đợi 3 giây cho Riot Client khởi động
                await Task.Delay(3000);
                
                // Hiển thị lại form thông tin tài khoản để người dùng copy
                ShowAccountInfoPopup();
                
                // Hướng dẫn
                MessageBox.Show(
                    "Đã mở Riot Client!\n\n" +
                    "1. Copy tên đăng nhập và mật khẩu từ form vừa hiện ra\n" +
                    "2. Dán vào Riot Client và đăng nhập\n" +
                    "3. Chọn VALORANT và bắt đầu chơi!\n\n" +
                    "⚠️ KHÔNG tắt phần mềm này khi đang chơi!",
                    "Hướng dẫn đăng nhập",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi mở Riot Client: {ex.Message}", "Lỗi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowAccountInfoPopup()
        {
            var infoForm = new Form
            {
                Text = "Thông tin tài khoản Valorant",
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                TopMost = true,
                BackColor = Color.FromArgb(35, 45, 55),
                ForeColor = Color.White
            };

            var lblInstruction = new Label
            {
                Text = "Đăng nhập Riot Client với thông tin sau:",
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White
            };

            var lblUsername = new Label
            {
                Text = $"👤 Tên đăng nhập: {_currentAccount.username}",
                Location = new Point(20, 60),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Cyan
            };

            var lblPassword = new Label
            {
                Text = $"🔑 Mật khẩu: {_currentAccount.password}",
                Location = new Point(20, 90),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.Yellow
            };

            var btnCopyUser = new Button
            {
                Text = "📋 Copy tên",
                Location = new Point(20, 130),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 70, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnCopyPass = new Button
            {
                Text = "📋 Copy mk",
                Location = new Point(130, 130),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 70, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            var btnClose = new Button
            {
                Text = "Đã đăng nhập xong",
                Location = new Point(240, 130),
                Size = new Size(120, 35),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };

            btnCopyUser.Click += (s, e) => 
            { 
                Clipboard.SetText(_currentAccount.username);
                MessageBox.Show("Đã copy tên đăng nhập!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnCopyPass.Click += (s, e) => 
            { 
                Clipboard.SetText(_currentAccount.password);
                MessageBox.Show("Đã copy mật khẩu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnClose.Click += (s, e) => infoForm.Close();

            infoForm.Controls.AddRange(new Control[] { 
                lblInstruction, lblUsername, lblPassword, 
                btnCopyUser, btnCopyPass, btnClose 
            });

            infoForm.Show();
        }

        private void UpdateAccountDisplay()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdateAccountDisplay));
                return;
            }

            var txtUsername = this.Controls.Find("txtUsername", true)[0] as TextBox;
            var txtPassword = this.Controls.Find("txtPassword", true)[0] as TextBox;
            var btnLaunchRiot = this.Controls.Find("btnLaunchRiot", true)[0] as Button;
            var btnEndSession = this.Controls.Find("btnEndSession", true)[0] as Button;

            if (txtUsername != null && _currentAccount != null)
                txtUsername.Text = _currentAccount.username;
            
            if (txtPassword != null && _currentAccount != null)
                txtPassword.Text = _currentAccount.password;
            
            if (btnLaunchRiot != null)
                btnLaunchRiot.Enabled = true;
            
            if (btnEndSession != null)
                btnEndSession.Enabled = true;
        }

        private void StartSessionTimer(int minutesLeft)
        {
            _sessionTimer = new Timer();
            _sessionTimer.Interval = 60000; // 1 phút
            _sessionTimer.Tick += (s, e) =>
            {
                _timeLeft--;
                
                if (_timeLeft <= 0)
                {
                    _sessionTimer.Stop();
                    MessageBox.Show("⏰ Phiên chơi đã hết hạn!\n\nCảm ơn bạn đã sử dụng dịch vụ!", 
                        "Hết giờ", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateStatus(false, 0);
                    
                    // Kết thúc session tự động
                    _ = EndValorantSession();
                }
                else
                {
                    UpdateStatus(true, _timeLeft);
                }
            };
            _sessionTimer.Start();
        }

        private void UpdateStatus(bool isActive, int minutesLeft = 0)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, int>(UpdateStatus), isActive, minutesLeft);
                return;
            }

            var statusLabel = this.Controls.Find("statusLabel", true)[0] as Label;
            var lblTimeLeft = this.Controls.Find("lblTimeLeft", true)[0] as Label;

            if (isActive)
            {
                var hours = minutesLeft / 60;
                var mins = minutesLeft % 60;
                statusLabel.Text = $"🟢 Đang chơi - {hours:D2}:{mins:D2}:00";
                statusLabel.ForeColor = Color.LightGreen;
                
                if (lblTimeLeft != null)
                    lblTimeLeft.Text = $"⏳ Thời gian còn lại: {hours:D2}:{mins:D2}:00";
            }
            else
            {
                statusLabel.Text = "🔴 Chưa kích hoạt";
                statusLabel.ForeColor = Color.LightCoral;
                
                if (lblTimeLeft != null)
                    lblTimeLeft.Text = "Thời gian còn lại: --:--:--";
            }
        }

        private async Task EndValorantSession()
        {
            try
            {
                var request = new
                {
                    SessionId = _currentSessionId,
                    AccountId = _currentAccount?.accountId ?? 0
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json");

                await _httpClient.PostAsync("valorant/end-session", content);
                
                _sessionTimer?.Stop();
                _antiCheat.Stop();
                
                UpdateStatus(false, 0);
                
                var btnLaunchRiot = this.Controls.Find("btnLaunchRiot", true)[0] as Button;
                var btnEndSession = this.Controls.Find("btnEndSession", true)[0] as Button;
                
                if (btnLaunchRiot != null) btnLaunchRiot.Enabled = false;
                if (btnEndSession != null) btnEndSession.Enabled = false;
                
                MessageBox.Show("Đã kết thúc phiên chơi. Cảm ơn bạn!", "Thông báo", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi kết thúc phiên: {ex.Message}", "Lỗi", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void CheckSavedSession()
        {
            if (Properties.Settings.Default.UserId > 0)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"valorant/check-session/{Properties.Settings.Default.UserId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var result = JsonSerializer.Deserialize<SessionCheckResponse>(json);
                        
                        if (result.active)
                        {
                            _currentUserId = Properties.Settings.Default.UserId;
                            _currentSessionId = result.sessionId;
                            _currentAccountId = result.accountId;
                            _timeLeft = result.timeLeft;
                            
                            // Lấy thông tin tài khoản
                            await GetAccountForLogin();
                            
                            // Khôi phục session
                            StartSessionTimer(_timeLeft);
                            _antiCheat.StartMonitoring(_currentUserId, _currentSessionId, OnCheatDetected);
                            
                            MessageBox.Show("Đã khôi phục phiên chơi trước đó!", "Thông báo", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch { }
            }
        }

        private void OnCheatDetected(string cheatName)
        {
            // Gửi báo cáo lên server
            ReportViolation(cheatName);
            
            // Dừng session
            _sessionTimer?.Stop();
            
            // Hiển thị cảnh báo
            MessageBox.Show(
                $"🚨 PHÁT HIỆN PHẦN MỀM GIAN LẬN: {cheatName}\n\n" +
                "Tài khoản của bạn đã bị khóa vĩnh viễn!\n" +
                "Mọi hành vi gian lận đều được ghi lại.",
                "CẢNH BÁO BẢO MẬT",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            
            // Đóng ứng dụng
            Application.Exit();
        }

        private async void ReportViolation(string cheatName)
        {
            try
            {
                var report = new
                {
                    UserId = _currentUserId,
                    ViolationType = "Cheat",
                    Details = $"Phát hiện cheat khi chơi Valorant: {cheatName}",
                    ProcessName = cheatName
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(report),
                    Encoding.UTF8,
                    "application/json");

                await _httpClient.PostAsync("valorant/report-violation", content);
            }
            catch { }
        }

        private string FindRiotClientPath()
        {
            string[] commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "Riot Games", "Riot Client", "RiotClientServices.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                    "Riot Games", "Riot Client", "RiotClientServices.exe"),
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"D:\Riot Games\Riot Client\RiotClientServices.exe",
                @"E:\Riot Games\Riot Client\RiotClientServices.exe"
            };
            
            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            
            return null;
        }

        private string GetMachineId()
        {
            // Tạo machine ID từ thông tin phần cứng
            return Environment.MachineName + "_" + Environment.UserName;
        }

        private string GetLocalIPAddress()
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _sessionTimer?.Dispose();
            _antiCheat.Stop();
            _trayIcon?.Dispose();
            base.OnFormClosing(e);
        }

        // Response classes
        public class RiotAccountInfo
        {
            public int accountId { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string region { get; set; }
        }

        public class ActivationResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
            public int userId { get; set; }
            public int sessionId { get; set; }
            public AccountDetail account { get; set; }
            public int timeLeft { get; set; }
        }

        public class AccountDetail
        {
            public int accountId { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string region { get; set; }
        }

        public class AccountResponse
        {
            public bool success { get; set; }
            public AccountDetail account { get; set; }
            public int timeLeft { get; set; }
        }

        public class SessionCheckResponse
        {
            public bool active { get; set; }
            public int sessionId { get; set; }
            public int accountId { get; set; }
            public int timeLeft { get; set; }
        }

        public class ErrorResponse
        {
            public bool success { get; set; }
            public string message { get; set; }
        }
    }
}