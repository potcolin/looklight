using System.Windows;
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
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
            => Attach(e.NewValue as MainViewModel);

        private void Attach(MainViewModel? viewModel)
        {
            if (ReferenceEquals(_viewModel, viewModel)) return;

            if (_viewModel is not null)
                _viewModel.QueryCompleted -= OnQueryCompleted;

            _viewModel = viewModel;

            if (_viewModel is not null)
                _viewModel.QueryCompleted += OnQueryCompleted;
        }

        // 查询结束后（成功或失败）把键盘焦点交还输入框，方便直接输入下一次查询
        private void OnQueryCompleted(object? sender, EventArgs e) => InputBox.Focus();
    }
}
