# Task 4 文档精度修复报告

## 修改文件

本次只修改以下三个文档文件，未修改代码或脚本：

- `README.md`：将 Velopack 安装器统一写为 `*-Setup.exe`，并保留当前实际示例 `qingranawa.CassieWordCheck-win-Setup.exe`；说明首次发布需要安装器、`releases.win.json` feed 和 `*-full.nupkg`，只有存在上一版本时才生成 `*-delta.nupkg`；将最低系统版本统一为 Windows 10 build 19041 或更高版本，并标注 `Package.appxmanifest` 仅为历史 MSIX 清单、不参与当前 Velopack 发布。
- `CONTRIBUTING.md`：同步安装器、full/feed/delta 产物条件和最低系统版本说明，保留中文本地安装与打包、GitHub Release、Core 测试和代码审查说明。
- `.superpowers/sdd/2026-08-08-velopack-packaging/docs-fix-report.md`：记录本次文档修复与验证结果。

## 提交

提交命令：

```powershell
git commit -m "docs(packaging): clarify velopack artifacts"
```

提交 SHA：提交完成后以 `git rev-parse HEAD` 的结果为准。

## 精确验证命令与结果

文档文本检查使用 `rg` 确认安装器采用通配符或实际文件名，确认 delta 包只在存在上一版本时生成，确认首次发布需要 full 包和 feed，确认最低系统版本与历史 MSIX 清单说明均存在，并确认未出现固定安装器名称或旧系统版本文字。结果：通过，退出码为 `0`。

```powershell
$docFiles = @(
    'README.md',
    'CONTRIBUTING.md',
    '.superpowers/sdd/2026-08-08-velopack-packaging/docs-fix-report.md'
)
$fixedSetup = & rg -n -P '(?<![\w*`-])Setup\.exe(?![\w-])' @docFiles
if ($LASTEXITCODE -eq 0) { throw "发现固定安装器名称：$fixedSetup" }
$oldSystem = & rg -n -P 'Windows 10 (1809|22H2)' @docFiles
if ($LASTEXITCODE -eq 0) { throw "发现旧系统版本：$oldSystem" }
$required = @(
    '\*-Setup\.exe',
    'qingranawa\.CassieWordCheck-win-Setup\.exe',
    '只有存在上一版本',
    '首次发布必须',
    'releases\.win\.json',
    'Windows 10 build 19041',
    '历史 MSIX 清单，不参与当前 Velopack 发布'
)
foreach ($pattern in $required) {
    & rg -n -P -- $pattern @docFiles
    if ($LASTEXITCODE -ne 0) { throw "缺少文档契约：$pattern" }
}
Write-Output 'PASS: documentation text check'
```

结果：`PASS: documentation text check`，退出码为 `0`。

差异空白检查命令：

```powershell
git diff --check
```

结果：退出码为 `0`，没有发现空白错误。

## Concerns

本次只做文档精度修复，未执行代码构建、测试或 Velopack 打包；现有 GitHub Release 与 Core 测试说明保留不变。工作树中其他代理或用户的改动未回滚、未修改，也未纳入本次提交。
