// Scaffolded by `any` (JustDummies), then reworked by hand: the constructor's two guards tie `end` to
// `start` (start < end <= start + 1 year), and the scaffolded shape draws every parameter on its own.
// `end` is therefore no longer a parameter of the generator: it is derived from `start` and a length.

using System;

using JustDummies;

namespace Insurance.Domain;

/// <summary>
///     A generator of arbitrary <see cref="CoveragePeriod" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyCoveragePeriod : IAny<CoveragePeriod> {

    private readonly IAny<DateOnly> _start;
    private readonly IAny<int>      _lengthInDays;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyCoveragePeriod()
        : this(start:        AnyValidStart(),
               lengthInDays: AnyValidLengthInDays()) { }

    private static AnyDateOnly AnyValidStart() {
        // An ordinary underwriting window, so that start + one year never overflows DateOnly.
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2040, 12, 31));
    }

    private static AnyInt32 AnyValidLengthInDays() {
        // end - start, in days: at least one, at most 365 (a year is never shorter than that).
        return Any.Int32().Between(1, 365);
    }

    private AnyCoveragePeriod(IAny<DateOnly> start,
                              IAny<int>      lengthInDays) {
        _start        = start;
        _lengthInDays = lengthInDays;
    }

    /// <summary>Pins <c>start</c> to a fixed value.</summary>
    public AnyCoveragePeriod WithStart(DateOnly value) {
        return new AnyCoveragePeriod(new FixedValue<DateOnly>(value), _lengthInDays);
    }

    /// <summary>Draws <c>start</c> from <c>AnyValidStart()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCoveragePeriod WithStart(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyCoveragePeriod(configure(AnyValidStart()), _lengthInDays);
    }

    /// <summary>Pins the number of days between start and end.</summary>
    public AnyCoveragePeriod WithLengthInDays(int value) {
        return new AnyCoveragePeriod(_start, new FixedValue<int>(value));
    }

    /// <summary>Draws the length from <c>AnyValidLengthInDays()</c>, refined by <paramref name="configure" />.</summary>
    public AnyCoveragePeriod WithLengthInDays(Func<AnyInt32, AnyInt32> configure) {
        return new AnyCoveragePeriod(_start, configure(AnyValidLengthInDays()));
    }

    /// <summary>Produces one arbitrary <see cref="CoveragePeriod" />.</summary>
    public CoveragePeriod Generate() {
        DateOnly start = _start.Generate();

        return new CoveragePeriod(start, start.AddDays(_lengthInDays.Generate()));
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
