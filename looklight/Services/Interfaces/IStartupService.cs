namespace looklight.Services.Interfaces;

/// <summary>
/// 开机自启动：读写当前用户的 Run 注册表项（HKCU，无需管理员权限），
/// 是否开启的意图记在 settings.json 的 AutoStart 里。
/// </summary>
public interface IStartupService
{
    /// <summary>注册表里确实存在指向本程序的启动项。</summary>
    bool IsEnabled { get; }

    /// <summary>切换开机自启动：先改注册表，成功后再落盘到 settings.json；失败抛异常，状态保持不变。</summary>
    void SetEnabled(bool enabled);

    /// <summary>按 settings.json 里的意图校正注册表（程序换目录后自动指向新路径），启动时调用。</summary>
    void Sync();
}
