namespace looklight.Services;

public interface IDeepSeekClient
{
    Task<string> QueryAsync(string input, CancellationToken ct = default);
}
