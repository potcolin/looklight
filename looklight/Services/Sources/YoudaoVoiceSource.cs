using System.Net.Http;
using looklight.Services.Interfaces;

namespace looklight.Services.Sources;

/// <summary>
/// 有道词典的发音音频：type=1 英音、type=2 美音，返回 mp3。
/// 接口是非官方的，失效时换一个 IPronunciationSource 实现即可，调用方不受影响。
/// </summary>
public sealed class YoudaoVoiceSource : IPronunciationSource
{
    private const string VoiceUrl = "https://dict.youdao.com/dictvoice";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(10) };

    public string Name => "youdao";

    public async Task<byte[]> GetAudioAsync(string word, Accent accent, CancellationToken ct = default)
    {
        var url = $"{VoiceUrl}?audio={Uri.EscapeDataString(word)}&type={TypeOf(accent)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        // 不带 UA 时发音源偶尔会直接拒掉
        request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        using var resp = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(NotFoundMessage(word, accent));

        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length == 0)
            throw new InvalidOperationException(NotFoundMessage(word, accent));

        return bytes;
    }

    private static int TypeOf(Accent accent) => accent == Accent.British ? 1 : 2;

    private static string NotFoundMessage(string word, Accent accent)
        => $"没有找到「{word}」的{(accent == Accent.British ? "英" : "美")}音发音";
}
