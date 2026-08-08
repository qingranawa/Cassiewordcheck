using System.Text.RegularExpressions;
using CassieWordCheck.Models;

namespace CassieWordCheck.Services;

/// <summary>
/// 将关于页使用的有限 Markdown 语法解析为 UI 无关节点喵
/// </summary>
public static partial class MarkdownBlockParser
{
    public static IReadOnlyList<MarkdownBlock> Parse(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var blocks = new List<MarkdownBlock>();
        var paragraphLines = new List<string>();
        var listItems = new List<MarkdownParagraph>();

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
                return;

            var text = string.Join(' ', paragraphLines.Select(line => line.Trim()));
            blocks.Add(new MarkdownParagraph(ParseInlines(text)));
            paragraphLines.Clear();
        }

        void FlushList()
        {
            if (listItems.Count == 0)
                return;

            blocks.Add(new MarkdownList(listItems.ToArray()));
            listItems.Clear();
        }

        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            if (TryParseImage(trimmed, out var image))
            {
                FlushParagraph();
                FlushList();
                blocks.Add(image);
                continue;
            }

            if (IsSeparator(trimmed))
            {
                FlushParagraph();
                FlushList();
                blocks.Add(new MarkdownSeparator());
                continue;
            }

            if (TryParseHeading(trimmed, out var heading))
            {
                FlushParagraph();
                FlushList();
                blocks.Add(heading);
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                trimmed.StartsWith("• ", StringComparison.Ordinal))
            {
                FlushParagraph();
                listItems.Add(new MarkdownParagraph(ParseInlines(trimmed[2..].Trim())));
                continue;
            }

            FlushList();
            paragraphLines.Add(trimmed);
        }

        FlushParagraph();
        FlushList();
        return blocks;
    }

    private static bool IsSeparator(string line) =>
        line is "---" or "———" or "——" or "___";

    private static bool TryParseHeading(string line, out MarkdownHeading heading)
    {
        var level = 0;
        while (level < line.Length && level < 3 && line[level] == '#')
            level++;

        if (level == 0 || level == line.Length || line[level] != ' ')
        {
            heading = null!;
            return false;
        }

        heading = new MarkdownHeading(level, ParseInlines(line[(level + 1)..].Trim()));
        return true;
    }

    private static bool TryParseImage(string line, out MarkdownImage image)
    {
        var match = ImagePattern().Match(line);
        if (!match.Success)
        {
            image = null!;
            return false;
        }

        image = new MarkdownImage(
            match.Groups[2].Value,
            ParseInlines(match.Groups[1].Value));
        return true;
    }

    private static IReadOnlyList<MarkdownInline> ParseInlines(string text)
    {
        var inlines = new List<MarkdownInline>();
        var position = 0;

        while (position < text.Length)
        {
            if (TryReadDelimited(text, position, '`', out var code, out var codeEnd))
            {
                inlines.Add(new MarkdownCode(code));
                position = codeEnd;
                continue;
            }

            if (text.AsSpan(position).StartsWith("**", StringComparison.Ordinal) &&
                TryReadDelimited(text, position + 2, "**", out var bold, out var boldEnd))
            {
                inlines.Add(new MarkdownBold(ParseInlines(bold)));
                position = boldEnd;
                continue;
            }

            if (text[position] == '*' &&
                TryReadDelimited(text, position + 1, '*', out var italic, out var italicEnd))
            {
                inlines.Add(new MarkdownItalic(ParseInlines(italic)));
                position = italicEnd;
                continue;
            }

            if (text[position] == '[')
            {
                var closeText = text.IndexOf("](", position + 1, StringComparison.Ordinal);
                var closeUri = closeText >= 0 ? text.IndexOf(')', closeText + 2) : -1;
                if (closeText > position && closeUri > closeText)
                {
                    var linkText = text[(position + 1)..closeText];
                    var uri = text[(closeText + 2)..closeUri];
                    if (Uri.TryCreate(uri, UriKind.Absolute, out _))
                    {
                        inlines.Add(new MarkdownLink(linkText, uri));
                        position = closeUri + 1;
                        continue;
                    }
                }
            }

            var next = FindNextInlineMarker(text, position + 1);
            inlines.Add(new MarkdownText(text[position..next]));
            position = next;
        }

        return inlines;
    }

    private static int FindNextInlineMarker(string text, int start)
    {
        var candidates = new[]
        {
            text.IndexOf('`', start),
            text.IndexOf('*', start),
            text.IndexOf('[', start),
        }.Where(index => index >= 0);

        return candidates.DefaultIfEmpty(text.Length).Min();
    }

    private static bool TryReadDelimited(string text, int contentStart, char delimiter,
        out string content, out int end)
    {
        var close = text.IndexOf(delimiter, contentStart);
        if (close < contentStart)
        {
            content = string.Empty;
            end = contentStart;
            return false;
        }

        content = text[contentStart..close];
        end = close + 1;
        return true;
    }

    private static bool TryReadDelimited(string text, int contentStart, string delimiter,
        out string content, out int end)
    {
        var close = text.IndexOf(delimiter, contentStart, StringComparison.Ordinal);
        if (close < contentStart)
        {
            content = string.Empty;
            end = contentStart;
            return false;
        }

        content = text[contentStart..close];
        end = close + delimiter.Length;
        return true;
    }

    [GeneratedRegex("^!\\[([^]]*)\\]\\(([^)]+)\\)$")]
    private static partial Regex ImagePattern();
}
