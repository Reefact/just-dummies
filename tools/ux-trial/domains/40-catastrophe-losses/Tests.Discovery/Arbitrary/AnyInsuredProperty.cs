// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate InsuredProperty --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="InsuredProperty" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyInsuredProperty : IAny<InsuredProperty> {

    private readonly IAny<string>             _id;
    private readonly IAny<GeoPoint>           _location;
    private readonly IAny<decimal>            _insuredValue;
    private readonly IAny<VulnerabilityCurve> _vulnerability;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyInsuredProperty()
        : this(id:            AnyValidId(),
               location:      AnyValidLocation(),
               insuredValue:  AnyValidInsuredValue(),
               vulnerability: AnyValidVulnerability()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching("PRP-[0-9]{6}");
    }

    private static AnyGeoPoint AnyValidLocation() {
        return new AnyGeoPoint();
    }

    private static AnyDecimal AnyValidInsuredValue() {
        return Any.Decimal().Positive().WithScale(2);
    }

    private static AnyVulnerabilityCurve AnyValidVulnerability() {
        return new AnyVulnerabilityCurve();
    }

    private AnyInsuredProperty(IAny<string>             id,
                               IAny<GeoPoint>           location,
                               IAny<decimal>            insuredValue,
                               IAny<VulnerabilityCurve> vulnerability) {
        _id            = id;
        _location      = location;
        _insuredValue  = insuredValue;
        _vulnerability = vulnerability;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyInsuredProperty WithId(string value) {
        return new AnyInsuredProperty(new FixedValue<string>(value), _location, _insuredValue, _vulnerability);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyInsuredProperty WithId(Func<AnyPattern, AnyPattern> configure) {
        return new AnyInsuredProperty(configure(AnyValidId()), _location, _insuredValue, _vulnerability);
    }

    /// <summary>Pins <c>location</c> to a fixed value.</summary>
    public AnyInsuredProperty WithLocation(GeoPoint value) {
        return new AnyInsuredProperty(_id, new FixedValue<GeoPoint>(value), _insuredValue, _vulnerability);
    }

    /// <summary>Draws <c>location</c> from <c>AnyValidLocation()</c>, refined by <paramref name="configure" />.</summary>
    public AnyInsuredProperty WithLocation(Func<AnyGeoPoint, AnyGeoPoint> configure) {
        return new AnyInsuredProperty(_id, configure(AnyValidLocation()), _insuredValue, _vulnerability);
    }

    /// <summary>Pins <c>insuredValue</c> to a fixed value.</summary>
    public AnyInsuredProperty WithInsuredValue(decimal value) {
        return new AnyInsuredProperty(_id, _location, new FixedValue<decimal>(value), _vulnerability);
    }

    /// <summary>Draws <c>insuredValue</c> from <c>AnyValidInsuredValue()</c>, refined by <paramref name="configure" />.</summary>
    public AnyInsuredProperty WithInsuredValue(Func<AnyDecimal, AnyDecimal> configure) {
        return new AnyInsuredProperty(_id, _location, configure(AnyValidInsuredValue()), _vulnerability);
    }

    /// <summary>Pins <c>vulnerability</c> to a fixed value.</summary>
    public AnyInsuredProperty WithVulnerability(VulnerabilityCurve value) {
        return new AnyInsuredProperty(_id, _location, _insuredValue, new FixedValue<VulnerabilityCurve>(value));
    }

    /// <summary>Draws <c>vulnerability</c> from <c>AnyValidVulnerability()</c>, refined by <paramref name="configure" />.</summary>
    public AnyInsuredProperty WithVulnerability(Func<AnyVulnerabilityCurve, AnyVulnerabilityCurve> configure) {
        return new AnyInsuredProperty(_id, _location, _insuredValue, configure(AnyValidVulnerability()));
    }

    /// <summary>Produces one arbitrary <see cref="InsuredProperty" />.</summary>
    public InsuredProperty Generate() {
        return new InsuredProperty(_id.Generate(),
                                   _location.Generate(),
                                   _insuredValue.Generate(),
                                   _vulnerability.Generate());
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
