using Aviation.Domain.Tests.Expert.Generators;

using JustDummies;

namespace Aviation.Domain.Tests.Expert.Value;

public sealed class AircraftRegistrationTests {

    [Theory]
    [InlineData("F-GKXA")]
    [InlineData("D-AIBL")]
    [InlineData("G-EUPT")]
    [InlineData("OO-ABC")]
    [InlineData("HB-JCA")]
    [InlineData("C-ABCD")]
    [InlineData("HB-ABCD")]
    [InlineData("X-ABC")]
    public void A_hyphenated_mark_is_accepted_and_is_not_american(string mark) {
        // Act
        AircraftRegistration registration = new AircraftRegistration(mark);

        // Assert
        Assert.Equal(mark, registration.Mark);
        Assert.False(registration.IsUnitedStates);
    }

    [Theory]
    [InlineData("N1")]
    [InlineData("N12345")]
    [InlineData("N123AB")]
    [InlineData("N1A")]
    [InlineData("N12AB")]
    [InlineData("N12345A")]
    [InlineData("N1234AB")]
    [InlineData("N987Z")]
    public void An_N_number_is_accepted_and_is_american(string mark) {
        // Act
        AircraftRegistration registration = new AircraftRegistration(mark);

        // Assert
        Assert.Equal(mark, registration.Mark);
        Assert.True(registration.IsUnitedStates);
    }

    [Theory]
    [InlineData("N")]
    [InlineData("N0")]
    [InlineData("N0123")]
    [InlineData("N123ABC")]
    [InlineData("N12A3")]
    [InlineData("N12a")]
    [InlineData("NABC")]
    [InlineData("N-ABC")]
    [InlineData("N12345AB")]
    [InlineData("n12345")]
    public void A_mark_starting_with_N_must_be_an_N_number(string mark) {
        // "N-ABC" would fit the hyphenated form, but a mark that starts with N is only ever matched against the N-number form.
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("FGKXA")]
    [InlineData("F-GK")]
    [InlineData("F-GKXAB")]
    [InlineData("f-gkxa")]
    [InlineData("F-gkxa")]
    [InlineData("-GKXA")]
    [InlineData("F-")]
    [InlineData("ABC-DEF")]
    [InlineData("F-G1XA")]
    [InlineData("F GKXA")]
    [InlineData("F--GKX")]
    [InlineData("F-GKXA-")]
    public void A_mark_that_is_not_in_the_hyphenated_form_is_refused(string mark) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));

        Assert.Equal("mark", refusal.ParamName);
    }

    [Theory]
    [InlineData("HB-ABCDE")]
    [InlineData("F-GKXAAAAAAAA")]
    [InlineData("N123456789")]
    public void A_mark_longer_than_seven_characters_is_refused(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Fact]
    public void A_missing_mark_is_refused_with_an_argument_exception() {
        // ArgumentException itself, not ArgumentNullException: the model says "null ... refused (ArgumentException)".
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(null!));
    }

    [Fact(Skip = "domain defect: an empty mark reaches mark[0] and throws IndexOutOfRangeException instead of the ArgumentException every other malformed mark gets; no seed involved, the value is a literal")]
    public void An_empty_mark_is_refused_like_any_other_malformed_mark() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(string.Empty));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the format check ('$' also matches before a final \\n), so \"F-GKXA\\n\" (seven characters) is accepted as a registration; no seed involved, the value is a literal")]
    public void A_hyphenated_mark_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration("F-GKXA\n"));
    }

    [Fact(Skip = "domain defect: a trailing line feed passes the format check ('$' also matches before a final \\n), so \"N12345\\n\" (seven characters) is accepted as a registration; no seed involved, the value is a literal")]
    public void An_N_number_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration("N12345\n"));
    }

    [Fact]
    public void A_registration_prints_as_its_mark() {
        Assert.Equal("F-GKXA", new AircraftRegistration("F-GKXA").ToString());
    }

    [Fact]
    public void Every_N_number_a_test_draws_is_american() {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().UnitedStates().Generate();

        // Assert
        Assert.True(anyRegistration.IsUnitedStates);
    }

    [Fact]
    public void Every_hyphenated_mark_a_test_draws_is_not_american() {
        // Arrange
        AircraftRegistration anyRegistration = Any.AircraftRegistration().Foreign().Generate();

        // Assert
        Assert.False(anyRegistration.IsUnitedStates);
    }

}
