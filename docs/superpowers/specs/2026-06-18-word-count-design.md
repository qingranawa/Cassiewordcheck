# 字数统计功能 — 设计文档

## 1. 需求定义

### 目标用户
CASSIE 配音演员/广播文本编辑者，需要了解待录制文本的详细构成。

### 用户需求
1. **文本体积评估** — 总字符数、字数，快速判断工作量
2. **配音时长估算** — 中/英文字符分类统计，辅助估算录制时间
3. **文本结构分析** — 行数、词频分布、词长分布，帮助理解脚本结构
4. **常用词追踪** — 高频词 Top 列表，辅助针对性练习

### 与现有功能的边界
- 已有 `CharCountLabel` 显示基础字符数 `"字符：{0}"`（在输入框 Header）
- 已有底部 `StatsBar` 显示可用/不可用/覆盖率
- 已有 `StatisticsWindow` 显示**历史趋势**（覆盖率/不可用词随时间变化）
- **本功能专注当前输入文本的详细静态统计**，不涉及历史数据

---

## 2. 统计指标清单

### 字符级统计
| 指标 | 说明 | 来源 |
|------|------|------|
| 总字符数 | 含空格 | `text.Length` |
| 有效字符数 | 不含空格 | 去掉空格后计数 |
| 中文字符 | 匹配 Unicode 范围 `一-鿿` | `char.IsSurrogate` 判断 |
| 英文字母 | 匹配 `a-zA-Z` | `char.IsLetter` |
| 数字 | 匹配 `0-9` | `char.IsDigit` |
| 标点符号 | 匹配标点类别 | `char.IsPunctuation` |
| 空格 | 空格/制表符等 | `char.IsWhiteSpace` |

### 单词级统计
| 指标 | 说明 | 来源 |
|------|------|------|
| 总单词数 | 按空白分割 | `text.Split()` |
| 唯一单词数 | 去重后计数 | `Distinct()` |
| 平均词长 | 总字母数/总单词数 | 计算 |
| 词频 Top N | 出现最多的词及其次数 | `GroupBy` 统计 |
| 词长分布 | 按长度分桶（1-3/4-6/7-9/10+） | `GroupBy` 统计 |

### 行级统计
| 指标 | 说明 | 来源 |
|------|------|------|
| 总行数 | 按换行分割 | `Split('\n')` |
| 非空行数 | 去掉空白行 | `Where` 过滤 |

---

## 3. UI 设计

### 交互方式
- **入口**：MainWindow 工具栏新增按钮 `W`（或等价图标）
- **展现方式**：独立模态窗口 `WordCountWindow`
- **窗口尺寸**：600x480，可 resize，居中于 Owner

### 窗口布局

```
┌─────────────────────────────────────────────────────┐
│ 标题栏：📊 字数统计                         [×]     │
├─────────────────────────────────────────────────────┤
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │
│ │总字符 │ │有效字 │ │中文字 │ │英文  │ │数字  │      │
│ │1,234  │ │1,080  │ │ 567  │ │ 456  │ │ 89   │      │
│ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘      │
│ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐      │
│ │总单词 │ │唯一词 │ │平均  │ │总行数 │ │非空行│      │
│ │ 345  │ │ 120  │ │ 5.2  │ │  28  │ │  25  │      │
│ └──────┘ └──────┘ └──────┘ └──────┘ └──────┘      │
│                                                      │
│ ┌─ 词频分布 Top 12 ───────────────────────────────┐ │
│ │ the      ████████████████████  15 次  ########  │ │
│ │ and      ████████████████      12 次  ########  │ │
│ │ override ████████             8 次   ########  │ │
│ │ ...                                              │ │
│ └──────────────────────────────────────────────────┘ │
│ ┌─ 词长分布 ───────────────────────────────────────┐ │
│ │ 1-3   █████████████████████████  45              │ │
│ │ 4-6   █████████████████████████████████  67      │ │
│ │ 7-9   ██████████████  23                         │ │
│ │ 10+   ██████████  12                             │ │
│ └──────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
```

### 设计风格
- 与现有 `StatisticsWindow` 视觉风格一致
- 暗色主题，Card 式统计块
- 统计数字使用 `MonoFont` 等宽字体
- 条形图使用 `Rectangle` 原生渲染 + 颜色渐变
- 窗口入场：缩放淡入动画（与 StatisticsWindow 一致）

---

## 4. 数据模型

```csharp
public class WordCountResult
{
    // 字符统计
    public int TotalChars { get; set; }
    public int CharsNoSpaces { get; set; }
    public int ChineseChars { get; set; }
    public int EnglishLetters { get; set; }
    public int DigitChars { get; set; }
    public int PunctuationChars { get; set; }
    
    // 单词统计
    public int TotalWords { get; set; }
    public int UniqueWords { get; set; }
    public double AvgWordLength { get; set; }
    public List<WordFreqItem> TopFrequentWords { get; set; } = [];
    public List<WordLengthBucket> WordLengthDistribution { get; set; } = [];
    
    // 行统计
    public int TotalLines { get; set; }
    public int NonEmptyLines { get; set; }
}

public class WordFreqItem
{
    public string Word { get; set; } = "";
    public int Count { get; set; }
}

public class WordLengthBucket
{
    public string Label { get; set; } = "";  // "1-3", "4-6", "7-9", "10+"
    public int Count { get; set; }
}
```

---

## 5. 服务层

`WordCountService` 是纯静态统计类，无副作用，适合单元测试。

方法签名：
```csharp
public static WordCountResult Count(string text)
```

处理逻辑：
1. 空文本返回全零的 `WordCountResult`
2. 逐字符分类计数
3. 按空白分割统计单词
4. 按换行分割统计行
5. 提取 Top 12 高频词
6. 计算词长分布（1-3 / 4-6 / 7-9 / 10+ 四桶）

---

## 6. 涉及文件清单

| 文件 | 操作 | 说明 |
|------|------|------|
| `Models/WordCountResult.cs` | **新建** | 统计结果数据模型 |
| `Services/WordCountService.cs` | **新建** | 核心统计逻辑 |
| `Views/WordCountWindow.xaml` | **新建** | 窗口 UI 布局 |
| `Views/WordCountWindow.xaml.cs` | **新建** | 窗口代码后置 |
| `Views/MainWindow.xaml` | 修改 | 工具栏新增按钮 |
| `Views/MainWindow.xaml.cs` | 修改 | 新增按钮事件处理 |
| `Resources/Locales/zh-CN.json` | 修改 | 添加本地化 key |
| `Resources/Locales/en-US.json` | 修改 | 添加本地化 key |
| `Resources/Locales/de-DE.json` | 修改 | 添加本地化 key（用英文填充） |
| `Resources/Locales/fr-FR.json` | 修改 | 同上 |
| `Resources/Locales/ja-JP.json` | 修改 | 同上 |
| `Resources/Locales/ko-KR.json` | 修改 | 同上 |
| `Resources/Locales/ru-RU.json` | 修改 | 同上 |
| `Resources/Locales/th-TH.json` | 修改 | 同上 |
| `CassieWordCheck.Tests/WordCountServiceTests.cs` | **新建** | 单元测试 |
| `Views/AboutWindow.xaml.cs` | 修改 | 更新日志 |

---

## 7. 无侵入原则

- 不修改 `Checker`、`WordList`、`Settings` 等现有核心模型
- 不修改 `StatisticsWindow`（职责分离：历史趋势 vs 当前静态统计）
- 不修改现有动画/布局代码
- `WordCountService` 为纯静态方法，无依赖注入需求
