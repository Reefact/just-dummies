// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Segment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace AdaptiveVideo.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Segment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnySegment : IAny<Segment> {

    private readonly IAny<int>    _index;
    private readonly IAny<double> _durationSeconds;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnySegment()
        : this(index:           AnyValidIndex(),
               durationSeconds: AnyValidDurationSeconds()) { }

    private static AnyInt32 AnyValidIndex() {
        return Any.Int32().GreaterThanOrEqualTo(0);
    }

    private static AnyDouble AnyValidDurationSeconds() {
        return Any.Double().Between(Segment.MinDurationSeconds, Segment.MaxDurationSeconds);
    }

    private AnySegment(IAny<int>    index,
                       IAny<double> durationSeconds) {
        _index           = index;
        _durationSeconds = durationSeconds;
    }

    /// <summary>Pins <c>index</c> to a fixed value.</summary>
    public AnySegment WithIndex(int value) {
        return new AnySegment(new FixedValue<int>(value), _durationSeconds);
    }

    /// <summary>Draws <c>index</c> from <c>AnyValidIndex()</c>, refined by <paramref name="configure" />.</summary>
    public AnySegment WithIndex(Func<AnyInt32, AnyInt32> configure) {
        return new AnySegment(configure(AnyValidIndex()), _durationSeconds);
    }

    /// <summary>Pins <c>durationSeconds</c> to a fixed value.</summary>
    public AnySegment WithDurationSeconds(double value) {
        return new AnySegment(_index, new FixedValue<double>(value));
    }

    /// <summary>Draws <c>durationSeconds</c> from <c>AnyValidDurationSeconds()</c>, refined by <paramref name="configure" />.</summary>
    public AnySegment WithDurationSeconds(Func<AnyDouble, AnyDouble> configure) {
        return new AnySegment(_index, configure(AnyValidDurationSeconds()));
    }

    /// <summary>Produces one arbitrary <see cref="Segment" />.</summary>
    public Segment Generate() {
        return new Segment(_index.Generate(),
                           _durationSeconds.Generate());
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
