namespace CassieWordCheck.Models;

/// <summary>
/// Markdown 块模型，不依赖任何 UI 框架喵
/// </summary>
public abstract record MarkdownBlock;

public sealed record MarkdownHeading(int Level, IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

public sealed record MarkdownParagraph(IReadOnlyList<MarkdownInline> Inlines) : MarkdownBlock;

public sealed record MarkdownList(IReadOnlyList<MarkdownParagraph> Items) : MarkdownBlock;

public sealed record MarkdownSeparator : MarkdownBlock;

public sealed record MarkdownImage(
    string Path,
    IReadOnlyList<MarkdownInline> Caption) : MarkdownBlock;

public abstract record MarkdownInline;

public sealed record MarkdownText(string Text) : MarkdownInline;

public sealed record MarkdownBold(IReadOnlyList<MarkdownInline> Inlines) : MarkdownInline;

public sealed record MarkdownItalic(IReadOnlyList<MarkdownInline> Inlines) : MarkdownInline;

public sealed record MarkdownCode(string Text) : MarkdownInline;

public sealed record MarkdownLink(string Text, string Uri) : MarkdownInline;
