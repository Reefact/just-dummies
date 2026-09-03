// Scaffolded by dum (JustDummies). This file is yours: read it, edit it, commit it.
// `dum generate Email --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using JustDummies;

namespace Shop.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Email" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins it; to draw from an isolated
///     <c>Dummy.WithSeed(...)</c> context, pass that context's generators through the
///     <c>With…</c> overloads.
/// </summary>
public sealed partial class DummyEmail : IDummy<Email> {

    private readonly IDummy<string> _value;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public DummyEmail()
        : this(value: Dummy.String().NonEmpty()) { }

    private DummyEmail(IDummy<string> value) {
        _value = value;
    }

    /// <summary>Pins <c>value</c> to a fixed value.</summary>
    public DummyEmail WithValue(string value) {
        return WithValue(new FixedValue<string>(value));
    }

    /// <summary>Draws <c>value</c> from <paramref name="generator" />.</summary>
    public DummyEmail WithValue(IDummy<string> generator) {
        return new DummyEmail(generator);
    }

    /// <summary>Produces one arbitrary <see cref="Email" />.</summary>
    public Email Generate() {
        return Email.Create(_value.Generate());
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
