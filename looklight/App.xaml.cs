using System.Threading;
using System.Windows;
using looklight.Services;
using looklight.Services.Interfaces;
using looklight.Services.Sources;
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
        // 当前登录会话内的单实例互斥体（不带 Global\ 前缀，每个用户会话可各跑一份）
        private const string SingleInstanceMutexName = "looklight.SingleInstance";

        private Mutex? _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 没抢到互斥体说明已有实例在运行（含最小化到托盘的情况），直接静默退出
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // 程序可能被挪到别的目录，按设置把注册表里的启动项校正成当前路径
            Container.Resolve<IStartupService>().Sync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_singleInstanceMutex is not null)
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }

            base.OnExit(e);
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // 默认约定是 MainWindowViewModel，这里显式映射到 MainViewModel
            ViewModelLocationProvider.Register<MainWindow, MainViewModel>();

            containerRegistry.RegisterSingleton<IConfigService, ConfigService>();
            containerRegistry.RegisterSingleton<IDeepSeekClient, DeepSeekClient>();
            containerRegistry.RegisterSingleton<IHotkeyService, HotkeyService>();
            containerRegistry.RegisterSingleton<ITrayService, TrayService>();
            containerRegistry.RegisterSingleton<IStartupService, StartupService>();
            containerRegistry.RegisterSingleton<IPronunciationSource, YoudaoVoiceSource>();
            containerRegistry.RegisterSingleton<IPronunciationService, PronunciationService>();

            containerRegistry.Register<MainWindow>();
            containerRegistry.Register<MainViewModel>();
        }
    }
}
