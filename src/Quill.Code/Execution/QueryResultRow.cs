namespace Quill.Code.Execution;

/// <summary>
/// Ligne d'un jeu de resultats, deja convertie en texte affichable.
/// Une cellule null represente un NULL SQL.
/// </summary>
/// <param name="Number">Numero de la ligne (base 1).</param>
/// <param name="Cells">Valeurs, dans l'ordre des colonnes.</param>
public sealed record QueryResultRow(int Number, string?[] Cells);
