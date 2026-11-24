using System.Diagnostics;
using looklight.Services.Interfaces;
using Microsoft.Win32;

namespace looklight.Services;

/// <summary>
/// 用 HKCU\Software\Microsoft\Windows\CurrentVersion\Run 实现开机自启动。
/// 选当前用户而不是 HKLM，是为了不需要管理员权限，也不影响其他账户。
/// </summary>
public sealed class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "looklight";

    private readonly IConfigService _configService;

    public StartupService(IConfigService configService) => _configService = configService;

    public bool IsEnabled => ReadRegisteredCommand() is not null;

    public void SetEnabled(bool enabled)
    {
        if (enabled)
            WriteRegisteredCommand();
        else
            DeleteRegisteredCommand();

        _configService.SaveAutoStart(enabled);
    }

    public void Sync()
    {
        try
        {
            if (!_configService.AutoStart)
            {
                DeleteRegisteredCommand();
                return;
            }

            // 结果与期望不一致才写，避免每次启动都动注册表
            if (ReadRegisteredCommand() != BuildCommand())
                WriteRegisteredCommand();
        }
        catch (Exception ex)
        {
            // 自启动拉不起来不该阻止程序本身运行，记一笔就继续
            Console.Error.WriteLine($"校正开机自启动失败: {ex.Message}");
        }
    }

    private static string BuildCommand()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
            exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

        return $"\"{exePath}\"";
    }

    private static string? ReadRegisteredCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(ValueName) as string;
    }

    private static void WriteRegisteredCommand()
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
            ?? throw new InvalidOperationException("无法打开注册表启动项");
        key.SetValue(ValueName, BuildCommand(), RegistryValueKind.String);
    }

    private static void DeleteRegisteredCommand()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
        if (key?.GetValue(ValueName) is null) return;
        key.DeleteValue(ValueName, false);
    }
}
