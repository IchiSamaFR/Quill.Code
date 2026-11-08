using System.Reflection;

namespace Quill.Code.CSharp.Linq.Constants;

/// <summary>C# frame of a LINQ query: names generated around the editor text.</summary>
internal static class LinqQueryConstants
{
    /// <summary>Alias of the generated DbContext: a query stays valid from one database to another.</summary>
    internal const string ContextAlias = "QueryDbContext";

    /// <summary>Static class of the context and parameters, usable without declaring them.</summary>
    internal const string GlobalsClass = "QueryGlobals";

    /// <summary>Name, in <see cref="GlobalsClass"/>, of the query context.</summary>
    internal const string ContextGlobal = "Db";

    /// <summary>Fixed document of implicit usings (including the context alias and the import of <see cref="GlobalsClass"/>).</summary>
    internal const string GlobalUsings = "GlobalUsings.cs";

    /// <summary>Document of the query in the Roslyn project.</summary>
    internal const string QueryDocumentName = "query.cs";

    /// <summary>Name of projects compiled without a DbContext (C# only).</summary>
    internal const string WithoutModelName = "SansConnexion";

    /// <summary>Roslyn error for a program without statements or Main: not an error in the editor, or an empty text would report it.</summary>
    internal const string NoEntryPointCode = "CS5001";

    /// <summary>Generated static members (QueryGlobals), public or not.</summary>
    internal const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
}
