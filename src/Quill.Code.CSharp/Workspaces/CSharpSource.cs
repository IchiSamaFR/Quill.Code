namespace Quill.Code.CSharp.Workspaces;

/// <summary>
/// Source analysé par Roslyn pour un texte d'éditeur : le texte (tel quel, ou neutralisé à longueur égale)
/// placé à <see cref="TextStart"/>, éventuellement entouré de code généré.
/// </summary>
/// <param name="Source">Source complet.</param>
/// <param name="TextStart">Position du texte de l'éditeur dans <paramref name="Source"/>.</param>
public readonly record struct CSharpSource(string Source, int TextStart)
{
    /// <summary>Source qui est le texte lui-même (transformé à longueur égale), en position 0.</summary>
    public static implicit operator CSharpSource(string source) => FromString(source);

    /// <inheritdoc cref="op_Implicit(string)"/>
    public static CSharpSource FromString(string source) => new(source, 0);
}
