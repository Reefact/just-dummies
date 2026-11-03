using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     Hand-written. By default a language with an optional region: a tag that names no script, so it fits a
///     record of any script. <see cref="ForScript" /> adds a script subtag that designates the given script.
/// </summary>
public sealed partial class AnyLanguageTag : IAny<LanguageTag> {

    private readonly WritingScript? _script;
    private readonly bool           _bare;

    public AnyLanguageTag()
        : this(null, false) { }

    private AnyLanguageTag(WritingScript? script, bool bare) {
        _script = script;
        _bare   = bare;
    }

    /// <summary>A language alone, as an article rule requires.</summary>
    public AnyLanguageTag Bare() {
        return new AnyLanguageTag(_script, true);
    }

    /// <summary>A tag that may carry the script subtag of <paramref name="script" /> (and then names it).</summary>
    public AnyLanguageTag ForScript(WritingScript script) {
        return new AnyLanguageTag(script, _bare);
    }

    public LanguageTag Generate() {
        string language = Any.StringMatching("(?i)[a-z]{2,3}").Generate();
        string? subtag  = _script is WritingScript script && Any.Boolean().Generate() ? Any.ElementOf(SubtagsOf(script)).Generate() : null;
        string? region  = _bare ? null : Any.StringMatching("([a-z]{2}|[0-9]{3})").OrNull().Generate();

        return new LanguageTag(string.Join("-", new[] { language, subtag, region }.Where(part => part is not null)));
    }

    private static string[] SubtagsOf(WritingScript script) {
        return script switch {
            WritingScript.Latin      => ["Latn"],
            WritingScript.Greek      => ["Grek"],
            WritingScript.Cyrillic   => ["Cyrl"],
            WritingScript.Arabic     => ["Arab"],
            WritingScript.Hebrew     => ["Hebr"],
            WritingScript.Devanagari => ["Deva"],
            WritingScript.Cjk        => ["Hani", "Hans", "Hant", "Jpan", "Kore", "Hira", "Kana", "Hang"],
            _                        => throw new ArgumentOutOfRangeException(nameof(script), script, null),
        };
    }

}
