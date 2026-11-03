using JustDummies;

namespace InternationalCatalogue.Domain.Tests.Discovery;

/// <summary>Per-script letter classes (NFC-stable letters only) and the text patterns built on them.</summary>
internal static class Scripts {

    public static string LetterClass(WritingScript script) {
        return script switch {
            WritingScript.Latin      => @"A-Za-zÀ-ÖØ-öø-ſ",
            WritingScript.Greek      => @"Α-ΡΣ-Ωα-ω",
            WritingScript.Cyrillic   => @"А-я",
            WritingScript.Hebrew     => @"א-ת",
            WritingScript.Arabic     => @"ء-غف-ي",
            WritingScript.Devanagari => @"क-नप-र",
            WritingScript.Cjk        => @"ぁ-ん一-龥",
            _                        => throw new ArgumentOutOfRangeException(nameof(script)),
        };
    }

    /// <summary>One letter of the script.</summary>
    public static IAny<string> Letter(WritingScript script) {
        return Any.StringMatching($"[{LetterClass(script)}]");
    }

    /// <summary>A title text: starts with a letter, ends with a letter or a digit, 1 to 62 characters.</summary>
    public static IAny<string> TitleText(WritingScript script) {
        string letters = LetterClass(script);
        return Any.StringMatching($"[{letters}]([{letters}0-9 ,.:;!?'-]{{0,60}}[{letters}0-9])?");
    }

    /// <summary>A family name: letters, optionally two words joined by a space, an apostrophe or a hyphen.</summary>
    public static IAny<string> FamilyNameText(WritingScript script) {
        string letters = LetterClass(script);
        return Any.StringMatching($"[{letters}]{{1,15}}([ '-][{letters}]{{1,15}})?");
    }

    /// <summary>Given names: one to three space-separated words.</summary>
    public static IAny<string> GivenNamesText(WritingScript script) {
        string letters = LetterClass(script);
        return Any.StringMatching($"[{letters}]{{1,12}}( [{letters}]{{1,12}}){{0,2}}");
    }

}
