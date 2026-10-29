using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quill.Code.Analysis;

/// <summary>
/// Source d'information au survol d'un éditeur, sur le texte brut : découple le contrôle du moteur (Roslyn…).
/// </summary>
public interface ICodeHover
{
    /// <summary>Informations valables à <paramref name="position"/> dans <paramref name="code"/>.</summary>
    Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default);
}
