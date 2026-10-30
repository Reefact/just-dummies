// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Fit --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Fit" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyFit : IAny<Fit> {

    private readonly IAny<Dimension> _hole;
    private readonly IAny<Dimension> _shaft;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyFit()
        : this(hole:  AnyValidHole(),
               shaft: AnyValidShaft()) { }

    private static AnyDimension AnyValidHole() {
        // A narrower OneOf("H", "JS") is refused against the base OneOf; Except is what narrows a pool.
        return new AnyDimension().WithTolerance(t => t.WithZone(z => z.Except("h", "js")));
    }

    private static AnyDimension AnyValidShaft() {
        return new AnyDimension().WithTolerance(t => t.WithZone(z => z.Except("H", "JS")));
    }

    private AnyFit(IAny<Dimension> hole,
                   IAny<Dimension> shaft) {
        _hole  = hole;
        _shaft = shaft;
    }

    /// <summary>Pins <c>hole</c> to a fixed value.</summary>
    public AnyFit WithHole(Dimension value) {
        return new AnyFit(new FixedValue<Dimension>(value), _shaft);
    }

    /// <summary>Draws <c>hole</c> from <c>AnyValidHole()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFit WithHole(Func<AnyDimension, AnyDimension> configure) {
        return new AnyFit(configure(AnyValidHole()), _shaft);
    }

    /// <summary>Pins <c>shaft</c> to a fixed value.</summary>
    public AnyFit WithShaft(Dimension value) {
        return new AnyFit(_hole, new FixedValue<Dimension>(value));
    }

    /// <summary>Draws <c>shaft</c> from <c>AnyValidShaft()</c>, refined by <paramref name="configure" />.</summary>
    public AnyFit WithShaft(Func<AnyDimension, AnyDimension> configure) {
        return new AnyFit(_hole, configure(AnyValidShaft()));
    }

    /// <summary>Produces one arbitrary <see cref="Fit" />.</summary>
    public Fit Generate() {
        // A fit pairs two features of the same nominal size: the shaft keeps its drawn tolerance
        // and takes the hole's size. The parameters are not independent, and no With… can say so.
        Dimension hole  = _hole.Generate();
        Dimension shaft = _shaft.Generate();

        return new Fit(hole, new Dimension(hole.Nominal, hole.Unit, shaft.Tolerance));
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
