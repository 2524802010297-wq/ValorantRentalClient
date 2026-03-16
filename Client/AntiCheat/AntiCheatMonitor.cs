using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ValorantRentalClient
{
    public class AntiCheatMonitor
    {
        #region WinAPI
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("psapi.dll")]
        private static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, System.Text.StringBuilder lpFilename, int nSize);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint PROCESS_VM_READ = 0x0010;
        #endregion

        // SỬA QUAN TRỌNG: chỉ rõ dùng System.Threading.Timer
        private System.Threading.Timer _scanTimer;
        private int _userId;
        private int _sessionId;
        private Action<string> _onCheatDetected;
        private bool _isRunning = false;
        private List<string> _runningProcesses = new List<string>();
        private HashSet<string> _allowedProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "valorant", "valorant.exe", "valorant-win64-shipping", "valorant-win64-shipping.exe",
            "riotclientservices", "riotclientservices.exe", "riot client", "riotclientservices",
            "vanguard", "vgtray", "vgc", "vanguard.exe",
            "discord", "discord.exe", "spotify", "chrome", "firefox", "edge",
            "explorer", "taskmgr", "cmd", "powershell", "notepad",
            "winrar", "7zfm", "winzip", "vlc", "mpc-hc",
            "zalo", "zalo.exe", "facebook", "messenger",
            "garena", "garena.exe", "steam", "steam.exe", "epicgameslauncher"
        };

        private readonly HashSet<string> _cheatProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cheatengine", "cheatengine.exe", "artmoney", "artmoney.exe",
            "wemod", "wemod.exe", "trainer", "trainer.exe", "aimbot", "aimbot.exe",
            "ollydbg", "ollydbg.exe", "x64dbg", "x64dbg.exe",
            "processhacker", "processhacker.exe", "memoryhacker", "memoryhacker.exe"
        };

        public void StartMonitoring(int userId, int sessionId, Action<string> onCheatDetected)
        {
            _userId = userId;
            _sessionId = sessionId;
            _onCheatDetected = onCheatDetected;
            _isRunning = true;

            // SỬA: dùng System.Threading.TimerCallback
            _scanTimer = new System.Threading.Timer(new TimerCallback(ScanCallback), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private void ScanCallback(object state)
        {
            if (!_isRunning) return;
            // Phần còn lại giữ nguyên
        }

        public void Stop()
        {
            _isRunning = false;
            _scanTimer?.Dispose();
        }
    }
}