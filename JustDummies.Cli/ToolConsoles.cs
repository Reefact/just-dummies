using System;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     Where the tool writes: what the caller asked for, and what went wrong.
/// </summary>
/// <remarks>
///     Two, from the start, because §6 splits them: <c>--dry-run</c> prints the file to stdout and the recap to
///     stderr, so a developer can pipe one into a file while still reading the other.
/// </remarks>
internal sealed class ToolConsoles {

    internal ToolConsoles(IAnsiConsole output, IAnsiConsole error) {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        Error  = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>What the caller asked for.</summary>
    internal IAnsiConsole Output { get; }

    /// <summary>What went wrong, and the recap when stdout is carrying the file.</summary>
    internal IAnsiConsole Error { get; }

}
