using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>One constructor parameter: what it resolved to, and where that came from (§6).</summary>
internal sealed record ParameterReport(string Name,
                                       string Type,
                                       string? Expression,
                                       bool Resolved,
                                       IReadOnlyList<string> Provenance,
                                       IReadOnlyList<string> Candidates) {

    internal static ParameterReport Of(ScaffoldedParameter parameter) {
        return new ParameterReport(parameter.Name,
                                   parameter.TypeDisplay,
                                   parameter.Expression,
                                   !parameter.IsUnresolved,
                                   Recap.WordsFor(parameter.Provenance),
                                   parameter.Candidates);
    }

}
