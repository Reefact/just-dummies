// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TimeBucket --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace LiquidityGap.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TimeBucket" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTimeBucket : IAny<TimeBucket> {

    private readonly IAny<string> _label;
    private readonly IAny<int>    _startDay;
    private readonly IAny<int?>   _endDay;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTimeBucket()
        : this(label:    AnyValidLabel(),
               startDay: AnyValidStartDay(),
               endDay:   AnyValidEndDay().AsNullable()) { }

    private static AnyString AnyValidLabel() {
        return Any.String().NotBlank();
    }

    private static AnyInt32 AnyValidStartDay() {
        // A standalone bucket: starts within ten years of the as-of date; endDay is drawn past that window,
        // so 'endDay > startDay' holds by construction. A ladder is built by AnyBucketLadder, not from this.
        return Any.Int32().Between(0, 3_650);
    }

    private static AnyInt32 AnyValidEndDay() {
        // Past every start AnyValidStartDay can draw (see there).
        return Any.Int32().Between(3_651, 7_300);
    }

    private AnyTimeBucket(IAny<string> label,
                          IAny<int>    startDay,
                          IAny<int?>   endDay) {
        _label    = label;
        _startDay = startDay;
        _endDay   = endDay;
    }

    /// <summary>Pins <c>label</c> to a fixed value.</summary>
    public AnyTimeBucket WithLabel(string value) {
        return new AnyTimeBucket(new FixedValue<string>(value), _startDay, _endDay);
    }

    /// <summary>Draws <c>label</c> from <c>AnyValidLabel()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeBucket WithLabel(Func<AnyString, AnyString> configure) {
        return new AnyTimeBucket(configure(AnyValidLabel()), _startDay, _endDay);
    }

    /// <summary>Pins <c>startDay</c> to a fixed value.</summary>
    public AnyTimeBucket WithStartDay(int value) {
        return new AnyTimeBucket(_label, new FixedValue<int>(value), _endDay);
    }

    /// <summary>Draws <c>startDay</c> from <c>AnyValidStartDay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeBucket WithStartDay(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTimeBucket(_label, configure(AnyValidStartDay()), _endDay);
    }

    /// <summary>Pins <c>endDay</c> to a fixed value.</summary>
    public AnyTimeBucket WithEndDay(int? value) {
        return new AnyTimeBucket(_label, _startDay, new FixedValue<int?>(value));
    }

    /// <summary>Draws <c>endDay</c> from <c>AnyValidEndDay()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTimeBucket WithEndDay(Func<AnyInt32, AnyInt32> configure) {
        return new AnyTimeBucket(_label, _startDay, configure(AnyValidEndDay()).AsNullable());
    }

    /// <summary>Produces one arbitrary <see cref="TimeBucket" />.</summary>
    public TimeBucket Generate() {
        return new TimeBucket(_label.Generate(),
                              _startDay.Generate(),
                              _endDay.Generate());
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
