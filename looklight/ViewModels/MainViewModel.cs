using looklight.Services;
using Prism.Commands;
using Prism.Mvvm;

namespace looklight.ViewModels;

public class MainViewModel : BindableBase
{
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
                RaisePropertyChanged(nameof(QueryButtonText));
        }
    }

    public string QueryButtonText => IsBusy ? "查询中…" : "查询";

    public DelegateCommand QueryCommand { get; }

    public MainViewModel(IConfigService configService, IDeepSeekClient client)
    {
        _configService = configService;
        _client = client;

        QueryCommand = new DelegateCommand(
            async () => await QueryAsync(),
            () => !IsBusy);

        StatusText = string.IsNullOrEmpty(configService.Config.ApiKey)
            ? $"未配置 API Key，请编辑 {configService.ConfigPath}"
            : "就绪";
    }

    private async Task QueryAsync()
    {
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
            ResultMarkdown = await _client.QueryAsync(input);
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
