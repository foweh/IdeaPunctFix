using System.Runtime.InteropServices;

namespace IdeaPunctFix;

/// <summary>
/// IME 辅助 — 检测输入法是否处于中文模式
/// </summary>
internal static class ImeHelper
{
    /// <summary>
    /// 检查前台窗口的输入法是否处于中文/全角模式
    /// </summary>
    public static bool IsChineseInputMode()
    {
        var hWnd = WindowHelper.GetForegroundWindowHandle();
        if (hWnd == IntPtr.Zero)
            return false;

        var hImc = ImmGetContext(hWnd);
        if (hImc == IntPtr.Zero)
            return false;

        try
        {
            _ = ImmGetConversionStatus(hImc, out var conversion, out _);

            // conversion 位标志:
            //   bit 0 (IME_CMODE_NATIVE)  = 中文输入模式
            //   bit 1 (IME_CMODE_FULLSHAPE) = 全角模式
            // 只要设置了 IME_CMODE_NATIVE 就认为是中文模式
            return (conversion & IME_CMODE_NATIVE) != 0;
        }
        finally
        {
            _ = ImmReleaseContext(hWnd, hImc);
        }
    }

    private const int IME_CMODE_NATIVE = 0x0001;
    private const int IME_CMODE_FULLSHAPE = 0x0002;

    [DllImport("imm32.dll", SetLastError = false)]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmGetConversionStatus(IntPtr hImc, out int conversion, out int sentence);

    [DllImport("imm32.dll", SetLastError = false)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);
}
