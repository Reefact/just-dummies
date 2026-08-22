using System.Collections.Generic;
using System.Linq;

using JustDummies.GenAny;

namespace JustDummies.Cli;

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
