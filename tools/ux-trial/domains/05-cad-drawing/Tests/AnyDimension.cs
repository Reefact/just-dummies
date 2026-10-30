// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Dimension --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Drafting.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Dimension" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyDimension : IAny<Dimension> {

    private readonly IAny<double>       _nominal;
    private readonly IAny<LengthUnit>   _unit;
    private readonly IAny<IsoTolerance> _tolerance;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyDimension()
        : this(nominal:   AnyValidNominal(),
               unit:      AnyValidUnit(),
               tolerance: AnyValidTolerance()) { }

    private static AnyDouble AnyValidNominal() {
        // Validate() caps the nominal at 500 mm whatever the unit; an inch value is multiplied by 25.4,
        // so a bound that holds for both units is the inch one.
        return Any.Double().Positive().LessThanOrEqualTo(StandardTolerance.LargestNominalMillimetres / Dimension.MillimetresPerInch);
    }

    private static AnyEnum<LengthUnit> AnyValidUnit() {
        return Any.Enum<LengthUnit>();
    }

    private static AnyIsoTolerance AnyValidTolerance() {
        return new AnyIsoTolerance();
    }

    private AnyDimension(IAny<double>       nominal,
                         IAny<LengthUnit>   unit,
                         IAny<IsoTolerance> tolerance) {
        _nominal   = nominal;
        _unit      = unit;
        _tolerance = tolerance;
    }

    /// <summary>Pins <c>nominal</c> to a fixed value.</summary>
    public AnyDimension WithNominal(double value) {
        return new AnyDimension(new FixedValue<double>(value), _unit, _tolerance);
    }

    /// <summary>Draws <c>nominal</c> from <c>AnyValidNominal()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDimension WithNominal(Func<AnyDouble, AnyDouble> configure) {
        return new AnyDimension(configure(AnyValidNominal()), _unit, _tolerance);
    }

    /// <summary>Pins <c>unit</c> to a fixed value.</summary>
    public AnyDimension WithUnit(LengthUnit value) {
        return new AnyDimension(_nominal, new FixedValue<LengthUnit>(value), _tolerance);
    }

    /// <summary>Draws <c>unit</c> from <c>AnyValidUnit()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDimension WithUnit(Func<AnyEnum<LengthUnit>, AnyEnum<LengthUnit>> configure) {
        return new AnyDimension(_nominal, configure(AnyValidUnit()), _tolerance);
    }

    /// <summary>Pins <c>tolerance</c> to a fixed value.</summary>
    public AnyDimension WithTolerance(IsoTolerance value) {
        return new AnyDimension(_nominal, _unit, new FixedValue<IsoTolerance>(value));
    }

    /// <summary>Draws <c>tolerance</c> from <c>AnyValidTolerance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyDimension WithTolerance(Func<AnyIsoTolerance, AnyIsoTolerance> configure) {
        return new AnyDimension(_nominal, _unit, configure(AnyValidTolerance()));
    }

    /// <summary>Produces one arbitrary <see cref="Dimension" />.</summary>
    public Dimension Generate() {
        return new Dimension(_nominal.Generate(),
                             _unit.Generate(),
                             _tolerance.Generate());
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
