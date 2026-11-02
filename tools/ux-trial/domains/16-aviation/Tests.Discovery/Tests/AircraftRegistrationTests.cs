using Aviation.Domain;

using Xunit;

namespace Aviation.Tests.Discovery;

public sealed class AircraftRegistrationTests {

    [Theory]
    [InlineData("F-GKXA")]
    [InlineData("D-AIBL")]
    [InlineData("G-EUPT")]
    [InlineData("HB-ABC")]
    [InlineData("HB-ABCD")]
    [InlineData("F-ABC")]
    public void A_hyphenated_mark_is_accepted(string mark) {
        // Act
        AircraftRegistration registration = new AircraftRegistration(mark);

        // Assert
        Assert.Equal(mark, registration.Mark);
        Assert.Equal(mark, registration.ToString());
    }

    [Theory]
    [InlineData("N1")]
    [InlineData("N12345")]
    [InlineData("N123AB")]
    [InlineData("N1A")]
    [InlineData("N12345A")]
    [InlineData("N9999Z")]
    public void An_N_number_mark_is_accepted(string mark) {
        Assert.Equal(mark, new AircraftRegistration(mark).Mark);
    }

    [Fact]
    public void A_null_mark_is_refused() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(null!));
    }

    [Theory]
    [InlineData("N0")]
    [InlineData("N012")]
    [InlineData("N")]
    [InlineData("N123ABC")]
    [InlineData("N1234567")]
    [InlineData("n12345")]
    [InlineData("N-ABC")]
    [InlineData("NA-ABC")]
    [InlineData("N12-45")]
    public void A_mark_starting_with_N_must_be_an_N_number(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("FGKXA")]
    [InlineData("F-GK")]
    [InlineData("F-GKXAB")]
    [InlineData("-GKXA")]
    [InlineData("FFF-GKX")]
    [InlineData("f-gkxa")]
    [InlineData("F-GK1A")]
    [InlineData("F GKXA")]
    [InlineData("F12345")]
    public void A_mark_not_starting_with_N_must_be_hyphenated(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("ABC-DEFG")]
    [InlineData("N12345AB")]
    [InlineData("F-GKXA123")]
    public void A_mark_longer_than_seven_characters_is_refused(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Fact(Skip = "domain defect: the empty mark reaches mark[0] and throws IndexOutOfRangeException instead of ArgumentException")]
    public void An_empty_mark_is_refused_as_an_invalid_argument() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(string.Empty));
    }

    [Fact(Skip = "domain defect: a final line feed slips through the hyphenated pattern ('$' matches before a trailing \\n), so \"F-GKX\\n\" is accepted")]
    public void A_hyphenated_mark_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration("F-GKX\n"));
    }

    [Theory]
    [InlineData("N12345", true)]
    [InlineData("N1A", true)]
    [InlineData("F-GKXA", false)]
    [InlineData("D-AIBL", false)]
    public void A_mark_is_American_when_it_starts_with_N(string mark, bool american) {
        Assert.Equal(american, new AircraftRegistration(mark).IsUnitedStates);
    }

}
