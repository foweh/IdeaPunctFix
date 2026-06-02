namespace IdeaPunctFix;

/// <summary>
/// 托盘应用程序 — 管理系统托盘图标、菜单和钩子生命周期
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly MenuItem _toggleItem;
    private readonly MenuItem _statusItem;
    private KeyboardHook? _hook;

    public TrayApp()
    {
        // 创建托盘图标
        _trayIcon = new NotifyIcon
        {
            Text = "IdeaPunctFix — JetBrains 智能标点",
            Icon = CreateIcon(),
            Visible = true,
        };

        // 状态菜单项
        _statusItem = new MenuItem("状态: 运行中") { Enabled = false };

        // 启用/禁用切换
        _toggleItem = new MenuItem("启用转换", OnToggle)
        {
            Checked = true
        };

        // 菜单
        _trayIcon.ContextMenu = new ContextMenu(new[]
        {
            _statusItem,
            new MenuItem("-"),
            _toggleItem,
            new MenuItem("-"),
            new MenuItem("关于 IdeaPunctFix", OnAbout),
            new MenuItem("退出", OnExit),
        });

        _trayIcon.MouseDoubleClick += OnTrayDoubleClick;

        try
        {
            _hook = new KeyboardHook();
            _statusItem.Text = "✓ 运行中 (JetBrains IDE 自动转换)";
        }
        catch (Exception ex)
        {
            _statusItem.Text = "✗ 启动失败";
            _ = MessageBox.Show(
                $"无法启动键盘钩子:\n{ex.Message}\n\n请尝试以管理员身份运行。",
                "IdeaPunctFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        // 窗口关闭时退出
        Application.ApplicationExit += OnApplicationExit;
    }

    /// <summary>
    /// 创建一个简单的应用程序图标（使用系统图标 + 文字）
    /// </summary>
    private static Icon CreateIcon()
    {
        // 使用 .NET 内置的应用程序图标
        // 也可以使用 SystemIcons.Application
        return SystemIcons.Application;
    }

    private void OnTrayDoubleClick(object? sender, EventArgs e)
    {
        // 双击切换启用/禁用
        if (_hook != null)
        {
            _hook.Enabled = !_hook.Enabled;
            UpdateToggleState();
        }
    }

    private void OnToggle(object? sender, EventArgs e)
    {
        if (_hook != null)
        {
            _hook.Enabled = !_hook.Enabled;
            UpdateToggleState();
        }
    }

    private void UpdateToggleState()
    {
        if (_hook == null) return;

        _toggleItem.Checked = _hook.Enabled;
        _toggleItem.Text = _hook.Enabled ? "禁用转换" : "启用转换";

        if (_hook.Enabled)
            _statusItem.Text = "✓ 运行中 (JetBrains IDE 自动转换)";
        else
            _statusItem.Text = "⏸ 已暂停 (手动关闭)";

        _trayIcon.Text = _hook.Enabled
            ? "IdeaPunctFix — JetBrains 智能标点"
            : "IdeaPunctFix — 已暂停";
    }

    private void OnAbout(object? sender, EventArgs e)
    {
        _ = MessageBox.Show(
            "IdeaPunctFix v1.0\n\n" +
            "当 IntelliJ IDEA / WebStorm / PyCharm 等 JetBrains IDE 窗口在前台时，\n" +
            "自动将中文标点符号转换为英文标点。\n\n" +
            "像 HBuilder 一样，写代码再也不用切输入法了。\n\n" +
            "支持的 IDE:\n" +
            "  IntelliJ IDEA, WebStorm, PyCharm, GoLand,\n" +
            "  CLion, Rider, DataGrip, RubyMine, PhpStorm\n\n" +
            "右键托盘图标可暂停/恢复转换。",
            "关于 IdeaPunctFix",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    private void OnApplicationExit(object? sender, EventArgs e)
    {
        _hook?.Dispose();
        _hook = null;
        _trayIcon.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook?.Dispose();
            _trayIcon?.Dispose();
        }
        base.Dispose(disposing);
    }
}
