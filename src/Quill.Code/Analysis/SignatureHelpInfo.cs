namespace Quill.Code.Analysis;

/// <summary>Surcharges de la méthode dont le curseur est dans la liste d'arguments.</summary>
/// <param name="Signatures">Surcharges disponibles.</param>
/// <param name="ActiveSignature">Surcharge retenue par le compilateur, sinon la première qui accepte assez d'arguments.</param>
/// <param name="ActiveParameter">Index de l'argument sous le curseur (nombre de virgules qui le précèdent).</param>
/// <param name="ArgumentListStart">Position de la parenthèse ouvrante : identifie l'appel d'une frappe à l'autre.</param>
public sealed record SignatureHelpInfo(IReadOnlyList<SignatureInfo> Signatures, int ActiveSignature, int ActiveParameter, int ArgumentListStart);
