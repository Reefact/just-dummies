using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>Writing-script knowledge shared by the generators: letter classes, language subtags, check characters.</summary>
public static class Generated {

    /// <summary>Regular-subset character classes holding only letters that <c>ScriptOfLetter</c> maps to the script.</summary>
    public static string LetterClass(WritingScript script) {
        return script switch {
            WritingScript.Latin      => "[A-Za-z\\u00C0-\\u00D6\\u00D8-\\u00F6\\u00F8-\\u00FF\\u0100-\\u017F]",
            WritingScript.Greek      => "[\\u03B1-\\u03C9\\u0391-\\u03A1]",
            WritingScript.Cyrillic   => "[\\u0410-\\u044F]",
            WritingScript.Hebrew     => "[\\u05D0-\\u05EA]",
            WritingScript.Arabic     => "[\\u0621-\\u064A]",
            WritingScript.Devanagari => "[\\u0905-\\u0939]",
            WritingScript.Cjk        => "[\\u4E00-\\u9FA5\\u3041-\\u3096\\uAC00-\\uD7A3]",
            _                        => throw new ArgumentOutOfRangeException(nameof(script), script, null)
        };
    }

    /// <summary>Words of letters of the script separated by single spaces: never blank, never padded, never starting with a mark.</summary>
    public static AnyPattern Words(WritingScript script, int maxExtraWords) {
        string letter = LetterClass(script);

        return Any.StringMatching($"{letter}{{1,12}}( {letter}{{1,12}}){{0,{maxExtraWords}}}");
    }

    public static string[] ScriptSubtags(WritingScript script) {
        return script switch {
            WritingScript.Latin      => ["Latn"],
            WritingScript.Greek      => ["Grek"],
            WritingScript.Cyrillic   => ["Cyrl"],
            WritingScript.Arabic     => ["Arab"],
            WritingScript.Hebrew     => ["Hebr"],
            WritingScript.Devanagari => ["Deva"],
            WritingScript.Cjk        => ["Hani", "Hans", "Hant", "Jpan", "Kore", "Hira", "Kana", "Hang"],
            _                        => throw new ArgumentOutOfRangeException(nameof(script), script, null)
        };
    }

    public static string NonZeroRecordNumber(string candidate) {
        return candidate[2..].All(c => c == '0') ? candidate[..9] + "1" : candidate;
    }

    public static string WithIsbn13CheckDigit(string twelveDigits) {
        return twelveDigits + Isbn13.ComputeCheckDigit(twelveDigits);
    }

    public static string WithIsbn10CheckCharacter(string nineDigits) {
        return nineDigits + Isbn10.ComputeCheckCharacter(nineDigits);
    }

    public static string AsIssn(string sevenDigits) {
        return sevenDigits[..4] + "-" + sevenDigits[4..] + Issn.ComputeCheckCharacter(sevenDigits);
    }

}
