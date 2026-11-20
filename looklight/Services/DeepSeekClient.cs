using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using looklight.Services.Interfaces;

namespace looklight.Services;

/// <summary>
/// 调用 DeepSeek chat completions 接口，使用固定询问模板。
/// </summary>
public sealed class DeepSeekClient : IDeepSeekClient
{
    /// <summary>固定询问模板。</summary>
    public const string PromptTemplate =
        "你是一个简洁的词典助手。用户会输入英文单词、词语或中英文句子。" +
        "如果输入是单词或短语，请按以下顺序回答：" +
        "一、音标与词性；" +
        "二、释义：按词性分组，并按不同含义逐条列出中文释义，" +
        "每条释义后面紧跟一条能体现该特定含义的英文例句及其中文翻译；" +
        "不同含义必须各配各自的例句，不要让多个义项共用一条例句，也不要出现例句与含义对不上的情况；" +
        "如果有多个词性，每个词性下的各个义项同样各配例句；" +
        "三、变形小节：用 Markdown 列表逐条列出对应的词形变化，只列该词性实际存在的项：" +
        "动词：第三人称单数、过去式、过去分词、现在分词（不规则变化在括号内注明 irregular）；" +
        "名词：复数（不可数则注明「无复数」）、所有格；" +
        "形容词与副词：比较级、最高级（无比较级则注明）；" +
        "代词与限定词：主格、宾格、形容词性物主代词、名词性物主代词、反身代词等适用项；" +
        "同一单词有多种词性时，按词性分组分别给出变形；" +
        "短语按中心词给出整条短语的变形，如动词短语给出各时态与第三人称单数形式。" +
        "如果输入是句子而不是单词或短语，请给出整句翻译和简要语法说明，不必给释义、例句和变形。" +
        "回答请使用 Markdown 格式，简洁直接，不要多余的开场白和客套话。";

    /// <summary>输入为纯中文时追加的要求：先给一行简单英文释义。</summary>
    public const string ChineseInputNote =
        "当用户输入是纯中文词语时，请在整个回答的最开头先用一行给出简单的英文释义，" +
        "格式为：**英文释义**：<对应的英文单词或短语>（<词性>）— <一句简短英文解释>，" +
        "然后再按上述格式继续回答。";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(60) };

    private readonly IConfigService _configService;

    public DeepSeekClient(IConfigService configService) => _configService = configService;

    public async Task<string> QueryAsync(string input, bool chineseOnly = false, CancellationToken ct = default)
    {
        var config = _configService.Config;
        if (string.IsNullOrWhiteSpace(config.ApiKey))
            throw new InvalidOperationException("尚未配置 API Key，请点击主窗口的「设置 API Key」填写。");

        var systemPrompt = chineseOnly
            ? PromptTemplate + "\n\n" + ChineseInputNote
            : PromptTemplate;

        var payload = new
        {
            model = config.Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = input }
            },
            temperature = 0.3,
            // 词典查询用不上推理：关掉默认开启的思考模式，省下思维链那部分输出 token，也快得多
            thinking = new { type = "disabled" }
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
