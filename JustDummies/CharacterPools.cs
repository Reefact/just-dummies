namespace JustDummies;

/// <summary>The character families a string or char generator can be restricted to.</summary>
internal enum CharacterSet {

    Alpha,
    Numeric,
    AlphaNumeric,
    Punctuation,
    Printable,
    NonPrintable,
    Whitespaces,
    Hexadecimal

}

/// <summary>The casing a string or char generator can impose on alphabetic characters.</summary>
internal enum LetterCasing {

    Lower,
    Upper

}

/// <summary>
///     The ASCII universe every character draw starts from, and the families that narrow it — one definition
///     shared by <see cref="AnyString" />'s filler and <see cref="AnyChar" />, so the two generators can never
///     drift apart on what they draw. An unconstrained draw is the whole of ASCII and every family is a subset of
///     it, with no exception (ADR-0074); <see cref="RegexAlphabet" /> keeps its own, narrower universe for the
///     positions a pattern leaves free, which is a decision of its own.
/// </summary>
internal static class CharacterPools {

    internal const string UpperLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    internal const string LowerLetters = "abcdefghijklmnopqrstuvwxyz";
    internal const string Digits       = "0123456789";

    internal const char MinAscii     = '\u0000';  // NUL
    internal const char MaxAscii     = '\u007F';  // DEL
    internal const char MinPrintable = ' ';   // 0x20
    internal const char MaxPrintable = '~';   // 0x7E
    internal const char Space        = ' ';
    internal const char Tab          = '\t';

    /// <summary>
    ///     Every ASCII character, in code-point order — what an unconstrained draw starts from. A seed replays the
    ///     values it drew before (ADR-0049), so neither the characters nor their order may change while the major
    ///     version stands.
    /// </summary>
    internal static readonly string Ascii = Range(MinAscii, MaxAscii);

    /// <summary>Every printable ASCII character — the space included, the controls and <c>DEL</c> excluded.</summary>
    internal static readonly string Printable = Range(MinPrintable, MaxPrintable);

    /// <summary>The ASCII characters that are not printable: the C0 controls and <c>DEL</c>.</summary>
    internal static readonly string NonPrintable = Filter(IsAsciiNonPrintable);

    /// <summary>
    ///     Printable ASCII that is neither a letter, a digit nor the space — POSIX <c>[:punct:]</c>, the 32
    ///     characters an ASCII table calls punctuation and symbols.
    /// </summary>
    internal static readonly string Punctuation = Filter(IsAsciiPunctuation);

    /// <summary>The space and the tab — the readable pair <see cref="RegexAlphabet" /> already draws <c>\s</c> from.</summary>
    internal static readonly string Whitespaces = Filter(IsAsciiWhitespace);

    /// <summary>The base-16 alphabet of RFC 4648, both cases — a casing narrows it to sixteen characters.</summary>
    internal static readonly string Hexadecimal = Filter(IsAsciiHexadecimal);

    internal static bool IsAscii(char character) {
        return character <= MaxAscii;
    }

    internal static bool IsAsciiLetter(char character) {
        return character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    internal static bool IsAsciiDigit(char character) {
        return character is >= '0' and <= '9';
    }

    internal static bool IsAsciiPrintable(char character) {
        return character is >= MinPrintable and <= MaxPrintable;
    }

    internal static bool IsAsciiNonPrintable(char character) {
        return IsAscii(character) && !IsAsciiPrintable(character);
    }

    /// <summary>
    ///     Whether the character belongs to <see cref="Punctuation" />. Deliberately <b>not</b>
    ///     <see cref="char.IsPunctuation(char)" />, which classifies <c>+</c>, <c>&lt;</c> and <c>$</c> as symbols
    ///     rather than punctuation: this pool is the whole non-alphanumeric printable block, minus the space. The
    ///     space stays out because it is the one character a <c>Trim()</c> removes in silence, and a family whose
    ///     purpose is a separator a test can rely on must not draw one; <see cref="Whitespaces" /> names it instead.
    /// </summary>
    internal static bool IsAsciiPunctuation(char character) {
        return IsAsciiPrintable(character) && character != Space && !IsAsciiLetter(character) && !IsAsciiDigit(character);
    }

    internal static bool IsAsciiWhitespace(char character) {
        return character is Space or Tab;
    }

    internal static bool IsAsciiHexadecimal(char character) {
        return IsAsciiDigit(character) || character is >= 'A' and <= 'F' or >= 'a' and <= 'f';
    }

    /// <summary>
    ///     Whether the character belongs to the family <paramref name="set" /> names. One definition for both
    ///     generators and for both uses: narrowing the pool a draw comes from, and judging a character a caller
    ///     anchored. A generator with no family declared admits everything — the universe is already the bound, and
    ///     an anchored fragment is the caller's own text rather than something the library drew.
    /// </summary>
    internal static bool Belongs(char character, CharacterSet? set) {
        return set switch {
            CharacterSet.Alpha        => IsAsciiLetter(character),
            CharacterSet.Numeric      => IsAsciiDigit(character),
            CharacterSet.AlphaNumeric => IsAsciiLetter(character) || IsAsciiDigit(character),
            CharacterSet.Punctuation  => IsAsciiPunctuation(character),
            CharacterSet.Printable    => IsAsciiPrintable(character),
            CharacterSet.NonPrintable => IsAsciiNonPrintable(character),
            CharacterSet.Whitespaces  => IsAsciiWhitespace(character),
            CharacterSet.Hexadecimal  => IsAsciiHexadecimal(character),
            _                         => true
        };
    }

    /// <summary>Whether the character survives the casing <paramref name="casing" /> imposes on ASCII letters.</summary>
    internal static bool MatchesCasing(char character, LetterCasing? casing) {
        return casing switch {
            LetterCasing.Lower => character is not (>= 'A' and <= 'Z'),
            LetterCasing.Upper => character is not (>= 'a' and <= 'z'),
            _                  => true
        };
    }

    /// <summary>
    ///     The character as it should appear in a message the library writes. A default draw can now carry a control
    ///     character, and <c>ESC</c> would open an ANSI sequence in the terminal reporting the failure — so a
    ///     diagnostic escapes what it cannot safely print (ADR-0074).
    /// </summary>
    internal static string Escape(char character) {
        return character switch {
            '\0'                              => "\\0",
            '\a'                              => "\\a",
            '\b'                              => "\\b",
            '\f'                              => "\\f",
            '\n'                              => "\\n",
            '\r'                              => "\\r",
            '\t'                              => "\\t",
            '\v'                              => "\\v",
            _ when IsAsciiNonPrintable(character) => $"\\x{(int)character:x2}",
            _                                 => character.ToString()
        };
    }

    /// <summary>The text with every character it is unsafe to print escaped — <see cref="Escape(char)" />, applied throughout.</summary>
    internal static string Escape(string text) {
        if (text is null) { throw new ArgumentNullException(nameof(text)); }
        if (!text.Any(IsAsciiNonPrintable)) { return text; }

        return string.Concat(text.Select(Escape));
    }

    private static string Filter(Func<char, bool> keep) {
        return new string(Ascii.Where(keep).ToArray());
    }

    private static string Range(char low, char high) {
        // Index an int, not a char: a high of U+FFFF would wrap a 16-bit char back to 0x0000 and loop forever.
        // Every current caller passes a bounded high, so this is defense in depth against a future wide range.
        char[] characters = new char[high - low + 1];
        for (int index = 0; index < characters.Length; index++) { characters[index] = (char)(low + index); }

        return new string(characters);
    }

}
