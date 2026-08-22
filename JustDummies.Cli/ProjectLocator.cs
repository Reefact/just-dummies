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
