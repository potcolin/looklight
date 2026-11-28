# looklight

有道词典实在太恶心了，自己写一个调用deepseek token的简单查询软件。
就实现一个功能：输入单词、词语、句子，使用固定询问模板，调用deepseek，界面显示结果；软件可通过alt+m快捷键唤醒和托盘到最小化。

## API Key

仓库里的 `config.json` 只放 `Endpoint`、`Model`、`PronunciationSource`，可以放心提交。
首次运行点状态栏的「设置 API Key」填自己的密钥，程序把它保存在
`%APPDATA%\looklight\settings.json`，不会进入仓库，也不会被重新构建覆盖。

## 单词发音

输入框右边两个按钮，「英」念英音、「美」念美音，快捷键 `Ctrl+1` / `Ctrl+2`。
音频取自有道词典的发音接口（`dict.youdao.com/dictvoice`，`type=1` 英音、`type=2` 美音），
不需要 API Key，也不消耗 DeepSeek 的 token；输入框内容直接朗读，不必先查询。

第一次点某个词时才下载，之后缓存在 `%APPDATA%\looklight\audio\uk|us\`，同一个词重复点、
断网都能直接播；想清缓存删掉这个目录即可。纯中文输入时按钮自动置灰（中文没有英文读音）。

发音源是可替换的：`Services/Sources/` 下每个 `IPronunciationSource` 实现对应一个源，
`config.json` 的 `PronunciationSource` 填它的 `Name`（目前只有 `youdao`）就优先用它，
其余已注册的源自动作为降级备用。想换源（比如 Edge TTS、Azure）只需新增一个实现并在
`App.xaml.cs` 注册，缓存、播放和界面都不用动。

## 开机自启动

设置面板（右上角齿轮）里的「开机自启动」开关即开即生效，开关状态记在
`%APPDATA%\looklight\settings.json` 的 `AutoStart` 字段里，也可以直接改这个文件：

```json
{ "ApiKey": "", "AutoStart": true }
```

实现方式是写当前用户的
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\looklight`，不需要管理员权限，
也不影响其他账户。程序每次启动会按 `AutoStart` 校正这个注册表值，所以把程序挪到
别的目录后，自启动会自动指向新路径；手动删掉了注册表项也会被补回来。

