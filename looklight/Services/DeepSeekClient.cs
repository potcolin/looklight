using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace looklight.Services;

/// <summary>
/// 调用 DeepSeek chat completions 接口，使用固定询问模板。
/// </summary>
public sealed class DeepSeekClient : IDeepSeekClient
{
    /// <summary>固定询问模板。</summary>
    public const string PromptTemplate =
        "你是一个简洁的词典助手。用户会输入英文单词、词语或中英文句子，" +
        "请给出该输入的中文释义、音标（如有）、词性（如有）、简明例句及翻译。" +
        "如果是句子，请给出整句翻译和简要语法说明。" +
        "回答请使用 Markdown 格式，简洁直接，不要多余的开场白和客套话。";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly IConfigService _configService;

    public DeepSeekClient(IConfigService configService) => _configService = configService;

    public async Task<string> QueryAsync(string input, CancellationToken ct = default)
    {
        var config = _configService.Config;
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException(
                $"尚未配置 API Key，请编辑配置文件：{_configService.ConfigPath}");

        var payload = new
        {
            model = config.Model,
            messages = new object[]
            {
                new { role = "system", content = PromptTemplate },
                new { role = "user", content = input }
            },
            temperature = 0.3
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, config.Endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        using var resp = await Http.SendAsync(request, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"DeepSeek 请求失败（HTTP {(int)resp.StatusCode}）：{body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }
}
