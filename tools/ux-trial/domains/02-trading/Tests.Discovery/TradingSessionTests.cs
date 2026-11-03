using JustDummies;

using Xunit;

namespace Trading.Domain;

public sealed class TradingSessionTests {

    private static readonly TimeOnly Open  = new TimeOnly(9, 30);
    private static readonly TimeOnly Close = new TimeOnly(16, 0);

    private static readonly DateOnly Tuesday = new DateOnly(2026, 10, 27);

    // ---- construction ----

    [Theory]
    [InlineData(2026, 10, 26)]
    [InlineData(2026, 10, 27)]
    [InlineData(2026, 10, 28)]
    [InlineData(2026, 10, 29)]
    [InlineData(2026, 10, 30)]
    public void A_weekday_is_a_trading_date(int year, int month, int day) {
        // Act
        TradingSession session = new TradingSession(new DateOnly(year, month, day), Open, Close);

        // Assert
        Assert.Equal(new DateOnly(year, month, day), session.Date);
    }

    [Theory]
    [InlineData(2026, 10, 31)]
    [InlineData(2026, 11, 1)]
    public void A_weekend_day_is_refused(int year, int month, int day) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(new DateOnly(year, month, day), Open, Close));

        // Assert
        Assert.Equal("date", refusal.ParamName);
    }

    [Fact]
    public void A_session_keeps_its_hours() {
        // Act
        TradingSession session = new TradingSession(Tuesday, Open, Close);

        // Assert
        Assert.Equal(Open, session.OpensAt);
        Assert.Equal(Close, session.ClosesAt);
    }

    [Fact]
    public void A_session_that_closes_when_it_opens_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(Tuesday, Open, Open));

        // Assert
        Assert.Equal("closesAt", refusal.ParamName);
    }

    [Fact]
    public void A_session_that_closes_before_it_opens_is_refused() {
        Assert.Throws<ArgumentException>(() => new TradingSession(Tuesday, Close, Open));
    }

    [Fact]
    public void A_session_may_span_nearly_the_whole_day() {
        // Act
        TradingSession session = new TradingSession(Tuesday, TimeOnly.MinValue, TimeOnly.MaxValue);

        // Assert
        Assert.True(session.IsOpenAt(new TimeOnly(12, 0)));
    }

    // ---- opening hours ----

    [Theory]
    [InlineData(9, 29, 59, false)]
    [InlineData(9, 30, 0, true)]
    [InlineData(12, 0, 0, true)]
    [InlineData(15, 59, 59, true)]
    [InlineData(16, 0, 0, false)]
    [InlineData(23, 0, 0, false)]
    public void A_session_is_open_from_its_opening_included_to_its_closing_excluded(int hour, int minute, int second, bool expected) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();

        // Act
        bool open = session.IsOpenAt(new TimeOnly(hour, minute, second));

        // Assert
        Assert.Equal(expected, open);
    }

    [Fact]
    public void The_last_tick_before_the_close_is_still_open() {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();

        // Assert
        Assert.True(session.IsOpenAt(Close.Add(TimeSpan.FromTicks(-1))));
    }

    // ---- expiry ----

    [Theory]
    [InlineData(15, 59, false)]
    [InlineData(16, 0, true)]
    [InlineData(16, 1, true)]
    [InlineData(23, 59, true)]
    public void A_day_order_expires_from_the_close_included(int hour, int minute, bool expected) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();
        Order          order   = Any.Order().WithTimeInForce(TimeInForce.Day).Generate();

        // Act
        bool expires = session.Expires(order, new TimeOnly(hour, minute));

        // Assert
        Assert.Equal(expected, expires);
    }

    [Theory]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    [InlineData(TimeInForce.FillOrKill)]
    public void An_order_that_is_not_for_the_day_never_expires_with_the_session(TimeInForce timeInForce) {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();
        Order          order   = Any.Order().WithTimeInForce(timeInForce).Generate();

        // Act
        bool expires = session.Expires(order, new TimeOnly(23, 59));

        // Assert
        Assert.False(expires);
    }

    [Fact]
    public void Expiry_depends_on_neither_the_orders_status_nor_its_ticker() {
        // Arrange
        TradingSession session = Any.TradingSession().WithOpensAt(Open).WithClosesAt(Close).Generate();
        Order          filled  = Any.Order().WithTimeInForce(TimeInForce.Day).WithQuantity(new Quantity(10)).Generate();
        filled.Fill(10);

        // Act
        bool expires = session.Expires(filled, new TimeOnly(17, 0));

        // Assert
        Assert.True(expires);
    }

}
