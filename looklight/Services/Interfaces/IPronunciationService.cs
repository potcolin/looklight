namespace looklight.Services.Interfaces;

/// <summary>发音口音，对应发音源的两个音色。</summary>
public enum Accent
{
    /// <summary>英音。</summary>
    British,

    /// <summary>美音。</summary>
    American
}

public interface IPronunciationService
{
    /// <summary>
    /// 播放阶段的失败（MediaPlayer 打开音频出错是在 Open 之后异步回来的，
    /// 走事件而不是异常）。回调在 UI 线程上触发。
    /// </summary>
    event Action<string>? PlaybackFailed;

    /// <summary>
    /// 播放发音；本地缓存命中时不联网。再次调用会取消上一次尚未完成的下载，
    /// 避免两段音频前后脚一起响。
    /// </summary>
    Task PlayAsync(string text, Accent accent, CancellationToken ct = default);

    /// <summary>停止播放并取消未完成的下载。</summary>
    void Stop();
}
