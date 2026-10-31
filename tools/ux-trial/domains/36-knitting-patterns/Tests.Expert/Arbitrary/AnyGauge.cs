// Scaffolded by `any` (JustDummies), then rewritten by hand: the row gauge depends on the stitch
// gauge (rows within half to twice the stitches), a rule that lives between the two parameters.
// `any generate Gauge --force` would overwrite it.

using System;

using JustDummies;

namespace KnittingPatterns.Domain;

/// <summary>A generator of arbitrary, plausible <see cref="Gauge" /> values.</summary>
public sealed partial class AnyGauge : IAny<Gauge> {

    private readonly IAny<decimal>                _stitchesPer10Cm;
    private readonly Func<decimal, IAny<decimal>> _rowsPer10CmFor;

    public AnyGauge()
        : this(AnyValidStitchesPer10Cm(), stitches => AnyValidRowsPer10Cm(stitches)) { }

    private AnyGauge(IAny<decimal> stitchesPer10Cm, Func<decimal, IAny<decimal>> rowsPer10CmFor) {
        _stitchesPer10Cm = stitchesPer10Cm;
        _rowsPer10CmFor  = rowsPer10CmFor;
    }

    // Gauges are measured to the half or tenth of a stitch on a swatch: one decimal place.
    private static AnyDecimal AnyValidStitchesPer10Cm() {
        return Any.Decimal().Between(4m, 60m).WithScale(1);
    }

    // A knit stitch is wider than it is tall: rows lie within half to twice the stitches, and 4..80.
    private static AnyDecimal AnyValidRowsPer10Cm(decimal stitchesPer10Cm) {
        decimal lowest  = Math.Max(4m, Math.Ceiling(stitchesPer10Cm * 5m) / 10m);   // stitches / 2, rounded up to 0.1
        decimal highest = Math.Min(80m, stitchesPer10Cm * 2m);

        return Any.Decimal().Between(lowest, highest).WithScale(1);
    }

    public AnyGauge WithStitchesPer10Cm(decimal value) {
        return new AnyGauge(new FixedValue<decimal>(value), _rowsPer10CmFor);
    }

    public AnyGauge WithStitchesPer10Cm(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyGauge(configure(AnyValidStitchesPer10Cm()), _rowsPer10CmFor);
    }

    public AnyGauge WithRowsPer10Cm(decimal value) {
        return new AnyGauge(_stitchesPer10Cm, _ => new FixedValue<decimal>(value));
    }

    public Gauge Generate() {
        decimal stitchesPer10Cm = _stitchesPer10Cm.Generate();

        return new Gauge(stitchesPer10Cm, _rowsPer10CmFor(stitchesPer10Cm).Generate());
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
