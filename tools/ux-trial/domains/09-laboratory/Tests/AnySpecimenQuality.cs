// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate SpecimenQuality --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Laboratory.Domain;

/// <summary>
///     A generator of arbitrary <see cref="SpecimenQuality" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySpecimenQuality : IAny<SpecimenQuality> {

    private readonly IAny<double>   _volumeMillilitres;
    private readonly IAny<int>      _hemolysisIndex;
    private readonly IAny<TimeSpan> _maxTransit;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySpecimenQuality()
        : this(volumeMillilitres: AnyValidVolumeMillilitres(),
               hemolysisIndex:    AnyValidHemolysisIndex(),
               maxTransit:        AnyValidMaxTransit()) { }

    private static AnyDouble AnyValidVolumeMillilitres() {
        // !(volume > 0) also refuses NaN, which is never drawn: Positive() says the rest.
        return Any.Double().Positive().LessThanOrEqualTo(50d);
    }

    private static AnyInt32 AnyValidHemolysisIndex() {
        // hemolysisIndex is < 0 or > 500 is refused.
        return Any.Int32().Between(0, 500);
    }

    private static AnyTimeSpan AnyValidMaxTransit() {
        // ThrowIfLessThanOrEqual(maxTransit, TimeSpan.Zero).
        return Any.TimeSpan().Positive();
    }

    private AnySpecimenQuality(IAny<double>   volumeMillilitres,
                               IAny<int>      hemolysisIndex,
                               IAny<TimeSpan> maxTransit) {
        _volumeMillilitres = volumeMillilitres;
        _hemolysisIndex    = hemolysisIndex;
        _maxTransit        = maxTransit;
    }

    /// <summary>Pins <c>volumeMillilitres</c> to a fixed value.</summary>
    public AnySpecimenQuality WithVolumeMillilitres(double value) {
        return new AnySpecimenQuality(new FixedValue<double>(value), _hemolysisIndex, _maxTransit);
    }

    /// <summary>Draws <c>volumeMillilitres</c> from <c>AnyValidVolumeMillilitres()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimenQuality WithVolumeMillilitres(Func<AnyDouble, AnyDouble> configure) {
        return new AnySpecimenQuality(configure(AnyValidVolumeMillilitres()), _hemolysisIndex, _maxTransit);
    }

    /// <summary>Pins <c>hemolysisIndex</c> to a fixed value.</summary>
    public AnySpecimenQuality WithHemolysisIndex(int value) {
        return new AnySpecimenQuality(_volumeMillilitres, new FixedValue<int>(value), _maxTransit);
    }

    /// <summary>Draws <c>hemolysisIndex</c> from <c>AnyValidHemolysisIndex()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimenQuality WithHemolysisIndex(Func<AnyInt32, AnyInt32> configure) {
        return new AnySpecimenQuality(_volumeMillilitres, configure(AnyValidHemolysisIndex()), _maxTransit);
    }

    /// <summary>Pins <c>maxTransit</c> to a fixed value.</summary>
    public AnySpecimenQuality WithMaxTransit(TimeSpan value) {
        return new AnySpecimenQuality(_volumeMillilitres, _hemolysisIndex, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws <c>maxTransit</c> from <c>AnyValidMaxTransit()</c>, refined by <paramref name="configure" />.</summary>
    public AnySpecimenQuality WithMaxTransit(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnySpecimenQuality(_volumeMillilitres, _hemolysisIndex, configure(AnyValidMaxTransit()));
    }

    /// <summary>Produces one arbitrary <see cref="SpecimenQuality" />.</summary>
    public SpecimenQuality Generate() {
        return new SpecimenQuality(_volumeMillilitres.Generate(),
                                   _hemolysisIndex.Generate(),
                                   _maxTransit.Generate());
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
