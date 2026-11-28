using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using looklight.Services.Interfaces;

namespace looklight.Services;

/// <summary>
/// 发音编排：按 config.json 里 PronunciationSource 指定的源优先取音频，失败再降级到其他已注册的源。
/// 取回的音频缓存到 %APPDATA%\looklight\audio，重复查词和断网时都能直接播放。
/// </summary>
public sealed class PronunciationService : IPronunciationService
{
    /// <summary>缓存文件名上限；超长输入截断后追加哈希，避免不同长句抹掉前缀后撞名。</summary>
    private const int MaxCacheNameLength = 80;

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly string _cacheRoot;

    // 配置指定的源排在最前，其余作为降级备用；OrderBy 是稳定排序，同级别保持注册顺序
    private readonly IReadOnlyList<IPronunciationSource> _sources;

    // MediaPlayer 是 DispatcherObject：只能在创建它的线程（UI 线程）上使用
    private readonly MediaPlayer _player = new() { Volume = 1.0 };

    private CancellationTokenSource? _pending;

    public event Action<string>? PlaybackFailed;

    public PronunciationService(IEnumerable<IPronunciationSource> sources, IConfigService configService)
    {
        _cacheRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "looklight",
            "audio");

        var configured = configService.Config.PronunciationSource;
        _sources = sources
            .OrderBy(source => string.Equals(source.Name, configured, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        _player.MediaFailed += (_, e) =>
            PlaybackFailed?.Invoke(e.ErrorException?.Message ?? "系统无法播放这段音频");
    }

    public async Task PlayAsync(string text, Accent accent, CancellationToken ct = default)
    {
        var word = WhitespaceRegex.Replace(text?.Trim() ?? "", " ");
        if (word.Length == 0) throw new ArgumentException("没有可播放的内容。", nameof(text));

        var previous = _pending;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _pending = cts;
        previous?.Cancel();

        try
        {
            var file = GetCachePath(word, accent);
            if (!File.Exists(file))
                await SaveAsync(file, await FetchAsync(word, accent, cts.Token), cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            Play(file);
        }
        finally
        {
            if (ReferenceEquals(_pending, cts)) _pending = null;
            cts.Dispose();
        }
    }

    public void Stop()
    {
        _pending?.Cancel();
        _player.Stop();
    }

    /// <summary>依次问每个源，第一个拿到的就用；全都失败时把第一个源的消息原样抛出。</summary>
    private async Task<byte[]> FetchAsync(string word, Accent accent, CancellationToken ct)
    {
        var errors = new List<Exception>();
        foreach (var source in _sources)
        {
            try
            {
                return await source.GetAudioAsync(word, accent, ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0) throw errors[0];
        throw new InvalidOperationException("没有可用的发音源，请检查 config.json 的 PronunciationSource");
    }

    private static async Task SaveAsync(string file, byte[] bytes, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        // 先写临时文件再改名：下载中断时不会留下半个 mp3 被下次当成已缓存
        var temp = $"{file}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct);
        File.Move(temp, file, overwrite: true);
    }

    private void Play(string file)
    {
        // 重复点同一个词也要从头播，所以每次都先 Close 再 Open
        _player.Close();
        _player.Open(new Uri(file, UriKind.Absolute));
        _player.Play();
    }

    private string GetCachePath(string word, Accent accent)
    {
        var key = word.ToLowerInvariant();

        var name = new StringBuilder(key.Length);
        foreach (var ch in key)
            name.Append(Array.IndexOf(InvalidFileNameChars, ch) >= 0 ? '_' : ch);

        if (name.Length == 0)
            name.Append(ShortHash(key));
        else if (name.Length > MaxCacheNameLength)
            name.Length = MaxCacheNameLength;

        // 截断过就补哈希：长句的前缀经常一模一样
        if (name.Length != key.Length)
            name.Append('-').Append(ShortHash(key));

        return Path.Combine(_cacheRoot, accent == Accent.British ? "uk" : "us", name + ".mp3");
    }

    private static string ShortHash(string key)
        => Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(key)))[..8];
}
