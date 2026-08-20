using System;
using System.Linq;

namespace JustDummies.Analyzers;

/// <summary>
///     The alphabets the <c>AnyString</c> and <c>AnyChar</c> character families draw from, mirrored from
///     <c>JustDummies/CharacterPools.cs</c>. An analyzer references no JustDummies assembly and cannot call it, so
///     the definition is duplicated — but only once, and it stays the single mirror: JD029 reads it from here, and any
///     further rule needing a family must too, because two rules disagreeing about what a family admits would be worse
///     than either being silent about it.
/// </summary>
internal static class CharacterFamilies {

    internal const string UpperLetters     = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    internal const string LowerLetters     = "abcdefghijklmnopqrstuvwxyz";
    internal const string Digits           = "0123456789";
    internal const string Letters          = UpperLetters + LowerLetters;
    internal const string LettersAndDigits = Letters + Digits;

    /// <summary>Every ASCII character — what an unconstrained draw starts from (ADR-0075).</summary>
    internal static readonly string Ascii = Range('\u0000', '\u007F');

    /// <summary>Every printable ASCII character, 0x20 to 0x7E — the space included.</summary>
    internal static readonly string Printable = Range(' ', '~');

    /// <summary>The ASCII characters that are not printable: the C0 controls and <c>DEL</c>.</summary>
    internal static readonly string NonPrintable = Filter(character => !Printable.Contains(character));

    /// <summary>
    ///     Printable ASCII that is neither a letter, a digit nor the space — POSIX <c>[:punct:]</c>. Deliberately
    ///     not <c>char.IsPunctuation</c>, which reads <c>+</c> and <c>&lt;</c> as symbols instead.
    /// </summary>
    internal static readonly string Punctuation =
        Filter(character => Printable.Contains(character) && character != ' ' && !LettersAndDigits.Contains(character));

    /// <summary>The tab and the space, in that order — same as <c>CharacterPools.Whitespaces</c>'s ascending code-point walk.</summary>
    internal const string Whitespaces = "\t ";

    /// <summary>The base-16 alphabet of RFC 4648, both cases.</summary>
    internal const string Hexadecimal = Digits + "ABCDEFabcdef";

    /// <summary>The alphabet the family <paramref name="name" /> names, or <c>null</c> when it names no family.</summary>
    internal static string? PoolFor(string name) {
        switch (name) {
            case "Alpha":        return Letters;
            case "Numeric":      return Digits;
            case "AlphaNumeric": return LettersAndDigits;
            case "Punctuation":  return Punctuation;
            case "Printable":    return Printable;
            case "NonPrintable": return NonPrintable;
            case "Whitespaces":  return Whitespaces;
            case "Hexadecimal":  return Hexadecimal;
            default:             return null;
        }
    }

    /// <summary>
    ///     What survives <paramref name="pool" /> once the family <paramref name="removed" /> names is taken out of
    ///     it — the subtractive constraints, which accumulate rather than occupying the family slot.
    /// </summary>
    internal static string Without(string pool, string removed) {
        string? subtracted = PoolFor(removed);
        if (subtracted is null) { return pool; }

        return new string(pool.Where(character => !subtracted.Contains(character)).ToArray());
    }

    private static string Filter(Func<char, bool> keep) {
        return new string(Ascii.Where(keep).ToArray());
    }

    private static string Range(char low, char high) {
        char[] characters = new char[high - low + 1];
        for (int index = 0; index < characters.Length; index++) { characters[index] = (char)(low + index); }

        return new string(characters);
    }

}
