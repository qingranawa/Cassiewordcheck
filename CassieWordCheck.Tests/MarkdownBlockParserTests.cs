using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Tests;

public class MarkdownBlockParserTests
{
    [Fact]
    public void Parse_CommonMarkdown_ReturnsTypedBlocksAndInlines()
    {
        var blocks = MarkdownBlockParser.Parse(
            "# Title\n\n**bold** `code` [site](https://example.com)\n\n- item\n---");

        Assert.Collection(blocks,
            block => Assert.IsType<MarkdownHeading>(block),
            block => Assert.IsType<MarkdownParagraph>(block),
            block => Assert.IsType<MarkdownList>(block),
            block => Assert.IsType<MarkdownSeparator>(block));
    }

    [Fact]
    public void Parse_InvalidLink_KeepsTextAsPlainInline()
    {
        var blocks = MarkdownBlockParser.Parse("[broken](not a uri)");

        var paragraph = Assert.IsType<MarkdownParagraph>(Assert.Single(blocks));
        Assert.Contains(paragraph.Inlines, inline => inline is MarkdownText text && text.Text.Contains("broken"));
    }
}
