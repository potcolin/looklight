# looklight

有道词典实在太恶心了，自己写一个调用deepseek token的简单查询软件。
就实现一个功能：输入单词、词语、句子，使用固定询问模板，调用deepseek，界面显示结果；软件可通过alt+m快捷键唤醒和托盘到最小化。

## API Key

仓库里的 `config.json` 只放 `Endpoint` 和 `Model`，可以放心提交。
首次运行点状态栏的「设置 API Key」填自己的密钥，程序把它保存在
`%APPDATA%\looklight\settings.json`，不会进入仓库，也不会被重新构建覆盖。
