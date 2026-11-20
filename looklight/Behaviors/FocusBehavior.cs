using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace looklight.Behaviors;

/// <summary>
/// 焦点行为：由视图自己决定什么时候把焦点交给哪个控件，
/// ViewModel 不需要为了聚焦触发事件让 code-behind 回来操作控件。
/// </summary>
public static class FocusBehavior
{
    /// <summary>元素一旦可见就获得焦点（用于首次运行自动展开的 API Key 输入框）。</summary>
    public static readonly DependencyProperty AutoFocusProperty = DependencyProperty.RegisterAttached(
        "AutoFocus", typeof(bool), typeof(FocusBehavior), new PropertyMetadata(false, OnAutoFocusChanged));

    /// <summary>绑定 ViewModel 的忙碌状态：由忙碌变回空闲（查询结束）时把焦点交还该元素。</summary>
    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.RegisterAttached(
        "IsBusy", typeof(bool), typeof(FocusBehavior), new PropertyMetadata(false, OnIsBusyChanged));

    /// <summary>绑定校验出错的元素：校验错误产生时把焦点交还它。</summary>
    public static readonly DependencyProperty RefocusOnValidationErrorProperty = DependencyProperty.RegisterAttached(
        "RefocusOnValidationError", typeof(bool), typeof(FocusBehavior),
        new PropertyMetadata(false, OnRefocusOnValidationErrorChanged));

    public static bool GetAutoFocus(DependencyObject d) => (bool)d.GetValue(AutoFocusProperty);
    public static void SetAutoFocus(DependencyObject d, bool value) => d.SetValue(AutoFocusProperty, value);

    public static bool GetIsBusy(DependencyObject d) => (bool)d.GetValue(IsBusyProperty);
    public static void SetIsBusy(DependencyObject d, bool value) => d.SetValue(IsBusyProperty, value);

    public static bool GetRefocusOnValidationError(DependencyObject d) => (bool)d.GetValue(RefocusOnValidationErrorProperty);
    public static void SetRefocusOnValidationError(DependencyObject d, bool value) => d.SetValue(RefocusOnValidationErrorProperty, value);

    private static void OnAutoFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        element.IsVisibleChanged -= OnAutoFocusVisibilityChanged;
        if (e.NewValue is not true) return;

        element.IsVisibleChanged += OnAutoFocusVisibilityChanged;
        // 元素已经可见时立即生效；还没加载完的话，等它变成可见时由 IsVisibleChanged 兜底
        element.Focus();
    }

    private static void OnAutoFocusVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && sender is UIElement element)
            element.Focus();
    }

    private static void OnIsBusyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element || e.OldValue is not true || e.NewValue is not false) return;

        // 忙碌结束时 IsEnabled 还挂在同一批通知里恢复，等绑定都刷新完再抢焦点，否则会被禁用的控件吞掉
        element.Dispatcher.InvokeAsync(element.Focus, DispatcherPriority.Input);
    }

    private static void OnRefocusOnValidationErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element) return;

        Validation.RemoveErrorHandler(element, OnValidationError);
        if (e.NewValue is true)
            Validation.AddErrorHandler(element, OnValidationError);
    }

    private static void OnValidationError(object? sender, ValidationErrorEventArgs e)
    {
        // 只在新错误出现时抢焦点，错误被清掉时不要把光标从别处拽走
        if (e.Action != ValidationErrorEventAction.Added) return;

        if (sender is UIElement element)
            element.Focus();
    }
}
