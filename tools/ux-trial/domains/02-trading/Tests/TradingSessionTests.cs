using JustDummies;
using JustDummies.Xunit;

using Trading.Domain;

using Xunit;

namespace Trading.Tests;

[Reproducible]
public sealed class TradingSessionTests {

    [Fact]
    public void A_session_is_open_from_its_opening_and_closed_at_its_close() {
        // Arrange
        TradingSession anySessionFrom930To1600 = Arbitrary.TradingSession()
                                                          .WithOpensAt(new TimeOnly(9, 30))
                                                          .WithClosesAt(new TimeOnly(16, 0))
                                                          .Generate();

        // Act
        bool openAtOpening = anySessionFrom930To1600.IsOpenAt(new TimeOnly(9, 30));
        bool openAtClose   = anySessionFrom930To1600.IsOpenAt(new TimeOnly(16, 0));

        // Assert
        Assert.True(openAtOpening);
        Assert.False(openAtClose);
    }

    [Fact]
    public void A_day_order_expires_at_the_close_and_a_good_till_cancelled_one_does_not() {
        // Arrange
        TradingSession anySession  = Arbitrary.TradingSession()
                                              .WithOpensAt(new TimeOnly(9, 30))
                                              .WithClosesAt(new TimeOnly(16, 0))
                                              .Generate();
        Order          anyDayOrder = Arbitrary.Order().WithTimeInForce(TimeInForce.Day).Generate();
        Order          anyGtcOrder = Arbitrary.Order().WithTimeInForce(TimeInForce.GoodTillCancelled).Generate();
        TimeOnly       theClose    = new(16, 0);

        // Act
        bool dayExpires = anySession.Expires(anyDayOrder, theClose);
        bool gtcExpires = anySession.Expires(anyGtcOrder, theClose);

        // Assert
        Assert.True(dayExpires);
        Assert.False(gtcExpires);
    }

}
