using System.Windows;

namespace looklight.Services.Interfaces;

/// <summary>
/// 全局热键（Alt+M）：Attach 到窗口后，按下热键触发 HotkeyPressed。
/// </summary>
public interface IHotkeyService
{
    event Action? HotkeyPressed;
    void Attach(Window window);
    void Detach();
}
