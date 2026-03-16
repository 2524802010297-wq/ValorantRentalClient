using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using ValorantRentalClient.Utils;

namespace ValorantRentalClient
{
    static class Program
    {
        private static Mutex _mutex = null;

        [STAThread]
        static void Main()
        {
            // Chỉ cho phép chạy một instance duy nhất
            const string appName = "ValorantRentalClient";
            bool createdNew;

            _mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Phần mềm đã được mở!\nVui lòng kiểm tra khay hệ thống (system tray).", 
                    "Valorant Rental", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Kích hoạt instance cũ
                ActivatePreviousInstance();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Kiểm tra kết nối server
            CheckServerConnection();

            // Kiểm tra quyền Admin (cần cho AntiCheat)
            if (!IsRunningAsAdministrator())
            {
                var result = MessageBox.Show(
                    "Phần mềm cần chạy với quyền Administrator để hoạt động hiệu quả.\n\n" +
                    "Bạn có muốn khởi động lại với quyền Administrator không?",
                    "Yêu cầu quyền Admin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    RestartAsAdministrator();
                    return;
                }
            }

            // Chạy ứng dụng chính
            Application.Run(new MainForm());

            // Giải phóng Mutex khi thoát
            _mutex.ReleaseMutex();
        }

        private static void CheckServerConnection()
        {
            try
            {
                string serverUrl = Properties.Settings.Default.ServerUrl;
                if (string.IsNullOrEmpty(serverUrl))
                {
                    serverUrl = "http://localhost:5000/api/";
                    Properties.Settings.Default.ServerUrl = serverUrl;
                    Properties.Settings.Default.Save();
                }

                // Kiểm tra nhanh trong background
                _ = NetworkHelper.TestConnection(serverUrl);
            }
            catch { }
        }

        private static bool IsRunningAsAdministrator()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }

        private static void RestartAsAdministrator()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                // User declined UAC
            }
        }

        private static void ActivatePreviousInstance()
        {
            // Tìm và kích hoạt cửa sổ của instance cũ
            try
            {
                var processes = Process.GetProcessesByName("ValorantRentalClient");
                foreach (var proc in processes)
                {
                    if (proc.Id != Process.GetCurrentProcess().Id)
                    {
                        var handle = proc.MainWindowHandle;
                        if (handle != IntPtr.Zero)
                        {
                            ShowWindow(handle, SW_RESTORE);
                            SetForegroundWindow(handle);
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;
    }
}