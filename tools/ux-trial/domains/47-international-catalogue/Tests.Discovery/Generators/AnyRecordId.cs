// Scaffolded by `any`, then completed by hand: the recipe builds the check character itself.

using System;

using InternationalCatalogue.Domain.Tests.Discovery;

using JustDummies;

namespace InternationalCatalogue.Domain;

public sealed partial class AnyRecordId : IAny<RecordId> {

    private IAny<string> _value;

    public AnyRecordId()
        : this(value: AnyValidValue()) { }

    private static IAny<string> AnyValidValue() {
        return Any.Combine(Any.StringMatching("[A-Z]{2}"), Any.Int32().Between(1, 99_999_999), (agency, number) => agency + number.ToString("D8"));
    }

    private AnyRecordId(IAny<string> value) {
        _value = value;
    }

    private AnyRecordId(AnyRecordId from) {
        _value = from._value;
    }

    /// <summary>Pins the text, which every draw hands over as it is.</summary>
    public AnyRecordId WithValue(string value) {
        AnyRecordId copy = new AnyRecordId(this);

        copy._value = Any.Fixed(value);

        return copy;
    }

    public RecordId Generate() {
        return new RecordId(_value.Generate());
    }

}
