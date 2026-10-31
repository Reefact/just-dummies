// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate CatastropheEvent --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace CatastropheLosses.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CatastropheEvent" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCatastropheEvent : IAny<CatastropheEvent> {

    private readonly IAny<string>    _id;
    private readonly IAny<Peril>     _peril;
    private readonly IAny<Footprint> _footprint;
    private readonly IAny<double>    _annualRate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCatastropheEvent()
        : this(id:         AnyValidId(),
               peril:      AnyValidPeril(),
               footprint:  AnyValidFootprint(),
               annualRate: AnyValidAnnualRate()) { }

    private static AnyPattern AnyValidId() {
        return Any.StringMatching("EV-[0-9]{4,8}");
    }

    private static AnyEnum<Peril> AnyValidPeril() {
        return Any.Enum<Peril>();
    }

    private static AnyFootprint AnyValidFootprint() {
        return new AnyFootprint();
    }

    private static AnyDouble AnyValidAnnualRate() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(CatastropheEvent.MaxAnnualRate);
    }

    private AnyCatastropheEvent(IAny<string>    id,
                                IAny<Peril>     peril,
                                IAny<Footprint> footprint,
                                IAny<double>    annualRate) {
        _id         = id;
        _peril      = peril;
        _footprint  = footprint;
        _annualRate = annualRate;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyCatastropheEvent WithId(string value) {
        return new AnyCatastropheEvent(new FixedValue<string>(value), _peril, _footprint, _annualRate);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheEvent WithId(Func<AnyPattern, AnyPattern> configure) {
        return new AnyCatastropheEvent(configure(AnyValidId()), _peril, _footprint, _annualRate);
    }

    /// <summary>Pins <c>peril</c> to a fixed value.</summary>
    public AnyCatastropheEvent WithPeril(Peril value) {
        return new AnyCatastropheEvent(_id, new FixedValue<Peril>(value), _footprint, _annualRate);
    }

    /// <summary>Draws <c>peril</c> from <c>AnyValidPeril()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheEvent WithPeril(Func<AnyEnum<Peril>, AnyEnum<Peril>> configure) {
        return new AnyCatastropheEvent(_id, configure(AnyValidPeril()), _footprint, _annualRate);
    }

    /// <summary>Pins <c>footprint</c> to a fixed value.</summary>
    public AnyCatastropheEvent WithFootprint(Footprint value) {
        return new AnyCatastropheEvent(_id, _peril, new FixedValue<Footprint>(value), _annualRate);
    }

    /// <summary>Draws <c>footprint</c> from <c>AnyValidFootprint()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheEvent WithFootprint(Func<AnyFootprint, AnyFootprint> configure) {
        return new AnyCatastropheEvent(_id, _peril, configure(AnyValidFootprint()), _annualRate);
    }

    /// <summary>Pins <c>annualRate</c> to a fixed value.</summary>
    public AnyCatastropheEvent WithAnnualRate(double value) {
        return new AnyCatastropheEvent(_id, _peril, _footprint, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>annualRate</c> from <c>AnyValidAnnualRate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCatastropheEvent WithAnnualRate(Func<AnyDouble, AnyDouble> configure) {
        return new AnyCatastropheEvent(_id, _peril, _footprint, configure(AnyValidAnnualRate()));
    }

    /// <summary>Produces one arbitrary <see cref="CatastropheEvent" />.</summary>
    public CatastropheEvent Generate() {
        return new CatastropheEvent(_id.Generate(),
                                    _peril.Generate(),
                                    _footprint.Generate(),
                                    _annualRate.Generate());
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
