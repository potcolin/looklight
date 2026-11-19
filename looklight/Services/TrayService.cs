using System.Windows;
using Forms = System.Windows.Forms;

namespace looklight.Services;

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? _icon;

    public bool IsExiting { get; private set; }
    public event Action? ShowRequested;
    public event Action? ExitRequested;

    public void Attach(Window window)
    {
        if (_icon is not null) return;

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("显示主窗口", null, (_, _) => ShowRequested?.Invoke());
        menu.Items.Add("退出", null, (_, _) =>
        {
            IsExiting = true;
            ExitRequested?.Invoke();
        });

        _icon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "looklight（Alt+M 唤醒）",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    public void Dispose()
    {
        if (_icon is null) return;
        _icon.Visible = false;
        _icon.Dispose();
        _icon = null;
    }
}
