using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

/// <summary>One type argument, and everything the run has to say about it.</summary>
internal sealed record ScaffoldReport(string Argument,
                                      string Status,
                                      string? Type,
                                      string? Generator,
                                      IReadOnlyList<FileReport> Files,
                                      IReadOnlyList<ParameterReport> Parameters,
                                      int OpenParameters,
                                      int ParametersToVerify,
                                      EntryPointReport? EntryPoint,
                                      IReadOnlyList<WarningReport> Warnings,
                                      IReadOnlyList<string> Candidates) {

    /// <summary>
    ///     An argument that produced nothing, carrying the engine's reason and whatever that reason leaves
    ///     the developer to settle — type names for §3.2, tied static factories for §5.1.2.
    /// </summary>
    internal static ScaffoldReport Refused(string argument, ScaffoldOutcome outcome) {
        return new ScaffoldReport(argument,
                                  outcome.Status.ToString(),
                                  Type: null,
                                  Generator: null,
                                  Files: [],
                                  Parameters: [],
                                  OpenParameters: 0,
                                  ParametersToVerify: 0,
                                  EntryPoint: null,
                                  Warnings: [],
                                  outcome.Candidates);
    }

    /// <summary>
    ///     The status of an argument the engine scaffolded and the shell then refused to write, because a file
    ///     already stood where it would have gone and <c>--force</c> was not given (§6.1).
    /// </summary>
    /// <remarks>
    ///     The shell's own status rather than the engine's: the engine did scaffold, so copying its
    ///     <c>Scaffolded</c> into the report counted a success the exit code had already called a failure.
    /// </remarks>
    internal const string FileExists = "FileExists";

    /// <summary>
    ///     An argument whose files were not written, each kept with the path it would have taken — and at
    ///     least one of them already stands there.
    /// </summary>
    /// <remarks>
    ///     Nothing landed, so no parameter is reported open or to verify: the summary counts what the run
    ///     left in the working tree, and this argument left nothing.
    /// </remarks>
    internal static ScaffoldReport NotWritten(string argument, ScaffoldOutcome outcome, IReadOnlyList<FileReport> files) {
        ScaffoldPlan plan = outcome.Plan!;

        return new ScaffoldReport(argument,
                                  FileExists,
                                  FullNameOf(plan),
                                  plan.GeneratorName,
                                  files,
                                  Parameters: [],
                                  OpenParameters: 0,
                                  ParametersToVerify: 0,
                                  EntryPoint: null,
                                  Warnings: [],
                                  Candidates: []);
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
                                  plan.Parameters.Count(parameter => parameter.RequiresVerification),
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
