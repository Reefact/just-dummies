// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate VolumeChange --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;
using PulmonaryNodules.Domain;

namespace PulmonaryNodules.Tests.Expert.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="VolumeChange" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyVolumeChange : IAny<VolumeChange> {

    private readonly IAny<MeasuredVolume> _before;
    private readonly IAny<MeasuredVolume> _after;
    private readonly IAny<int>            _intervalDays;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyVolumeChange()
        : this(before:       AnyValidBefore(),
               after:        AnyValidAfter(),
               intervalDays: AnyValidIntervalDays()) { }

    private static AnyMeasuredVolume AnyValidBefore() {
        return new AnyMeasuredVolume();
    }

    private static AnyMeasuredVolume AnyValidAfter() {
        return new AnyMeasuredVolume();
    }

    private static AnyInt32 AnyValidIntervalDays() {
        return Any.Int32().Positive();
    }

    private AnyVolumeChange(IAny<MeasuredVolume> before,
                            IAny<MeasuredVolume> after,
                            IAny<int>            intervalDays) {
        _before       = before;
        _after        = after;
        _intervalDays = intervalDays;
    }

    /// <summary>Pins <c>before</c> to a fixed value.</summary>
    public AnyVolumeChange WithBefore(MeasuredVolume value) {
        return new AnyVolumeChange(new FixedValue<MeasuredVolume>(value), _after, _intervalDays);
    }

    /// <summary>Draws <c>before</c> from <c>AnyValidBefore()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVolumeChange WithBefore(Func<AnyMeasuredVolume, AnyMeasuredVolume> configure) {
        return new AnyVolumeChange(configure(AnyValidBefore()), _after, _intervalDays);
    }

    /// <summary>Pins <c>after</c> to a fixed value.</summary>
    public AnyVolumeChange WithAfter(MeasuredVolume value) {
        return new AnyVolumeChange(_before, new FixedValue<MeasuredVolume>(value), _intervalDays);
    }

    /// <summary>Draws <c>after</c> from <c>AnyValidAfter()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVolumeChange WithAfter(Func<AnyMeasuredVolume, AnyMeasuredVolume> configure) {
        return new AnyVolumeChange(_before, configure(AnyValidAfter()), _intervalDays);
    }

    /// <summary>Pins <c>intervalDays</c> to a fixed value.</summary>
    public AnyVolumeChange WithIntervalDays(int value) {
        return new AnyVolumeChange(_before, _after, new FixedValue<int>(value));
    }

    /// <summary>Draws <c>intervalDays</c> from <c>AnyValidIntervalDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyVolumeChange WithIntervalDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyVolumeChange(_before, _after, configure(AnyValidIntervalDays()));
    }

    /// <summary>Produces one arbitrary <see cref="VolumeChange" />.</summary>
    public VolumeChange Generate() {
        return new VolumeChange(_before.Generate(),
                                _after.Generate(),
                                _intervalDays.Generate());
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
