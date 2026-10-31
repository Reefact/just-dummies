// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Transformer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace GridLoadFlow.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Transformer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTransformer : IAny<Transformer> {

    private readonly IAny<string>    _id;
    private readonly IAny<BusId>     _highVoltageBus;
    private readonly Func<BusId, IAny<BusId>> _lowVoltageBus;
    private readonly IAny<Impedance> _impedance;
    private readonly IAny<double>    _ratingMva;
    private readonly IAny<double>    _tapRatio;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTransformer()
        : this(id:             AnyValidId(),
               highVoltageBus: AnyValidHighVoltageBus(),
               lowVoltageBus:  h => AnyValidLowVoltageBus(h),
               impedance:      AnyValidImpedance(),
               ratingMva:      AnyValidRatingMva(),
               tapRatio:       AnyValidTapRatio()) { }

    private static AnyString AnyValidId() {
        return Any.String().NotBlank().WithMaxLength(32);
    }

    private static AnyBusId AnyValidHighVoltageBus() {
        // Any bus; the other end is drawn apart from it (see AnyValidLowVoltageBus).
        return new AnyBusId();
    }

    private static AnyBusId AnyValidLowVoltageBus(BusId highVoltageBus) {
        // The constructor refuses a branch joining a bus to itself: drawn apart from the other end.
        return new AnyBusId().WithValue(v => v.DifferentFrom(highVoltageBus.Value));
    }

    private static AnyImpedance AnyValidImpedance() {
        return new AnyImpedance();
    }

    private static AnyDouble AnyValidRatingMva() {
        // Rating in (0, Line.MaximumRatingMva].
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(Line.MaximumRatingMva);
    }

    private static AnyDouble AnyValidTapRatio() {
        // Off-nominal tap within the end stops.
        return Any.Double().Between(Transformer.MinimumTap, Transformer.MaximumTap);
    }

    private AnyTransformer(IAny<string>    id,
                           IAny<BusId>     highVoltageBus,
                           Func<BusId, IAny<BusId>> lowVoltageBus,
                           IAny<Impedance> impedance,
                           IAny<double>    ratingMva,
                           IAny<double>    tapRatio) {
        _id             = id;
        _highVoltageBus = highVoltageBus;
        _lowVoltageBus  = lowVoltageBus;
        _impedance      = impedance;
        _ratingMva      = ratingMva;
        _tapRatio       = tapRatio;
    }

    /// <summary>Pins <c>id</c> to a fixed value.</summary>
    public AnyTransformer WithId(string value) {
        return new AnyTransformer(new FixedValue<string>(value), _highVoltageBus, _lowVoltageBus, _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Draws <c>id</c> from <c>AnyValidId()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithId(Func<AnyString, AnyString> configure) {
        return new AnyTransformer(configure(AnyValidId()), _highVoltageBus, _lowVoltageBus, _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Pins <c>highVoltageBus</c> to a fixed value.</summary>
    public AnyTransformer WithHighVoltageBus(BusId value) {
        return new AnyTransformer(_id, new FixedValue<BusId>(value), _lowVoltageBus, _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Draws <c>highVoltageBus</c> from <c>AnyValidHighVoltageBus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithHighVoltageBus(Func<AnyBusId, AnyBusId> configure) {
        return new AnyTransformer(_id, configure(AnyValidHighVoltageBus()), _lowVoltageBus, _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Pins <c>lowVoltageBus</c> to a fixed value.</summary>
    public AnyTransformer WithLowVoltageBus(BusId value) {
        return new AnyTransformer(_id, _highVoltageBus, _ => new FixedValue<BusId>(value), _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Draws <c>lowVoltageBus</c> from <c>AnyValidLowVoltageBus()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithLowVoltageBus(Func<AnyBusId, AnyBusId> configure) {
        return new AnyTransformer(_id, _highVoltageBus, h => configure(AnyValidLowVoltageBus(h)), _impedance, _ratingMva, _tapRatio);
    }

    /// <summary>Pins <c>impedance</c> to a fixed value.</summary>
    public AnyTransformer WithImpedance(Impedance value) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, new FixedValue<Impedance>(value), _ratingMva, _tapRatio);
    }

    /// <summary>Draws <c>impedance</c> from <c>AnyValidImpedance()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithImpedance(Func<AnyImpedance, AnyImpedance> configure) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, configure(AnyValidImpedance()), _ratingMva, _tapRatio);
    }

    /// <summary>Pins <c>ratingMva</c> to a fixed value.</summary>
    public AnyTransformer WithRatingMva(double value) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, _impedance, new FixedValue<double>(value), _tapRatio);
    }

    /// <summary>Draws <c>ratingMva</c> from <c>AnyValidRatingMva()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithRatingMva(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, _impedance, configure(AnyValidRatingMva()), _tapRatio);
    }

    /// <summary>Pins <c>tapRatio</c> to a fixed value.</summary>
    public AnyTransformer WithTapRatio(double value) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, _impedance, _ratingMva, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>tapRatio</c> from <c>AnyValidTapRatio()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTransformer WithTapRatio(Func<AnyDouble, AnyDouble> configure) {
        return new AnyTransformer(_id, _highVoltageBus, _lowVoltageBus, _impedance, _ratingMva, configure(AnyValidTapRatio()));
    }

    /// <summary>Produces one arbitrary <see cref="Transformer" />.</summary>
    public Transformer Generate() {
        BusId highVoltageBus = _highVoltageBus.Generate();

        return new Transformer(_id.Generate(),
                               highVoltageBus,
                               _lowVoltageBus(highVoltageBus).Generate(),
                               _impedance.Generate(),
                               _ratingMva.Generate(),
                               _tapRatio.Generate());
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
