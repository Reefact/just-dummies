// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate TradingSession --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Trading.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TradingSession" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyTradingSession : IAny<TradingSession> {

    private readonly IAny<DateOnly> _date;
    private readonly IAny<TimeOnly> _opensAt;
    private readonly IAny<TimeOnly> _closesAt;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyTradingSession()
        : this(date:     AnyValidDate(),
               opensAt:  AnyValidOpensAt(),
               closesAt: AnyValidClosesAt()) { }

    private static AnyDateOnly AnyValidDate() {
        // TradingSession: never a Saturday or a Sunday. No day-of-week constraint exists, so the
        // weekends of a bounded window are excluded one by one.
        DateOnly       first    = new(2020, 1, 1);
        DateOnly       last     = new(2030, 12, 31);
        List<DateOnly> weekends = [];
        for (DateOnly day = first; day <= last; day = day.AddDays(1)) {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) {
                weekends.Add(day);
            }
        }

        return Any.DateOnly().Between(first, last).Except(weekends.ToArray());
    }

    private static AnyTimeOnly AnyValidOpensAt() {
        // TradingSession: opensAt < closesAt, a relation between two parameters. Splitting the day
        // at noon keeps each base independent and the pair always ordered.
        return Any.TimeOnly().Between(new TimeOnly(0, 0), new TimeOnly(11, 59)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private static AnyTimeOnly AnyValidClosesAt() {
        return Any.TimeOnly().Between(new TimeOnly(12, 0), new TimeOnly(23, 59)).WithGranularity(TimeSpan.FromMinutes(1));
    }

    private AnyTradingSession(IAny<DateOnly> date,
                              IAny<TimeOnly> opensAt,
                              IAny<TimeOnly> closesAt) {
        _date     = date;
        _opensAt  = opensAt;
        _closesAt = closesAt;
    }

    /// <summary>Pins <c>date</c> to a fixed value.</summary>
    public AnyTradingSession WithDate(DateOnly value) {
        return new AnyTradingSession(new FixedValue<DateOnly>(value), _opensAt, _closesAt);
    }

    /// <summary>Draws <c>date</c> from <c>AnyValidDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTradingSession WithDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyTradingSession(configure(AnyValidDate()), _opensAt, _closesAt);
    }

    /// <summary>Pins <c>opensAt</c> to a fixed value.</summary>
    public AnyTradingSession WithOpensAt(TimeOnly value) {
        return new AnyTradingSession(_date, new FixedValue<TimeOnly>(value), _closesAt);
    }

    /// <summary>Draws <c>opensAt</c> from <c>AnyValidOpensAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTradingSession WithOpensAt(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        return new AnyTradingSession(_date, configure(AnyValidOpensAt()), _closesAt);
    }

    /// <summary>Pins <c>closesAt</c> to a fixed value.</summary>
    public AnyTradingSession WithClosesAt(TimeOnly value) {
        return new AnyTradingSession(_date, _opensAt, new FixedValue<TimeOnly>(value));
    }

    /// <summary>Draws <c>closesAt</c> from <c>AnyValidClosesAt()</c>, refined by <paramref name="configure" />.</summary>
    public AnyTradingSession WithClosesAt(Func<AnyTimeOnly, AnyTimeOnly> configure) {
        return new AnyTradingSession(_date, _opensAt, configure(AnyValidClosesAt()));
    }

    /// <summary>Produces one arbitrary <see cref="TradingSession" />.</summary>
    public TradingSession Generate() {
        return new TradingSession(_date.Generate(),
                                  _opensAt.Generate(),
                                  _closesAt.Generate());
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
