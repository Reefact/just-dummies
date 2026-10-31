using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class CatastropheEventTests {

    [Fact]
    public void An_event_occurring_four_times_a_century_returns_every_25_years() {
        // Arrange
        CatastropheEvent anyEvent = Any.CatastropheEvent().WithAnnualRate(0.04).Generate();

        // Act
        double returnPeriod = anyEvent.ReturnPeriodYears;

        // Assert
        Assert.Equal(25.0, returnPeriod, precision: 9);
    }

    [Theory]
    [InlineData("EV-123")]
    [InlineData("EV-123456789")]
    [InlineData("EQ-0042")]
    public void An_event_id_off_the_EV_four_to_eight_digit_format_is_refused(string malformedId) {
        // Arrange
        AnyCatastropheEvent anyEventWithMalformedId = Any.CatastropheEvent().WithId(malformedId);

        // Act
        Exception? refusal = Record.Exception(() => anyEventWithMalformedId.Generate());

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

}
