# CASSIE CWC Tool

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203-0078D4)](https://learn.microsoft.com/windows/apps/winui/)
[![Version](https://img.shields.io/github/v/release/qingranawa/Cassiewordcheck)](https://github.com/qingranawa/Cassiewordcheck/releases)
[![Downloads](https://img.shields.io/github/downloads/qingranawa/Cassiewordcheck/total)](https://github.com/qingranawa/Cassiewordcheck/releases)
[![Stars](https://img.shields.io/github/stars/qingranawa/Cassiewordcheck)](https://github.com/qingranawa/Cassiewordcheck)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

**CASSIE Word Check** — SCP: Secret Laboratory 广播文本检查工具，逐词比对 CASSIE 配音词库，标记可用/不可用单词，避免游戏内「播音事故」。

> SCP:SL 中的 CASSIE 系统是一个文本转语音（TTS）合成器，用于播放游戏内广播。但并非所有英文单词都能被 CASSIE 正确朗读——本工具帮助你快速检查广播文本中的单词是否在 CASSIE 词库中，避免「播音事故」。

---

## ✨ 功能一览

| 功能 | 说明 |
|------|------|
| ✅ **词库检查** | 逐词比对 CASSIE 配音词库，绿色=可用，红色=不可用 |
| 📊 **实时统计** | 可用数、不可用数、忽略数、覆盖率一目了然，进度条可视化 |
| 💡 **拼写建议** | 对不可用词自动 Levenshtein 相似词推荐 |
| 📋 **白名单管理** | 添加自定义豁免词，支持导入/导出 |
| 🌐 **多语言界面** | 简体中文 / English / 日本語 / 한국어 / Deutsch / Русский / Français |
| 🔍 **格式过滤** | 自动忽略 link、color、size、split 等 CASSIE 格式标记 |
| 🏷️ **命名过滤** | 屏蔽 MTF/UIU/GOC 等阵营缩写及北约代号、希腊字母 |
| 🌙 **暗色主题** | Mica 毛玻璃效果，全组件平滑动画 |
| 📤 **结果导出** | 检查结果可保存为 `.txt` 文件 |
| ↩️ **撤销重做** | Ctrl+Z / Ctrl+Y，支持清空/粘贴/加载文件 |
| 📂 **批量检查** | 打开多个文本文件，自动合并检查 |
| 🕐 **检查历史** | 最近 50 次检查记录，点击即可恢复文本 |
| 📈 **统计面板** | 折线图展示覆盖率 / 不可用词历史趋势 |
| 🔄 **自动更新** | 启动时自动检查 GitHub 新版本 |
| 🚫 **单实例** | 防止多开，避免数据冲突 |
| 🧰 **词库管理** | 排除列表、差异对比和差异报告导出 |
| 📦 **MSIX 发布** | WinUI 单项目 MSIX，支持 x64 安装包生成 |

## 🖼️ 截图

> ⏳ 截图待补充 — 欢迎贡献运行截图，建议包含：主界面检查效果、统计面板、设置页面

## 📦 安装

### 方法一：下载 Release（推荐）

前往 [Releases 页面](https://github.com/qingranawa/Cassiewordcheck/releases) 下载最新的 `CassieWordCheck*.msix`，按 Windows 提示安装即可。开发包默认未签名，正式发行时应替换为受信任的签名证书。

### 方法二：自行构建

```bash
# 克隆仓库
git clone https://github.com/qingranawa/Cassiewordcheck.git
cd CassieWordCheck

# 恢复依赖
dotnet restore CassieWordCheck.WinUI/CassieWordCheck.WinUI.csproj

# 构建 WinUI 3 应用
dotnet build CassieWordCheck.WinUI/CassieWordCheck.WinUI.csproj -c Release -p:Platform=x64

# 生成 MSIX（输出到 CassieWordCheck.WinUI/bin/Release/AppPackages/）
dotnet build CassieWordCheck.WinUI/CassieWordCheck.WinUI.csproj -c Release -p:Platform=x64 -p:PublishProfile=Properties/PublishProfiles/win10-x64.pubxml -p:GenerateAppxPackageOnBuild=true
```

也可以运行项目根目录的 `publish.bat`。

## 🎮 使用方式

1. 在输入框中粘贴或输入你的 CASSIE 广播文本
2. 右侧结果卡实时显示检查结果：
   - <span style="color:#10B981">**绿色**</span> — 单词在词库中，CASSIE 可读出
   - <span style="color:#EF4444">**红色 + 下划线**</span> — 单词不在词库中
   - <span style="color:#6B7280">**灰色**</span> — 已被过滤（格式标记/中文等）
3. 点击不可用词下方的建议面板查看相似词推荐
4. 点击顶部工具栏可管理白名单、调整设置、查看统计

## 🏗️ 项目架构

```
CassieWordCheck/
├── CassieWordCheck.sln       解决方案
├── CassieWordCheck.Core/     无 UI 的模型、检查引擎和数据服务
├── CassieWordCheck.WinUI/    WinUI 3 + Windows App SDK 应用与单项目 MSIX
│   ├── App.xaml              应用入口与全局资源
│   ├── MainWindow.xaml       Fluent NavigationView 主窗口
│   ├── Views/Pages/          检查、历史、统计、词库、设置和关于页面
│   ├── Assets/               MSIX 图标与启动图资源
│   └── Package.appxmanifest  MSIX 包清单
├── CassieWordCheck.Tests/    核心逻辑与迁移回归测试
│
├── Models/                   数据模型 & 核心逻辑
│   ├── Checker.cs           检查引擎（分词/过滤/统计）
│   ├── CheckResult.cs       检查结果 + 状态枚举
│   ├── WordList.cs          词库加载/查询（FrozenSet）
│   ├── HistoryStore.cs      检查历史持久化
│   └── Settings.cs          设置读写（JSON）
│
├── Resources/
│   ├── Locales/             多语言翻译 JSON（8 种语言）
│   └── Services/            Core 复用的无 UI 服务
│
├── data/                    运行时数据
│   ├── cassie-text.txt      CASSIE 配音词库
│   ├── AAA.ico              应用图标
│   ├── AAA.JPG              工具栏图标
│   └── qr.JPG               头像
│
├── publish.bat              一键生成 WinUI MSIX 脚本
├── .github/workflows/       GitHub Actions 自动发布
└── CassieWordCheck.WinUI/bin/  构建与 MSIX 输出
```

## 🔧 技术栈

- **.NET 8** + **WinUI 3** + **Windows App SDK 1.8**
- **单项目 MSIX** — 使用 Windows SDK Build Tools 生成 x64 安装包
- **CommunityToolkit.Mvvm** — MVVM 辅助
- **ClosedXML** — Excel (.xlsx) 导入支持
- **System.Text.Json** — JSON 序列化
- **GitHub API** — 自动更新检查
- **GitHub Actions** — CI/CD 自动构建

## 📋 系统要求

- Windows 10 22H2（最低构建版本 19041）或 Windows 11
- MSIX 安装时由包管理器处理 Windows App SDK 依赖

## 🤝 参与贡献

欢迎提交 Issue 和 Pull Request！详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## 📄 许可证

本项目基于 [MIT 许可证](LICENSE) 开源。

本项目采用 **CC BY-SA 3.0** 协议引用 SCP 基金会相关内容。

## 🙏 鸣谢

- **Awni**
- **虚无**
