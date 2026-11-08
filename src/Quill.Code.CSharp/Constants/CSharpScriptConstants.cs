using System.Reflection;

namespace Quill.Code.CSharp.Constants;

/// <summary>Constants of the script runtime injected into projects.</summary>
internal static class CSharpScriptConstants
{
    internal const string FileName = "QuillScriptRuntime.g.cs";

    internal const BindingFlags StaticMembers = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
}
