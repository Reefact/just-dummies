using System.Globalization;
using System.Text;

namespace InternationalCatalogue.Domain;

/// <summary>Shared Unicode checks for catalogue text: well-formedness, normalisation, scripts, matching.</summary>
internal static class UnicodeText
{
    /// <summary>Refuses unpaired surrogates, then returns the text in normalisation form C.</summary>
    public static string ToNfc(string value, string paramName)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                {
                    throw new ArgumentException($"Unpaired high surrogate at index {i}.", paramName);
                }

                i++;
            }
            else if (char.IsLowSurrogate(c))
            {
                throw new ArgumentException($"Unpaired low surrogate at index {i}.", paramName);
            }
        }

        return value.Normalize(NormalizationForm.FormC);
    }

    public static int GraphemeCount(string value) => new StringInfo(value).LengthInTextElements;

    public static bool IsLineBreakOrControl(Rune rune) =>
        Rune.GetUnicodeCategory(rune) is UnicodeCategory.Control or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator;

    public static bool StartsWithCombiningMark(string value) =>
        value.Length > 0
        && Rune.GetUnicodeCategory(Rune.GetRuneAt(value, 0)) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark;

    /// <summary>LRM, RLM, ALM, the embeddings and overrides (LRE…RLO), and the isolates (LRI…PDI).</summary>
    public static bool IsBidiControl(Rune rune) =>
        rune.Value is 0x200E or 0x200F or 0x061C
        || IsEmbeddingOrOverride(rune)
        || rune.Value is >= 0x2066 and <= 0x2069;

    public static bool IsEmbeddingOrOverride(Rune rune) => rune.Value is >= 0x202A and <= 0x202E;

    public static bool IsIsolateInitiator(Rune rune) => rune.Value is >= 0x2066 and <= 0x2068;

    public static bool IsPopDirectionalIsolate(Rune rune) => rune.Value == 0x2069;

    /// <summary>Pictographs and dingbats, plus the emoji presentation selector and the combining keycap.</summary>
    public static bool IsEmoji(Rune rune) =>
        rune.Value is >= 0x1F000 and <= 0x1FAFF
        or >= 0x2600 and <= 0x27BF
        or >= 0x2B50 and <= 0x2B55
        or 0xFE0F
        or 0x20E3;

    /// <summary>The script of a letter; <see langword="null"/> for a non-letter or a letter of an unsupported script.</summary>
    public static WritingScript? ScriptOfLetter(Rune rune)
    {
        if (!Rune.IsLetter(rune))
        {
            return null;
        }

        int v = rune.Value;
        if (v <= 0x024F || v is >= 0x0250 and <= 0x02AF or >= 0x1E00 and <= 0x1EFF or >= 0x2C60 and <= 0x2C7F or >= 0xA720 and <= 0xA7FF)
        {
            return WritingScript.Latin;
        }

        if (v is >= 0x0370 and <= 0x03FF or >= 0x1F00 and <= 0x1FFF)
        {
            return WritingScript.Greek;
        }

        if (v is >= 0x0400 and <= 0x052F or >= 0x1C80 and <= 0x1C8F or >= 0x2DE0 and <= 0x2DFF or >= 0xA640 and <= 0xA69F)
        {
            return WritingScript.Cyrillic;
        }

        if (v is >= 0x0590 and <= 0x05FF or >= 0xFB1D and <= 0xFB4F)
        {
            return WritingScript.Hebrew;
        }

        if (v is >= 0x0600 and <= 0x06FF or >= 0x0750 and <= 0x077F or >= 0x08A0 and <= 0x08FF or >= 0xFB50 and <= 0xFDFF or >= 0xFE70 and <= 0xFEFF)
        {
            return WritingScript.Arabic;
        }

        if (v is >= 0x0900 and <= 0x097F or >= 0xA8E0 and <= 0xA8FF)
        {
            return WritingScript.Devanagari;
        }

        if (v is >= 0x1100 and <= 0x11FF or >= 0x3040 and <= 0x30FF or >= 0x3400 and <= 0x4DBF or >= 0x4E00 and <= 0x9FFF
            or >= 0xAC00 and <= 0xD7AF or >= 0xF900 and <= 0xFAFF or >= 0x20000 and <= 0x3134F)
        {
            return WritingScript.Cjk;
        }

        return null;
    }

    public static string WithoutBidiControls(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (!IsBidiControl(rune))
            {
                builder.Append(rune.ToString());
            }
        }

        return builder.ToString();
    }

    /// <summary>The key two texts are compared under: bidi controls removed, simple case folding, NFC.</summary>
    public static string MatchKey(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (Rune rune in value.EnumerateRunes())
        {
            if (!IsBidiControl(rune))
            {
                builder.Append(Rune.ToUpperInvariant(rune).ToString());
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
