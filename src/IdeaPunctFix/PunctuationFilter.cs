using System.Runtime.InteropServices;

namespace IdeaPunctFix;

/// <summary>
/// 标点符号映射 — 将中文标点的 VirtualKey 映射为对应的英文 ASCII 字符
/// </summary>
internal static class PunctuationFilter
{
    // VirtualKey 常量
    private const byte VK_OEM_1 = 0xBA;     // ;:
    private const byte VK_OEM_PLUS = 0xBB;   // =+
    private const byte VK_OEM_COMMA = 0xBC;  // ,<
    private const byte VK_OEM_MINUS = 0xBD;  // -_
    private const byte VK_OEM_PERIOD = 0xBE; // .>
    private const byte VK_OEM_2 = 0xBF;      // /?
    private const byte VK_OEM_3 = 0xC0;      // `~
    private const byte VK_OEM_4 = 0xDB;      // [{
    private const byte VK_OEM_5 = 0xDC;      // \|
    private const byte VK_OEM_6 = 0xDD;      // ]}
    private const byte VK_OEM_7 = 0xDE;      // '"
    private const byte VK_OEM_8 = 0xDF;      // 特殊

    // 映射表: VirtualKey -> (noShiftChar, shiftChar)
    // 当在中文输入法下按这些键时，输出对应的英文 ASCII 字符
    private static readonly Dictionary<byte, (char NoShift, char Shift)> Map = new()
    {
        { VK_OEM_COMMA,  (',', '<') },
        { VK_OEM_PERIOD, ('.', '>') },
        { VK_OEM_2,      ('/', '?') },
        { VK_OEM_1,      (';', ':') },
        { VK_OEM_7,      ('\'', '"') },
        { VK_OEM_4,      ('[', '{') },
        { VK_OEM_6,      (']', '}') },
        { VK_OEM_5,      ('\\', '|') },
        { VK_OEM_3,      ('`', '~') },
        { VK_OEM_MINUS,  ('-', '_') },
        { VK_OEM_PLUS,   ('=', '+') },
    };

    // 这些键的 Shift 版本在中文输入法下通常是全角符号
    // 对于数字行上面的符号（!@#$%^&*()），大多数输入法在中文模式下不会变全角，
    // 所以我们不处理它们，只处理上面的标点键

    // 同时处理 0-9 数字键上方的符号（部分输入法会全角化这些符号）
    // 例如：! → ！, @ → ＠, # → ＃, $ → ＄, % → ％, ^ → ＾, & → ＆, * → ＊, ( → （, ) → ）
    private static readonly Dictionary<byte, char> ShiftNumberMap = new()
    {
        { (byte)'1', '!' },
        { (byte)'2', '@' },
        { (byte)'3', '#' },
        { (byte)'4', '$' },
        { (byte)'5', '%' },
        { (byte)'6', '^' },
        { (byte)'7', '&' },
        { (byte)'8', '*' },
        { (byte)'9', '(' },
        { (byte)'0', ')' },
    };

    /// <summary>
    /// 检查指定的 VirtualKey 是否是需要过滤的标点键
    /// </summary>
    public static bool IsPunctuationKey(byte vkCode)
    {
        return Map.ContainsKey(vkCode);
    }

    /// <summary>
    /// 检查是否是需要过滤的 Shift+数字 符号键
    /// </summary>
    public static bool IsShiftNumberKey(byte vkCode)
    {
        return ShiftNumberMap.ContainsKey(vkCode);
    }

    /// <summary>
    /// 获取按键对应的英文 ASCII 字符（根据 Shift 状态选择）
    /// </summary>
    public static char GetAsciiChar(byte vkCode, bool shiftDown)
    {
        if (Map.TryGetValue(vkCode, out var pair))
            return shiftDown ? pair.Shift : pair.NoShift;

        if (shiftDown && ShiftNumberMap.TryGetValue(vkCode, out var ch))
            return ch;

        return '\0';
    }

    /// <summary>
    /// 获取 Shift 键状态
    /// </summary>
    public static bool IsShiftDown()
    {
        return (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0;
    }

    private const int VK_SHIFT = 0x10;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
