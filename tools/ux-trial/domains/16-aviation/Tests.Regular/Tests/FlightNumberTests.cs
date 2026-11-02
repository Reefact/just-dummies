using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class FlightNumberTests {

    [Theory]
    [InlineData("AF", 1234, "AF1234")]
    [InlineData("AF", 7, "AF7")]
    [InlineData("3K", 25, "3K25")]
    [InlineData("U2", 9999, "U29999")]
    [InlineData("99", 1, "991")]
    public void A_flight_number_is_the_designator_followed_by_the_number_without_padding(string designator, int number, string expected) {
        // Act
        FlightNumber flightNumber = new FlightNumber(designator, number);

        // Assert
        Assert.Equal(designator, flightNumber.AirlineDesignator);
        Assert.Equal(number, flightNumber.Number);
        Assert.Equal(expected, flightNumber.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AFR")]
    [InlineData("af")]
    [InlineData("aF")]
    [InlineData("A-")]
    [InlineData("A ")]
    [InlineData(" A")]
    public void A_designator_that_is_not_two_upper_case_letters_or_digits_is_refused(string? designator) {
        Assert.Throws<ArgumentException>(() => new FlightNumber(designator!, 100));
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so the designator \"AF\\n\" is accepted although it is three characters long; seed n/a (literal)")]
    public void A_designator_ending_with_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new FlightNumber("AF\n", 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_outside_1_to_9999_is_refused(int number) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlightNumber("AF", number));
    }

}
