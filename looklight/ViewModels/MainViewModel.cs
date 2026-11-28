using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using looklight.Services.Interfaces;
using Prism.Commands;
using Prism.Mvvm;

namespace looklight.ViewModels;

public class MainViewModel : BindableBase, INotifyDataErrorInfo
{
    /// <summary>纯中文输入判定（基本区 + 扩展 A 区，允许首尾空白）。</summary>
    private static readonly Regex ChineseOnlyRegex =
        new(@"^\s*[\u3400-\u4dbf\u4e00-\u9fff]+\s*$", RegexOptions.Compiled);

    private const string ApiKeyPageUrl = "https://platform.deepseek.com/api_keys";

    /// <summary>查词用不到这么长的输入，挡住误粘贴整篇文章。</summary>
    private const int MaxInputLength = 500;

    /// <summary>句子也能朗读，但太长的内容不适合整段念出来。</summary>
    private const int MaxSpeakLength = 200;

    private readonly IDeepSeekClient _client;
    private readonly IConfigService _configService;
    private readonly IStartupService _startupService;
    private readonly IPronunciationService _pronunciation;
    private readonly Dictionary<string, string> _errors = new();

    private string _inputText = "";
    private string _resultMarkdown = "";
    private string _statusText = "";
    private string _apiKeyInput = "";
    private bool _isBusy;
    private bool _isPreparingAudio;
    private bool _isSettingsOpen;
    private bool _isTopMost = true;
    private bool _isAutoStartEnabled;

    public string InputText
    {
        get => _inputText;
        set
        {
            if (!SetProperty(ref _inputText, value)) return;
            // 用户一动输入就把错误提示收掉，别一直红着
            SetValidationError(nameof(InputText), null);
        }
    }

    public string ResultMarkdown
    {
        get => _resultMarkdown;
        set
        {
            if (SetProperty(ref _resultMarkdown, value))
                RaisePropertyChanged(nameof(HasResult));
        }
    }

    /// <summary>结果区是否已有内容，视图用它决定要不要显示空状态提示。</summary>
    public bool HasResult => !string.IsNullOrWhiteSpace(ResultMarkdown);

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>设置面板里待保存的 API Key（保存成功后立即清空）。</summary>
    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set
        {
            if (!SetProperty(ref _apiKeyInput, value)) return;
            SetValidationError(nameof(ApiKeyInput), null);
        }
    }

    public bool IsSettingsOpen
    {
        get => _isSettingsOpen;
        set => SetProperty(ref _isSettingsOpen, value);
    }

    /// <summary>主窗口是否置顶显示（默认开启），由应用栏的图钉按钮切换。</summary>
    public bool IsTopMost
    {
        get => _isTopMost;
        set => SetProperty(ref _isTopMost, value);
    }

    /// <summary>
    /// 开机自启动开关：改动直接落到 HKCU 的 Run 注册表项，成功后再记进 settings.json。
    /// 写注册表失败时把开关拨回去，避免界面显示成已生效。
    /// </summary>
    public bool IsAutoStartEnabled
    {
        get => _isAutoStartEnabled;
        set
        {
            if (_isAutoStartEnabled == value) return;

            try
            {
                _startupService.SetEnabled(value);
                SetProperty(ref _isAutoStartEnabled, value);
                StatusText = value ? "已开启开机自启动" : "已关闭开机自启动";
            }
            catch (Exception ex)
            {
                RaisePropertyChanged();
                StatusText = $"设置开机自启动失败：{ex.Message}";
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RaisePropertyChanged(nameof(QueryButtonText));
                RaisePropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    public string QueryButtonText => IsBusy ? "查询中…" : "查询";

    public bool IsNotBusy => !IsBusy;

    /// <summary>正在下载/启动发音，期间先挡住连点。</summary>
    public bool IsPreparingAudio
    {
        get => _isPreparingAudio;
        private set => SetProperty(ref _isPreparingAudio, value);
    }

    public DelegateCommand QueryCommand { get; }
    public DelegateCommand SaveApiKeyCommand { get; }
    public DelegateCommand ToggleSettingsCommand { get; }
    public DelegateCommand ToggleTopMostCommand { get; }
    public DelegateCommand OpenApiKeyPageCommand { get; }
    public DelegateCommand PlayBritishCommand { get; }
    public DelegateCommand PlayAmericanCommand { get; }

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public bool HasErrors => _errors.Count > 0;

    public IEnumerable GetErrors(string? propertyName)
        => propertyName is not null && _errors.TryGetValue(propertyName, out var error)
            ? new[] { error }
            : Array.Empty<string>();

    /// <summary>设置/清除某个属性的校验错误；error 为 null 表示清除。</summary>
    private void SetValidationError(string propertyName, string? error)
    {
        var had = _errors.ContainsKey(propertyName);
        if (string.IsNullOrEmpty(error))
        {
            if (!had) return;
            _errors.Remove(propertyName);
        }
        else
        {
            if (had && _errors[propertyName] == error) return;
            _errors[propertyName] = error;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }

    public MainViewModel(IConfigService configService, IDeepSeekClient client, IStartupService startupService,
        IPronunciationService pronunciation)
    {
        _configService = configService;
        _client = client;
        _startupService = startupService;
        _pronunciation = pronunciation;

        // 以注册表为准：用户可能在外部删掉了启动项
        _isAutoStartEnabled = startupService.IsEnabled;

        QueryCommand = new DelegateCommand(
            async () => await QueryAsync(),
            () => !IsBusy)
            .ObservesProperty(() => IsBusy);

        PlayBritishCommand = new DelegateCommand(
            async () => await PlayAsync(Accent.British),
            CanPlay)
            .ObservesProperty(() => InputText)
            .ObservesProperty(() => IsPreparingAudio);

        PlayAmericanCommand = new DelegateCommand(
            async () => await PlayAsync(Accent.American),
            CanPlay)
            .ObservesProperty(() => InputText)
            .ObservesProperty(() => IsPreparingAudio);

        SaveApiKeyCommand = new DelegateCommand(SaveApiKey);
        ToggleSettingsCommand = new DelegateCommand(() => IsSettingsOpen = !IsSettingsOpen);
        ToggleTopMostCommand = new DelegateCommand(() => IsTopMost = !IsTopMost);
        OpenApiKeyPageCommand = new DelegateCommand(OpenApiKeyPage);

        // 音频开始播放后 MediaPlayer 才可能报解码错误，这里统一接到状态栏
        pronunciation.PlaybackFailed += message => StatusText = $"播放发音失败：{message}";

        // 没有 Key 时直接展开设置面板，省得用户对着"未配置"发呆却找不到入口
        IsSettingsOpen = string.IsNullOrWhiteSpace(configService.Config.ApiKey);
        StatusText = IsSettingsOpen ? "尚未配置 API Key，请在下方填写" : "就绪";
    }

    /// <summary>
    /// 只有英文内容才念：纯中文没有英文读音，超长内容也不适合整段朗读。
    /// </summary>
    private bool CanPlay()
    {
        if (IsPreparingAudio) return false;

        var text = InputText.Trim();
        return text.Length is > 0 and <= MaxSpeakLength && !ChineseOnlyRegex.IsMatch(text);
    }

    private async Task PlayAsync(Accent accent)
    {
        if (!CanPlay()) return;

        var text = InputText.Trim();
        var accentName = accent == Accent.British ? "英音" : "美音";

        IsPreparingAudio = true;
        StatusText = $"正在载入{accentName}…";

        try
        {
            await _pronunciation.PlayAsync(text, accent);
            StatusText = $"播放{accentName}";
        }
        catch (OperationCanceledException)
        {
            // 被下一次点击取消了，不算错误
        }
        catch (Exception ex)
        {
            StatusText = $"{accentName}播放失败：{ex.Message}";
        }
        finally
        {
            IsPreparingAudio = false;
        }
    }

    private void SaveApiKey()
    {
        var key = ApiKeyInput.Trim();
        if (key.Length == 0)
        {
            SetValidationError(nameof(ApiKeyInput), "请输入 API Key");
            StatusText = "请输入 API Key";
            return;
        }

        try
        {
            _configService.SaveApiKey(key);
            ApiKeyInput = "";
            IsSettingsOpen = false;
            StatusText = "API Key 已保存";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败：{ex.Message}";
        }
    }

    private void OpenApiKeyPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ApiKeyPageUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusText = $"无法打开浏览器：{ex.Message}";
        }
    }

    private async Task QueryAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(_configService.Config.ApiKey))
        {
            IsSettingsOpen = true;
            StatusText = "请先填写 API Key";
            return;
        }

        var input = InputText.Trim();
        if (input.Length == 0)
        {
            SetValidationError(nameof(InputText), "请输入要查询的内容");
            StatusText = "请输入要查询的内容";
            return;
        }

        if (input.Length > MaxInputLength)
        {
            SetValidationError(nameof(InputText), $"输入过长，请控制在 {MaxInputLength} 个字符以内");
            StatusText = "输入过长";
            return;
        }

        SetValidationError(nameof(InputText), null);

        IsBusy = true;
        StatusText = "正在查询…";
        ResultMarkdown = "";

        try
        {
            ResultMarkdown = await _client.QueryAsync(input, ChineseOnlyRegex.IsMatch(input));
            StatusText = "完成";
        }
        catch (Exception ex)
        {
            ResultMarkdown = $"查询失败：{ex.Message}";
            StatusText = "查询失败";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
