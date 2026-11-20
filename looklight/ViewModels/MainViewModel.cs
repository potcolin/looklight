using System.Diagnostics;
using System.Text.RegularExpressions;
using looklight.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace looklight.ViewModels;

public class MainViewModel : BindableBase
{
    /// <summary>纯中文输入判定（基本区 + 扩展 A 区，允许首尾空白）。</summary>
    private static readonly Regex ChineseOnlyRegex =
        new(@"^\s*[\u3400-\u4dbf\u4e00-\u9fff]+\s*$", RegexOptions.Compiled);

    private const string ApiKeyPageUrl = "https://platform.deepseek.com/api_keys";

    private readonly IDeepSeekClient _client;
    private readonly IConfigService _configService;

    private string _inputText = "";
    private string _resultMarkdown = "";
    private string _statusText = "";
    private string _apiKeyInput = "";
    private bool _isBusy;
    private bool _isSettingsOpen;

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value);
    }

    public string ResultMarkdown
    {
        get => _resultMarkdown;
        set => SetProperty(ref _resultMarkdown, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    /// <summary>设置面板里待保存的 API Key（保存成功后立即清空）。</summary>
    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set => SetProperty(ref _apiKeyInput, value);
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

    /// <summary>一次查询结束（无论成功或失败）后触发，视图借此把焦点交还输入框。</summary>
    public event EventHandler? QueryCompleted;

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
            StatusText = "请输入要查询的内容";
            return;
        }

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
            QueryCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
