using System.Windows;
using looklight.Services;
using looklight.ViewModels;
using looklight.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Mvvm;

namespace looklight
{
    /// <summary>
    /// Prism 启动类：容器注册 + 创建 Shell。
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 默认约定是 MainWindowViewModel，这里显式映射到 MainViewModel
            ViewModelLocationProvider.Register<MainWindow, MainViewModel>();

            containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
            containerRegistry.RegisterSingleton<IDeepSeekClient, DeepSeekClient>();
            containerRegistry.RegisterSingleton<IHotkeyService, HotkeyService>();
            containerRegistry.RegisterSingleton<ITrayService, TrayService>();

            containerRegistry.Register<MainWindow>();
            containerRegistry.Register<MainViewModel>();
        }
    }
}
