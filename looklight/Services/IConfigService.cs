namespace looklight.Services;

public interface IConfigService
{
    AppConfig Config { get; }
    string ConfigPath { get; }
}
