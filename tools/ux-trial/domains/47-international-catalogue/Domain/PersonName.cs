using System.Globalization;
using System.Text;

namespace InternationalCatalogue.Domain;

/// <summary>
/// A contributor's name: a family name and optional given names, in NFC, both in one supported script.
/// Equality is under NFC and simple case folding.
/// </summary>
public sealed class PersonName : IEquatable<PersonName>
{
    /// <summary>The longest name part, in grapheme clusters.</summary>
    public const int MaxPartLength = 100;

    private readonly string _matchKey;

    public PersonName(string familyName, string? givenNames = null)
    {
        ArgumentNullException.ThrowIfNull(familyName);
        string family = CheckPart(familyName, nameof(familyName), out WritingScript familyScript);
        string? given = null;
        if (givenNames is not null)
        {
            given = CheckPart(givenNames, nameof(givenNames), out WritingScript givenScript);
            if (givenScript != familyScript)
            {
                throw new ArgumentException($"Given names are written in {givenScript} but the family name in {familyScript}; record both in one script.", nameof(givenNames));
            }
        }

        FamilyName = family;
        GivenNames = given;
        Script = familyScript;
        _matchKey = UnicodeText.MatchKey(given is null ? family : family + "\u0000" + given);
    }

    public string FamilyName { get; }

    public string? GivenNames { get; }

    public WritingScript Script { get; }

    /// <summary>The heading form: <c>Family, Given</c>; for CJK names family then given names with no separator.</summary>
    public string Heading => GivenNames is null ? FamilyName
        : Script == WritingScript.Cjk ? FamilyName + GivenNames
        : FamilyName + ", " + GivenNames;

    private static string CheckPart(string raw, string paramName, out WritingScript script)
    {
        string part = UnicodeText.ToNfc(raw, paramName).Trim();
        if (part.Length == 0)
        {
            throw new ArgumentException("A name part cannot be empty or blank.", paramName);
        }

        if (UnicodeText.StartsWithCombiningMark(part))
        {
            throw new ArgumentException("A name cannot start with a combining mark.", paramName);
        }

        if (part.Contains("  ", StringComparison.Ordinal))
        {
            throw new ArgumentException("A name cannot contain consecutive spaces.", paramName);
        }

        WritingScript? found = null;
        foreach (Rune rune in part.EnumerateRunes())
        {
            if (Rune.IsLetter(rune))
            {
                WritingScript letterScript = UnicodeText.ScriptOfLetter(rune)
                    ?? throw new ArgumentException($"Letter U+{rune.Value:X4} belongs to no supported script.", paramName);
                if (found is not null && found != letterScript)
                {
                    throw new ArgumentException($"A name mixes {found} and {letterScript} letters.", paramName);
                }

                found = letterScript;
            }
            else if (!IsAllowedNonLetter(rune))
            {
                throw new ArgumentException($"A name cannot contain U+{rune.Value:X4}; only letters, marks, spaces, apostrophes, hyphens and periods.", paramName);
            }
        }

        script = found ?? throw new ArgumentException("A name part must contain at least one letter.", paramName);
        int length = UnicodeText.GraphemeCount(part);
        if (length > MaxPartLength)
        {
            throw new ArgumentOutOfRangeException(paramName, length, $"A name part has at most {MaxPartLength} grapheme clusters.");
        }

        return part;
    }

    private static bool IsAllowedNonLetter(Rune rune) =>
        Rune.GetUnicodeCategory(rune) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark
        || rune.Value is ' ' or '\'' or '’' or '-' or '‐' or '.' or '・';

    public bool Equals(PersonName? other) => other is not null && string.Equals(other._matchKey, _matchKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as PersonName);

    public override int GetHashCode() => _matchKey.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Heading;
}
