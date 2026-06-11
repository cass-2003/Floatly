using System.Windows.Forms;
using DeskLite.Services;

namespace DeskLite;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly MainWindow _window;
    private readonly Action _onOpenSettings;
    private readonly Action _onAddTodo;
    private readonly Action _onAddCountdown;
    private readonly Action _onJumpCalendarDate;
    private readonly Action _onResetCalendarToday;
    private readonly Action _onExportBackup;
    private readonly Action _onExit;

    public TrayService(
        MainWindow window,
        Action onOpenSettings,
        Action onAddTodo,
        Action onAddCountdown,
        Action onJumpCalendarDate,
        Action onResetCalendarToday,
        Action onExportBackup,
        Action onExit)
    {
        _window = window;
        _onOpenSettings = onOpenSettings;
        _onAddTodo = onAddTodo;
        _onAddCountdown = onAddCountdown;
        _onJumpCalendarDate = onJumpCalendarDate;
        _onResetCalendarToday = onResetCalendarToday;
        _onExportBackup = onExportBackup;
        _onExit = onExit;

        _icon = new NotifyIcon
        {
            Text = "DeskLite",
            Icon = AppIconService.LoadTrayIcon(),
            Visible = true
        };

        _icon.DoubleClick += (_, _) => ToggleWindow();
        RefreshMenu();
    }

    public void RefreshMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("显示/隐藏", null, (_, _) => ToggleWindow());
        menu.Items.Add("设置...", null, (_, _) => _onOpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("添加待办", null, (_, _) => _onAddTodo());
        menu.Items.Add("添加倒数日...", null, (_, _) => _onAddCountdown());
        menu.Items.Add(BuildCalendarMenu());
        menu.Items.Add("导出数据备份", null, (_, _) => _onExportBackup());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => _onExit());
        _icon.ContextMenuStrip = menu;
    }

    private ToolStripMenuItem BuildCalendarMenu()
    {
        var calendar = new ToolStripMenuItem("日历");
        calendar.DropDownItems.Add("回到今天", null, (_, _) => _onResetCalendarToday());
        calendar.DropDownItems.Add("跳转日期...", null, (_, _) => _onJumpCalendarDate());
        return calendar;
    }

    private void ToggleWindow()
    {
        if (_window.IsVisible)
        {
            _window.Hide();
        }
        else
        {
            _window.Show();
            _window.Activate();
        }
    }

    public void ShowBalloon(string message, string title = "DeskLite")
    {
        _icon.BalloonTipTitle = title;
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        var icon = _icon.Icon;
        _icon.Visible = false;
        _icon.Dispose();
        if (icon is not null && !ReferenceEquals(icon, System.Drawing.SystemIcons.Application))
        {
            icon.Dispose();
        }
    }
}
