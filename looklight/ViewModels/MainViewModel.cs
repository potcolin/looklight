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

    private readonly IDeepSeekClient _client;
    private readonly IConfigService _configService;

    private string _inputText = "";
    private string _resultMarkdown = "";
    private string _statusText = "";
    private bool _isBusy;

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

        StatusText = string.IsNullOrEmpty(configService.Config.ApiKey)
            ? $"未配置 API Key，请编辑 {configService.ConfigPath}"
            : "就绪";
    }

    private async Task QueryAsync()
    {
        if (IsBusy) return;

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
