// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Gauge --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Gauge" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGauge : IAny<Gauge> {

    private readonly IAny<decimal> _stitchesPer10Cm;
    private readonly IAny<decimal>? _rowsPer10Cm;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGauge()
        : this(stitchesPer10Cm: AnyValidStitchesPer10Cm(),
               rowsPer10Cm:     null) { }

    private static AnyDecimal AnyValidStitchesPer10Cm() {
        return Any.Decimal().Between(4m, 60m).WithScale(1);
    }

    private static AnyDecimal AnyValidRowsPer10Cm() {
        return Any.Decimal().Between(4m, 80m).WithScale(1);
    }

    private AnyGauge(IAny<decimal> stitchesPer10Cm,
                     IAny<decimal>? rowsPer10Cm) {
        _stitchesPer10Cm = stitchesPer10Cm;
        _rowsPer10Cm     = rowsPer10Cm;
    }

    /// <summary>Pins <c>stitchesPer10Cm</c> to a fixed value.</summary>
    public AnyGauge WithStitchesPer10Cm(decimal value) {
        return new AnyGauge(new FixedValue<decimal>(value), _rowsPer10Cm);
    }

    /// <summary>Draws <c>stitchesPer10Cm</c> from <c>AnyValidStitchesPer10Cm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGauge WithStitchesPer10Cm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyGauge(configure(AnyValidStitchesPer10Cm()), _rowsPer10Cm);
    }

    /// <summary>Pins <c>rowsPer10Cm</c> to a fixed value.</summary>
    public AnyGauge WithRowsPer10Cm(decimal value) {
        return new AnyGauge(_stitchesPer10Cm, new FixedValue<decimal>(value));
    }

    /// <summary>Draws <c>rowsPer10Cm</c> from <c>AnyValidRowsPer10Cm()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGauge WithRowsPer10Cm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyGauge(_stitchesPer10Cm, configure(AnyValidRowsPer10Cm()));
    }

    /// <summary>Produces one arbitrary <see cref="Gauge" />.</summary>
    public Gauge Generate() {
        // Rows per 10 cm lie within half to twice the stitches: unless a test set the rows itself, they are
        // drawn inside the window the drawn stitches leave.
        decimal stitchesPer10Cm = _stitchesPer10Cm.Generate();
        decimal rowsPer10Cm     = _rowsPer10Cm?.Generate()
                                  ?? Any.Decimal()
                                        .Between(Math.Max(4m, stitchesPer10Cm / 2m), Math.Min(80m, stitchesPer10Cm * 2m))
                                        .Generate();

        return new Gauge(stitchesPer10Cm, rowsPer10Cm);
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
