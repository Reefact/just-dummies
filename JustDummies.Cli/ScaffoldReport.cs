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
