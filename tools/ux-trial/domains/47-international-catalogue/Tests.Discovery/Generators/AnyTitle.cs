// Scaffolded by `any`, then completed by hand: a title is drawn in one script (random unless pinned), as text of that script.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyTitle : IAny<Title> {

    private IAny<string> _value;
    private IAny<bool>  _allowBidiControls;

    public AnyTitle()
        : this(value:             Any.Enum<WritingScript>().Then(Scripts.TitleText),
               allowBidiControls: Any.Fixed(false)) { }

    private AnyTitle(IAny<string> value,
                     IAny<bool>   allowBidiControls) {
        _value             = value;
        _allowBidiControls = allowBidiControls;
    }

    private AnyTitle(AnyTitle from) {
        _value             = from._value;
        _allowBidiControls = from._allowBidiControls;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyTitle WithValue(string value) {
        AnyTitle copy = new AnyTitle(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    /// <summary>Draws a text of the given script, still well-formed for a title.</summary>
    public AnyTitle InScript(WritingScript script) {
        AnyTitle copy = new AnyTitle(this);

        copy._value = Scripts.TitleText(script);

        return copy;
    }

    public AnyTitle WithAllowBidiControls(bool value) {
        AnyTitle copy = new AnyTitle(this);

        copy._allowBidiControls = Any.Fixed(value);

        return copy;
    }

    public Title Generate() {
        return new Title(_value.Generate(), _allowBidiControls.Generate());
    }

}
