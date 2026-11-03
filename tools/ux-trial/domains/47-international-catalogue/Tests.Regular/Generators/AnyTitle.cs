using System;

using JustDummies;

namespace InternationalCatalogue.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Title" /> values: a few words of letters of one script
///     (Latin unless <see cref="InScript" /> says otherwise), never blank, padded or over-long.
/// </summary>
public sealed partial class AnyTitle : IAny<Title> {

    private IAny<string> _value;
    private IAny<bool>   _allowBidiControls;

    public AnyTitle()
        : this(value: Generated.Words(WritingScript.Latin, 5), allowBidiControls: Any.Fixed(false)) { }

    private AnyTitle(IAny<string> value, IAny<bool> allowBidiControls) {
        _value             = value;
        _allowBidiControls = allowBidiControls;
    }

    private AnyTitle(AnyTitle from) {
        _value             = from._value;
        _allowBidiControls = from._allowBidiControls;
    }

    /// <summary>Pins the text; the constructor still judges it.</summary>
    public AnyTitle WithValue(string value) {
        AnyTitle copy = new AnyTitle(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public AnyTitle WithAllowBidiControls(bool value) {
        AnyTitle copy = new AnyTitle(this);

        copy._allowBidiControls = Any.Fixed(value);

        return copy;
    }

    /// <summary>Words of letters of <paramref name="script" />, so that a record declared in that script accepts the title.</summary>
    public AnyTitle InScript(WritingScript script) {
        AnyTitle copy = new AnyTitle(this);

        copy._value = Generated.Words(script, 5);

        return copy;
    }

    /// <summary>Exactly <paramref name="graphemes" /> Latin letters: a single word, so no space can land on an edge.</summary>
    public AnyTitle OfLength(int graphemes) {
        AnyTitle copy = new AnyTitle(this);

        copy._value = Any.StringMatching($"{Generated.LetterClass(WritingScript.Latin)}{{{graphemes}}}");

        return copy;
    }

    public Title Generate() {
        return new Title(_value.Generate(), _allowBidiControls.Generate());
    }

}
