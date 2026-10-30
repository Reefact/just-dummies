// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Tolerance --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Tolerance" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTolerance : IAny<Tolerance> {

    private readonly IAny<decimal> _nominalMm;
    private readonly IAny<decimal> _minusMm;
    private readonly IAny<decimal> _plusMm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTolerance()
        : this(nominalMm: AnyValidNominalMm(),
               minusMm:   AnyValidMinusMm(),
               plusMm:    AnyValidPlusMm()) { }

    private static AnyDecimal AnyValidNominalMm() {
        // (0, 5000] mm to the micrometre; kept at 1 mm or more so any drawn minus deviation stays below it.
        return Any.Decimal().Between(1m, 5_000m).WithScale(3);
    }

    private static AnyDecimal AnyValidMinusMm() {
        // Never negative, and always below the nominal (which is at least 1 mm).
        return Any.Decimal().Between(0m, 0.5m).WithScale(3);
    }

    private static AnyDecimal AnyValidPlusMm() {
        // Strictly positive, so the band is never empty whatever the minus deviation is.
        return Any.Decimal().Between(0.001m, 0.5m).WithScale(3);
    }

    private AnyTolerance(IAny<decimal> nominalMm,
                         IAny<decimal> minusMm,
                         IAny<decimal> plusMm) {
        _nominalMm = nominalMm;
        _minusMm   = minusMm;
        _plusMm    = plusMm;
    }

    /// <summary>Pins <c>nominalMm</c> to a fixed value.</summary>
    public AnyTolerance WithNominalMm(decimal value) {
        return new AnyTolerance(new FixedValue<decimal>(value), _minusMm, _plusMm);
    }

    /// <summary>Draws <c>nominalMm</c> from <c>AnyValidNominalMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTolerance WithNominalMm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTolerance(configure(AnyValidNominalMm()), _minusMm, _plusMm);
    }

    /// <summary>Pins <c>minusMm</c> to a fixed value.</summary>
    public AnyTolerance WithMinusMm(decimal value) {
        return new AnyTolerance(_nominalMm, new FixedValue<decimal>(value), _plusMm);
    }

    /// <summary>Draws <c>minusMm</c> from <c>AnyValidMinusMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTolerance WithMinusMm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTolerance(_nominalMm, configure(AnyValidMinusMm()), _plusMm);
    }

    /// <summary>Pins <c>plusMm</c> to a fixed value.</summary>
    public AnyTolerance WithPlusMm(decimal value) {
        return new AnyTolerance(_nominalMm, _minusMm, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>plusMm</c> from <c>AnyValidPlusMm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTolerance WithPlusMm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyTolerance(_nominalMm, _minusMm, configure(AnyValidPlusMm()));
    }

    /// <summary>Produces one arbitrary <see cref="Tolerance" />.</summary>
    public Tolerance Generate() {
        return new Tolerance(_nominalMm.Generate(),
                             _minusMm.Generate(),
                             _plusMm.Generate());
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
