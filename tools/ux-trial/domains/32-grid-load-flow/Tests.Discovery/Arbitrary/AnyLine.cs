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
    private readonly IAny<BusId>     _toBus;
    private readonly IAny<Impedance> _impedance;
    private readonly IAny<double>    _chargingSusceptancePu;
    private readonly IAny<double>    _ratingMva;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyLine()
        : this(id:                    AnyValidId(),
               fromBus:               AnyValidFromBus(),
               toBus:                 AnyValidToBus(),
               impedance:             AnyValidImpedance(),
               chargingSusceptancePu: AnyValidChargingSusceptancePu(),
               ratingMva:             AnyValidRatingMva()) { }

    private static AnyString AnyValidId() {
        return Any.String().NotBlank().WithMaxLength(32);
    }

    private static AnyBusId AnyValidFromBus() {
        // Line.cs refuses a line from a bus to itself: the two ends are drawn from disjoint halves of the alphabet.
        return new AnyBusId().WithValue(_ => Any.StringMatching("[A-M][A-Z0-9_-]{0,15}"));
    }

    private static AnyBusId AnyValidToBus() {
        // Line.cs refuses a line from a bus to itself: the two ends are drawn from disjoint halves of the alphabet.
        return new AnyBusId().WithValue(_ => Any.StringMatching("[N-Z][A-Z0-9_-]{0,15}"));
    }

    private static AnyImpedance AnyValidImpedance() {
        return new AnyImpedance();
    }

    private static AnyDouble AnyValidChargingSusceptancePu() {
        return Any.Double().Between(0.0, 5.0);
    }

    private static AnyDouble AnyValidRatingMva() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(10_000.0);
    }

    private AnyLine(IAny<string>    id,
                    IAny<BusId>     fromBus,
                    IAny<BusId>     toBus,
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
        return new AnyLine(_id, _fromBus, new FixedValue<BusId>(value), _impedance, _chargingSusceptancePu, _ratingMva);
    }

    /// <summary>Draws <c>toBus</c> from <c>AnyValidToBus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyLine WithToBus(Func<AnyBusId, AnyBusId> configure) {
        return new AnyLine(_id, _fromBus, configure(AnyValidToBus()), _impedance, _chargingSusceptancePu, _ratingMva);
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
        return new Line(_id.Generate(),
                        _fromBus.Generate(),
                        _toBus.Generate(),
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
