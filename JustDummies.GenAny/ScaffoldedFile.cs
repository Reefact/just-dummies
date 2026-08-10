using System;
using System.Diagnostics.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     What the emitter produces: a file name and its full text, never a path.
/// </summary>
/// <remarks>
///     The engine performs no IO (§10.2). Where this lands — the current directory, an <c>--output</c>, stdout
///     under <c>--dry-run</c> — is the shell's decision, and an IDE consumer will apply the text without writing
///     a file at all.
/// </remarks>
public sealed class ScaffoldedFile {

    internal ScaffoldedFile(string fileName, string sourceText, bool containsTodo) {
        FileName     = fileName;
        SourceText   = sourceText;
        ContainsTodo = containsTodo;
    }

    /// <summary>The file's name, with its extension — <c>AnyOrder.cs</c>.</summary>
    public string FileName { get; }

    /// <summary>The file's full text, ending in a newline.</summary>
    public string SourceText { get; }

    /// <summary>
    ///     Whether the text carries at least one TODO, and therefore does not compile until the developer acts.
    /// </summary>
    /// <remarks>
    ///     Carried as a flag rather than left for a caller to find by searching the text: the console says so in
    ///     its closing line (§6), and searching emitted source for a marker is exactly the kind of check that
    ///     stops working the day the marker is reworded.
    /// </remarks>
    [SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = SuppressionJustification.S1135.DocumentsTheMarkerTheToolEmits)]
    public bool ContainsTodo { get; }

    /// <inheritdoc />
    public override string ToString() {
        return FileName + (ContainsTodo ? " (with TODOs)" : string.Empty);
    }

}
