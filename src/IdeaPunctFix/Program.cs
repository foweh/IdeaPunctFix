namespace IdeaPunctFix;

/// <summary>
/// IdeaPunctFix — JetBrains IDE 智能标点转换工具
/// 当 JetBrains IDE（IntelliJ IDEA、WebStorm、PyCharm 等）窗口在前台时，
/// 自动将中文标点符号转换为英文标点，像 HBuilder 一样方便写代码。
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
