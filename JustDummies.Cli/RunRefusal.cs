using System.Collections.Generic;
using System.Linq;

using JustDummies.GenDummy;

namespace JustDummies.Cli;

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
