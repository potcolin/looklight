using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace looklight.Services;

public sealed class HotkeyService : IHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint VkM = 0x4D;

    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _registered;

    public event Action? HotkeyPressed;

    public void Attach(Window window)
    {
        if (_registered) return;

        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        if (_source is null) return;

        _source.AddHook(WndProc);
        _registered = RegisterHotKey(_hwnd, 1, ModAlt, VkM);
        if (!_registered)
            System.Windows.MessageBox.Show("Alt+M 全局热键注册失败，可能已被其他程序占用。", "looklight");
    }

    public void Detach()
    {
        if (_registered)
        {
            UnregisterHotKey(_hwnd, 1);
            _registered = false;
        }
        _source?.RemoveHook(WndProc);
        _source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam == (IntPtr)1)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
