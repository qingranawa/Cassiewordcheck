# Contributing

欢迎贡献代码、提交 Issue 或提出改进建议！

## 🍴 协作流程（Fork + Pull Request）

1. **Fork** 本仓库到你的 GitHub 账号
2. **Clone** 你的 Fork 到本地
3. 创建**功能分支**进行开发
4. 推送你的分支到你的 Fork
5. 提交 **Pull Request** 到本仓库的 `main` 分支
6. 审核通过后合并

```bash
# 1. Fork 后克隆你的版本
git clone https://github.com/你的用户名/Cassiewordcheck.git
cd CassieWordCheck

# 2. 添加原仓库为上游（可选，用于同步更新）
git remote add upstream https://github.com/qingranawa/Cassiewordcheck.git

# 3. 创建功能分支
git checkout -b feat/my-awesome-feature

# 4. 开发、提交、推送
git push origin feat/my-awesome-feature

# 5. 在 GitHub 上提交 Pull Request
```
## 📋 报告问题

提交 Issue 时请包含：

- 问题描述（发生了什么，期望什么）
- 复现步骤
- 截图或错误信息（如有）
- 环境信息（Windows 版本、屏幕缩放比例等）

## 💻 开发环境

| 工具 | 版本 |
|------|------|
| .NET SDK | 8.0+ |
| IDE | JetBrains Rider / Visual Studio 2022+ |
| 运行时 | Windows 10 build 19041 或更高版本（包括 Windows 11） |

### 快速开始

```bash
git clone https://github.com/qingranawa/Cassiewordcheck.git
cd CassieWordCheck
dotnet restore
dotnet build
```

### 本地一键打包

项目根目录的 `publish.bat` 会调用 `scripts/pack-velopack.ps1`，用于生成 WinUI 3 unpackaged 应用的 Velopack 安装产物。也可以直接调用打包脚本：

```powershell
# 直接调用 Velopack 打包脚本
pwsh -NoProfile -File scripts/pack-velopack.ps1 -Configuration Release -Version 2.5.1

# 或使用根目录批处理
publish.bat
```

执行流程：

1. 恢复项目依赖和本地 .NET 工具
2. 以 x64 发布带 Windows App SDK self-contained 运行时的 WinUI 3 unpackaged 应用
3. 使用 Velopack 生成安装器、发布 feed 和完整包；仅在存在上一版本时生成增量包

输出结构：

```
dist/velopack/Releases/
├── qingranawa.CassieWordCheck-win-Setup.exe
├── releases.win.json
└── qingranawa.CassieWordCheck-2.5.1-full.nupkg
```

其中安装器名称匹配 `*-Setup.exe`，上例为 `qingranawa.CassieWordCheck-win-Setup.exe`；完整包文件名会随 `-Version` 参数变化。首次发布必须包含安装器、`releases.win.json` feed 和 `*-full.nupkg`。只有存在上一版本可用于生成差分更新时，才会生成 `*-delta.nupkg`，该包不属于首次发布的必需产物。发布或提交变更前，请确认安装器、feed 和完整包均位于 `dist/velopack/Releases`；若存在上一版本，再确认预期的增量包也位于该目录。

## 🔧 代码规范

### 命名约定

| 类别 | 规范 | 示例 |
|------|------|------|
| 类/方法/属性 | PascalCase | `LoadWordListAsync()` |
| 接口 | `I` 前缀 | `IWordRepository` |
| 私有字段 | `_camelCase` | `_wordlist` |
| 常量 | PascalCase | `MaxRetryCount` |
| 本地变量 | camelCase | `wordCount` |

### 编码原则

- **异步优先**：I/O 操作（文件、数据库、HTTP）始终使用 `async/await`
- **依赖注入**：通过构造函数传递依赖，避免静态单例
- **明确异常**：捕获特定异常类型，避免裸露的 `catch (Exception)`
- **资源管理**：优先使用 `using` 声明
- **现代语法**：优先使用 C# 最新特性（记录类型、集合表达式、模式匹配）

### XAML 规范

- 组件命名：`PascalCase` + 类型后缀（`InputBox`、`ResultLabel`）
- 事件处理：`On` 前缀（`OnLanguageChanged`）
- 资源引用：优先使用 `StaticResource`，避免硬编码值
- 动画：使用 `DoubleAnimation` + `EasingFunction`，避免 Storyboard 过度嵌套

## 🔄 提交信息

格式参考 [Conventional Commits](https://www.conventionalcommits.org/)：

```
<type>: <简短描述>

[可选的详细描述]
```

### 类型

| 类型 | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `ui` | UI 样式/布局变更 |
| `perf` | 性能优化 |
| `refactor` | 代码重构（无功能变化） |
| `docs` | 文档 |
| `chore` | 构建/工具/依赖 |
| `locale` | 多语言翻译 |

### 示例

```
feat: 新增 CSV 单词导入支持
fix: 修复统计窗口崩溃
ui: 优化入场动画错峰播放
perf: 减少同时并发动画数
```

## 🚀 发版流程

1. 更新 `Directory.Build.props` 中的 `<VersionPrefix>`、`<AssemblyVersion>` 和 `<FileVersion>`
2. 更新 `Views/AboutWindow.xaml.cs` 中的更新日志 `ChangelogText`
3. 确认所有 locale JSON 文件已更新
4. 运行现有测试并完成代码审查
5. 提交代码并推送至 `main` 分支
6. 创建并推送符合 `vX.Y.Z` 格式的标签，触发 GitHub Actions 自动构建

```bash
git tag v2.5.1
git push origin v2.5.1
```

推送 `vX.Y.Z` 标签会触发 GitHub Actions 工作流。工作流会保留标签格式校验和 Core 测试，然后调用 `scripts/pack-velopack.ps1`，将 `dist/velopack/Releases` 下匹配 `*-Setup.exe` 的安装器、`releases.win.json`、完整包和已有的增量包全部上传到 GitHub Release。

## 📁 项目结构说明

- `Models/` — 纯数据模型和业务逻辑，不依赖桌面 UI
- `CassieWordCheck.WinUI/Views/Pages/` — WinUI 页面和交互逻辑
- `CassieWordCheck.WinUI/Services/` — WinUI 状态、文件选择、剪贴板和窗口服务
- `Resources/Services/` — Core 复用的无 UI 服务
- `Resources/Locales/` — 多语言 JSON，新增语言时添加文件即可
- `data/` — 运行时数据和静态资源

## ✅ Pull Request 检查清单

- [ ] 代码编译通过
- [ ] 遵循命名规范
- [ ] 新增功能已添加本地化翻译 key
- [ ] 已测试（如适用）
- [ ] Commit 信息清晰

---

感谢你的贡献！🌟
