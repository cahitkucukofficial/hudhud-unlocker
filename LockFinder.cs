using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HudhudUnlocker;

public class LockInfo
{
    public int Pid { get; set; }
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public static class LockFinder
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RM_PROCESS_INFO
    {
        public RM_UNIQUE_PROCESS Process;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strAppName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string strServiceShortName;
        public int ApplicationType;
        public uint AppStatus;
        public uint TSSessionId;
        [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
    }

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, StringBuilder strSessionKey);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmEndSession(uint pSessionHandle);

    [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
    private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames,
        uint nApplications, RM_UNIQUE_PROCESS[]? rgApplications, uint nServices, string[]? rgsServiceNames);

    [DllImport("rstrtmgr.dll")]
    private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo,
        [In, Out] RM_PROCESS_INFO[]? rgAffectedApps, ref uint lpdwRebootReasons);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags);

    private const int ERROR_MORE_DATA = 234;

    /// <summary>Dosyayı (klasörse içindeki dosyaları) kullanan işlemleri Windows Restart Manager ile bulur.</summary>
    public static List<LockInfo> Find(string target)
    {
        var files = new List<string>();
        if (Directory.Exists(target))
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(target, "*", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true }))
                {
                    files.Add(f);
                    if (files.Count >= 3000) break;
                }
            }
            catch { }
        }
        else if (File.Exists(target)) files.Add(target);

        var result = new List<LockInfo>();
        if (files.Count == 0) return result;

        // Restart Manager tek seferde çok dosyayı kaydedebilir; yine de parçalara bölüyoruz.
        var seen = new HashSet<int>();
        for (int i = 0; i < files.Count; i += 500)
        {
            var chunk = files.Skip(i).Take(500).ToArray();
            if (RmStartSession(out uint handle, 0, new StringBuilder(33)) != 0) continue;
            try
            {
                if (RmRegisterResources(handle, (uint)chunk.Length, chunk, 0, null, 0, null) != 0) continue;
                uint needed = 0, count = 0, reasons = 0;
                int res = RmGetList(handle, out needed, ref count, null, ref reasons);
                if (res == ERROR_MORE_DATA)
                {
                    var infos = new RM_PROCESS_INFO[needed];
                    count = needed;
                    res = RmGetList(handle, out needed, ref count, infos, ref reasons);
                    if (res == 0)
                    {
                        for (int k = 0; k < count; k++)
                        {
                            int pid = infos[k].Process.dwProcessId;
                            if (!seen.Add(pid)) continue;
                            string path = "";
                            string name = infos[k].strAppName;
                            try
                            {
                                var p = Process.GetProcessById(pid);
                                path = p.MainModule?.FileName ?? "";
                                if (string.IsNullOrWhiteSpace(name)) name = p.ProcessName;
                            }
                            catch { }
                            result.Add(new LockInfo { Pid = pid, Name = name, Path = path });
                        }
                    }
                }
            }
            finally { RmEndSession(handle); }
        }
        return result;
    }

    /// <summary>Sistem için kritik (veya bu uygulamanın kendisi olan) işlemlerin sonlandırılmasını engeller.</summary>
    public static bool IsProtected(int pid) =>
        pid <= 4 || pid == Environment.ProcessId;

    public static bool Kill(int pid)
    {
        if (IsProtected(pid)) return false;
        try
        {
            using var p = Process.GetProcessById(pid);
            p.Kill(true);
            p.WaitForExit(3000);
            return true;
        }
        catch { return false; }
    }

    /// <summary>Başarısız silmede, dosyayı bir sonraki yeniden başlatmada silinmek üzere işaretler.</summary>
    public static bool DeleteOnReboot(string path) => MoveFileEx(path, null, 4);
}
