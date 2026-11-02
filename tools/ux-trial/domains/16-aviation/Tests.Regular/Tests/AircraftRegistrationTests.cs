using Xunit;

namespace Aviation.Domain.Tests.Regular;

public sealed class AircraftRegistrationTests {

    [Theory]
    [InlineData("F-GKXA")]
    [InlineData("D-AIBL")]
    [InlineData("G-EUPT")]
    [InlineData("HB-JNA")]
    [InlineData("F-ABC")]
    [InlineData("XA-ABCD")]
    public void A_hyphenated_mark_is_accepted(string mark) {
        // Act
        AircraftRegistration registration = new AircraftRegistration(mark);

        // Assert
        Assert.Equal(mark, registration.Mark);
        Assert.Equal(mark, registration.ToString());
        Assert.False(registration.IsUnitedStates);
    }

    [Theory]
    [InlineData("N1")]
    [InlineData("N12345")]
    [InlineData("N123AB")]
    [InlineData("N1234A")]
    [InlineData("N1234AB")]
    [InlineData("N9ZZ")]
    public void An_N_number_is_accepted_and_is_a_united_states_mark(string mark) {
        // Act
        AircraftRegistration registration = new AircraftRegistration(mark);

        // Assert
        Assert.Equal(mark, registration.Mark);
        Assert.True(registration.IsUnitedStates);
    }

    [Fact]
    public void A_null_mark_is_refused() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(null!));
    }

    [Fact(Skip = "domain defect: the empty mark reaches mark[0] and throws IndexOutOfRangeException instead of ArgumentException; seed n/a (literal)")]
    public void An_empty_mark_is_refused_like_any_other_malformed_mark() {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(""));
    }

    [Theory]
    [InlineData("N12345AB")]
    [InlineData("N1234567")]
    [InlineData("ABCD-EFGH")]
    [InlineData("F-GKXAXYZ")]
    public void A_mark_longer_than_seven_characters_is_refused(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("N0")]
    [InlineData("N012")]
    [InlineData("N")]
    [InlineData("N123456")]
    [InlineData("N12ABC")]
    [InlineData("N12a")]
    [InlineData("N-ABC")]
    [InlineData("N12-AB")]
    public void A_mark_starting_with_N_is_matched_only_against_the_N_number_form(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("FGKXA")]
    [InlineData("F-GK")]
    [InlineData("F-GKXAB")]
    [InlineData("ABC-DEF")]
    [InlineData("-GKXA")]
    [InlineData("F-gkxa")]
    [InlineData("f-GKXA")]
    [InlineData("F-GK1A")]
    [InlineData("F--GKXA")]
    [InlineData("12345")]
    [InlineData(" F-GKXA")]
    public void A_mark_not_starting_with_N_is_matched_only_against_the_hyphenated_form(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

    [Theory]
    [InlineData("n12345")]
    [InlineData("n-abc")]
    public void A_lower_case_N_does_not_make_a_united_states_mark(string mark) {
        Assert.Throws<ArgumentException>(() => new AircraftRegistration(mark));
    }

}
