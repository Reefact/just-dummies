// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyIsbn13 : IAny<Isbn13> {

    private IAny<string> _value;

    public AnyIsbn13()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("97[89][0-9]{9}").As(body => body + CheckCharacters.Isbn13(body));
    }

    private AnyIsbn13(IAny<string> value) {
        _value = value;
    }

    private AnyIsbn13(AnyIsbn13 from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyIsbn13 WithValue(string value) {
        AnyIsbn13 copy = new AnyIsbn13(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public Isbn13 Generate() {
        return new Isbn13(_value.Generate());
    }

}
