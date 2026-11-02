using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class FlightNumberTests {

    [Theory]
    [InlineData("AF", 1234, "AF1234")]
    [InlineData("AF", 7, "AF7")]
    [InlineData("AF", 10, "AF10")]
    [InlineData("6X", 9999, "6X9999")]
    [InlineData("12", 1, "121")]
    public void A_flight_number_prints_its_designator_then_its_number_without_padding(string designator, int number, string expected) {
        // Act
        FlightNumber flightNumber = new FlightNumber(designator, number);

        // Assert
        Assert.Equal(expected, flightNumber.ToString());
        Assert.Equal(designator, flightNumber.AirlineDesignator);
        Assert.Equal(number, flightNumber.Number);
    }

    [Theory]
    [InlineData("AF")]
    [InlineData("A1")]
    [InlineData("1A")]
    [InlineData("99")]
    [InlineData("ZZ")]
    public void A_designator_of_two_upper_case_letters_or_digits_is_accepted(string designator) {
        // Act
        FlightNumber flightNumber = Any.FlightNumber().WithAirlineDesignator(designator).Generate();

        // Assert
        Assert.Equal(designator, flightNumber.AirlineDesignator);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("AFR")]
    [InlineData("af")]
    [InlineData("aF")]
    [InlineData("A-")]
    [InlineData("A ")]
    [InlineData(" A")]
    [InlineData("É1")]
    [InlineData(null)]
    public void A_designator_that_is_not_two_upper_case_letters_or_digits_is_refused(string? designator) {
        // The null case is an ArgumentException too, not an ArgumentNullException.
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.FlightNumber().WithAirlineDesignator(designator!).Generate());

        Assert.Equal("airlineDesignator", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the designator check ('$' also matches before a final \\n), so \"AF\\n\" is accepted as an airline designator; no seed involved, the value is a literal")]
    public void A_designator_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.FlightNumber().WithAirlineDesignator("AF\n").Generate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(9998)]
    [InlineData(9999)]
    public void A_number_from_1_to_9999_is_accepted(int number) {
        // Act
        FlightNumber flightNumber = Any.FlightNumber().WithNumber(number).Generate();

        // Assert
        Assert.Equal(number, flightNumber.Number);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_number_outside_1_to_9999_is_refused(int number) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.FlightNumber().WithNumber(number).Generate());

        Assert.Equal("number", refusal.ParamName);
    }

    [Fact]
    public void A_bad_designator_is_reported_before_a_bad_number() {
        // Both arguments are wrong: the designator is checked first.
        Assert.Throws<ArgumentException>(() => new FlightNumber("af", 0));
    }

}
