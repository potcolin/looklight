using System.Windows;
using System.Windows.Input;

namespace looklight.Behaviors;

/// <summary>
/// 窗口级视图行为：窗口没有系统标题栏，拖动和关闭都通过附加属性在 XAML 里声明，
/// code-behind 不必再为窗口操作写事件处理器。
/// </summary>
public static class WindowBehavior
{
    /// <summary>挂到标题栏空白区域：按住左键拖动窗口。</summary>
    public static readonly DependencyProperty IsDragAreaProperty = DependencyProperty.RegisterAttached(
        "IsDragArea", typeof(bool), typeof(WindowBehavior), new PropertyMetadata(false, OnIsDragAreaChanged));

    /// <summary>挂到按钮上：点击后关闭所在窗口（关窗由 ShellBehavior 变成最小化到托盘）。</summary>
    public static readonly DependencyProperty IsCloseButtonProperty = DependencyProperty.RegisterAttached(
        "IsCloseButton", typeof(bool), typeof(WindowBehavior), new PropertyMetadata(false, OnIsCloseButtonChanged));

    public static bool GetIsDragArea(DependencyObject d) => (bool)d.GetValue(IsDragAreaProperty);
    public static void SetIsDragArea(DependencyObject d, bool value) => d.SetValue(IsDragAreaProperty, value);

    public static bool GetIsCloseButton(DependencyObject d) => (bool)d.GetValue(IsCloseButtonProperty);
    public static void SetIsCloseButton(DependencyObject d, bool value) => d.SetValue(IsCloseButtonProperty, value);

    private static void OnIsDragAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.MouseLeftButtonDown -= OnDragAreaMouseLeftButtonDown;
        if (e.NewValue is true)
            element.MouseLeftButtonDown += OnDragAreaMouseLeftButtonDown;
    }

    private static void OnDragAreaMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // 松开鼠标之后再调用 DragMove 会抛异常
        if (e.ButtonState != MouseButtonState.Pressed) return;

        Window.GetWindow((DependencyObject)sender)?.DragMove();
    }

    private static void OnIsCloseButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not System.Windows.Controls.Primitives.ButtonBase button) return;

        button.Click -= OnCloseButtonClick;
        if (e.NewValue is true)
            button.Click += OnCloseButtonClick;
    }

    private static void OnCloseButtonClick(object sender, RoutedEventArgs e)
        => Window.GetWindow((DependencyObject)sender)?.Close();
}
