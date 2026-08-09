using System;
using System.IO;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     Builds the consoles the tool writes through.
/// </summary>
/// <remarks>
///     One place rather than two, because the width below is not a preference: a console built without it renders
///     a redirected run as an ellipsis and no text at all. The process and the suite must therefore build their
///     consoles the same way, or the suite reads output the tool never produces.
/// </remarks>
internal static class ToolConsole {

    /// <summary>
    ///     The width assumed when the terminal reports none. 80 columns is the conventional fallback, and it is
    ///     the width the help layout is read at.
    /// </summary>
    private const int RedirectedWidth = 80;

    /// <summary>
    ///     Below this, a reported width is not a narrow terminal but an absent one — a pipe, a file, a CI log,
    ///     a test's <see cref="StringWriter" />. Taken literally it wraps every line to nothing, so a redirected
    ///     `dum --help` prints `……` and stops. Measured, not guessed.
    /// </summary>
    private const int NarrowestRealTerminal = 20;

    /// <summary>
    ///     A console writing to <paramref name="writer" />, readable whether or not a terminal is attached.
    /// </summary>
    internal static IAnsiConsole On(TextWriter writer) {
        if (writer is null) { throw new ArgumentNullException(nameof(writer)); }

        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) });

        if (console.Profile.Width < NarrowestRealTerminal) { console.Profile.Width = RedirectedWidth; }

        return console;
    }

    /// <summary>The width a console falls back to when no terminal reports one.</summary>
    internal static int WidthWhenRedirected => RedirectedWidth;

}
