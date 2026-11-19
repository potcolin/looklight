namespace looklight.Services;

public interface IDeepSeekClient
{
    /// <param name="input">用户查询内容。</param>
    /// <param name="chineseOnly">输入是否为纯中文，是则要求先给出简单英文释义。</param>
    Task<string> QueryAsync(string input, bool chineseOnly = false, CancellationToken ct = default);
}
