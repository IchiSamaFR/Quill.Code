using System.Text.RegularExpressions;

namespace Quill.Code.Highlighting;

/// <summary>Règle <c>Rule</c> ou <c>Keywords</c> (une expression) ou <c>Span</c> (début, fin).</summary>
internal sealed class SyntaxRule(Regex begin, Regex? end, bool isSpan, bool isMultiline, SyntaxColor? color)
{
    /// <summary>Expression de la règle, ou début du span.</summary>
    public Regex Begin { get; } = begin;

    /// <summary>Fin du span ; nulle pour un span qui court jusqu'à la fin de la ligne.</summary>
    public Regex? End { get; } = end;

    public bool IsSpan { get; } = isSpan;

    /// <summary>Le span peut se poursuivre sur les lignes suivantes.</summary>
    public bool IsMultiline { get; } = isMultiline;

    public SyntaxColor? Color { get; } = color;
}
