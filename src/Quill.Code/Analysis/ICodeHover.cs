using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quill.Code.Analysis;

/// <summary>
/// Hover information source of an editor, on raw text: decouples the control from the engine (Roslyn…).
/// </summary>
public interface ICodeHover
{
    /// <summary>Information valid at <paramref name="position"/> in <paramref name="code"/>.</summary>
    Task<HoverInfo?> GetHoverAsync(string code, int position, CancellationToken cancellationToken = default);
}
