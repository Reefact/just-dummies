// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyLanguageTag : IAny<LanguageTag> {

    private IAny<string> _value;

    public AnyLanguageTag()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[A-Za-z]{2,3}(-[A-Za-z]{4})?(-([A-Za-z]{2}|[0-9]{3}))?");
    }

    private AnyLanguageTag(IAny<string> value) {
        _value = value;
    }

    private AnyLanguageTag(AnyLanguageTag from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyLanguageTag WithValue(string value) {
        AnyLanguageTag copy = new AnyLanguageTag(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public LanguageTag Generate() {
        return new LanguageTag(_value.Generate());
    }

}
