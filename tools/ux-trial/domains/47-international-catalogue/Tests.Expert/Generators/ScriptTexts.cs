using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     What each writing script draws its letters from. Hand-written: no scaffold can know which characters
///     belong to a script, and <c>Any.String()</c> reaches no further than ASCII. Elements are strings, not
///     chars, so a supplementary ideograph (a surrogate pair) or a letter with its combining mark stays whole.
/// </summary>
public static class ScriptTexts {

    private static readonly string[] Neutral = [" ", "-", ":", ",", ".", "'", "’", "!", "?", "(", ")", "/", "&", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

    public static IReadOnlyList<string> LettersOf(WritingScript script) {
        return script switch {
            WritingScript.Latin      => Split("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZàâäçéèêëîïôöùûüÿñßøåæœłžšđÀÉÇÑÖÜÅØŁŽŠ").Concat(["e\u0301", "a\u0308", "n\u0303", "o\u0302", "c\u0327"]).ToArray(),
            WritingScript.Greek      => Split("αβγδεζηθικλμνξοπρστυφχψωΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩάέήίόύώἀἑὴ"),
            WritingScript.Cyrillic   => Split("абвгдежзийклмнопрстуфхцчшщъыьэюяёАБВГДЕЖЗИЙКЛМНОПРСТУФХЦЧШЩЭЮЯЁ"),
            WritingScript.Hebrew     => Split("אבגדהוזחטיכלמנסעפצקרשתךםןףץ"),
            WritingScript.Arabic     => Split("ابتثجحخدذرزسشصضطظعغفقكلمنهويءةى"),
            WritingScript.Devanagari => Split("अआइईउऊएऐओऔकखगघचछजझटठडढणतथदधनपफबभमयरलवशषसह").Concat(["क\u093F", "क\u093E", "क\u0940", "क\u0941", "क\u0947", "क\u094B", "म\u093E", "त\u093F"]).ToArray(),
            WritingScript.Cjk        => Split("漢字日本語中文書籍図館大学한국어조선글あいうえおかきくけこアイウエオカタカナ").Concat(["𠀀", "𠀁", "𠀂", "𪚥"]).ToArray(),
            _                        => throw new ArgumentOutOfRangeException(nameof(script), script, null),
        };
    }

    /// <summary>A name word: one to ten letters of the script, an apostrophe or a hyphen now and then inside.</summary>
    public static IAny<string> WordIn(WritingScript script) {
        IReadOnlyList<string> letters = LettersOf(script);

        return Any.Combine(Any.ElementOf(letters),
                           Any.ListOf(Any.ElementOf(letters.Concat(["'", "-"]).ToArray())).WithCountBetween(0, 9),
                           (first, rest) => first + string.Concat(rest));
    }

    /// <summary>Title text: starts with a letter of the script, then letters, digits, spaces and punctuation.</summary>
    public static IAny<string> TextIn(WritingScript script) {
        IReadOnlyList<string> letters = LettersOf(script);
        string[]              pool    = letters.Concat(letters).Concat(Neutral).ToArray();

        return Any.Combine(Any.ElementOf(letters),
                           Any.ListOf(Any.ElementOf(pool)).WithCountBetween(0, 60),
                           (first, rest) => first + string.Concat(rest));
    }

    private static string[] Split(string chars) {
        return chars.Select(c => c.ToString()).ToArray();
    }

}
