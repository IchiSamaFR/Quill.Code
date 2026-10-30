using System.Reflection;

namespace Quill.Code.CSharp.Constants;

/// <summary>Constantes du runtime de script injecté dans les projets.</summary>
internal static class CSharpScriptConstants
{
    internal const string FileName = "QuillScriptRuntime.g.cs";

    internal const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
}
