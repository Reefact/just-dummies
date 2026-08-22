using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>
///     One <c>dum generate</c> run, as data — what <c>--format json</c> puts on stdout (§6.1).
/// </summary>
/// <remarks>
///     It exists because the exit code cannot carry it. §7 makes a file written with open parameters a
///     <b>success</b>, which is right for a developer — their own build reports the rest — and useless to a
///     script scaffolding forty types at once: exit <c>0</c> reads the same whether every parameter resolved
///     or a third of them did not. <see cref="RunSummary.OpenParameters" /> is that missing number, and the
///     per-parameter rows below are why it is what it is.
///     <para>
///         Assembled from the engine's own result model, never from the console's text: the recap of §6 and
///         this report are two renderings of one set of facts, which is what keeps them from drifting into two
///         answers. The provenance words come from <see cref="Recap.WordsFor" /> for exactly that reason.
///     </para>
/// </remarks>
internal sealed record RunReport(string Tool,
                                 string? Refusal,
                                 IReadOnlyList<ScaffoldReport> Results,
                                 RunSummary Summary) {

    /// <summary>A run that never reached its first scaffold, and the reason it did not.</summary>
    /// <remarks>
    ///     A document is produced anyway, so that <c>--format json</c> means "stdout carries one JSON document"
    ///     without exception. A script that has to tell an empty stdout from a failed run apart has been given
    ///     a contract with a hole in it.
    /// </remarks>
    internal static RunReport Refused(string refusal) {
        return new RunReport("dum", refusal, [],
                             new RunSummary(Scaffolded: 0, Failed: 0, OpenParameters: 0, ParametersToVerify: 0));
    }

    /// <summary>A run that scaffolded, whatever each of its arguments came to.</summary>
    internal static RunReport Of(IReadOnlyList<ScaffoldReport> results) {
        return new RunReport("dum",
                             Refusal: null,
                             results,
                             new RunSummary(results.Count(result => result.Status == nameof(ScaffoldStatus.Scaffolded)),
                                            results.Count(result => result.Status != nameof(ScaffoldStatus.Scaffolded)),
                                            results.Sum(result => result.OpenParameters),
                                            results.Sum(result => result.ParametersToVerify)));
    }

}
