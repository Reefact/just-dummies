using System;

using Spectre.Console;

namespace JustDummies.Cli;

/// <summary>
///     Writing to a console without Spectre's two conveniences: wrapping and markup.
/// </summary>
/// <remarks>
///     Both are wrong for what this tool prints. Spectre wraps at the terminal's width, which reads well for a
///     sentence and destroys a path or a table — and a path is exactly what a failure names. Markup is worse
///     than wrong: a type name or a file name carrying a <c>[</c> would be read as a tag, so the tool would
///     mangle its own error message, or throw while writing it. Going through the profile's writer means
///     nothing has to be escaped, and it fixes the line ending for the same reason the emitter fixes its own
///     (§8.1) — so what the tool says reads the same, and is checkable the same, on every platform.
/// </remarks>
internal static class Unwrapped {

    /// <summary>Writes one line, ending in the newline every platform reads the same.</summary>
    internal static void Line(IAnsiConsole console, string text) {
        Text(console, text);
        console.Profile.Out.Writer.Write('\n');
    }

    /// <summary>Writes text exactly as it stands, adding nothing to it.</summary>
    /// <remarks>
    ///     Which is what <c>--dry-run</c> needs: the file it prints to stdout must be the bytes that would have
    ///     been written to disk, or piping it into one would not be the same thing as running without the flag.
    /// </remarks>
    internal static void Text(IAnsiConsole console, string text) {
        ArgumentNullException.ThrowIfNull(console);
        ArgumentNullException.ThrowIfNull(text);

        console.Profile.Out.Writer.Write(text);
    }

}
