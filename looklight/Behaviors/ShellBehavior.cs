using System.ComponentModel;
using System.Windows;
using looklight.Services;
using Prism;
using Prism.Ioc;

namespace looklight.Behaviors;

/// <summary>
/// Shell 生命周期行为：通过附加属性挂接到主窗口，
/// 统一负责全局热键、托盘、关闭入托盘、显隐切换等窗口级逻辑，
/// 使视图 code-behind 保持干净。
/// </summary>
public static class ShellBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ShellBehavior), new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject d) => (bool)d.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject d, bool value) => d.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Window window || e.NewValue is not true) return;

        var container = ContainerLocator.Container;
        var hotkey = container.Resolve<IHotkeyService>();
        var tray = container.Resolve<ITrayService>();

        void OnHotkeyPressed() => ToggleVisibility(window);

        void OnShowRequested() => Show(window);

        void OnExitRequested() => window.Close();

        // HWND 就绪后才能注册全局热键；托盘一并挂上
        void OnSourceInitialized(object? sender, EventArgs args)
        {
            hotkey.Attach(window);
            tray.Attach(window);
        }

        void OnClosing(object? sender, CancelEventArgs args)
        {
            if (!tray.IsExiting)
            {
                // 关闭按钮 = 最小化到托盘
                args.Cancel = true;
                window.Hide();
            }
        }

        void OnClosed(object? sender, EventArgs args)
        {
            window.SourceInitialized -= OnSourceInitialized;
            window.Closing -= OnClosing;
            window.Closed -= OnClosed;
            hotkey.HotkeyPressed -= OnHotkeyPressed;
            tray.ShowRequested -= OnShowRequested;
            tray.ExitRequested -= OnExitRequested;
            hotkey.Detach();
            if (tray.IsExiting) tray.Dispose();
        }

        window.SourceInitialized += OnSourceInitialized;
        window.Closing += OnClosing;
        window.Closed += OnClosed;
        hotkey.HotkeyPressed += OnHotkeyPressed;
        tray.ShowRequested += OnShowRequested;
        tray.ExitRequested += OnExitRequested;
    }

    /// <summary>Alt+M：显示 / 隐藏切换。</summary>
    private static void ToggleVisibility(Window window)
    {
        if (window.IsVisible && window.IsActive)
            window.Hide();
        else
            Show(window);
    }

    private static void Show(Window window)
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
        if (window.FindName("InputBox") is System.Windows.Controls.TextBox input)
        {
            input.Focus();
            input.SelectAll();
        }
    }
}
