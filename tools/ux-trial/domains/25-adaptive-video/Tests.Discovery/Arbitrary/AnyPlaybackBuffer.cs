// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate PlaybackBuffer --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="PlaybackBuffer" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyPlaybackBuffer : IAny<PlaybackBuffer> {

    private readonly IAny<double> _levelSeconds;
    private readonly IAny<double> _capacitySeconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyPlaybackBuffer()
        : this(levelSeconds:    AnyValidLevelSeconds(),
               capacitySeconds: AnyValidCapacitySeconds()) { }

    private static AnyDouble AnyValidLevelSeconds() {
        // The level may not exceed the capacity: the level base stays below the smallest capacity.
        return Any.Double().Between(0, 4);
    }

    private static AnyDouble AnyValidCapacitySeconds() {
        return Any.Double().Between(4, 300);
    }

    private AnyPlaybackBuffer(IAny<double> levelSeconds,
                              IAny<double> capacitySeconds) {
        _levelSeconds    = levelSeconds;
        _capacitySeconds = capacitySeconds;
    }

    /// <summary>Pins <c>levelSeconds</c> to a fixed value.</summary>
    public AnyPlaybackBuffer WithLevelSeconds(double value) {
        return new AnyPlaybackBuffer(new FixedValue<double>(value), _capacitySeconds);
    }

    /// <summary>Draws <c>levelSeconds</c> from <c>AnyValidLevelSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackBuffer WithLevelSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPlaybackBuffer(configure(AnyValidLevelSeconds()), _capacitySeconds);
    }

    /// <summary>Pins <c>capacitySeconds</c> to a fixed value.</summary>
    public AnyPlaybackBuffer WithCapacitySeconds(double value) {
        return new AnyPlaybackBuffer(_levelSeconds, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>capacitySeconds</c> from <c>AnyValidCapacitySeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnyPlaybackBuffer WithCapacitySeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnyPlaybackBuffer(_levelSeconds, configure(AnyValidCapacitySeconds()));
    }

    /// <summary>Produces one arbitrary <see cref="PlaybackBuffer" />.</summary>
    public PlaybackBuffer Generate() {
        return new PlaybackBuffer(_levelSeconds.Generate(),
                                  _capacitySeconds.Generate());
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
