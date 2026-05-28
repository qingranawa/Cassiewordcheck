# C# / .NET 开发规范

你是我的 C# / .NET 开发搭档。技术基准：.NET 8，开启可为空引用类型和隐式 using。

## 命名约定
- 类/方法/属性：PascalCase（`GetUserById`）
- 接口：以 `I` 开头（`IRepository`）
- 私有字段：_camelCase（`_userRepository`）
- 常量：PascalCase（`MaxRetryCount`）
- 枚举：单数形式的 PascalCase（`UserRole`）

## 异步编程
- I/O 操作（数据库、文件、HTTP）一律使用 `async/await`
- 库代码中避免 `.Result` 或 `.Wait()`，可能引发死锁
- 使用 `ConfigureAwait(false)` 避免不必要的上下文切换
- 异步方法命名以 `Async` 结尾，并提供 CancellationToken 重载

## 错误处理
- 捕获特定异常类型，避免裸露的 `catch (Exception)`
- 使用 `throw` 而非 `throw ex` 保留堆栈信息
- 自定义异常继承 `Exception`，命名以 `Exception` 结尾
- 使用 `using` 或 `await using` 确保资源释放

## 依赖注入
- 构造函数注入为默认方式，避免服务定位器模式
- 生命周期选择：Transient（无状态）、Scoped（请求级别）、Singleton（全局）
- 使用 `IServiceCollection` 扩展方法注册服务
- 避免在 Singleton 中注入 Scoped 服务

## 代码风格
- 使用 C# 最新语言特性（记录类型、集合表达式、主构造函数）
- 使用 `var` 当类型明确时（`var user = new User()`）
- 成员访问修饰符明确：`public`/`private`/`protected`/`internal`
- 使用 `readonly` 标记不可变字段

## 文档与注释
- 公共 API 使用 XML 注释（`///`）
- 注释解释"为什么"而非"做什么"
- TODO 注释包含作者和日期：`// TODO(user, 2026-05-06): 描述`

## 项目配置
- 目标框架：.NET 8 或更高版本
- 使用 `Directory.Build.props` 统一项目配置
- 开启可为空引用类型（`<Nullable>enable</Nullable>`）
- 开启隐式 using（`<ImplicitUsings>enable</ImplicitUsings>`）

## 测试要求
- 单元测试使用 xUnit 或 NUnit
- 测试文件命名 `{ClassName}Tests.cs`
- 测试方法命名 `MethodName_Scenario_ExpectedResult`
- Mock 框架使用 Moq 或 NSubstitute

## 性能要点
- 字符串大量拼接使用 `StringBuilder`
- LINQ 避免在循环中多次枚举（使用 `.ToList()` 或 `.ToArray()`）
- 使用 `ArrayPool<T>` 减少大数组分配
- 热路径代码考虑 `Span<T>` 和 `Memory<T>`

## 日志与可观测性
- 使用 `ILogger<T>` 依赖注入
- 日志级别：Debug（开发）、Information（关键操作）、Warning（可恢复问题）、Error（异常）
- 使用结构化日志（`logger.LogInformation("User {UserId} logged in", userId)`）
- 禁止在生产代码中使用 `Console.WriteLine`

## 代码生成要求
- 代码块标注语言和路径（如 `\```csharp path=src/Services/UserService.cs\```）
- 多文件修改时先列出变更清单，再逐文件输出
- 需要 NuGet 包时先列出包名和版本（如 `// NuGet: Serilog 8.0`）
- 涉及依赖注入时标注生命周期（Transient/Scoped/Singleton）
