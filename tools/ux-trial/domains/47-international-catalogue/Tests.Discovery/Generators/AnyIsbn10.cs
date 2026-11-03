// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyIsbn10 : IAny<Isbn10> {

    private IAny<string> _value;

    public AnyIsbn10()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[0-9]{9}").As(body => body + CheckCharacters.Isbn10(body));
    }

    private AnyIsbn10(IAny<string> value) {
        _value = value;
    }

    private AnyIsbn10(AnyIsbn10 from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyIsbn10 WithValue(string value) {
        AnyIsbn10 copy = new AnyIsbn10(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public Isbn10 Generate() {
        return new Isbn10(_value.Generate());
    }

}
