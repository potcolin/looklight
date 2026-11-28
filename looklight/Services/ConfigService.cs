using System.IO;
using System.Text.Json;
using looklight.Services.Interfaces;

namespace looklight.Services;

public class AppConfig
{
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.deepseek.com/chat/completions";
    public string Model { get; set; } = "deepseek-flash";

    /// <summary>优先使用的发音源名字，对应 IPronunciationSource.Name；其余已注册的源作为降级备用。</summary>
    public string PronunciationSource { get; set; } = "youdao";
}

/// <summary>用户自己填写、只存在本机的内容，不随仓库发布。</summary>
public sealed class UserSettings
{
    public string ApiKey { get; set; } = "";

    /// <summary>登录 Windows 后是否自动启动，由设置面板的开关写入。</summary>
    public bool AutoStart { get; set; }
}

/// <summary>
/// Endpoint / Model 来自程序目录下随仓库发布的 config.json；
/// API Key 与开机自启动开关只存在用户目录的 settings.json，避免被提交。
/// </summary>
public sealed class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    // 始终保持这份实例，写 API Key / 自启动时整体落盘，避免互相覆盖
    private readonly UserSettings _user;

    public AppConfig Config { get; }
    public string SettingsPath { get; }

    public bool AutoStart => _user.AutoStart;

    public ConfigService()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "looklight",
            "settings.json");

        Config = Load(configPath);
        _user = ReadJson<UserSettings>(SettingsPath) ?? new UserSettings();
        if (!string.IsNullOrWhiteSpace(_user.ApiKey))
            Config.ApiKey = _user.ApiKey;
    }

    public void SaveApiKey(string apiKey)
    {
        Config.ApiKey = apiKey;
        _user.ApiKey = apiKey;
        WriteJson(SettingsPath, _user);
    }

    public void SaveAutoStart(bool enabled)
    {
        _user.AutoStart = enabled;
        WriteJson(SettingsPath, _user);
    }

    private AppConfig Load(string configPath)
    {
        var config = new AppConfig();

        // config.json 会随仓库发布，因此只取它的 Endpoint / Model / PronunciationSource；其中的 ApiKey 一律忽略，
        // 免得有人把真实密钥写回这个会被提交的文件。
        var shipped = ReadJson<AppConfig>(configPath);
        if (shipped is not null)
        {
            if (!string.IsNullOrWhiteSpace(shipped.Endpoint)) config.Endpoint = shipped.Endpoint;
            if (!string.IsNullOrWhiteSpace(shipped.Model)) config.Model = shipped.Model;
            if (!string.IsNullOrWhiteSpace(shipped.PronunciationSource)) config.PronunciationSource = shipped.PronunciationSource;
        }

        return config;
    }

    private static T? ReadJson<T>(string path) where T : class
    {
        try
        {
            if (File.Exists(path))
                return JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"读取配置失败（{path}）: {ex.Message}");
        }
        return null;
    }

    private static void WriteJson<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions));
    }
}
