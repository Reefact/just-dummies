using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JustDummies.Cli;

/// <summary>
///     Which project's compilation is analyzed (§3.1).
/// </summary>
/// <remarks>
///     The tool is run from the <b>test</b> project, because that is where the scaffolded file belongs: the
///     test project references the production one, so the target type is reachable from its compilation, and
///     <c>--output</c>'s default puts the file next to the tests that will use it.
/// </remarks>
internal static class ProjectLocator {

    private const string ProjectPattern = "*.csproj";

    /// <summary>
    ///     The project to open: the one <paramref name="explicitPath" /> names, or the only one in
    ///     <paramref name="directory" />.
    /// </summary>
    /// <remarks>
    ///     None or several is a refusal, not a guess. Picking the alphabetically first would scaffold against
    ///     a compilation the developer did not choose, and the file would look right while resolving the wrong
    ///     types — the kind of wrong that is found much later.
    /// </remarks>
    internal static ProjectChoice Locate(string directory, string? explicitPath) {
        if (explicitPath is not null) {
            string named = Path.GetFullPath(explicitPath);

            return File.Exists(named)
                       ? ProjectChoice.At(named)
                       : ProjectChoice.None($"No project at {named}.");
        }

        string[] found = Directory.Exists(directory)
                             ? [.. Directory.GetFiles(directory, ProjectPattern).OrderBy(path => path, StringComparer.Ordinal)]
                             : [];

        return found.Length switch {
            1 => ProjectChoice.At(found[0]),
            0 => ProjectChoice.None($"No {ProjectPattern} in {directory}. Run dum from a project directory, "
                                  + "or name one with --project."),
            _ => ProjectChoice.Between(found)
        };
    }

}

/// <summary>What <see cref="ProjectLocator" /> concluded: one project, or the reason there is not one.</summary>
internal sealed class ProjectChoice {

    private ProjectChoice(string? path, string? refusal, IReadOnlyList<string> candidates) {
        Path       = path;
        Refusal    = refusal;
        Candidates = candidates;
    }

    /// <summary>The project to open, or null when there is none to open.</summary>
    internal string? Path { get; }

    /// <summary>Why there is none, in one sentence for the console.</summary>
    internal string? Refusal { get; }

    /// <summary>The projects that were found, when there were too many.</summary>
    internal IReadOnlyList<string> Candidates { get; }

    /// <summary>Whether exactly one project was settled on.</summary>
    internal bool Found => Path is not null;

    internal static ProjectChoice At(string path) {
        return new ProjectChoice(path, refusal: null, candidates: []);
    }

    internal static ProjectChoice None(string refusal) {
        return new ProjectChoice(path: null, refusal, candidates: []);
    }

    internal static ProjectChoice Between(IReadOnlyList<string> candidates) {
        return new ProjectChoice(path: null,
                                 "Several projects here; name the one to analyze with --project.",
                                 candidates);
    }

}
