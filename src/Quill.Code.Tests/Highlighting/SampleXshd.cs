namespace Quill.Code.Tests.Highlighting;

/// <summary>Définition réduite, inspirée des fichiers de Querify et PocketForge.</summary>
internal static class SampleXshd
{
    public const string Xml = """
        <?xml version="1.0" encoding="utf-8" ?>
        <SyntaxDefinition name="Sample" extensions=".cs; .csx" xmlns="http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008">
          <Property name="LineComment" value="//" />
          <Color name="Default" foreground="#D4D4D4" />
          <Color name="Comment" foreground="#6A9955" fontStyle="Italic" />
          <Color name="String" foreground="#CE9178" />
          <Color name="Keyword" foreground="#569CD6" fontWeight="Bold" />
          <Color name="Number" foreground="#B5CEA8" />
          <RuleSet ignoreCase="false">
            <Span color="Comment"><Begin>//</Begin></Span>
            <Span color="Comment" multiline="true"><Begin>/\*</Begin><End>\*/</End></Span>
            <Span color="String"><Begin>"</Begin><End>"</End></Span>
            <Keywords color="Keyword">
              <Word>do</Word>
              <Word>double</Word>
              <Word>return</Word>
            </Keywords>
            <Rule color="Number">\b\d+\b</Rule>
          </RuleSet>
        </SyntaxDefinition>
        """;

    public static Quill.Code.Highlighting.SyntaxDefinition Definition { get; } = Quill.Code.Highlighting.XshdReader.Parse(Xml);
}
