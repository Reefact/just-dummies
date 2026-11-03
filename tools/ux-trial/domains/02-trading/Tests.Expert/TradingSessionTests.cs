using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Domain.Tests.Expert;

public sealed class TradingSessionTests {

    private static readonly TimeOnly Open  = new TimeOnly(9, 30);
    private static readonly TimeOnly Close = new TimeOnly(16, 0);

    [Theory]
    [InlineData(2026, 10, 26)] // Monday
    [InlineData(2026, 10, 27)] // Tuesday
    [InlineData(2026, 10, 28)] // Wednesday
    [InlineData(2026, 10, 29)] // Thursday
    [InlineData(2026, 10, 30)] // Friday
    public void A_weekday_is_a_trading_day(int year, int month, int day) {
        // Arrange
        DateOnly date = new DateOnly(year, month, day);

        // Act
        TradingSession session = new TradingSession(date, Open, Close);

        // Assert
        Assert.Equal(date, session.Date);
        Assert.Equal(Open, session.OpensAt);
        Assert.Equal(Close, session.ClosesAt);
    }

    [Theory]
    [InlineData(2026, 10, 31)] // Saturday
    [InlineData(2026, 11, 1)]  // Sunday
    public void A_weekend_day_is_refused(int year, int month, int day) {
        // Arrange
        DateOnly weekend = new DateOnly(year, month, day);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(weekend, Open, Close));

        // Assert
        Assert.Equal("date", refusal.ParamName);
    }

    [Fact]
    public void A_session_that_opens_when_it_closes_is_refused() {
        // Arrange
        DateOnly anyWeekday = Any.TradingSession().Generate().Date;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(anyWeekday, Open, Open));

        // Assert
        Assert.Equal("closesAt", refusal.ParamName);
    }

    [Fact]
    public void A_session_that_closes_before_it_opens_is_refused_even_when_it_would_run_past_midnight() {
        // Arrange
        DateOnly anyWeekday = Any.TradingSession().Generate().Date;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TradingSession(anyWeekday, new TimeOnly(22, 0), new TimeOnly(6, 0)));
    }

    [Fact]
    public void A_session_may_close_one_tick_after_it_opens() {
        // Arrange
        DateOnly anyWeekday = Any.TradingSession().Generate().Date;
        TimeOnly opensAt    = new TimeOnly(9, 30);

        // Act
        TradingSession session = new TradingSession(anyWeekday, opensAt, opensAt.Add(TimeSpan.FromTicks(1)));

        // Assert
        Assert.True(session.IsOpenAt(opensAt));
    }

    [Fact]
    public void The_weekend_is_checked_before_the_hours() {
        // Arrange
        DateOnly saturday = new DateOnly(2026, 10, 31);

        // Act & Assert
        Assert.Equal("date", Assert.Throws<ArgumentException>(() => new TradingSession(saturday, Close, Open)).ParamName);
    }

    [Theory]
    [InlineData(9, 30, 0, true)]   // the very moment it opens
    [InlineData(12, 0, 0, true)]
    [InlineData(15, 59, 59, true)] // the last second before it closes
    [InlineData(16, 0, 0, false)]  // the very moment it closes
    [InlineData(16, 0, 1, false)]
    [InlineData(9, 29, 59, false)] // the last second before it opens
    [InlineData(0, 0, 0, false)]
    public void A_session_is_open_from_the_moment_it_opens_up_to_but_excluding_the_moment_it_closes(int hour, int minute, int second, bool expected) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();

        // Act
        bool open = session.IsOpenAt(new TimeOnly(hour, minute, second));

        // Assert
        Assert.Equal(expected, open);
    }

    [Fact]
    public void A_session_is_still_open_one_tick_before_it_closes() {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();

        // Act
        bool open = session.IsOpenAt(Close.Add(TimeSpan.FromTicks(-1)));

        // Assert
        Assert.True(open);
    }

    [Theory]
    [InlineData(16, 0, 0, true)]   // at the close
    [InlineData(16, 0, 1, true)]
    [InlineData(23, 59, 59, true)]
    [InlineData(15, 59, 59, false)]
    [InlineData(9, 30, 0, false)]
    [InlineData(0, 0, 0, false)]   // before the opening
    public void A_day_order_expires_at_the_close_and_not_before(int hour, int minute, int second, bool expected) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();
        Order          dayOrder = Any.Order().WithTimeInForce(TimeInForce.Day).Generate();

        // Act
        bool expires = session.Expires(dayOrder, new TimeOnly(hour, minute, second));

        // Assert
        Assert.Equal(expected, expires);
    }

    [Theory]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    [InlineData(TimeInForce.FillOrKill)]
    public void An_order_that_is_not_a_day_order_does_not_expire_even_long_after_the_close(TimeInForce timeInForce) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();
        Order          order   = Any.Order().WithTimeInForce(timeInForce).Generate();

        // Act
        bool expires = session.Expires(order, new TimeOnly(23, 59));

        // Assert
        Assert.False(expires);
    }

}
