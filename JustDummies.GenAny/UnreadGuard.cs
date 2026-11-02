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
    /// <param name="note">
    ///     Why this place marks the parameter, where the excerpt alone does not say — the trail-lost mark of
    ///     ADR-0110, which marks every parameter — or null where the guard speaks for itself.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="excerpt" /> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="line" /> is below one.</exception>
    /// <param name="firstLine">
    ///     The guard's whole first line, marked where the statement goes on below it, for the file's comment,
    ///     which has room for it; null where <paramref name="excerpt" /> is the whole of it.
    /// </param>
    /// <param name="further">How many more places earned the parameter the same mark (#373).</param>
    /// <param name="pattern">The regex pattern the guard tests, where it is one and the pattern is a constant (#374).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="further" /> is negative.</exception>
    public UnreadGuard(string? file, int line, string excerpt, string? note = null, string? firstLine = null, int further = 0, string? pattern = null) {
        if (excerpt is null) { throw new ArgumentNullException(nameof(excerpt)); }
        if (line < 1) { throw new ArgumentOutOfRangeException(nameof(line), line, "A line is counted from one."); }
        if (further < 0) { throw new ArgumentOutOfRangeException(nameof(further), further, "A count of further guards is zero or more."); }

        File      = file;
        Line      = line;
        Excerpt   = excerpt;
        Note      = note;
        FirstLine = firstLine ?? excerpt;
        Further   = further;
        Pattern   = pattern;
    }

    /// <summary>
    ///     The guard's whole first line, trimmed, ending in <c>...</c> where the statement goes on below it —
    ///     what the emitted comment quotes, since a file has room for it where a console row does not (#372).
    /// </summary>
    public string FirstLine { get; }

    /// <summary>
    ///     How many more places earned the parameter the same mark: a developer who resolves the one named and
    ///     deletes the sentinel would otherwise get a generator the constructor still refuses (#373).
    /// </summary>
    public int Further { get; }

    /// <summary>
    ///     The pattern a regex guard tests the parameter against, where the guard is one and its pattern is a
    ///     constant — written in the comment, since the remedy starts from it (#374); null otherwise.
    /// </summary>
    public string? Pattern { get; }

    /// <summary>The same place, with <see cref="Further" /> set once every mark is in.</summary>
    public UnreadGuard WithFurther(int further) {
        return new UnreadGuard(File, Line, Excerpt, Note, FirstLine, further, Pattern);
    }

    /// <summary>The source file's name, without its folder, or null when the tree has none.</summary>
    public string? File { get; }

    /// <summary>The one-based line the guard starts on.</summary>
    public int Line { get; }

    /// <summary>The guard's first line of source, trimmed, and shortened past a hundred characters.</summary>
    public string Excerpt { get; }

    /// <summary>
    ///     Why this place marks the parameter, where the excerpt alone does not say so, or null where it does.
    /// </summary>
    /// <remarks>
    ///     The one mark that needs it is the fallback of ADR-0110: a throw decided by a value the engine cannot
    ///     trace marks <b>every</b> parameter, so the place named may test none of the parameter's own, and a
    ///     reader sent there with no word about why would look for a guard on it that is not there (#371). The
    ///     recap, the emitted comment and the machine report carry it alike.
    /// </remarks>
    public string? Note { get; }

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
    internal static UnreadGuard At(SyntaxNode node, string? note = null, string? pattern = null) {
        SyntaxNode           shown = node.AncestorsAndSelf().OfType<StatementSyntax>().FirstOrDefault() ?? node;
        FileLinePositionSpan span  = shown.GetLocation().GetLineSpan();
        string               whole = shown.ToString();
        string               first = whole.Split('\n')[0].Trim();

        // A cut at the end of the first line is marked as a cut past a hundred characters is, so the quote never
        // reads as the whole guard (#372).
        if (whole.IndexOf('\n') >= 0) { first += " ..."; }

        return new UnreadGuard(span.Path.Length == 0 ? null : Path.GetFileName(span.Path),
                               span.StartLinePosition.Line + 1,
                               first.Length <= ExcerptLength ? first : first.Substring(0, ExcerptLength) + "...",
                               note,
                               first,
                               pattern: pattern);
    }

}
