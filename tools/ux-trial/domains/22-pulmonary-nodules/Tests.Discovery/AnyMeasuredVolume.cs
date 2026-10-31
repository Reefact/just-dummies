// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate MeasuredVolume --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace PulmonaryNodules.Domain;

/// <summary>
///     A generator of arbitrary <see cref="MeasuredVolume" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyMeasuredVolume : IAny<MeasuredVolume> {

    private readonly IAny<double> _volumeMm3;
    private readonly IAny<double> _uncertaintyMm3;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyMeasuredVolume()
        : this(volumeMm3:      AnyValidVolumeMm3(),
               uncertaintyMm3: AnyValidUncertaintyMm3()) { }

    private static AnyDouble AnyValidVolumeMm3() {
        // Cross-parameter rule: the uncertainty stays below the volume. Volume from 100 mm3 and an
        // uncertainty below 100 mm3 hold it by construction.
        return Any.Double().Between(100.0, MeasuredVolume.MaxVolumeMm3);
    }

    private static AnyDouble AnyValidUncertaintyMm3() {
        return Any.Double().GreaterThanOrEqualTo(0.0).LessThan(100.0);
    }

    private AnyMeasuredVolume(IAny<double> volumeMm3,
                              IAny<double> uncertaintyMm3) {
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
        return new AnyMeasuredVolume(_volumeMm3, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>uncertaintyMm3</c> from <c>AnyValidUncertaintyMm3()</c>, refined by <paramref name="configure" />.</summary>
    public AnyMeasuredVolume WithUncertaintyMm3(Func<AnyDouble, AnyDouble> configure) {
        return new AnyMeasuredVolume(_volumeMm3, configure(AnyValidUncertaintyMm3()));
    }

    /// <summary>Produces one arbitrary <see cref="MeasuredVolume" />.</summary>
    public MeasuredVolume Generate() {
        return new MeasuredVolume(_volumeMm3.Generate(),
                                  _uncertaintyMm3.Generate());
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
