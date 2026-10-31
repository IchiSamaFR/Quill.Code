namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>Noms du modele EF genere depuis la structure d'une base.</summary>
internal static class EfModelConstants
{
    /// <summary>Espace de noms du DbContext et des entites generes.</summary>
    internal const string GeneratedNamespace = "Quill.Generated";

    /// <summary>Classe generee portant Dump() ; sa propriete privee <see cref="DumpSinkPropertyName"/> recoit les valeurs.</summary>
    internal const string ExtensionsClassName = "QueryExtensions";

    /// <inheritdoc cref="ExtensionsClassName"/>
    internal const string DumpSinkPropertyName = "DumpSink";
}
