namespace Quill.Code.CSharp.Constants;

/// <summary>C# diagnostic constants.</summary>
internal static class CSharpDiagnosticConstants
{
    // Au-delà, une erreur en amont fait cascader les suivantes : les premières suffisent.
    internal const int MaxDiagnostics = 100;
}
