using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using ValorantRentalClient.Controls;
using ValorantRentalClient.Styles;

namespace ValorantRentalClient
{
    public partial class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private Panel titleBar;
        private Label lblTitle;
        private Label lblStatus;
        private Label lblTime;
        private Button btnMinimize;
        private Button btnMaximize;
        private Button btnClose;
        private ModernTabControl tabControl;
        private Timer clockTimer;

        // Panels cho từng tab
        private Panel homePanel;
        private Panel activatePanel;
        private Panel gamePanel;
        private Panel historyPanel;
        private Panel guidePanel;
        private Panel settingsPanel;

        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Theme.Background;
            this.WindowState = FormWindowState.Maximized;
            this.DoubleBuffered = true;

            CreateTitleBar();
            CreateTabControl();
            CreateHomePanel();
            CreateActivatePanel();
            CreateGamePanel();
            CreateHistoryPanel();
            CreateGuidePanel();
            CreateSettingsPanel();

            // Add panels to tab pages
            tabControl.TabPages[0].Controls.Add(homePanel);
            tabControl.TabPages[1].Controls.Add(activatePanel);
            tabControl.TabPages[2].Controls.Add(gamePanel);
            tabControl.TabPages[3].Controls.Add(historyPanel);
            tabControl.TabPages[4].Controls.Add(guidePanel);
            tabControl.TabPages[5].Controls.Add(settingsPanel);

            // Start clock
            clockTimer = new Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy");
            clockTimer.Start();
        }

        private void CreateTitleBar()
        {
            titleBar = new Panel
            {
                Height = Theme.TitleBarHeight,
                Dock = DockStyle.Top,
                BackColor = Theme.SurfaceDark
            };
            titleBar.MouseDown += TitleBar_MouseDown;

            // Logo
            var lblLogo = new Label
            {
                Text = "✦ VALORENT",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Theme.Primary,
                Location = new Point(30, 12),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblLogo);

            // Version
            var lblVersion = new Label
            {
                Text = "v5.0 • Cyber Edition",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(200, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblVersion);

            // Title
            lblTitle = new Label
            {
                Text = "Trang chủ",
                Font = Theme.LargeFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(400, 18),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblTitle);

            // Status
            lblStatus = new Label
            {
                Text = "● OFFLINE",
                Font = Theme.NormalFont,
                ForeColor = Theme.Danger,
                Location = new Point(this.Width - 400, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblStatus);

            // Time
            lblTime = new Label
            {
                Text = DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy"),
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(this.Width - 250, 22),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            titleBar.Controls.Add(lblTime);

            // Window buttons
            btnMinimize = CreateTitleBarButton("—", new Point(this.Width - 120, 15));
            btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMaximize = CreateTitleBarButton("□", new Point(this.Width - 80, 15));
            btnMaximize.Click += (s, e) =>
                this.WindowState = this.WindowState == FormWindowState.Maximized ?
                FormWindowState.Normal : FormWindowState.Maximized;

            btnClose = CreateTitleBarButton("✕", new Point(this.Width - 40, 15));
            btnClose.Click += (s, e) => Application.Exit();

            this.Controls.Add(titleBar);
        }

        private Button CreateTitleBarButton(string text, Point location)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.ForeColor = Theme.TextPrimary;
            btn.MouseLeave += (s, e) => btn.ForeColor = Theme.TextSecondary;
            titleBar.Controls.Add(btn);
            return btn;
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void CreateTabControl()
        {
            tabControl = new ModernTabControl
            {
                Dock = DockStyle.Left,
                Width = Theme.SidebarWidth,
                Location = new Point(0, Theme.TitleBarHeight)
            };

            // Add 6 tab pages
            for (int i = 0; i < 6; i++)
            {
                tabControl.TabPages.Add(new TabPage());
            }

            tabControl.SelectedIndexChanged += (s, e) =>
            {
                string[] titles = { "Trang chủ", "Kích hoạt", "Chơi game", "Lịch sử", "Hướng dẫn", "Cài đặt" };
                lblTitle.Text = titles[tabControl.SelectedIndex];
            };

            this.Controls.Add(tabControl);
        }

        private void CreateHomePanel()
        {
            homePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Welcome Section
            var welcomeCard = new ModernPanel
            {
                Size = new Size(900, 400),
                Location = new Point(50, 30),
                HasGradient = true,
                GradientStart = Theme.Primary,
                GradientEnd = Theme.Accent,
                Opacity = 0.9f
            };

            // Welcome text
            var welcomeLabel = new Label
            {
                Text = "CHÀO MỪNG ĐẾN VỚI",
                Font = new Font("Segoe UI", 20),
                ForeColor = Color.FromArgb(200, Color.White),
                Location = new Point(450, 80),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            welcomeCard.Controls.Add(welcomeLabel);

            var logoLabel = new Label
            {
                Text = "VALORENT",
                Font = new Font("Segoe UI", 60, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(450, 130),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            welcomeCard.Controls.Add(logoLabel);

            var sloganLabel = new Label
            {
                Text = "Hệ thống cho thuê tài khoản Valorant hàng đầu Việt Nam",
                Font = Theme.LargeFont,
                ForeColor = Color.FromArgb(220, Color.White),
                Location = new Point(450, 220),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            welcomeCard.Controls.Add(sloganLabel);

            // Decorative circles
            for (int i = 0; i < 5; i++)
            {
                var circle = new Panel
                {
                    Size = new Size(100 + i * 50, 100 + i * 50),
                    Location = new Point(50 + i * 30, 50 + i * 20),
                    BackColor = Color.Transparent
                };
                circle.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (var brush = new SolidBrush(Color.FromArgb(30 - i * 5, Color.White)))
                    {
                        e.Graphics.FillEllipse(brush, 0, 0, circle.Width, circle.Height);
                    }
                };
                welcomeCard.Controls.Add(circle);
                circle.SendToBack();
            }

            homePanel.Controls.Add(welcomeCard);

            // Stats Grid
            string[] stats = { "Người dùng", "Đang online", "Tài khoản", "Doanh thu" };
            string[] values = { "1,234", "56", "89", "12.5M" };
            string[] icons = { "👥", "🟢", "🎮", "💰" };
            Color[] colors = { Theme.Info, Theme.Success, Theme.Primary, Theme.Warning };

            for (int i = 0; i < stats.Length; i++)
            {
                var statCard = new ModernCard
                {
                    Size = new Size(200, 120),
                    Location = new Point(50 + i * 220, 460),
                    Title = stats[i],
                    Value = values[i],
                    AccentColor = colors[i]
                };

                // Tạo icon đơn giản
                var iconLabel = new Label
                {
                    Text = icons[i],
                    Font = new Font("Segoe UI", 30),
                    ForeColor = colors[i],
                    Location = new Point(140, 30),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                statCard.Controls.Add(iconLabel);

                homePanel.Controls.Add(statCard);
            }

            // Features Grid
            string[][] features = new string[][] {
                new string[] { "🚀", "Tự động đăng nhập", "Click một nút, tool tự làm mọi thứ" },
                new string[] { "🔒", "Bảo mật tuyệt đối", "Không lộ thông tin tài khoản" },
                new string[] { "⏱️", "Tính giờ chính xác", "Thông báo khi sắp hết giờ" },
                new string[] { "🛡️", "Chống cheat", "Phát hiện và ngăn chặn gian lận" }
            };

            for (int i = 0; i < features.Length; i++)
            {
                var featureCard = new ModernPanel
                {
                    Size = new Size(420, 100),
                    Location = new Point(50 + (i % 2) * 440, 600 + (i / 2) * 120),
                    Opacity = 0.8f
                };

                var iconLabel = new Label
                {
                    Text = features[i][0],
                    Font = new Font("Segoe UI", 40),
                    ForeColor = Theme.Accent,
                    Location = new Point(20, 25),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                featureCard.Controls.Add(iconLabel);

                var titleLabel = new Label
                {
                    Text = features[i][1],
                    Font = Theme.LargeFont,
                    ForeColor = Theme.TextPrimary,
                    Location = new Point(100, 25),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                featureCard.Controls.Add(titleLabel);

                var descLabel = new Label
                {
                    Text = features[i][2],
                    Font = Theme.SmallFont,
                    ForeColor = Theme.TextSecondary,
                    Location = new Point(100, 55),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                featureCard.Controls.Add(descLabel);

                homePanel.Controls.Add(featureCard);
            }

            // Start button
            var startButton = new ModernButton
            {
                Text = "🚀 BẮT ĐẦU NGAY",
                Size = new Size(300, 70),
                Location = new Point(350, 850),
                Font = new Font("Segoe UI", 16, FontStyle.Bold)
            };
            startButton.Click += (s, e) => tabControl.SelectedIndex = 1;
            homePanel.Controls.Add(startButton);
        }

        private void CreateActivatePanel()
        {
            activatePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var centerCard = new ModernPanel
            {
                Size = new Size(600, 500),
                Location = new Point((this.Width - Theme.SidebarWidth - 600) / 2, 100),
                Opacity = 0.9f
            };

            var iconLabel = new Label
            {
                Text = "🔑",
                Font = new Font("Segoe UI", 80),
                ForeColor = Theme.Primary,
                Location = new Point(250, 40),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            centerCard.Controls.Add(iconLabel);

            var titleLabel = new Label
            {
                Text = "KÍCH HOẠT KEY",
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(180, 140),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            centerCard.Controls.Add(titleLabel);

            var subtitleLabel = new Label
            {
                Text = "Nhập mã key đã mua để bắt đầu",
                Font = Theme.NormalFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(190, 180),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            centerCard.Controls.Add(subtitleLabel);

            var txtKey = new ModernTextBox
            {
                Location = new Point(100, 230),
                Size = new Size(400, 50),
                Font = new Font("Segoe UI", 14),
                Text = ""
            };
            txtKey.PlaceholderText = "Nhập key tại đây...";
            centerCard.Controls.Add(txtKey);

            var btnActivate = new ModernButton
            {
                Text = "🔑 KÍCH HOẠT NGAY",
                Location = new Point(150, 320),
                Size = new Size(300, 60),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            btnActivate.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtKey.Text))
                {
                    MessageBox.Show("Vui lòng nhập key!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                btnActivate.Enabled = false;
                btnActivate.Text = "⏳ ĐANG XỬ LÝ...";

                // TODO: Call API to activate key
                await System.Threading.Tasks.Task.Delay(2000); // Simulate

                btnActivate.Enabled = true;
                btnActivate.Text = "✅ KÍCH HOẠT NGAY";

                tabControl.SelectedIndex = 2;
            };
            centerCard.Controls.Add(btnActivate);

            var noteLabel = new Label
            {
                Text = "Mỗi key chỉ sử dụng được 1 lần duy nhất",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextMuted,
                Location = new Point(170, 400),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            centerCard.Controls.Add(noteLabel);

            activatePanel.Controls.Add(centerCard);
        }

        private void CreateGamePanel()
        {
            gamePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Left column - Account info
            var leftCard = new ModernPanel
            {
                Size = new Size(400, 500),
                Location = new Point(50, 50),
                Opacity = 0.9f
            };

            var accountIcon = new Label
            {
                Text = "🎮",
                Font = new Font("Segoe UI", 60),
                ForeColor = Theme.Success,
                Location = new Point(160, 30),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(accountIcon);

            var accountTitle = new Label
            {
                Text = "THÔNG TIN TÀI KHOẢN",
                Font = Theme.LargeFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(100, 100),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(accountTitle);

            // Username
            var userLabel = new Label
            {
                Text = "TÊN ĐĂNG NHẬP",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(50, 160),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(userLabel);

            var txtUsername = new TextBox
            {
                Location = new Point(50, 185),
                Size = new Size(300, 35),
                BackColor = Theme.SurfaceLight,
                ForeColor = Theme.Primary,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = "user123"
            };
            leftCard.Controls.Add(txtUsername);

            // Password
            var passLabel = new Label
            {
                Text = "MẬT KHẨU",
                Font = Theme.SmallFont,
                ForeColor = Theme.TextSecondary,
                Location = new Point(50, 240),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            leftCard.Controls.Add(passLabel);

            var txtPassword = new TextBox
            {
                Location = new Point(50, 265),
                Size = new Size(300, 35),
                BackColor = Theme.SurfaceLight,
                ForeColor = Theme.Warning,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Text = "••••••••"
            };
            leftCard.Controls.Add(txtPassword);

            // Buttons row
            var btnCopyUser = new ModernButton
            {
                Text = "📋 COPY USER",
                Location = new Point(50, 320),
                Size = new Size(140, 40),
                Font = Theme.SmallFont
            };
            leftCard.Controls.Add(btnCopyUser);

            var btnCopyPass = new ModernButton
            {
                Text = "📋 COPY PASS",
                Location = new Point(210, 320),
                Size = new Size(140, 40),
                Font = Theme.SmallFont
            };
            leftCard.Controls.Add(btnCopyPass);

            var btnShowPass = new ModernButton
            {
                Text = "👁️ HIỆN",
                Location = new Point(50, 370),
                Size = new Size(140, 40),
                Font = Theme.SmallFont,
                BackColor = Theme.SurfaceLight
            };
            leftCard.Controls.Add(btnShowPass);

            var btnRefresh = new ModernButton
            {
                Text = "🔄 LÀM MỚI",
                Location = new Point(210, 370),
                Size = new Size(140, 40),
                Font = Theme.SmallFont,
                BackColor = Theme.Success
            };
            leftCard.Controls.Add(btnRefresh);

            gamePanel.Controls.Add(leftCard);

            // Right column - Game control
            var rightCard = new ModernPanel
            {
                Size = new Size(600, 500),
                Location = new Point(500, 50),
                Opacity = 0.9f
            };

            // Timer
            var timerLabel = new Label
            {
                Text = "⏳ 02:30:45",
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                ForeColor = Theme.Accent,
                Location = new Point(150, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            rightCard.Controls.Add(timerLabel);

            var statusLabel = new Label
            {
                Text = "● ĐANG HOẠT ĐỘNG",
                Font = Theme.LargeFont,
                ForeColor = Theme.Success,
                Location = new Point(200, 120),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            rightCard.Controls.Add(statusLabel);

            // Progress bar
            var progressBar = new ModernProgressBar
            {
                Location = new Point(50, 180),
                Size = new Size(500, 20),
                Value = 65,
                Maximum = 100
            };
            rightCard.Controls.Add(progressBar);

            // Warning box
            var warningBox = new ModernPanel
            {
                Size = new Size(500, 80),
                Location = new Point(50, 220),
                BackColor = Color.FromArgb(50, Theme.Warning),
                Opacity = 0.3f
            };

            var warningText = new Label
            {
                Text = "⚠️ KHÔNG tắt tool trong khi đang chơi game",
                Font = Theme.NormalFont,
                ForeColor = Theme.Warning,
                Location = new Point(120, 30),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            warningBox.Controls.Add(warningText);

            var warningIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI", 24),
                ForeColor = Theme.Warning,
                Location = new Point(50, 20),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            warningBox.Controls.Add(warningIcon);

            rightCard.Controls.Add(warningBox);

            // Action buttons
            var btnLaunch = new ModernButton
            {
                Text = "🚀 BẮT ĐẦU ĐĂNG NHẬP",
                Location = new Point(150, 320),
                Size = new Size(300, 60),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            rightCard.Controls.Add(btnLaunch);

            var btnEnd = new ModernButton
            {
                Text = "⏹️ KẾT THÚC PHIÊN",
                Location = new Point(150, 400),
                Size = new Size(300, 50),
                Font = Theme.LargeFont,
                BackColor = Theme.Danger
            };
            rightCard.Controls.Add(btnEnd);

            gamePanel.Controls.Add(rightCard);
        }

        private void CreateHistoryPanel()
        {
            historyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var centerLabel = new Label
            {
                Text = "📊 LỊCH SỬ SỬ DỤNG",
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(400, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            historyPanel.Controls.Add(centerLabel);

            // TODO: Add history grid
        }

        private void CreateGuidePanel()
        {
            guidePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var centerLabel = new Label
            {
                Text = "📘 HƯỚNG DẪN SỬ DỤNG",
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(400, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            guidePanel.Controls.Add(centerLabel);

            // TODO: Add guide content
        }

        private void CreateSettingsPanel()
        {
            settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var centerLabel = new Label
            {
                Text = "⚙ CÀI ĐẶT HỆ THỐNG",
                Font = Theme.HeaderFont,
                ForeColor = Theme.TextPrimary,
                Location = new Point(400, 50),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            settingsPanel.Controls.Add(centerLabel);

            // TODO: Add settings controls
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (titleBar != null && lblStatus != null && lblTime != null)
            {
                lblStatus.Location = new Point(this.Width - 400, 20);
                lblTime.Location = new Point(this.Width - 250, 22);
                btnMinimize.Location = new Point(this.Width - 120, 15);
                btnMaximize.Location = new Point(this.Width - 80, 15);
                btnClose.Location = new Point(this.Width - 40, 15);
            }
        }
    }
}