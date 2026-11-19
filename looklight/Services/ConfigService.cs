using System.IO;
using System.Text.Json;

namespace looklight.Services;

public class AppConfig
{
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "https://api.deepseek.com/chat/completions";
    public string Model { get; set; } = "deepseek-chat";
}

/// <summary>
/// 配置读取自程序目录下的 config.json（由编译复制到输出目录），缺失时自动生成模板。
/// </summary>
public sealed class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppConfig Config { get; }
    public string ConfigPath { get; }

    public ConfigService()
    {
        ConfigPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        Config = Load();
    }

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var cfg = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath));
                if (cfg is not null) return cfg;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"读取配置失败: {ex.Message}");
        }

        var fresh = new AppConfig();
        Save(fresh);
        return fresh;
    }

    private void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config, JsonOptions));
    }
}
