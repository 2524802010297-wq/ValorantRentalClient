using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace ValorantRentalClient.Utils
{
    public static class RiotClientHelper
    {
        public static string FindRiotClientPath()
        {
            // Các đường dẫn phổ biến
            string[] commonPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "Riot Games", "Riot Client", "RiotClientServices.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                    "Riot Games", "Riot Client", "RiotClientServices.exe"),
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"D:\Riot Games\Riot Client\RiotClientServices.exe",
                @"E:\Riot Games\Riot Client\RiotClientServices.exe",
                @"F:\Riot Games\Riot Client\RiotClientServices.exe"
            };

            foreach (string path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // Tìm trong registry
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Riot Games\Riot Client"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("InstallPath")?.ToString();
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            string clientPath = Path.Combine(installPath, "RiotClientServices.exe");
                            if (File.Exists(clientPath))
                                return clientPath;
                        }
                    }
                }

                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Riot Games\Riot Client"))
                {
                    if (key != null)
                    {
                        string installPath = key.GetValue("InstallPath")?.ToString();
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            string clientPath = Path.Combine(installPath, "RiotClientServices.exe");
                            if (File.Exists(clientPath))
                                return clientPath;
                        }
                    }
                }
            }
            catch { }

            // Tìm bằng process đang chạy
            try
            {
                var processes = Process.GetProcessesByName("RiotClientServices");
                if (processes.Length > 0)
                {
                    return processes[0].MainModule?.FileName;
                }
            }
            catch { }

            return null;
        }

        public static bool IsRiotClientRunning()
        {
            return Process.GetProcessesByName("RiotClientServices").Length > 0;
        }

        public static bool IsValorantRunning()
        {
            return Process.GetProcessesByName("VALORANT").Length > 0 ||
                   Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0;
        }

        public static void BringRiotClientToFront()
        {
            try
            {
                var processes = Process.GetProcessesByName("RiotClientServices");
                if (processes.Length > 0)
                {
                    var window = processes[0].MainWindowHandle;
                    if (window != IntPtr.Zero)
                    {
                        ShowWindow(window, SW_RESTORE);
                        SetForegroundWindow(window);
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