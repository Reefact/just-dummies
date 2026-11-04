// Scaffolded by `any`, then rewritten by hand: the scaffold could not tie opensAt to closesAt
// (TimeOnly has no Any.Pair) nor keep the date off the weekend.

using System;

using JustDummies;

namespace Trading.Domain;

public sealed partial class AnyTradingSession : IAny<TradingSession> {

    private IAny<DateOnly> _date = AnyValidDate();
    private TimeOnly?      _opensAt;
    private TimeOnly?      _closesAt;

    public AnyTradingSession() { }

    private AnyTradingSession(AnyTradingSession from) {
        _date     = from._date;
        _opensAt  = from._opensAt;
        _closesAt = from._closesAt;
    }

    private static IAny<DateOnly> AnyValidDate() {
        return Any.DateOnly()
                  .Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31))
                  .As(date => date.DayOfWeek switch {
                      DayOfWeek.Saturday => date.AddDays(2),
                      DayOfWeek.Sunday   => date.AddDays(1),
                      _                  => date
                  });
    }

    public AnyTradingSession WithDate(DateOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._date = Any.Fixed(value);

        return copy;
    }

    public AnyTradingSession WithOpensAt(TimeOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._opensAt = value;

        return copy;
    }

    public AnyTradingSession WithClosesAt(TimeOnly value) {
        AnyTradingSession copy = new AnyTradingSession(this);

        copy._closesAt = value;

        return copy;
    }

    public TradingSession Generate() {
        DateOnly date = _date.Generate();
        TimeOnly opensAt;
        TimeOnly closesAt;
        if (_opensAt is not null && _closesAt is not null) {
            opensAt  = _opensAt.Value;
            closesAt = _closesAt.Value;
        } else if (_opensAt is not null) {
            opensAt  = _opensAt.Value;
            closesAt = Any.TimeOnly().After(opensAt).Generate();
        } else if (_closesAt is not null) {
            closesAt = _closesAt.Value;
            opensAt  = Any.TimeOnly().Before(closesAt).Generate();
        } else {
            opensAt  = Any.TimeOnly().Before(TimeOnly.MaxValue).Generate();
            closesAt = Any.TimeOnly().After(opensAt).Generate();
        }

        return new TradingSession(date, opensAt, closesAt);
    }

}
