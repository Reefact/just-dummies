namespace Trading.Tests.Regular;

public sealed class TradingSessionTests {

    private static readonly DateOnly AMonday = new DateOnly(2026, 10, 26);
    private static readonly TimeOnly Opening = new TimeOnly(9, 30);
    private static readonly TimeOnly Closing = new TimeOnly(16, 0);

    public static TheoryData<DateOnly> Weekdays => new() {
        new DateOnly(2026, 10, 26),
        new DateOnly(2026, 10, 27),
        new DateOnly(2026, 10, 28),
        new DateOnly(2026, 10, 29),
        new DateOnly(2026, 10, 30),
        DateOnly.MinValue,
        DateOnly.MaxValue
    };

    [Theory]
    [MemberData(nameof(Weekdays))]
    public void A_session_can_be_held_on_any_weekday(DateOnly date) {
        // Act
        TradingSession session = new TradingSession(date, Opening, Closing);

        // Assert
        Assert.Equal(date, session.Date);
    }

    public static TheoryData<DateOnly> WeekendDays => new() {
        new DateOnly(2026, 10, 31),
        new DateOnly(2026, 11, 1),
        new DateOnly(2000, 1, 1),
        new DateOnly(2000, 1, 2)
    };

    [Theory]
    [MemberData(nameof(WeekendDays))]
    public void A_session_cannot_be_held_at_the_weekend(DateOnly date) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(date, Opening, Closing));

        // Assert
        Assert.Equal("date", refusal.ParamName);
    }

    [Fact]
    public void A_session_keeps_its_hours() {
        // Act
        TradingSession session = new TradingSession(AMonday, Opening, Closing);

        // Assert
        Assert.Equal(Opening, session.OpensAt);
        Assert.Equal(Closing, session.ClosesAt);
    }

    [Fact]
    public void A_session_may_be_a_single_tick_long() {
        // Arrange
        TimeOnly closes = new TimeOnly(Opening.Ticks + 1);

        // Act
        TradingSession session = new TradingSession(AMonday, Opening, closes);

        // Assert
        Assert.Equal(closes, session.ClosesAt);
    }

    [Fact]
    public void A_session_may_span_the_whole_day_up_to_the_last_tick() {
        // Act
        TradingSession session = new TradingSession(AMonday, TimeOnly.MinValue, TimeOnly.MaxValue);

        // Assert
        Assert.Equal(TimeOnly.MaxValue, session.ClosesAt);
    }

    [Fact]
    public void A_session_cannot_open_when_it_closes() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(AMonday, Closing, Closing));

        // Assert
        Assert.Equal("closesAt", refusal.ParamName);
    }

    [Fact]
    public void A_session_cannot_open_after_it_closes() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(AMonday, Closing, Opening));

        // Assert
        Assert.Equal("closesAt", refusal.ParamName);
    }

    [Fact]
    public void An_overnight_session_is_not_modelled() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TradingSession(AMonday, new TimeOnly(22, 0), new TimeOnly(2, 0)));
    }

    [Fact]
    public void The_weekend_is_checked_before_the_hours() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradingSession(new DateOnly(2026, 10, 31), Closing, Opening));

        // Assert
        Assert.Equal("date", refusal.ParamName);
    }

    public static TheoryData<string, bool> Instants => new() {
        { "00:00", false },
        { "09:29:59.9999999", false },
        { "09:30", true },
        { "09:30:00.0000001", true },
        { "12:00", true },
        { "15:59:59.9999999", true },
        { "16:00", false },
        { "16:00:00.0000001", false },
        { "23:59:59.9999999", false }
    };

    [Theory]
    [MemberData(nameof(Instants))]
    public void A_session_is_open_from_its_opening_included_to_its_closing_excluded(string time, bool expected) {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        TimeOnly       instant = TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        bool open = session.IsOpenAt(instant);

        // Assert
        Assert.Equal(expected, open);
    }

    [Fact]
    public void A_one_tick_session_is_open_for_exactly_that_tick() {
        // Arrange
        TimeOnly       closes  = new TimeOnly(Opening.Ticks + 1);
        TradingSession session = new TradingSession(AMonday, Opening, closes);

        // Assert
        Assert.True(session.IsOpenAt(Opening));
        Assert.False(session.IsOpenAt(closes));
    }

    [Fact]
    public void A_generated_session_with_given_hours_is_open_at_noon() {
        // Arrange: only the hours matter here, the date is arbitrary
        TradingSession anySession = Any.TradingSession().WithOpensAt(Opening).WithClosesAt(Closing).Generate();

        // Act
        bool open = anySession.IsOpenAt(new TimeOnly(12, 0));

        // Assert
        Assert.True(open);
    }

    // ---------------------------------------------------------------- Expires

    [Fact]
    public void A_day_order_expires_at_the_close() {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        Order          order   = Any.Order().WithTimeInForce(TimeInForce.Day).Generate();

        // Act
        bool expires = session.Expires(order, Closing);

        // Assert
        Assert.True(expires);
    }

    [Fact]
    public void A_day_order_expires_after_the_close() {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        Order          order   = Any.Order().WithTimeInForce(TimeInForce.Day).Generate();

        // Act
        bool expires = session.Expires(order, new TimeOnly(16, 0, 0, 1));

        // Assert
        Assert.True(expires);
    }

    [Theory]
    [InlineData("00:00")]
    [InlineData("09:00")]
    [InlineData("12:00")]
    [InlineData("15:59:59.9999999")]
    public void A_day_order_does_not_expire_before_the_close(string time) {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        Order          order   = Any.Order().WithTimeInForce(TimeInForce.Day).Generate();
        TimeOnly       now     = TimeOnly.Parse(time, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        bool expires = session.Expires(order, now);

        // Assert
        Assert.False(expires);
    }

    [Theory]
    [InlineData(TimeInForce.GoodTillCancelled)]
    [InlineData(TimeInForce.ImmediateOrCancel)]
    [InlineData(TimeInForce.FillOrKill)]
    public void An_order_that_is_not_a_day_order_never_expires_at_the_close(TimeInForce timeInForce) {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        Order          order   = Any.Order().WithTimeInForce(timeInForce).Generate();

        // Act
        bool expires = session.Expires(order, TimeOnly.MaxValue);

        // Assert
        Assert.False(expires);
    }

    [Fact]
    public void A_day_order_is_judged_on_the_closing_time_alone_not_on_its_own_date_status_or_ticker() {
        // Arrange
        TradingSession session = new TradingSession(AMonday, Opening, Closing);
        Order          order   = Any.Order()
                                    .WithTimeInForce(TimeInForce.Day)
                                    .WithPlacedAt(new DateTime(1999, 3, 2, 8, 0, 0, DateTimeKind.Utc))
                                    .Generate();
        order.Cancel();

        // Act
        bool expires = session.Expires(order, Closing);

        // Assert
        Assert.True(expires);
    }

    [Fact]
    public void A_generated_session_closes_after_it_opens_and_never_at_the_weekend() {
        // Act
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < 300; draw++) {
                TradingSession anySession = Any.TradingSession().Generate();

                Assert.True(anySession.OpensAt < anySession.ClosesAt);
                Assert.NotEqual(DayOfWeek.Saturday, anySession.Date.DayOfWeek);
                Assert.NotEqual(DayOfWeek.Sunday, anySession.Date.DayOfWeek);
            }
        });

        // Assert
        Assert.Null(failure);
    }

}
