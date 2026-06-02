using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IdeaPunctFix;

/// <summary>
/// 低层键盘钩子 (WH_KEYBOARD_LL) — 拦截标点符号键并转换为英文
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    // Hook 类型
    private const int WH_KEYBOARD_LL = 13;

    // Windows 消息
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    // SendInput 类型
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private readonly LowLevelKeyboardProc _callback;
    private readonly IntPtr _hookId;

    // 防递归标志 — 防止我们自己 SendInput 产生的按键被再次拦截
    [ThreadStatic]
    private static int _sending;

    /// <summary>
    /// 是否启用转换（可通过托盘菜单切换）
    /// </summary>
    public bool Enabled { get; set; } = true;

    public KeyboardHook()
    {
        _callback = HookCallback;
        _hookId = SetHook(_callback);

        if (_hookId == IntPtr.Zero)
            throw new InvalidOperationException("无法安装低层键盘钩子。错误码: " + Marshal.GetLastWin32Error());
    }

    ~KeyboardHook()
    {
        Dispose(false);
    }

    private static IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        return SetWindowsHookEx(
            WH_KEYBOARD_LL,
            proc,
            GetModuleHandle(curModule.ModuleName),
            0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // 防递归：自己发送的按键不处理
        if (_sending != 0)
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var vkCode = Marshal.ReadByte(lParam);

            // 检查是否是需要处理的标点键
            if (!PunctuationFilter.IsPunctuationKey(vkCode))
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            // 检查是否启用
            if (!Enabled)
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            // 检查前台窗口是否是 JetBrains IDE
            if (!WindowHelper.IsJetBrainsIdeForeground())
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            // 检查 IME 是否在中文模式
            if (!ImeHelper.IsChineseInputMode())
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            // 获取对应的英文 ASCII 字符
            var shiftDown = PunctuationFilter.IsShiftDown();
            var asciiChar = PunctuationFilter.GetAsciiChar(vkCode, shiftDown);

            if (asciiChar == '\0')
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);

            // 阻止原始按键，发送英文标点
            return SendAsciiChar(asciiChar) ? (IntPtr)1 : CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }

    /// <summary>
    /// 通过 PostMessage 发送 WM_CHAR 到前台窗口，并阻止原始按键
    /// 使用 WM_CHAR 避免了 SendInput 的递归问题
    /// </summary>
    private static bool SendAsciiChar(char ch)
    {
        var hWnd = WindowHelper.GetForegroundWindowHandle();
        if (hWnd == IntPtr.Zero)
            return false;

        // 直接发送 WM_CHAR 消息到目标窗口
        // 对于 Java/Swing 编辑器，WM_CHAR 会被 AWT 事件队列正确接收
        _ = PostMessage(hWnd, WM_CHAR, (IntPtr)ch, IntPtr.Zero);
        return true;
    }

    // ===== P/Invoke =====

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private const int WM_CHAR = 0x0102;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    // ===== Dispose =====

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_hookId != IntPtr.Zero)
        {
            _ = UnhookWindowsHookEx(_hookId);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
}
