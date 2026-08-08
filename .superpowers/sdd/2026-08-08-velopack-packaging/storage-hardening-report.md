# 存储迁移独立审查加固报告

## 修改范围

本次只修改 `Models/AppDataPaths.cs`、`CassieWordCheck.Core/Services/StorageMigrationService.cs`、`CassieWordCheck.Tests/AppDataPathsTests.cs`、`Views/AboutWindow.xaml.cs` 和本报告文件喵 未修改打包脚本、CI、其他文档或用户文件喵

## 加固内容

`AppDataPaths.GetLegacyMsixDataDirectories` 现在以每个旧 MSIX `LocalState/data` 目录中旧数据文件的最新修改时间降序选择候选喵 修改时间相同时继续使用不区分大小写路径和区分大小写路径作为稳定平局键喵 `GetLegacyDataDirectories` 仍将 `AppContext.BaseDirectory/data` 放在所有旧 MSIX 候选之前喵 非 `CassieWordCheck_*` 包目录继续忽略喵

`StorageMigrationService.MigrateFromDirectories` 现在保护目标目录创建喵 目标路径为文件或创建过程中发生 `IOException`、`UnauthorizedAccessException` 时返回两个迁移标记均为 `false` 的结果喵 既有目标文件不覆盖和源文件不删除规则保持不变喵

测试新增了较新旧 MSIX 数据优先、非匹配目录忽略、相同修改时间按路径稳定排序、安装目录优先和目标路径为文件时安全返回的覆盖喵 `v2.5.1` 更新日志同步补充旧 MSIX 多源选择与迁移失败保护说明喵

## TDD 验证

RED 阶段运行：

```text
dotnet test CassieWordCheck.Tests/CassieWordCheck.Tests.csproj -c Release --filter FullyQualifiedName~AppDataPaths
```

结果为退出码 1喵 新增的较新候选测试实际得到字典序旧候选喵 目标路径为文件测试实际抛出 `IOException` 喵 其余聚焦测试通过 6 个喵

GREEN 阶段运行同一聚焦命令喵 结果为退出码 0喵 通过 8 个、失败 0 个、跳过 0 个喵

完整 Core 测试运行：

```text
dotnet test CassieWordCheck.Tests/CassieWordCheck.Tests.csproj -c Release
```

结果为退出码 0喵 通过 136 个、失败 0 个、跳过 0 个喵

## 提交

提交消息为 `fix(storage): harden legacy migration` 喵 最终 SHA 以提交完成后 `git rev-parse HEAD` 输出为准喵

## Concerns

测试使用可重复的临时目录和文件时间喵 未执行真实 MSIX 升级安装流程或权限拒绝环境验证喵 本次未改动打包和 CI 路径喵
