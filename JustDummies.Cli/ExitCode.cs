using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>
///     The process exit codes the tool returns.
/// </summary>
/// <remarks>
///     <see cref="Success" /> and the failure code of the specification's §7 belong to <c>generate</c>, and
///     describe how a scaffolding run ended. <see cref="Usage" /> belongs to the command line itself — it says the
///     tool never got as far as running anything — which is why it is a third value rather than a reuse of §7's
///     <c>1</c>: an invocation that could not start is not a scaffolding failure.
/// </remarks>
internal static class ExitCode {

    /// <summary>The command ran and did what it was asked.</summary>
    internal const int Success = 0;

    /// <summary>
    ///     A scaffolding run failed: nothing was written, and the console said why (§7).
    /// </summary>
    /// <remarks>
    ///     One code for every failure, deliberately. §7 lists eight of them — a type not found, an ambiguous
    ///     name, an existing file without <c>--force</c>, a project that will not load — and they differ in what
    ///     the developer reads, not in what a script does about them.
    /// </remarks>
    internal const int Failed = 1;

    /// <summary>The command line was not understood, or names something this build cannot do.</summary>
    internal const int Usage = 2;

    /// <summary>
    ///     What one scaffold exits with.
    /// </summary>
    /// <remarks>
    ///     A file carrying TODOs is a <b>success</b>: the write succeeded, and the developer's own build reports
    ///     the rest — which is the whole mechanism of ADR-0060. A warning does not change it either; under
    ///     design rule 4 the decision it raises is the developer's.
    /// </remarks>
    internal static int For(ScaffoldOutcome outcome) {
        return outcome.Succeeded ? Success : Failed;
    }

    /// <summary>
    ///     What a run over several types exits with: the worst of them (§7).
    /// </summary>
    /// <remarks>
    ///     The types are processed independently, so one failure does not stop the others being written — but
    ///     it does have to reach the caller, or a script would read a partial run as a whole one.
    /// </remarks>
    internal static int Worst(IEnumerable<int> codes) {
        List<int> reported = [.. codes];

        return reported.Count == 0 ? Success : reported.Max();
    }

}
