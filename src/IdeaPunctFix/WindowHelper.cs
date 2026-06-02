using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IdeaPunctFix;

/// <summary>
/// 窗口检测工具 — 判断前台窗口是否为目标 IDE 窗口
/// </summary>
internal static class WindowHelper
{
    // JetBrains IDE 进程名列表（不含 .exe 扩展名）
    private static readonly HashSet<string> TargetProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "idea",       // IntelliJ IDEA
        "idea64",     // IntelliJ IDEA (64-bit)
        "rider64",    // JetBrains Rider
        "webstorm64", // WebStorm
        "pycharm64",  // PyCharm
        "goland64",   // GoLand
        "clion64",    // CLion
        "datagrip64", // DataGrip
        "rubymine64", // RubyMine
        "phpstorm64", // PhpStorm
    };

    /// <summary>
    /// 当前前台窗口是否是 JetBrains IDE
    /// </summary>
    public static bool IsJetBrainsIdeForeground()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return false;

        _ = GetWindowThreadProcessId(hWnd, out var pid);
        if (pid == 0)
            return false;

        try
        {
            var process = Process.GetProcessById((int)pid);
            var name = process.ProcessName;
            return TargetProcesses.Contains(name);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取前台窗口句柄
    /// </summary>
    public static IntPtr GetForegroundWindowHandle()
    {
        return GetForegroundWindow();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
}
