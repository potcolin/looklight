using System.Windows;
using System.Windows.Input;
using looklight.ViewModels;

namespace looklight.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Closed += (_, _) => Attach(null);
            // Prism 在 InitializeComponent 期间就注入了 ViewModel，DataContextChanged 不会再触发，这里补一次
            Attach(DataContext as MainViewModel);

            // 首次运行没有 Key 时设置面板会自动展开，直接把焦点放到密钥框
            Loaded += (_, _) =>
            {
                if (ApiKeyBox.IsVisible) ApiKeyBox.Focus();
            };
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
            => Attach(e.NewValue as MainViewModel);

        private void Attach(MainViewModel? viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel)) return;

            if (_viewModel is not null)
            {
                _viewModel.QueryCompleted -= OnQueryCompleted;
                _viewModel.InputFocusRequested -= OnInputFocusRequested;
            }

            _viewModel = viewModel;

            if (_viewModel is not null)
            {
                _viewModel.QueryCompleted += OnQueryCompleted;
                _viewModel.InputFocusRequested += OnInputFocusRequested;
            }
        }

        // 查询结束后（成功或失败）把键盘焦点交还输入框，方便直接输入下一次查询
        private void OnQueryCompleted(object? sender, EventArgs e) => InputBox.Focus();

        // 输入没通过校验时也把光标送回输入框，让人能直接改
        private void OnInputFocusRequested(object? sender, EventArgs e) => InputBox.Focus();

        // 窗口没有系统标题栏，应用栏的空白处按住即可拖动
        private void OnAppBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState != MouseButtonState.Pressed) return;
            DragMove();
        }

        // 应用栏的关闭按钮：和原来的标题栏 X 走同一条路径，ShellBehavior 会把它变成最小化到托盘
        private void OnCloseButtonClick(object sender, RoutedEventArgs e) => Close();
    }
}
