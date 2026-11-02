using System.Text;

namespace InternationalCatalogue.Domain;

/// <summary>
/// A title or subtitle in any script, stored in Unicode normalisation form C. Its length counts grapheme
/// clusters, not UTF-16 code units. Two titles are equal when they match under NFC and simple case folding,
/// bidirectional controls aside.
/// </summary>
public sealed class Title : IEquatable<Title>
{
    /// <summary>The longest title, in grapheme clusters.</summary>
    public const int MaxLength = 250;

    private readonly string _matchKey;

    /// <param name="value">The title text; leading and trailing white space is trimmed.</param>
    /// <param name="allowBidiControls">
    /// Declares that the title carries bidirectional controls (to keep an embedded run in its own direction).
    /// Even then only the marks (LRM, RLM, ALM) and balanced isolates (LRI, RLI, FSI … PDI) are accepted.
    /// </param>
    public Title(string value, bool allowBidiControls = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        string text = UnicodeText.ToNfc(value, nameof(value)).Trim();
        if (text.Length == 0)
        {
            throw new ArgumentException("A title cannot be empty or blank.", nameof(value));
        }

        if (UnicodeText.StartsWithCombiningMark(text))
        {
            throw new ArgumentException("A title cannot start with a combining mark.", nameof(value));
        }

        int openIsolates = 0;
        bool hasBidiControls = false;
        foreach (Rune rune in text.EnumerateRunes())
        {
            if (UnicodeText.IsLineBreakOrControl(rune))
            {
                throw new ArgumentException($"A title cannot contain the control or line-break character U+{rune.Value:X4}.", nameof(value));
            }

            if (UnicodeText.IsEmoji(rune))
            {
                throw new ArgumentException($"A title cannot contain an emoji (U+{rune.Value:X4}).", nameof(value));
            }

            if (!UnicodeText.IsBidiControl(rune))
            {
                continue;
            }

            if (!allowBidiControls)
            {
                throw new ArgumentException($"Bidirectional control U+{rune.Value:X4} is only allowed in a title declared to carry them.", nameof(value));
            }

            if (UnicodeText.IsEmbeddingOrOverride(rune))
            {
                throw new ArgumentException($"Bidirectional embeddings and overrides (U+{rune.Value:X4}) are never allowed; use isolates.", nameof(value));
            }

            hasBidiControls = true;
            if (UnicodeText.IsIsolateInitiator(rune))
            {
                openIsolates++;
            }
            else if (UnicodeText.IsPopDirectionalIsolate(rune))
            {
                if (openIsolates == 0)
                {
                    throw new ArgumentException("A pop-directional-isolate (U+2069) closes no open isolate.", nameof(value));
                }

                openIsolates--;
            }
        }

        if (openIsolates != 0)
        {
            throw new ArgumentException($"{openIsolates} directional isolate(s) are left open.", nameof(value));
        }

        int length = UnicodeText.GraphemeCount(text);
        if (length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(value), length, $"A title has at most {MaxLength} grapheme clusters.");
        }

        Value = text;
        Length = length;
        HasBidiControls = hasBidiControls;
        _matchKey = UnicodeText.MatchKey(text);
    }

    /// <summary>The title in NFC, trimmed.</summary>
    public string Value { get; }

    /// <summary>The number of grapheme clusters (user-perceived characters).</summary>
    public int Length { get; }

    public bool HasBidiControls { get; }

    /// <summary>The scripts of the title's letters; an unsupported script's letters yield no entry.</summary>
    public IReadOnlySet<WritingScript> LetterScripts()
    {
        HashSet<WritingScript> scripts = [];
        foreach (Rune rune in Value.EnumerateRunes())
        {
            if (UnicodeText.ScriptOfLetter(rune) is WritingScript script)
            {
                scripts.Add(script);
            }
        }

        return scripts;
    }

    /// <summary>Whether some letter belongs to no script the catalogue supports.</summary>
    public bool HasLettersOfUnsupportedScript() =>
        Value.EnumerateRunes().Any(rune => Rune.IsLetter(rune) && UnicodeText.ScriptOfLetter(rune) is null);

    /// <summary>Whether the first letter is Arabic or Hebrew; a title without letters reads left to right.</summary>
    public bool IsRightToLeft()
    {
        foreach (Rune rune in Value.EnumerateRunes())
        {
            if (Rune.IsLetter(rune))
            {
                return UnicodeText.ScriptOfLetter(rune) is WritingScript.Arabic or WritingScript.Hebrew;
            }
        }

        return false;
    }

    public bool Equals(Title? other) => other is not null && string.Equals(other._matchKey, _matchKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Title);

    public override int GetHashCode() => _matchKey.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
