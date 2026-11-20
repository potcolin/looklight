namespace looklight.Services;

public interface IConfigService
{
    AppConfig Config { get; }

    /// <summary>API Key 的保存位置（%APPDATA%\looklight\settings.json），不在仓库里。</summary>
    string SettingsPath { get; }

    /// <summary>保存用户手动输入的 API Key，并立即作用于后续请求。</summary>
    void SaveApiKey(string apiKey);
}
