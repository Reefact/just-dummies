// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Line --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Line" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyLine : IAny<Line> {

    private readonly IAny<string>    _id;
    private readonly IAny<BusId>     _fromBus;
    private readonly Func<BusId, IAny<BusId>> _toBus;
    private readonly IAny<Impedance> _impedance;
    private readonly IAny<double>    _chargingSusceptancePu;
    private readonly IAny<double>    _ratingMva;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLine()
        : this(id:                    AnyValidId(),
               fromBus:               AnyValidFromBus(),
               toBus:                 f => AnyValidToBus(f),
               impedance:             AnyValidImpedance(),
               chargingSusceptancePu: AnyValidChargingSusceptancePu(),
               ratingMva:             AnyValidRatingMva()) { }

    private static AnyString AnyValidId() {
        return Any.String().NotBlank().WithMaxLength(32);
    }

    private static AnyBusId AnyValidFromBus() {
        // Any bus; the other end is drawn apart from it (see AnyValidToBus).
        return new AnyBusId();
    }

    private static AnyBusId AnyValidToBus(BusId fromBus) {
        // The constructor refuses a branch joining a bus to itself: drawn apart from the other end.
        return new AnyBusId().WithValue(v => v.DifferentFrom(fromBus.Value));
    }

    private static AnyImpedance AnyValidImpedance() {
        return new AnyImpedance();
    }

    private static AnyDouble AnyValidChargingSusceptancePu() {
        // B in [0, Line.MaximumChargingSusceptancePu].
        return Any.Double().Between(0.0, Line.MaximumChargingSusceptancePu);
    }

    private static AnyDouble AnyValidRatingMva() {
        // Rating in (0, Line.MaximumRatingMva].
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Line.MaximumRatingMva);
    }

    private AnyLine(IAny<string>    id,
                    IAny<BusId>     fromBus,
                    Func<BusId, IAny<BusId>> toBus,
                    IAny<Impedance> impedance,
                    IAny<double>    chargingSusceptancePu,
                    IAny<double>    ratingMva) {
        _id                    = id;
        _fromBus               = fromBus;
        _toBus                 = toBus;
        _impedance             = impedance;
        _chargingSusceptancePu = chargingSusceptancePu;
        _ratingMva             = ratingMva;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyLine WithId(string value) {
        return new AnyLine(new FixedValue<string>(value), _fromBus, _toBus, _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithId(Func<AnyString, AnyString> configure) {
        return new AnyLine(configure(AnyValidId()), _fromBus, _toBus, _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Pins <c>fromBus</c> to a fixed value.</summary>
    public AnyLine WithFromBus(BusId value) {
        return new AnyLine(_id, new FixedValue<BusId>(value), _toBus, _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Draws <c>fromBus</c> from <c>AnyValidFromBus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithFromBus(Func<AnyBusId, AnyBusId> configure) {
        return new AnyLine(_id, configure(AnyValidFromBus()), _toBus, _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Pins <c>toBus</c> to a fixed value.</summary>
    public AnyLine WithToBus(BusId value) {
        return new AnyLine(_id, _fromBus, _ => new FixedValue<BusId>(value), _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Draws <c>toBus</c> from <c>AnyValidToBus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithToBus(Func<AnyBusId, AnyBusId> configure) {
        return new AnyLine(_id, _fromBus, f => configure(AnyValidToBus(f)), _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Pins <c>impedance</c> to a fixed value.</summary>
    public AnyLine WithImpedance(Impedance value) {
        return new AnyLine(_id, _fromBus, _toBus, new FixedValue<Impedance>(value), _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Draws <c>impedance</c> from <c>AnyValidImpedance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithImpedance(Func<AnyImpedance, AnyImpedance> configure) {
        return new AnyLine(_id, _fromBus, _toBus, configure(AnyValidImpedance()), _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Pins <c>chargingSusceptancePu</c> to a fixed value.</summary>
    public AnyLine WithChargingSusceptancePu(double value) {
        return new AnyLine(_id, _fromBus, _toBus, _impedance, new FixedValue<double>(value), _ratingMva);
    }

    /// <summary>Draws <c>chargingSusceptancePu</c> from <c>AnyValidChargingSusceptancePu()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithChargingSusceptancePu(Func<AnyDouble, AnyDouble> configure) {
        return new AnyLine(_id, _fromBus, _toBus, _impedance, configure(AnyValidChargingSusceptancePu()), _ratingMva);
    }

    /// <summary>Pins <c>ratingMva</c> to a fixed value.</summary>
    public AnyLine WithRatingMva(double value) {
        return new AnyLine(_id, _fromBus, _toBus, _impedance, _chargingSusceptancePu, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>ratingMva</c> from <c>AnyValidRatingMva()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithRatingMva(Func<AnyDouble, AnyDouble> configure) {
        return new AnyLine(_id, _fromBus, _toBus, _impedance, _chargingSusceptancePu, configure(AnyValidRatingMva()));
    }

    /// <summary>Produces one arbitrary <see cref="Line" />.</summary>
    public Line Generate() {
        BusId fromBus = _fromBus.Generate();

        return new Line(_id.Generate(),
                        fromBus,
                        _toBus(fromBus).Generate(),
                        _impedance.Generate(),
                        _chargingSusceptancePu.Generate(),
                        _ratingMva.Generate());
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
