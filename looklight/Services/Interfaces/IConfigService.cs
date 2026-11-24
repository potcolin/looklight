using looklight.Services;

namespace looklight.Services.Interfaces;

public interface IConfigService
{
    AppConfig Config { get; }

    /// <summary>API Key 的保存位置（%APPDATA%\looklight\settings.json），不在仓库里。</summary>
    string SettingsPath { get; }

    /// <summary>settings.json 里记录的开机自启动意图。</summary>
    bool AutoStart { get; }

    /// <summary>保存用户手动输入的 API Key，并立即作用于后续请求。</summary>
    void SaveApiKey(string apiKey);

    /// <summary>记录开机自启动开关（只写 settings.json，注册表由 IStartupService 负责）。</summary>
    void SaveAutoStart(bool enabled);
}
