namespace Quill.Code.Execution;

/// <summary>Lot d'un script, execute en une commande.</summary>
/// <param name="Text">Texte envoye au serveur.</param>
/// <param name="StartLine">Ligne du script (base 1) ou commence le lot, pour situer ses erreurs.</param>
public sealed record QueryBatch(string Text, int StartLine);
