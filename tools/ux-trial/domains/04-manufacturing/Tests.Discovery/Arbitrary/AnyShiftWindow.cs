using System;

using JustDummies;
using Manufacturing.Domain;

namespace Manufacturing.Tests.Discovery.Arbitrary;

/// <summary>
///     A generator of arbitrary <see cref="ShiftWindow" /> values. Hand-completed from the scaffold: the constructor
///     ties <c>end</c> to <c>start</c> by a maximum length, so the window is drawn as a start and a length.
/// </summary>
public sealed partial class AnyShiftWindow : IAny<ShiftWindow> {

    private IAny<DateTime> _start;
    private IAny<TimeSpan> _length;

    public AnyShiftWindow()
        : this(Any.DateTime().Between(new DateTime(2020, 1, 1), new DateTime(2030, 1, 1)),
               Any.TimeSpan().Between(TimeSpan.FromTicks(1), ShiftWindow.LongestShift)) { }

    private AnyShiftWindow(IAny<DateTime> start, IAny<TimeSpan> length) {
        _start  = start;
        _length = length;
    }

    public AnyShiftWindow WithStart(DateTime value) {
        return new AnyShiftWindow(Any.Fixed(value), _length);
    }

    public AnyShiftWindow WithLength(TimeSpan value) {
        return new AnyShiftWindow(_start, Any.Fixed(value));
    }

    public ShiftWindow Generate() {
        DateTime start = _start.Generate();

        return new ShiftWindow(start, start + _length.Generate());
    }

}
