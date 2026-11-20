using System.IO;
using System.Text.Json;
using looklight.Services.Interfaces;

namespace looklight.Services;

public class AppConfig
{
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.deepseek.com/chat/completions";
    public string Model { get; set; } = "deepseek-flash";
}

/// <summary>用户自己填写、只存在本机的内容，不随仓库发布。</summary>
public sealed class UserSettings
{
    public string ApiKey { get; set; } = "";
}

/// <summary>
/// Endpoint / Model 来自程序目录下随仓库发布的 config.json；
/// API Key 只从用户目录的 settings.json 读取，避免真实密钥被提交。
/// </summary>
public sealed class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppConfig Config { get; }
    public string SettingsPath { get; }

    public ConfigService()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "looklight",
            "settings.json");
        Config = Load(configPath);
    }

    public void SaveApiKey(string apiKey)
    {
        Config.ApiKey = apiKey;
        WriteJson(SettingsPath, new UserSettings { ApiKey = apiKey });
    }

    private AppConfig Load(string configPath)
    {
        var config = new AppConfig();

        // config.json 会随仓库发布，因此只取它的 Endpoint / Model；其中的 ApiKey 一律忽略，
        // 免得有人把真实密钥写回这个会被提交的文件。
        var shipped = ReadJson<AppConfig>(configPath);
        if (shipped is not null)
        {
            if (!string.IsNullOrWhiteSpace(shipped.Endpoint)) config.Endpoint = shipped.Endpoint;
            if (!string.IsNullOrWhiteSpace(shipped.Model)) config.Model = shipped.Model;
        }

        var user = ReadJson<UserSettings>(SettingsPath);
        if (user is not null && !string.IsNullOrWhiteSpace(user.ApiKey))
            config.ApiKey = user.ApiKey;

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
