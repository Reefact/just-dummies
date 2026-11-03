// Scaffolded by `any` (JustDummies), then rewritten by hand: the constructor ties `end` to `start` twice
// (start before end, and at most twelve hours apart), which no per-parameter recipe can honour.
// The window is therefore drawn as a start and a length, and the end derived from them.

using System;

using JustDummies;

namespace Manufacturing.Domain;

/// <summary>A generator of arbitrary <see cref="ShiftWindow" /> values: a start and a length of one minute to twelve hours.</summary>
public sealed partial class AnyShiftWindow : IAny<ShiftWindow> {

    private IAny<DateTime> _start;
    private IAny<TimeSpan> _length;

    private AnyDateTime _startRecipe;
    private AnyTimeSpan _lengthRecipe;

    public AnyShiftWindow()
        : this(start:  AnyValidStart(),
               length: AnyValidLength()) { }

    private static AnyDateTime AnyValidStart() {
        return Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeSpan AnyValidLength() {
        return Any.TimeSpan().Between(TimeSpan.FromMinutes(1), ShiftWindow.LongestShift).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyShiftWindow(AnyDateTime start, AnyTimeSpan length) {
        _start        = start;
        _startRecipe  = start;
        _length       = length;
        _lengthRecipe = length;
    }

    private AnyShiftWindow(AnyShiftWindow from) {
        _start        = from._start;
        _startRecipe  = from._startRecipe;
        _length       = from._length;
        _lengthRecipe = from._lengthRecipe;
    }

    public AnyShiftWindow WithStart(DateTime value) {
        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._start = Any.Fixed(value);

        return copy;
    }

    public AnyShiftWindow WithStart(Func<AnyDateTime, AnyDateTime> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._startRecipe = configure(_startRecipe);
        copy._start       = copy._startRecipe;

        return copy;
    }

    public AnyShiftWindow WithLength(TimeSpan value) {
        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._length = Any.Fixed(value);

        return copy;
    }

    public AnyShiftWindow WithLength(Func<AnyTimeSpan, AnyTimeSpan> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyShiftWindow copy = new AnyShiftWindow(this);

        copy._lengthRecipe = configure(_lengthRecipe);
        copy._length       = copy._lengthRecipe;

        return copy;
    }

    public ShiftWindow Generate() {
        DateTime start = _start.Generate();

        return new ShiftWindow(start, start + _length.Generate());
    }

}
