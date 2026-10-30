// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Leg --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Repro.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Leg" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLeg : IAny<Leg> {

    private readonly IAny<int> _from;
    private readonly IAny<int> _to;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLeg()
        : this(from: AnyValidFrom(),
               to:   AnyValidTo()) { }

    private static AnyInt32 AnyValidFrom() {
        return Any.Int32();
    }

    private static AnyInt32 AnyValidTo() {
        return Any.Int32();
    }

    private AnyLeg(IAny<int> from,
                   IAny<int> to) {
        _from = from;
        _to   = to;
    }

    /// <summary>Pins <c>from</c> to a fixed value.</summary>
    public AnyLeg WithFrom(int value) {
        return new AnyLeg(new FixedValue<int>(value), _to);
    }

    /// <summary>Draws <c>from</c> from <c>AnyValidFrom()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithFrom(Func<AnyInt32, AnyInt32> configure) {
        return new AnyLeg(configure(AnyValidFrom()), _to);
    }

    /// <summary>Pins <c>to</c> to a fixed value.</summary>
    public AnyLeg WithTo(int value) {
        return new AnyLeg(_from, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>to</c> from <c>AnyValidTo()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLeg WithTo(Func<AnyInt32, AnyInt32> configure) {
        return new AnyLeg(_from, configure(AnyValidTo()));
    }

    /// <summary>Produces one arbitrary <see cref="Leg" />.</summary>
    public Leg Generate() {
        return new Leg(_from.Generate(),
                       _to.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
