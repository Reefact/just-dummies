// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyCallNumber : IAny<CallNumber> {

    private IAny<string> _value;

    public AnyCallNumber()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.StringMatching("[0-9]{3}(\\.[0-9]{1,8})? [A-Z][0-9]{1,4}");
    }

    private AnyCallNumber(IAny<string> value) {
        _value = value;
    }

    private AnyCallNumber(AnyCallNumber from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyCallNumber WithValue(string value) {
        AnyCallNumber copy = new AnyCallNumber(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public CallNumber Generate() {
        return new CallNumber(_value.Generate());
    }

}
