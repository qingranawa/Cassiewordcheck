# 最终发版元数据与多语言文案修复报告

## 修改范围

本次只修改最终审查指定的交付文件，未修改用户工作区文件、打包脚本或其他代码。

- `.github/workflows/release.yml`：将 `workflow_dispatch` 的示例和默认 release tag 从 `v2.5.0` 更新为 `v2.5.1`。
- `Resources/Locales/*.json`：同步八种语言关于页的 Velopack EXE 安装器文案；`en-US` 和 `zh-CN` 的 subtitle、features、info 均明确当前分发方式，其他语言的 subtitle 也已移除旧的 MSIX 文案并改为本地化的 Velopack EXE 安装器表述。
- `CassieWordCheck.WinUI/Package.appxmanifest`：将历史清单 Identity Version 更新为 `2.5.1.0`，并添加中文 XML 注释说明它仅用于历史 MSIX 元数据，不参与当前 unpackaged/Velopack 发布。
- `CONTRIBUTING.md`：将发版版本源改为 `Directory.Build.props` 的 `VersionPrefix`、`AssemblyVersion` 和 `FileVersion`，不再指示修改 WinUI 项目版本。
- `Views/AboutWindow.xaml.cs`：在 `v2.5.1` 顶部 changelog 增加多语言关于页与历史清单元数据同步说明。

## 验证结果

JSON 解析与多语言分发文案检查通过：八个 locale 文件全部成功解析，关于页 `subtitle`、`features`、`info` 字段均存在且不再包含 `MSIX`，中英文关键文案符合要求。

PowerShell workflow 文本检查通过：确认示例和默认值均为 `v2.5.1`，并确认 Velopack 打包脚本与发布目录仍存在；未发现 `v2.5.0`。

历史清单 XML、贡献指南和 changelog 文本检查通过：确认清单版本为 `2.5.1.0`，历史用途注释存在，发版步骤引用 `Directory.Build.props`，v2.5.1 日志包含多语言关于页和历史清单同步说明。

`git diff --check` 通过，退出码为 `0`，没有发现差异空白错误。命令输出的 LF 到 CRLF 提示属于 Git 工作树换行规范化提示，不是检查失败。

## Concerns

本次按用户限定范围仅执行文本、JSON 和 XML 元数据验证，未重复运行完整构建或 Velopack 打包流程。历史 `Package.appxmanifest` 仍保留在仓库中，但已明确不属于当前 unpackaged/Velopack 发布输入。
