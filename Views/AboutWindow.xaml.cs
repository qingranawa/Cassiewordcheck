using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class AboutWindow : Window
{
    private const string FeaturesText = @"
# 项目功能

### 核心功能

- **CASSIE 词库检查** — 逐词比对 CASSIE 配音词库，标记可用/不可用单词
- **实时统计** — 可用数、不可用数、忽略数、覆盖率一目了然
- **拼写建议** — 对不可用词自动 `Levenshtein` 相似词推荐
- **白名单管理** — 添加自定义豁免词，支持导入/导出
- **多语言界面** — 简体中文 / English / 日本語 / 한국어 / Deutsch / Русский / Français

### 过滤系统

- **格式标记过滤** — 自动忽略 `link`、`color`、`size`、`split` 等 CASSIE 标签及标点符号
- **命名标记过滤** — 可屏蔽 `MTF/UIU/GOC/CI/NTF/GRU/FBI` 等阵营缩写及希腊字母、北约代号
- **中文忽略** — 可开关，跳过中文字符

### 体验优化

- **暗色深色主题** + Mica 毛玻璃效果（Windows 11）
- **全组件平滑动画**（卡片弹入/按钮缩放/进度条过渡/逐字清空）
- **实时键入响应** — 输入即检查，结果框同步动画反馈
- **单文件自包含发布** — 无需安装 .NET 运行时

---
";

    private static readonly string AboutInfoText = $@"
# CASSIE CWC Tool
**CASSIE 单词检查器**

![avatar](qr.JPG) #开发者信息:\n**清然 (2816401189)**\n紧急事件制作组研发\n~喵喵喵~

---

### 版本信息

- **版本**：{AppInfo.Version}
- **框架**：.NET 8 (WPF)
- **仓库**：[github.com/qingranawa/Cassiewordcheck](https://github.com/qingranawa/Cassiewordcheck)

---

### 鸣谢

Awni、虚无

---
";

    private const string DisclaimerText = @"
# 声明

## 开源协议

本项目采用 **CC BY-SA 3.0** 协议开源，您可以自由使用、修改和分享，但需保留原作者署名。

## 第三方内容

- **世界观**：基于 **SCP 基金会**世界观，内容遵循 [SCP 维基](https://scp-wiki-cn.wikidot.com/) 的 CC BY-SA 3.0 协议
- **适用游戏**：本工具用于 **SCP: Secret Laboratory** 游戏配音检查，游戏官网：[scpslgame.com](https://en.scpslgame.com/)
- **CASSIE 系统**：CASSIE 是 SCP:SL 游戏内的语音合成系统，本工具与其无官方关联

## 免责声明

本工具为**第三方社区开发**，与 SCP:SL 官方开发团队 Northwood Studios 无直接关联。所有数据仅供参考，使用风险请自行承担。

---

*如果本工具对你有帮助，请给项目点个 Star ✨*
";

    private const string ChangelogText = @"
# 更新日志

## `v2.4.3`（2026-07-09）— <> 标签全面忽略 · Inno Setup 6 安装包 · CI/CD 自动发行
### 功能调整
- **`<>` 标签全面忽略** — 不再只忽略 `<split>`、`<size=`、`<color=` 三种，所有 `<...>` 内的中文/内容全部自动跳过，包括废弃的 `<link="">` 格式

### 其他
- **Inno Setup 6 安装包** — 改用专业安装程序替代 zip 发布，含桌面快捷方式/开始菜单/自动管理员提权
- **CI/CD 自动发行** — 推送 tag 自动编译 → 打包 → 上传 setup.exe 到 GitHub Releases
- **程序集版本同步** — Version / FileVersion 统一随 tag 更新

## `v2.4.2`（2026-07-09）— 图标加载修复 · 设置页 UI 统一 · 清理报错语气词

### Bug 修复
- **修复工具栏/关于页图标（AAA.JPG）永远加载失败** — `using (var stream) bitmap.StreamSource = stream;` 缺少大括号导致流在 `EndInit()` 前被释放，JPG 加载静默失败
- **去除所有弹窗报错信息中的「喵～」** — 保持专业感，App.xaml.cs 两处 + MainWindow.xaml.cs 两处

### 界面调整
- **设置页下拉框对齐主页功能区风格** — LanguageCombo / FontSizeCombo 固定 Height=32、设置 Width 限制，与工具栏 ModeCombo 保持视觉一致

## `v2.4.1`（2026-07-09）— 字体修复 · 图标后备 · 移除开发者卡片

### Bug 修复
- **修复中文字体渲染大小不一** — 全局默认字体改为 `Microsoft YaHei` 优先（微软雅汉），统一所有控件 FontSize 基准为 13，消除中英文混排时字号参差
- **修复工具栏/关于页图标渲染** — `Image.Clip` 显式设置 `RectangleGeometry` 裁剪圆角（WPF 的 `ClipToBounds` 只裁矩形不裁圆角），28px 图标容器内 26px 图片+5px 圆角裁剪；后备显示玫瑰色 Ellipse 圆点而非 emoji
- **移除关于页中不合理的开发者卡片** — 删除右侧「清然·2816401189」信息卡片，头部改为双列布局自动居中

## `v2.4.0`（2026-07-09）— 全面视觉精打细磨 · 发布目录优化

### 视觉升级
- **新增卡片式标签页** — 关于窗口标签从底部线指示改为卡片式激活，选中态带光晕阴影
- **Double-Bezel 嵌套卡片结构** — 所有窗口卡片采用双层嵌套（外层毛边 + 内层主体），创造物理纵深
- **全新的关于窗口布局** — 头部整合项目图标（AAA.JPG）+ 版本徽章 + 开发者信息卡片
- **渐变分割线** — 直线分割线改为渐变式（透明到玫瑰色到透明），增加视觉精致度
- **氛围光晕装饰** — 各窗口右上角玫瑰色径向渐变半透明光晕
- **GitHub 链接** — 关于页头部加入仓库链接，点击自动浏览器打开
- **统一入场动画** — 关于窗口保持弹性缩放 + 卡片错开延迟入场

### 发布目录
- **新增 App.xaml.cs 自定义程序集解析器** — 支持 natives/ + runtimes/ 子目录布局
- **新增静态构造 PATH 配置** — WPF 原生 DLL 可从 natives/ 子目录加载
- **新增 pack-dist.ps1 + pack-dist.py 打包脚本** — 自动分类系统 DLL
- **目录结构优化** — 原生 DLL 移入 natives/（13 个），托管 DLL 移入 runtimes/（227 个），根目录保留仅 12 个核心文件

## `v2.3.6`（2026-07-09）— 全面 UI 修复 · 稳定性增强

### Bug 修复
- **修复设置/关于按钮导致崩溃** — 添加全局异常捕获 + try-catch 保护对话框创建，移除不稳定的 Icon 引用
- **修复工具栏按钮对齐混乱** — 统一所有按钮尺寸为 Height 32，图标按钮统一 Width 36，标准化间距/边距
- **修复字数统计图标消失** — Segoe MDL2 Assets 替换为通用 emoji（✍），消除字体版本兼容问题
- **修复设置/关于图标仅显示一半** — 按钮宽度从 32 增至 36，Padding 归零，确保 emoji 完整渲染
- **修复其它 Segoe MDL2 Assets 图标适配** — 统计面板/历史/词库管理/文件打开/清空按钮全部改用通用 emoji
- **修复下拉框弹窗圆角不一致** — ComboBoxItem 圆角从 6 调至 8 与主控件统一
- **统一卡片左右边距** — 工具栏内边距从 20 改为 16，与输入/结果卡片对齐
- **修复卡片间距不统一** — 输入/结果两栏间距从 12 改为 8，顶部/底部间距统一为 8px
- **修复结果卡片按钮字体粗糙** — FontSize 11→12、FontWeight SemiBold、Padding 10,3→12,5，替换 Segoe MDL2 Assets 为 emoji
- **修复统计区进度条布局锁定** — 百分比列固定宽度 50px 防挤压，进度条高度 6→8，背景 #E8E8E8，圆角 Radius=""1""，QuadraticEase 改为 EaseOut 顺滑过渡

## `v2.3.5`（2026-07-09）— 修复设置按钮图标与 MaterialDesign 包

### Bug 修复
- **修复工具栏设置/关于按钮图标丢失** — Segoe MDL2 Assets 字体图标改回 emoji 方式，避免渲染失效
- **修复设置/关于按钮变成胶囊形** — 解决 TextBlock Content 撑高按钮导致 CornerRadius 过渡为椭圆
- **修复设置/关于窗口标题栏图标缺失** — 添加 Icon 属性引用 AAA.ico
- **修复 MaterialDesignThemes 包缺失** — Styles.xaml 引用了 materialDesign:PackIcon 但未安装 NuGet 包

## `v2.3.4`（2026-06-18）— 字数统计

### 新功能
- **新增字数统计窗口** — 工具栏新增 W 按钮，弹出独立窗口显示输入文本的详细统计
- **字符级统计** — 总字符、有效字符（不含空格）、中文字符、英文字母、数字分类计数
- **单词级统计** — 总单词数、唯一单词数、平均词长、词频 Top N 分布
- **行级统计** — 总行数、非空行数
- **词频横向条形图** — Canvas + Rectangle 渲染 Top 12 词频分布
- **词长分布条形图** — 1-3/4-6/7-9/10+ 四桶分布，Rectangle 原生渲染

### Bug 修复
- **修复关于页 Logo 加载** — LoadAppIcon 改用 BitmapDecoder.Create 支持 .ico 格式

## `v2.3.3`（2026-06-18）— 替换面板 · Bug 修复

### 新功能
- **列表模式新增替换面板** — 每行勾选框选中不可用词，顶部出现替换输入框，Enter 一键替换输入文本中所有匹配词
- **替换流程**：勾选 → 替换栏展开 → 输入替换词 → Enter（或点击""替换""）→ 自动更新检查结果

### Bug 修复
- **修复工具栏按钮重叠** — 设置/关于按钮渲染到最后一列导致叠加，补全 ColumnDefinitions 至 9 列
- **修复 Logo 图片加载失败** — BitmapImage 改用 FileStream + StreamSource，提高环境兼容性

## `v2.3.2`（2026-06-18）— 词库可视化 · 排版模式 · 拼写增强 · 泰语 · 差异对比 · 实时加载 · 工具栏优化

### 实时加载 & 版本号
- **自动监控词库文件** — FileSystemWatcher 实时监控，修改后自动重载（500ms 防抖）
- **移除手动重载按钮** — 不再需要手动点击 ⟳ 按钮
- **新增 AppInfo.Version 常量** — 统一管理全局版本号

### 词库可视化与管理
- **新增词库浏览器窗口** — 可视化查看已加载词库的全部单词
- **实时搜索过滤** — 顶部搜索框，大小写不敏感实时过滤
- **四种排序模式** — 字母升降序、词长升降序切换
- **词长/首字母分布柱状图** — 统计单词分布，Rectangle 原生渲染
- **底部状态栏** — 显示总词数 / 匹配词数
- **新增词库对比** — 双栏列表显示新增/移除/共有词，支持导出差异报告
- **词库管理合并** — 浏览器与对比合并为「词库管理」，Tab 页切换

### 排版模式切换
- **新增排版切换** — ComboBox 支持「内嵌」「列表」「两栏对比」三种结果展示
- **列表模式** — 逐词成行，带颜色圆点 + 状态标签
- **两栏对比模式** — 左栏可用词 / 右栏不可用词，去重排序
- **模式切换动画** — 淡出→淡入过渡（350ms），模式持久化到 appsettings.json

### 拼写建议增强
- **智能建议引擎** — 通配搜索(*/?)、编辑距离、复合拆词三种策略
- **复合拆词** — 自动识别连续输入的已知词组合（如 cancelOverride→cancel + override）
- **分类建议面板** — 按来源分三区展示，点击建议词自动替换

### 单词详情弹窗
- **悬停弹窗** — 鼠标移到不可用词显示词频 + Top 3 拼写建议
- **防抖优化** — 快速扫过不闪烁，离开 100ms 自动关闭

### 泰语界面支持
- **新增泰语界面** — ไทย（th-TH），支持语言扩展至 8 种

### 工具栏 & UI 修复
- **⚙ 设置与 ⓘ 关于移至右上角** — 释放工具栏空间，标题不再被遮挡
- **ComboBox 显示修复** — 模式下拉列表不再显示 {text = [模式]} 错误
- **建议面板修复** — 空文本/无可建议单词时自动隐藏

## `v2.3.1`（2026-05-03）— Bug 修复 & UI 优化

- **进度条修复** — 初始化时不再显示 100%（空文本强制 0%），去除 ScaleTransform 改为 Width 动画，圆角始终完整
- **进度条缩短** — 新增 MaxWidth 300，高度增至 10px，圆角 8px，视觉更圆润
- **修复输入闪烁** — 键入缩放动画改为仅前 5 次触发，避免反复缩放导致闪烁
- **移除 CC BY-SA 徽标** — 声明标签页不再显示协议图片
- **Markdown 格式优化** — 版本号加 code 样式，技术术语添加代码高亮

## `v2.3.0`（2026-05-02）— 单实例 · 自动更新 · 统计 · 批量导入 · 多语言扩展

- **单实例限制**：Mutex 防止多开
- **自动更新**：从 GitHub API 检查新版本
- **统计面板**：Canvas 折线图展示覆盖率 / 不可用词趋势
- **批量导入**：支持 `TXT` / `CSV` / `Excel(.xlsx)`，多选文件合并词库
- **新增法语**（Français）界面
- 设置窗口新增语言选择和检查更新
- 字体渲染优化（`TextFormattingMode = Ideal`）

## `v2.2.1`（2026-05-02）— BUG 修复 & 优化

- 修复悬停 ToolTip 不显示的问题
- 修复多次点击历史按钮导致建议面板内容不显示的问题
- 历史记录独立窗口 + 持久化保存到 `history.json`
- 白名单持久化修复

## `v2.2.0`（2026-05-02）— 新功能

- 结果导出为 `.txt` 文件
- **Ctrl+Z / Ctrl+Y** 撤销重做
- 批量打开多个文本文件
- 检查历史记录（最近 50 次）

## `v2.1.1`（2026-05-02）— 性能与代码质量

- `CheckText` 不再被 `GetStatistics` 重复调用
- 建议面板结果缓存（`Levenshtein` 编辑距离）
- 键入缩放动画加 **300ms 防抖**
- 阵营/希腊字母匹配改为 **HashSet O(1)** 查找
- 清理冗余代码

## `v2.1.0`（2026-05-02）— 代码优化

- 新增 `.sln` 解决方案文件
- 修复 `HexRegex` 误伤纯数字的问题
- `Clipboard.SetText` 增加 `try-catch`
- 移除冗余的 ViewModel 文件
- `.csproj` 单文件发布参数重构

## `v2.0.0`（2026-05-02）— 第二次迭代

- 重构 **Checker** 引擎：精确 token 过滤
- 新增十六进制色值过滤
- 新增可选命名过滤系统
- 新增 `pitch_` / `.G` / `JAM` 过滤
- **全局动画系统**：卡片弹性缩放、键入微动、进度条过渡
- Apple 风格卡片布局 + 阴影
- 设置页面：字体大小、自动换行

## `v1.0.0`（初始版本）

- 基础 CASSIE 词库检查功能
- Simple dark theme
- Basic settings

---
";

    // 0 = Features, 1 = Changelog, 2 = AboutInfo, 3 = Disclaimer
    private int _activeTab;

    public AboutWindow()
    {
        InitializeComponent();
        VersionLabel.Text = $"版本 {AppInfo.Version}";
        VersionBadge.Text = $"v{AppInfo.Version}";
        this.EnableDarkTitleBar();
        ContentArea.Opacity = 1;
        ContentArea.RenderTransform = new TranslateTransform(0, 0);
        try
        {
            ContentArea.Document = MarkdownConverter.Convert(FeaturesText, 400);
        }
        catch
        {
            // 转换异常时忽略，显示空白
        }
        SetActiveTab(0);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载应用图标
        LoadAppIcon();

        // ── 窗口入场：弹性缩放 + 淡入 ──
        var sb = new Storyboard();

        var scaleX = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        scaleX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(scaleX, this);
        Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

        var scaleY = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        scaleY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(scaleY, this);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fade);
        sb.Begin(this);

        // ── 应用图标错开入场（若已加载真实图标则保留入场） ──
        // 图标容器用简单淡入即可
        // 内容区淡入
        ContentArea.Opacity = 0;
        var contentFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(0.35)));
        contentFade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        contentFade.BeginTime = TimeSpan.FromSeconds(0.25);
        ContentArea.BeginAnimation(OpacityProperty, contentFade);
    }

    private void LoadAppIcon()
    {
        try
        {
            var baseDirs = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetDirectoryName(Environment.ProcessPath) ?? ".",
                Directory.GetCurrentDirectory(),
            };
            var names = new[] { "AAA.JPG", "AAA.ico", "AAA.png", "app.ico" };
            var paths = baseDirs
                .SelectMany(dir => names.Select(name => Path.Combine(dir, "data", name)))
                .ToArray();
            var imgPath = paths.FirstOrDefault(File.Exists);
            if (imgPath is not null)
            {
                var ext = Path.GetExtension(imgPath).ToLowerInvariant();
                if (ext == ".ico")
                {
                    using var stream = new FileStream(imgPath, FileMode.Open, FileAccess.Read);
                    var decoder = BitmapDecoder.Create(stream,
                        BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    AppIconImage.Source = decoder.Frames[0];
                }
                else
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    using (var stream = new FileStream(imgPath, FileMode.Open, FileAccess.Read))
                    {
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    AppIconImage.Source = bitmap;
                }
            }
        }
        catch { /* 图标加载失败时保持 Ellipse 后备色块 */ }
    }

    // ── GitHub 链接 ───────────────────────────────────────────────
    private void OnGitHubLink(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* 静默 */ }
        e.Handled = true;
    }

    // ── 标签页切换 ────────────────────────────────────────────
    private void OnShowFeatures(object sender, MouseButtonEventArgs e) => SwitchTab(0);
    private void OnShowChangelog(object sender, MouseButtonEventArgs e) => SwitchTab(1);
    private void OnShowAboutInfo(object sender, MouseButtonEventArgs e) => SwitchTab(2);
    private void OnShowDisclaimer(object sender, MouseButtonEventArgs e) => SwitchTab(3);

    // ── 切换标签 ────────────────────────────────────────────────
    private void SwitchTab(int tabIndex)
    {
        if (tabIndex == _activeTab) return;
        _activeTab = tabIndex;

        SetActiveTab(tabIndex);

        var rawText = tabIndex switch
        {
            0 => FeaturesText,
            1 => ChangelogText,
            2 => AboutInfoText,
            _ => DisclaimerText,
        };

        // 内容交叉淡出/淡入
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(180)));
        fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
        fadeOut.Completed += (_, _) =>
        {
            ContentArea.Document = MarkdownConverter.Convert(
                rawText, ContentArea.ActualWidth > 100 ? ContentArea.ActualWidth : 400);
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ContentArea.BeginAnimation(OpacityProperty, fadeIn);
        };
        ContentArea.BeginAnimation(OpacityProperty, fadeOut);
    }

    /// <summary>
    /// 激活指定标签页：设置 Border.Tag 触发 XAML 中的视觉样式切换
    /// </summary>
    private void SetActiveTab(int tabIndex)
    {
        FeaturesTab.Tag = tabIndex == 0 ? "Active" : null;
        ChangelogTab.Tag = tabIndex == 1 ? "Active" : null;
        AboutInfoTab.Tag = tabIndex == 2 ? "Active" : null;
        DisclaimerTab.Tag = tabIndex == 3 ? "Active" : null;

        // 同步设置子 TextBlock 的 Tag
        FeaturesTabText.Tag = tabIndex == 0 ? "Active" : null;
        ChangelogTabText.Tag = tabIndex == 1 ? "Active" : null;
        AboutInfoTabText.Tag = tabIndex == 2 ? "Active" : null;
        DisclaimerTabText.Tag = tabIndex == 3 ? "Active" : null;
    }

    // ── 禁止文本选中（只允许超链接点击）────────────────────────
    private void OnContentPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        try
        {
            var dep = e.OriginalSource as DependencyObject;
            while (dep is not null)
            {
                if (dep is Hyperlink) return;
                dep = LogicalTreeHelper.GetParent(dep);
            }
        }
        catch
        {
            // 视觉树遍历异常时放行
        }
    }

    // ── 动画辅助 ─────────────────────────────────────────────
    private static void AnimateElement(UIElement el, double fromScale, double toScale,
                                       double fromY, double toY, double delay, double duration)
    {
        el.Opacity = 0;
        el.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(fromScale, fromScale),
                new TranslateTransform(0, fromY)
            }
        };

        var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(duration)));
        fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        fade.BeginTime = TimeSpan.FromSeconds(delay);
        el.BeginAnimation(OpacityProperty, fade);

        var moveY = new DoubleAnimation(fromY, toY, new Duration(TimeSpan.FromSeconds(duration)));
        moveY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        moveY.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[1].BeginAnimation(TranslateTransform.YProperty, moveY);

        var scaleXAnim = new DoubleAnimation(fromScale, toScale, new Duration(TimeSpan.FromSeconds(duration)));
        scaleXAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        scaleXAnim.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[0].BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);

        var scaleYAnim = new DoubleAnimation(fromScale, toScale, new Duration(TimeSpan.FromSeconds(duration)));
        scaleYAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        scaleYAnim.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[0].BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
    }
}
