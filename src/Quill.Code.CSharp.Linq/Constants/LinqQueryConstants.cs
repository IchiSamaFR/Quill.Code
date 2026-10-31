using System.Reflection;

namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>Cadre C# d'une requete LINQ : noms generes autour du texte de l'editeur.</summary>
internal static class LinqQueryConstants
{
    /// <summary>Alias du DbContext genere : une requete reste valable d'une base a l'autre.</summary>
    internal const string ContextAlias = "QueryDbContext";

    /// <summary>Classe statique du contexte et des parametres, utilisables sans les declarer.</summary>
    internal const string GlobalsClass = "QueryGlobals";

    /// <summary>Nom, dans <see cref="GlobalsClass"/>, du contexte de la requete.</summary>
    internal const string ContextGlobal = "Db";

    /// <summary>Document fixe des usings implicites (dont l'alias du contexte et l'import de <see cref="GlobalsClass"/>).</summary>
    internal const string GlobalUsings = "GlobalUsings.cs";

    /// <summary>Document de la requete dans le projet Roslyn.</summary>
    internal const string QueryDocumentName = "requete.cs";

    /// <summary>Nom des projets compiles sans DbContext (C# seul).</summary>
    internal const string WithoutModelName = "SansConnexion";

    /// <summary>Erreur Roslyn d'un programme sans instruction ni Main : pas une faute dans l'editeur, ou un texte vide la signalerait.</summary>
    internal const string NoEntryPointCode = "CS5001";

    /// <summary>Membres statiques generes (QueryGlobals), publics ou non.</summary>
    internal const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
}
