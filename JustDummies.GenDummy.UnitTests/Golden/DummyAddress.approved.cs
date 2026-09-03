// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Address --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Address" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Dummy.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class DummyAddress : IDummy<Address> {

    private readonly IDummy<string> _street;
    private readonly IDummy<string> _city;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public DummyAddress()
        : this(street: Dummy.String().NonEmpty(),
               city:   Dummy.String().NonEmpty()) { }

    private DummyAddress(IDummy<string> street,
                         IDummy<string> city) {
        _street = street;
        _city   = city;
    }

    /// <summary>Pins <c>street</c> to a fixed value.</summary>
    public DummyAddress WithStreet(string value) {
        return WithStreet(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>street</c> from <paramref name="generator" />.</summary>
    public DummyAddress WithStreet(IDummy<string> generator) {
        return new DummyAddress(generator, _city);
    }

    /// <summary>Pins <c>city</c> to a fixed value.</summary>
    public DummyAddress WithCity(string value) {
        return WithCity(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>city</c> from <paramref name="generator" />.</summary>
    public DummyAddress WithCity(IDummy<string> generator) {
        return new DummyAddress(_street, generator);
    }

    /// <summary>Produces one arbitrary <see cref="Address" />.</summary>
    public Address Generate() {
        return new Address(_street.Generate(),
                           _city.Generate());
    }

    private sealed class FixedValue<TValue> : IDummy<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
