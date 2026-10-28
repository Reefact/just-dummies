using System;
using System.Globalization;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.GenAny;

/// <summary>
///     Where the first thing the engine could not vouch for sits, for a parameter marked <c>unread guards</c>.
/// </summary>
/// <remarks>
///     A mark alone sent the developer to read the whole constructor: on a parameter with three guards, which of
///     them was it? The place and one line of the source answer that, in the emitted file's comment, the recap
///     and the machine report alike (§5.6).
/// </remarks>
public sealed class UnreadGuard {

    /// <summary>How much of the line is kept: enough to recognise the guard, short enough to stay one line.</summary>
    private const int ExcerptLength = 100;

    /// <summary>
    ///     Declares where an unread guard sits.
    /// </summary>
    /// <param name="file">The source file's name, without its folder, or null when the tree has none.</param>
    /// <param name="line">The one-based line the guard starts on.</param>
    /// <param name="excerpt">The guard's first line of source, trimmed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="excerpt" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="line" /> is below one.</exception>
    public UnreadGuard(string? file, int line, string excerpt) {
        if (excerpt is null) { throw new ArgumentNullException(nameof(excerpt)); }
        if (line < 1) { throw new ArgumentOutOfRangeException(nameof(line), line, "A line is counted from one."); }

        File    = file;
        Line    = line;
        Excerpt = excerpt;
    }

    /// <summary>The source file's name, without its folder, or null when the tree has none.</summary>
    public string? File { get; }

    /// <summary>The one-based line the guard starts on.</summary>
    public int Line { get; }

    /// <summary>The guard's first line of source, trimmed, and shortened past a hundred characters.</summary>
    public string Excerpt { get; }

    /// <summary>Where it sits, as a reader would look for it — <c>Order.cs, line 14</c>.</summary>
    public string Place() {
        string line = "line " + Line.ToString(CultureInfo.InvariantCulture);

        return File is null ? line : File + ", " + line;
    }

    /// <summary>The guard at <paramref name="node" />, shown as the statement that holds it.</summary>
    /// <remarks>
    ///     A condition or a call reads better inside its statement — <c>if (…) { throw … }</c> says what the
    ///     condition alone leaves implicit. A hand-off in a constructor initializer has no statement, and is shown
    ///     as itself.
    /// </remarks>
    internal static UnreadGuard At(SyntaxNode node) {
        SyntaxNode           shown = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault() ?? node;
        FileLinePositionSpan span  = shown.GetLocation().GetLineSpan();
        string               first = shown.ToString().Split('\n')[0].Trim();

        return new UnreadGuard(span.Path.Length == 0 ? null : Path.GetFileName(span.Path),
                               span.StartLinePosition.Line + 1,
                               first.Length <= ExcerptLength ? first : first.Substring(0, ExcerptLength) + "...");
    }

}
