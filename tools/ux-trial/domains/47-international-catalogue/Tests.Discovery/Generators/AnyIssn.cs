// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyIssn : IAny<Issn> {

    private IAny<string> _value;

    public AnyIssn()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[0-9]{7}").As(body => body[..4] + "-" + body[4..] + CheckCharacters.Issn(body));
    }

    private AnyIssn(IAny<string> value) {
        _value = value;
    }

    private AnyIssn(AnyIssn from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyIssn WithValue(string value) {
        AnyIssn copy = new AnyIssn(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public Issn Generate() {
        return new Issn(_value.Generate());
    }

}
