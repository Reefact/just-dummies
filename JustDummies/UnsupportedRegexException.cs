namespace JustDummies;

/// <summary>
///     Thrown when a pattern passed to <see cref="Any.StringMatching(string)" /> is well-formed but uses a construct
///     outside the <b>regular</b> subset the library generates from — a lookahead or lookbehind, a backreference, a
///     balancing group, a Unicode category, a word boundary. These constructs are either not regular (so no finite generator can honour
///     them) or deliberately out of scope; the library refuses to guess rather than silently emit a value that does
///     not actually match. A syntactically malformed pattern is a caller mistake and surfaces as an
///     <see cref="ArgumentException" /> instead.
/// </summary>
public sealed class UnsupportedRegexException : DummyException {

    #region Statics members declarations

    /// <summary>
    ///     Builds the exception for a construct the generator cannot honour while building a value — a lookaround, a
    ///     backreference, a balancing, conditional or atomic group. Nothing in the subset is equivalent to it, and composing
    ///     generated parts does not enforce what it requires, so the remedy says so rather than offer a rewrite.
    /// </summary>
    internal static UnsupportedRegexException NotHonourable(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "No rewrite within the supported subset is equivalent in general: generate the value another way, and keep it matching the pattern yourself; composing parts with As(...) or Any.Combine(...) does not enforce the pattern's requirement on its own.");
    }

    /// <summary>Builds the exception for a construct that contributes nothing to what a pattern matches — an inline comment.</summary>
    internal static UnsupportedRegexException AddsNothing(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position, "It adds nothing to a generated value: remove it.");
    }

    /// <summary>
    ///     Builds the exception for a way of naming characters the subset does not read — a Unicode category, a class
    ///     subtraction. A class of characters the construct accepts keeps every drawn value matching the original.
    /// </summary>
    internal static UnsupportedRegexException WrittenAsAClass(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "Write out, as an explicit class, characters it accepts, such as [A-Za-z] for letters; values are drawn from printable ASCII.");
    }

    /// <summary>
    ///     Builds the exception for a word boundary or its negation. It reads the characters on both sides of a position,
    ///     the edges of the value included, so removing it can admit values the pattern rejects (<c>\b!</c> matches no
    ///     value that starts with <c>!</c>).
    /// </summary>
    internal static UnsupportedRegexException WordBoundary(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "It depends on the characters on both sides of a position, at the edges of the value too, so removing it can admit values the pattern rejects: rewrite the pattern only once you have checked that the rewrite accepts no value the pattern rejects, or generate the value another way.");
    }

    /// <summary>Builds the exception for the contiguous-match anchor, which ties a match to where a previous one ended.</summary>
    internal static UnsupportedRegexException ContiguousMatch(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "It ties a match to where a previous one ended, which a single generated value knows nothing of: rewrite the pattern only once you have checked that the rewrite accepts no value the pattern rejects, or generate the value another way.");
    }

    /// <summary>
    ///     Builds the exception for an anchor away from an edge, which no whole value can satisfy. Removing it would turn a
    ///     pattern that matches nothing into one that matches something, so the remedy is about where it was meant to be.
    /// </summary>
    internal static UnsupportedRegexException MisplacedAnchor(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "No whole value can satisfy it there: if it was meant for an edge, move it to the start or end of the pattern, or of a top-level alternation branch.");
    }

    /// <summary>
    ///     Builds the exception for a scoped or later <c>(?i)</c>. The one honoured form, a leading <c>(?i)</c>, makes the
    ///     whole pattern case-insensitive, which is not what a scoped one asks; explicit classes say it exactly.
    /// </summary>
    internal static UnsupportedRegexException ScopedCaseInsensitivity(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "Only one leading (?i), on its own, is honoured, and it makes the whole pattern case-insensitive: for a scoped or later one, write the affected letters as explicit classes, such as [Bb] for b.");
    }

    /// <summary>Builds the exception for an option group other than case-insensitivity, which the subset honours no form of.</summary>
    internal static UnsupportedRegexException UnhonouredOption(string pattern, string construct, int position) {
        return Sentence(pattern, construct, position,
                        "The supported subset honours no other option and offers no equivalent: rewrite the pattern only once you have checked that the rewrite accepts no value the pattern rejects, or generate the value another way.");
    }

    /// <summary>
    ///     Builds the exception for a negated character class that excludes the whole alphabet the library draws from.
    /// </summary>
    /// <remarks>
    ///     A universe limit rather than a grammar limit, which is why it has a name of its own: the class is regular and
    ///     the real engine compiles it, so refusing it as malformed would claim the caller wrote a broken pattern. What
    ///     it excludes is every character JustDummies can produce, and the remedy is about that range rather than about
    ///     the supported subset.
    /// </remarks>
    internal static UnsupportedRegexException EmptyNegatedClass(string pattern, int position) {
        return Sentence(pattern, "a negated character class that excludes every character JustDummies can generate (printable ASCII U+0020 to U+007E)", position,
                        "It draws values from printable ASCII; express the requirement with characters inside that range, or generate the value another way.");
    }

    /// <summary>
    ///     Writes the refusal sentence both factories share, naming the construct, where it occurs, and what to do
    ///     instead.
    /// </summary>
    /// <remarks>
    ///     Private on purpose: it names the grammar of the message, not a failure, so every caller is a named case
    ///     above. Nothing here guards its arguments — building an exception must never throw, or the failure being
    ///     reported is replaced by a failure about reporting it (ADR-0024, which exempts exception types for exactly
    ///     that reason).
    /// </remarks>
    private static UnsupportedRegexException Sentence(string pattern, string construct, int position, string remedy) {
        return new UnsupportedRegexException($"The regular expression pattern \"{pattern}\" uses {construct} at position {position}, which JustDummies cannot generate from. {remedy}");
    }

    #endregion

    /// <summary>
    ///     Initializes a new instance of the <see cref="UnsupportedRegexException" /> class.
    /// </summary>
    /// <param name="message">A description naming the unsupported construct and where it occurs.</param>
    public UnsupportedRegexException(string message) : base(message) { }

}
