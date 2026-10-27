using Quill.Code.Highlighting;

namespace Quill.Code.Tests.Highlighting;

public sealed class XshdReaderTests
{
    [Fact]
    public void Parse_ReadsHeaderPropertiesAndColors()
    {
        var definition = SampleXshd.Definition;

        Assert.Equal("Sample", definition.Name);
        Assert.Equal(new[] { ".cs", ".csx" }, definition.Extensions);
        Assert.Equal("//", definition.LineComment);
        Assert.Equal("#D4D4D4", definition.DefaultColor?.Foreground);
        Assert.Equal("Italic", definition.Colors["comment"].FontStyle);
    }

    [Fact]
    public void Parse_KeepsKeywordsWithTheirCategory()
    {
        Assert.Equal(
            new[] { new SyntaxKeyword("do", "Keyword"), new SyntaxKeyword("double", "Keyword"), new SyntaxKeyword("return", "Keyword") },
            SampleXshd.Definition.Keywords);
    }

    [Fact]
    public void Parse_UndeclaredColorIsAnError()
    {
        const string xml = """
            <SyntaxDefinition name="X" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
              <RuleSet><Rule color="Missing">a</Rule></RuleSet>
            </SyntaxDefinition>
            """;

        Assert.Throws<FormatException>(() => XshdReader.Parse(xml));
    }

    [Fact]
    public void Parse_WithoutRuleSetIsAnError()
    {
        Assert.Throws<FormatException>(() => XshdReader.Parse("""<SyntaxDefinition name="X" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008" />"""));
    }
}
