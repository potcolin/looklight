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

    private readonly IDeepSeekClient _client;
    private readonly IConfigService _configService;
    private readonly Dictionary<string, string> _errors = new();

    private string _inputText = "";
    private string _resultMarkdown = "";
    private string _statusText = "";
    private string _apiKeyInput = "";
    private bool _isBusy;
    private bool _isSettingsOpen;

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

    public DelegateCommand QueryCommand { get; }
    public DelegateCommand SaveApiKeyCommand { get; }
    public DelegateCommand ToggleSettingsCommand { get; }
    public DelegateCommand OpenApiKeyPageCommand { get; }

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

    public MainViewModel(IConfigService configService, IDeepSeekClient client)
    {
        _configService = configService;
        _client = client;

        QueryCommand = new DelegateCommand(
            async () => await QueryAsync(),
            () => !IsBusy)
            .ObservesProperty(() => IsBusy);

        SaveApiKeyCommand = new DelegateCommand(SaveApiKey);
        ToggleSettingsCommand = new DelegateCommand(() => IsSettingsOpen = !IsSettingsOpen);
        OpenApiKeyPageCommand = new DelegateCommand(OpenApiKeyPage);

        // 没有 Key 时直接展开设置面板，省得用户对着"未配置"发呆却找不到入口
        IsSettingsOpen = string.IsNullOrWhiteSpace(configService.Config.ApiKey);
        StatusText = IsSettingsOpen ? "尚未配置 API Key，请在下方填写" : "就绪";
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
