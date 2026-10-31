// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeasuredVolume --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="MeasuredVolume" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasuredVolume : IAny<MeasuredVolume> {

    private readonly IAny<double>               _volumeMm3;
    private readonly Func<double, IAny<double>> _uncertaintyMm3;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasuredVolume()
        : this(volumeMm3:      AnyValidVolumeMm3(),
               uncertaintyMm3: AnyValidUncertaintyMm3) { }

    private static AnyDouble AnyValidVolumeMm3() {
        return Any.Double().GreaterThan(0.0).LessThanOrEqualTo(MeasuredVolume.MaxVolumeMm3);
    }

    // The uncertainty is tied to the volume (it must stay below it), so its base is a function of the
    // volume drawn: every refinement starts from it and keeps the tie.
    private static AnyDouble AnyValidUncertaintyMm3(double volumeMm3) {
        return Any.Double().GreaterThanOrEqualTo(0.0).LessThan(volumeMm3);
    }

    private AnyMeasuredVolume(IAny<double>               volumeMm3,
                              Func<double, IAny<double>> uncertaintyMm3) {
        _volumeMm3      = volumeMm3;
        _uncertaintyMm3 = uncertaintyMm3;
    }

    /// <summary>Pins <c>volumeMm3</c> to a fixed value.</summary>
    public AnyMeasuredVolume WithVolumeMm3(double value) {
        return new AnyMeasuredVolume(new FixedValue<double>(value), _uncertaintyMm3);
    }

    /// <summary>Draws <c>volumeMm3</c> from <c>AnyValidVolumeMm3()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasuredVolume WithVolumeMm3(Func<AnyDouble, AnyDouble> configure) {
        return new AnyMeasuredVolume(configure(AnyValidVolumeMm3()), _uncertaintyMm3);
    }

    /// <summary>Pins <c>uncertaintyMm3</c> to a fixed value.</summary>
    public AnyMeasuredVolume WithUncertaintyMm3(double value) {
        return new AnyMeasuredVolume(_volumeMm3, _ => new FixedValue<double>(value));
    }

    /// <summary>Draws <c>uncertaintyMm3</c> below the drawn volume, refined by <paramref name="configure" />.</summary>
    public AnyMeasuredVolume WithUncertaintyMm3(Func<AnyDouble, AnyDouble> configure) {
        return new AnyMeasuredVolume(_volumeMm3, volume => configure(AnyValidUncertaintyMm3(volume)));
    }

    /// <summary>Produces one arbitrary <see cref="MeasuredVolume" />.</summary>
    public MeasuredVolume Generate() {
        double volumeMm3 = _volumeMm3.Generate();

        return new MeasuredVolume(volumeMm3,
                                  _uncertaintyMm3(volumeMm3).Generate());
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
