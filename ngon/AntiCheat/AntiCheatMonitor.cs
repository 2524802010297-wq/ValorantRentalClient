using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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

        private Timer _scanTimer;
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

        // Danh sách tiến trình cheat phổ biến khi chơi Valorant
        private readonly HashSet<string> _cheatProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Cheat Engine & memory scanners
            "cheatengine", "cheatengine-x86_64", "cheatengine-i386", "cheatengine.exe",
            "cheatengine77", "cheatengine80", "cheatengine81", "cheatengine82",
            "cheatengine83", "cheatengine84", "cheatengine85", "cheatengine86",
            "artmoney", "artmoney.exe", "artmoney7", "artmoney8", "artmoneypro",
            "artmoneypro.exe", "artmoney64", "artmoney32",
            "memoryhacker", "memoryhacker.exe", "memoryhacker64", "memoryhacker32",
            "memhack", "memhack.exe", "memhack64", "hackmem",
            "gameguardian", "gameguardian.exe", "gg", "gg.exe",
            "scanmem", "scanmem.exe", "gameconqueror", "gameconqueror.exe",
            
            // Trainers & Aimbots
            "wemod", "wemod.exe", "wemodapp", "wemodapp.exe", "wemodfree",
            "flingtrainer", "fling", "fling.exe", "flingtrainer.exe",
            "trainer", "trainer.exe", "trainerapp", "trainerapp.exe",
            "cheathappens", "cheathappens.exe", "ch_trainer",
            "aimbot", "aimbot.exe", "aimbotval", "aimbotvalorant", "valorantaimbot",
            "valorantcheat", "valorantcheat.exe", "valorantcheats", "valcheat",
            "wallhack", "wallhack.exe", "wallhackval", "esp", "esp.exe", "espval",
            "triggerbot", "triggerbot.exe", "triggerbotval", "softaim", "softaim.exe",
            "spoofer", "spoofer.exe", "hwidspoofer", "hwidspoofer.exe", "hwidchanger",
            "spoofervalorant", "spooferval", "spoofer64", "spoofer32",
            
            // Debuggers & reverse engineering
            "ollydbg", "ollydbg.exe", "ollydbg64", "ollydbg32", "ollyice",
            "x64dbg", "x64dbg.exe", "x32dbg", "x32dbg.exe", "x96dbg", "x96dbg.exe",
            "ida", "ida.exe", "ida64", "ida64.exe", "ida32", "ida32.exe",
            "idapro", "idapro.exe", "idapro64", "idapro64.exe",
            "ghidra", "ghidra.exe", "ghidra64", "ghidra64.exe",
            "dnspy", "dnspy.exe", "dnspy64", "dnspy32",
            "de4dot", "de4dot.exe", "de4dot64", "de4dot32",
            "confuser", "confuser.exe", "confuser64", "confuser32",
            "injector", "injector.exe", "injector64", "injector32",
            "extremeinjector", "extremeinjector.exe", "extremeinjector64",
            "sinper", "sinper.exe", "sinper64", "sinper32",
            
            // Known cheat loaders
            "playclaw", "playclaw.exe", "playclaw64", "playclaw32",
            "fraps", "fraps.exe", "fraps64", "fraps32",
            "msiafterburner", "msiafterburner.exe", "afterburner",
            "rtss", "rtss.exe", "rivatuner", "rivatuner.exe",
            "evgaprecision", "evgaprecision.exe", "precisionx",
            "dzichinjector", "dzichinjector.exe", "dzich",
            "chickeninjector", "chickeninjector.exe", "chicken",
            "perx", "perx.exe", "perxinjector", "perxinjector.exe",
            
            // Macro & automation
            "autohotkey", "autohotkey.exe", "ahk", "ahk.exe",
            "autoclick", "autoclick.exe", "autoclicker", "autoclicker.exe",
            "macros", "macros.exe", "macrorecorder", "macrorecorder.exe",
            "puloversmacrocreator", "puloversmacrocreator.exe",
            "tinytask", "tinytask.exe", "tinytask64", "tinytask32",
            "jitbit", "jitbit.exe", "jitbitmacro", "jitbitmacro.exe",
            
            // VPN & proxy tools (thường dùng để che giấu)
            "proxifier", "proxifier.exe", "proxifier64", "proxifier32",
            "proxycap", "proxycap.exe", "proxycap64", "proxycap32",
            "windscribe", "windscribe.exe", "windscribe64", "windscribe32",
            "tunnelbear", "tunnelbear.exe", "tunnelbear64", "tunnelbear32",
            "hotspotshield", "hotspotshield.exe", "hotspotshield64", "hotspotshield32",
            
            // Process hiding tools
            "processhacker", "processhacker.exe", "processhacker64", "processhacker32",
            "processexplorer", "processexplorer.exe", "procexp", "procexp.exe",
            "procexp64", "procexp64.exe", "procmon", "procmon.exe",
            "winsleep", "winsleep.exe", "winsleep64", "winsleep32",
            "hideprocess", "hideprocess.exe", "hideproc", "hideproc.exe"
        };

        // Danh sách driver đáng ngờ
        private readonly HashSet<string> _suspiciousDrivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "kprocesshacker", "kprocesshacker.sys", "processhacker.sys",
            "winring0", "winring0.sys", "winring0x64", "winring0x64.sys",
            "winring", "winring.sys", "winring64", "winring64.sys",
            "eac", "eac.sys", "easyanticheat", "easyanticheat.sys",
            "battleye", "battleye.sys", "battleye64", "battleye64.sys",
            "mhyprot", "mhyprot.sys", "mhyprot2", "mhyprot2.sys", "mhyprot3", "mhyprot3.sys",
            "xigncode", "xigncode.sys", "xigncode64", "xigncode64.sys",
            "nprotect", "nprotect.sys", "nprotect64", "nprotect64.sys",
            "gameguard", "gameguard.sys", "gameguard64", "gameguard64.sys",
            "hackshield", "hackshield.sys", "hackshield64", "hackshield64.sys",
            "anhkgg", "anhkgg.sys", "anhkgg64", "anhkgg64.sys",
            "anti", "anti.sys", "anticheat", "anticheat.sys",
            "inpout32", "inpout32.sys", "inpoutx64", "inpoutx64.sys",
            "physmem", "physmem.sys", "physmem64", "physmem64.sys"
        };

        public void StartMonitoring(int userId, int sessionId, Action<string> onCheatDetected)
        {
            _userId = userId;
            _sessionId = sessionId;
            _onCheatDetected = onCheatDetected;
            _isRunning = true;

            // Quét mỗi 3 giây
            _scanTimer = new Timer(ScanCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private void ScanCallback(object state)
        {
            if (!_isRunning) return;

            try
            {
                // Scan tiến trình
                if (ScanProcesses())
                    return;

                // Scan windows
                if (ScanWindows())
                    return;

                // Scan modules của process hiện tại
                if (ScanModules())
                    return;

                // Scan drivers
                if (ScanDrivers())
                    return;

                // Scan services đáng ngờ
                if (ScanServices())
                    return;

                // Kiểm tra Vanguard có đang chạy không
                CheckVanguardStatus();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Scan error: {ex.Message}");
            }
        }

        private bool ScanProcesses()
        {
            var processes = Process.GetProcesses();
            var currentProcesses = new List<string>();

            foreach (var proc in processes)
            {
                try
                {
                    string procName = proc.ProcessName.ToLower();
                    currentProcesses.Add(procName);

                    // Bỏ qua các process được phép
                    if (_allowedProcesses.Any(allowed => procName.Contains(allowed)))
                        continue;

                    // Kiểm tra tên tiến trình
                    foreach (var cheat in _cheatProcesses)
                    {
                        if (procName.Contains(cheat))
                        {
                            string fullPath = GetProcessPath(proc);
                            _onCheatDetected?.Invoke($"{proc.ProcessName} (Cheat process)");
                            LogViolation($"Phát hiện cheat process: {proc.ProcessName} - Path: {fullPath}");
                            return true;
                        }
                    }

                    // Kiểm tra process không có cửa sổ nhưng có nhiều module
                    if (proc.MainWindowHandle == IntPtr.Zero && proc.Modules.Count > 20)
                    {
                        if (CheckSuspiciousModules(proc))
                        {
                            _onCheatDetected?.Invoke($"{proc.ProcessName} (Suspicious module)");
                            return true;
                        }
                    }
                }
                catch
                {
                    // Bỏ qua process không truy cập được
                    continue;
                }
            }

            // Lưu danh sách process hiện tại để so sánh
            _runningProcesses = currentProcesses;
            return false;
        }

        private bool ScanWindows()
        {
            var windows = new List<WindowInfo>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    var sb = new System.Text.StringBuilder(256);
                    GetWindowText(hWnd, sb, sb.Capacity);
                    string title = sb.ToString();

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        GetWindowThreadProcessId(hWnd, out uint pid);
                        
                        windows.Add(new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title,
                            ProcessId = (int)pid
                        });
                    }
                }
                return true;
            }, IntPtr.Zero);

            foreach (var window in windows)
            {
                string title = window.Title.ToLower();

                // Tìm kiếm các từ khóa trong tiêu đề cửa sổ
                string[] suspiciousTitles = new[]
                {
                    "cheat", "hack", "trainer", "injector", "memory", "debug",
                    "aimbot", "wallhack", "esp", "triggerbot", "softaim",
                    "spoofer", "hwid", "bypass", "bypasser", "unlocker",
                    "crack", "keygen", "loader", "mod menu", "menu hack",
                    "valorant hack", "valorant cheat", "valorant aimbot"
                };

                foreach (var suspicious in suspiciousTitles)
                {
                    if (title.Contains(suspicious))
                    {
                        try
                        {
                            Process proc = Process.GetProcessById(window.ProcessId);
                            _onCheatDetected?.Invoke($"Window: {window.Title}");
                            LogViolation($"Phát hiện cửa sổ đáng ngờ: {window.Title} - Process: {proc.ProcessName}");
                            return true;
                        }
                        catch { }
                    }
                }
            }

            return false;
        }

        private bool ScanModules()
        {
            var currentProcess = Process.GetCurrentProcess();

            foreach (ProcessModule module in currentProcess.Modules)
            {
                try
                {
                    string moduleName = module.ModuleName.ToLower();

                    // Phát hiện DLL injection
                    string[] suspiciousModules = new[]
                    {
                        "inject", "hook", "detour", "proxy", "bypass",
                        "cheat", "hack", "trainer", "aimbot", "wallhack",
                        "mhook", "detours", "easyhook", "sharpdx",
                        "vanguard", "vgc", "vgk", "vgc.sys"
                    };

                    foreach (var susp in suspiciousModules)
                    {
                        if (moduleName.Contains(susp))
                        {
                            _onCheatDetected?.Invoke($"Module: {moduleName}");
                            LogViolation($"Phát hiện module đáng ngờ: {moduleName} - Path: {module.FileName}");
                            return true;
                        }
                    }
                }
                catch { }
            }

            return false;
        }

        private bool ScanDrivers()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_SystemDriver"))
                {
                    foreach (ManagementObject driver in searcher.Get())
                    {
                        try
                        {
                            string name = driver["Name"]?.ToString().ToLower() ?? "";
                            string displayName = driver["DisplayName"]?.ToString().ToLower() ?? "";
                            string state = driver["State"]?.ToString() ?? "";
                            string startMode = driver["StartMode"]?.ToString() ?? "";

                            // Kiểm tra driver đang chạy
                            if (state.Contains("Running"))
                            {
                                foreach (var suspicious in _suspiciousDrivers)
                                {
                                    if (name.Contains(suspicious) || displayName.Contains(suspicious))
                                    {
                                        _onCheatDetected?.Invoke($"Driver: {name}");
                                        LogViolation($"Phát hiện driver đáng ngờ: {name} - {displayName}");
                                        return true;
                                    }
                                }
                            }

                            // Kiểm tra driver tự khởi động nhưng đáng ngờ
                            if (startMode.Contains("Auto") && (name.Contains("cheat") || displayName.Contains("cheat")))
                            {
                                _onCheatDetected?.Invoke($"Driver: {name}");
                                return true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return false;
        }

        private bool ScanServices()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Service WHERE State='Running'"))
                {
                    foreach (ManagementObject service in searcher.Get())
                    {
                        try
                        {
                            string name = service["Name"]?.ToString().ToLower() ?? "";
                            string displayName = service["DisplayName"]?.ToString().ToLower() ?? "";
                            string pathName = service["PathName"]?.ToString().ToLower() ?? "";

                            // Kiểm tra service đáng ngờ
                            string[] suspiciousServices = new[]
                            {
                                "cheat", "hack", "trainer", "inject", "hook",
                                "vanguard", "vgc", "easyanticheat", "battleye",
                                "xigncode", "nprotect", "gameguard", "hackshield"
                            };

                            foreach (var susp in suspiciousServices)
                            {
                                if (name.Contains(susp) || displayName.Contains(susp) || pathName.Contains(susp))
                                {
                                    _onCheatDetected?.Invoke($"Service: {name}");
                                    LogViolation($"Phát hiện service đáng ngờ: {name} - {displayName}");
                                    return true;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }

            return false;
        }

        private void CheckVanguardStatus()
        {
            try
            {
                // Kiểm tra Vanguard có đang chạy không
                bool vanguardFound = false;
                var processes = Process.GetProcesses();

                foreach (var proc in processes)
                {
                    string procName = proc.ProcessName.ToLower();
                    if (procName.Contains("vanguard") || procName.Contains("vgc") || procName.Contains("vgtray"))
                    {
                        vanguardFound = true;
                        break;
                    }
                }

                if (!vanguardFound)
                {
                    // Có thể Vanguard không chạy, nhưng không phải cheat
                    // Chỉ log, không báo cheat
                    Debug.WriteLine("Warning: Vanguard service not detected");
                }
            }
            catch { }
        }

        private bool CheckSuspiciousModules(Process proc)
        {
            try
            {
                foreach (ProcessModule module in proc.Modules)
                {
                    string moduleName = module.ModuleName.ToLower();

                    string[] suspiciousModuleNames = new[]
                    {
                        "cheat", "hack", "trainer", "aimbot", "wallhack",
                        "inject", "hook", "detour", "bypass", "spoofer",
                        "memory", "debug", "dllinject", "easyhook"
                    };

                    foreach (var susp in suspiciousModuleNames)
                    {
                        if (moduleName.Contains(susp))
                        {
                            LogViolation($"Phát hiện module đáng ngờ trong process {proc.ProcessName}: {moduleName}");
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private string GetProcessPath(Process proc)
        {
            try
            {
                return proc.MainModule?.FileName ?? "Unknown";
            }
            catch
            {
                return "Access Denied";
            }
        }

        private void LogViolation(string details)
        {
            try
            {
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - User {_userId} - {details}";
                string logPath = Path.Combine(Application.StartupPath, "violation_log.txt");
                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch { }
        }

        public void Stop()
        {
            _isRunning = false;
            _scanTimer?.Dispose();
        }

        private class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
            public int ProcessId { get; set; }
        }
    }
}