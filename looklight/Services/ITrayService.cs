using System.Windows;

namespace looklight.Services;

/// <summary>
/// 系统托盘：双击/菜单“显示主窗口”触发 ShowRequested；“退出”先置 IsExiting 再触发 ExitRequested。
/// </summary>
public interface ITrayService : IDisposable
{
    bool IsExiting { get; }
    event Action? ShowRequested;
    event Action? ExitRequested;
    void Attach(Window window);
}
