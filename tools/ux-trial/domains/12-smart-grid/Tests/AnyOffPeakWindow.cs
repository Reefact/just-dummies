// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate OffPeakWindow --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Edited by hand: the end of a window must differ from its start, a rule between two parameters that
// the scaffolded shape (one independent recipe per parameter) has no place for. The end's recipe is
// therefore a function of the drawn start.

using System;

using JustDummies;

namespace SmartGrid.Domain;

/// <summary>
///     A generator of arbitrary <see cref="OffPeakWindow" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyOffPeakWindow : IAny<OffPeakWindow> {

    private static readonly TimeSpan HalfHour = TimeSpan.FromMinutes(30);

    private readonly IAny<TimeOnly>                 _start;
    private readonly Func<TimeOnly, IAny<TimeOnly>> _end;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyOffPeakWindow()
        : this(start: AnyValidStart(),
               end:   start => AnyValidEnd(start)) { }

    private static AnyTimeOnly AnyValidStart() {
        return Any.TimeOnly().WithGranularity(HalfHour);
    }

    private static AnyTimeOnly AnyValidEnd(TimeOnly start) {
        return Any.TimeOnly().WithGranularity(HalfHour).DifferentFrom(start);
    }

    private AnyOffPeakWindow(IAny<TimeOnly>                 start,
                             Func<TimeOnly, IAny<TimeOnly>> end) {
        _start = start;
        _end   = end;
    }

    /// <summary>Pins <c>start</c> to a fixed value.</summary>
    public AnyOffPeakWindow WithStart(TimeOnly value) {
        return new AnyOffPeakWindow(new FixedValue<TimeOnly>(value), _end);
    }

    /// <summary>Draws <c>start</c> from <c>AnyValidStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyOffPeakWindow WithStart(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        return new AnyOffPeakWindow(configure(AnyValidStart()), _end);
    }

    /// <summary>Pins <c>end</c> to a fixed value.</summary>
    public AnyOffPeakWindow WithEnd(TimeOnly value) {
        return new AnyOffPeakWindow(_start, _ => new FixedValue<TimeOnly>(value));
    }

    /// <summary>Draws <c>end</c> from <c>AnyValidEnd(start)</c>, refined by <paramref name="configure" />.</summary>
    public AnyOffPeakWindow WithEnd(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        return new AnyOffPeakWindow(_start, start => configure(AnyValidEnd(start)));
    }

    /// <summary>Produces one arbitrary <see cref="OffPeakWindow" />.</summary>
    public OffPeakWindow Generate() {
        TimeOnly start = _start.Generate();

        return new OffPeakWindow(start, _end(start).Generate());
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
