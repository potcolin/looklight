namespace looklight.Services.Interfaces;

/// <summary>
/// 发音音频的来源。想换或加发音源时实现这个接口、在 App.xaml.cs 里注册，
/// 再把 config.json 的 PronunciationSource 填成它的 Name 即可优先生效；
/// 其余已注册的源会作为降级备用。
/// </summary>
public interface IPronunciationSource
{
    /// <summary>配置里选中用的名字，如 "youdao"。</summary>
    string Name { get; }

    /// <summary>
    /// 取回发音音频（mp3 字节）。没有该词的发音时抛异常，
    /// 由 PronunciationService 决定是否降级到下一个源。
    /// </summary>
    Task<byte[]> GetAudioAsync(string word, Accent accent, CancellationToken ct = default);
}
