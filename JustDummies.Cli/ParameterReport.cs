using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>One constructor parameter: what it resolved to, and where that came from (§6).</summary>
internal sealed record ParameterReport(string Name,
                                       string Type,
                                       string? Expression,
                                       bool Resolved,
                                       bool RequiresVerification,
                                       IReadOnlyList<string> Provenance) {

    /// <summary>
    ///     A row states its own two outcomes, so the summary's counts can be checked against the rows.
    /// </summary>
    /// <remarks>
    ///     <c>resolved</c> and <c>requiresVerification</c> are not opposites: a parameter the engine inferred
    ///     but cannot vouch for (§5.6) is <b>resolved and to be verified at once</b>, and its file does not
    ///     compile until the developer acts. Reading <c>resolved: true</c> as "nothing to do here" is exactly
    ///     the silence ADR-0083 removed, so the second flag says it rather than leaving a script to infer it
    ///     from the provenance words.
    /// </remarks>
    internal static ParameterReport Of(ScaffoldedParameter parameter) {
        return new ParameterReport(parameter.Name,
                                   parameter.TypeDisplay,
                                   parameter.Expression,
                                   !parameter.IsUnresolved,
                                   parameter.RequiresVerification,
                                   Recap.WordsFor(parameter.Provenance));
    }

}
