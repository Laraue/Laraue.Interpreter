using Laraue.Interpreter.Markdown.Meta;
using Laraue.Interpreter.Markdown.UnitTests.Infrastructure;

namespace Laraue.Interpreter.Markdown.UnitTests;

public class MarkdownMetaTokenParserTests
{
    [Fact]
    public void MarkdownMetaTokenParser_ShouldParseTree_WhenFormattingIsNormal()
    {
        const string markdownFile = @"---
tags: [tag1, tag2]
project: project1
type: unitTestArticle

---

# Title of level 1";
        
        var parseResult = Parse(markdownFile);
        Assert.Equal("# Title of level 1", parseResult.Content);

        var headers = parseResult.Headers;
        
        Assert.Equal(3, headers.Length);
        
        CheckHeaderUtility.Check(
            headers[0],
            "tags",
            1,
            new[] { "tag1", "tag2" });
        
        CheckHeaderUtility.Check(
            headers[1],
            "project",
            2,
            "project1");
        
        CheckHeaderUtility.Check(
            headers[2],
            "type",
            3,
            "unitTestArticle");
    }
    
    [Fact]
    public void MarkdownMetaTokenParser_ShouldParseTree_WhenFormattingIsAbnormal()
    {
        const string markdownFile = @"---

tags  : [tag1,  tag2 ]


project: project1
type: unitTestArticle
createdAt: 2020-01-01
multiLineString: [person 1, person 2]
brokenArray: [asdqq, aa
---

# Title of level 1";
        
        var parseResult = Parse(markdownFile);
        Assert.Equal("# Title of level 1", parseResult.Content);

        var headers = parseResult.Headers;
        
        Assert.Equal(6, headers.Length);
        
        CheckHeaderUtility.Check(
            headers[0],
            "tags",
            2,
            new[] { "tag1", "tag2" });
        
        CheckHeaderUtility.Check(
            headers[1],
            "project",
            5,
            "project1");
        
        CheckHeaderUtility.Check(
            headers[2],
            "type",
            6,
            "unitTestArticle");
        
        CheckHeaderUtility.Check(
            headers[3],
            "createdAt",
            7,
            "2020-01-01");
        
        CheckHeaderUtility.Check(
            headers[4],
            "multiLineString",
            8,
            new[] { "person 1", "person 2" });
        
        CheckHeaderUtility.Check(
            headers[5],
            "brokenArray",
            9,
            "[asdqq, aa");
    }
    
    [Fact]
    public void MarkdownMetaTokenParser_ShouldParseTree_WhenContentMissing()
    {
        const string markdownFile = @"---
project: project1
---";
        
        var parseResult = Parse(markdownFile);
        Assert.Empty(parseResult.Content);

        var headers = parseResult.Headers;
        var header = Assert.Single(headers);
        
        CheckHeaderUtility.Check(
            header,
            "project",
            1,
            "project1");
    }

    [Theory]
    [InlineData("- Fix now")]
    [InlineData("- a\n- b")]
    [InlineData("-- not a delimiter")]
    [InlineData("  indented text")]
    [InlineData("\n\n- Fix now")]
    public void MarkdownMetaTokenParser_ShouldReturnWholeTextAsContent_WhenMetaIsMissing(string markdownFile)
    {
        var parseResult = Parse(markdownFile);

        Assert.Empty(parseResult.Headers);
        Assert.Equal(markdownFile.TrimStart('\n'), parseResult.Content);
    }

    [Fact]
    public void MarkdownMetaTokenParser_ShouldKeepListAsContent_WhenMetaIsFollowedByList()
    {
        const string markdownFile = @"---
project: project1
---
- a
- b";

        var parseResult = Parse(markdownFile);

        Assert.Single(parseResult.Headers);
        Assert.Equal("- a\n- b", parseResult.Content.Replace("\r\n", "\n"));
    }

    private static MarkdownMetaTree Parse(string markdown)
    {
        var scanner = new MarkdownMetaTokenScanner(markdown);
        var scanResult = scanner.ScanTokens();

        var parser = new MarkdownMetaTokenParser(scanResult.Tokens);
        var parseResult = parser.Parse();
        
        Assert.Empty(parseResult.Errors);
        Assert.NotNull(parseResult.Result);

        return parseResult.Result;
    }
}