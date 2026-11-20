using System.Windows;
using System.Windows.Controls;

namespace looklight.Behaviors;

/// <summary>
/// PasswordBox.Password 不是依赖属性，无法直接绑定，
/// 用附加属性把它双向接到 ViewModel 上，避免在 code-behind 里搬运密钥。
/// </summary>
public static class PasswordBoxBehavior
{
    public static readonly DependencyProperty BoundPasswordProperty = DependencyProperty.RegisterAttached(
        "BoundPassword", typeof(string), typeof(PasswordBoxBehavior),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnBoundPasswordChanged));

    /// <summary>程序内部改写 Password 时置位，避免回写 ViewModel 造成循环。</summary>
    private static readonly DependencyProperty UpdatingProperty = DependencyProperty.RegisterAttached(
        "Updating", typeof(bool), typeof(PasswordBoxBehavior), new PropertyMetadata(false));

    public static string GetBoundPassword(DependencyObject d) => (string?)d.GetValue(BoundPasswordProperty) ?? "";
    public static void SetBoundPassword(DependencyObject d, string value) => d.SetValue(BoundPasswordProperty, value);

    private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not PasswordBox box) return;

        box.PasswordChanged -= OnPasswordChanged;
        if (!(bool)box.GetValue(UpdatingProperty))
            box.Password = e.NewValue as string ?? "";
        box.PasswordChanged += OnPasswordChanged;
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var box = (PasswordBox)sender;
        box.SetValue(UpdatingProperty, true);
        SetBoundPassword(box, box.Password);
        box.SetValue(UpdatingProperty, false);
    }
}
