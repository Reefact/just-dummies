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
        return new RunReport("dum", refusal, [], new RunSummary(Scaffolded: 0, Failed: 0, OpenParameters: 0));
    }

    /// <summary>A run that scaffolded, whatever each of its arguments came to.</summary>
    internal static RunReport Of(IReadOnlyList<ScaffoldReport> results) {
        return new RunReport("dum",
                             Refusal: null,
                             results,
                             new RunSummary(results.Count(result => result.Status == nameof(ScaffoldStatus.Scaffolded)),
                                            results.Count(result => result.Status != nameof(ScaffoldStatus.Scaffolded)),
                                            results.Sum(result => result.OpenParameters)));
    }

}

/// <summary>What a run came to, in the three numbers a script branches on.</summary>
internal sealed record RunSummary(int Scaffolded, int Failed, int OpenParameters);

/// <summary>
///     Why a run stopped before its first scaffold — the rows of §7 that are about the project or the command
///     line rather than about a type.
/// </summary>
/// <remarks>
///     Named constants, and part of the contract: a script branching on these reads a value that does not
///     change when the sentence a developer reads is reworded.
/// </remarks>
internal static class RunRefusal {

    /// <summary>No project to analyze, or too many to choose from (§3.1).</summary>
    internal const string NoProject = "NoProject";

    /// <summary>The project would not open, and its diagnostics went to stderr verbatim.</summary>
    internal const string ProjectDidNotLoad = "ProjectDidNotLoad";

    /// <summary><c>--entry-point any</c> against a project that cannot compile what it would write (§4.5).</summary>
    internal const string LanguageVersionTooLow = "LanguageVersionTooLow";

    /// <summary>A <c>dum.json</c> that could not be read, or whose values were refused (§3.3).</summary>
    internal const string UnreadableDefaults = "UnreadableDefaults";

}

/// <summary>One type argument, and everything the run has to say about it.</summary>
internal sealed record ScaffoldReport(string Argument,
                                      string Status,
                                      string? Type,
                                      string? Generator,
                                      IReadOnlyList<FileReport> Files,
                                      IReadOnlyList<ParameterReport> Parameters,
                                      int OpenParameters,
                                      EntryPointReport? EntryPoint,
                                      IReadOnlyList<WarningReport> Warnings,
                                      IReadOnlyList<string> Candidates) {

    /// <summary>An argument that produced nothing, carrying the engine's reason and its candidates (§3.2).</summary>
    internal static ScaffoldReport Refused(string argument, ScaffoldOutcome outcome) {
        return new ScaffoldReport(argument,
                                  outcome.Status.ToString(),
                                  Type: null,
                                  Generator: null,
                                  Files: [],
                                  Parameters: [],
                                  OpenParameters: 0,
                                  EntryPoint: null,
                                  Warnings: [],
                                  outcome.Candidates);
    }

    /// <summary>An argument that scaffolded, with the files it produced and where each of them went.</summary>
    internal static ScaffoldReport Of(string argument, ScaffoldOutcome outcome, IReadOnlyList<FileReport> files) {
        ScaffoldPlan plan = outcome.Plan!;

        return new ScaffoldReport(argument,
                                  outcome.Status.ToString(),
                                  FullNameOf(plan),
                                  plan.GeneratorName,
                                  files,
                                  [.. plan.Parameters.Select(ParameterReport.Of)],
                                  plan.Parameters.Count(parameter => parameter.IsUnresolved),
                                  outcome.EntryPoint is null
                                      ? null
                                      : new EntryPointReport(outcome.EntryPoint.File.FileName, outcome.EntryPoint.Call),
                                  [.. outcome.Warnings.Select(WarningReport.Of)],
                                  Candidates: []);
    }

    private static string FullNameOf(ScaffoldPlan plan) {
        return plan.Target.Namespace is null ? plan.Target.Name : plan.Target.Namespace + "." + plan.Target.Name;
    }

}

/// <summary>
///     A file the run produced, and what became of it.
/// </summary>
/// <remarks>
///     <see cref="Path" /> and <see cref="Text" /> are the two halves of one question and never both answered:
///     a written file carries where it went, a <c>--dry-run</c> file carries what it would have been. Under
///     <c>--format json</c> the text has nowhere else to go — stdout is carrying the document — so it comes
///     back here rather than being lost.
/// </remarks>
internal sealed record FileReport(string Name, string? Path, bool Written, string? Text) {

    internal static FileReport WrittenTo(string name, string path) {
        return new FileReport(name, path, Written: true, Text: null);
    }

    internal static FileReport Printed(ScaffoldedFile file) {
        return new FileReport(file.FileName, Path: null, Written: false, file.SourceText);
    }

}

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

/// <summary>The entry point emitted beside the generator, and the call it opened (§4.5).</summary>
internal sealed record EntryPointReport(string File, string Call);

/// <summary>A warning the run carried without stopping — the shadowing row of §7.</summary>
internal sealed record WarningReport(string Kind, string Subject, string Other) {

    internal static WarningReport Of(ScaffoldWarning warning) {
        return new WarningReport(warning.Kind.ToString(), warning.Subject, warning.Other);
    }

}
