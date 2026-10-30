// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate ShiftWindow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>
///     A generator of arbitrary <see cref="ShiftWindow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyShiftWindow : IAny<ShiftWindow> {

    private readonly IAny<DateTime> _start;
    private readonly IAny<TimeSpan> _length;

    // Rewritten by hand: start < end and (end - start) <= 12 h tie the two parameters together, which
    // two independent per-parameter recipes cannot express. The window is drawn as a start plus a length.

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyShiftWindow()
        : this(start:  AnyValidStart(),
               length: AnyValidLength()) { }

    private static AnyDateTime AnyValidStart() {
        return Any.DateTime()
                  .Between(new DateTime(2020, 1, 1), new DateTime(2035, 12, 31))
                  .WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeSpan AnyValidLength() {
        return Any.TimeSpan()
                  .Between(TimeSpan.FromMinutes(1), ShiftWindow.LongestShift)
                  .WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyShiftWindow(IAny<DateTime> start,
                           IAny<TimeSpan> length) {
        _start  = start;
        _length = length;
    }

    /// <summary>Pins <c>start</c> to a fixed value.</summary>
    public AnyShiftWindow WithStart(DateTime value) {
        return new AnyShiftWindow(new FixedValue<DateTime>(value), _length);
    }

    /// <summary>Draws <c>start</c> from <c>AnyValidStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShiftWindow WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        return new AnyShiftWindow(configure(AnyValidStart()), _length);
    }

    /// <summary>Pins the window's length to a fixed value.</summary>
    public AnyShiftWindow WithLength(TimeSpan value) {
        return new AnyShiftWindow(_start, new FixedValue<TimeSpan>(value));
    }

    /// <summary>Draws the window's length from <c>AnyValidLength()</c>, refined by <paramref name="configure" />.</summary>
    public AnyShiftWindow WithLength(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        return new AnyShiftWindow(_start, configure(AnyValidLength()));
    }

    /// <summary>Produces one arbitrary <see cref="ShiftWindow" />.</summary>
    public ShiftWindow Generate() {
        DateTime start = _start.Generate();

        return new ShiftWindow(start, start + _length.Generate());
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
