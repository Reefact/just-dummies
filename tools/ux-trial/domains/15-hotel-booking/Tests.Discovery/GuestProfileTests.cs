using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class GuestProfileTests {

    [Fact]
    public void A_profile_keeps_what_it_was_given() {
        // Arrange
        GuestName             anyFirstName = Any.GuestName().Generate();
        GuestName             anyLastName  = Any.GuestName().Generate();
        IReadOnlyList<string> requests     = ["quiet room", "late check-in"];

        // Act
        GuestProfile profile = new GuestProfile(anyFirstName, anyLastName, "marie.curie@example.org", "+33123456789", "FR",
                                                new DateOnly(1980, 5, 17), "LY12345678", RoomType.Twin, true, requests);

        // Assert
        Assert.Same(anyFirstName, profile.FirstName);
        Assert.Same(anyLastName, profile.LastName);
        Assert.Equal("marie.curie@example.org", profile.Email);
        Assert.Equal("+33123456789", profile.Phone);
        Assert.Equal("FR", profile.Nationality);
        Assert.Equal(new DateOnly(1980, 5, 17), profile.DateOfBirth);
        Assert.Equal("LY12345678", profile.LoyaltyNumber);
        Assert.Equal(RoomType.Twin, profile.PreferredRoomType);
        Assert.True(profile.MarketingConsent);
        Assert.Equal(requests, profile.SpecialRequests);
    }

    [Fact]
    public void The_display_name_is_the_upper_case_last_name_then_the_first_name() {
        // Arrange
        GuestProfile profile = Any.GuestProfile()
                                  .WithFirstName(new GuestName("Marie"))
                                  .WithLastName(new GuestName("Curie-Sklodowska"))
                                  .Generate();

        // Act
        string displayName = profile.DisplayName;

        // Assert
        Assert.Equal("CURIE-SKLODOWSKA, Marie", displayName);
    }

    [Fact]
    public void A_profile_with_a_loyalty_number_is_a_loyalty_member() {
        // Arrange
        GuestProfile profile = Any.GuestProfile().WithLoyaltyNumber("LY00000001").Generate();

        // Act / Assert
        Assert.True(profile.IsLoyaltyMember);
    }

    [Fact]
    public void A_profile_without_a_loyalty_number_is_not_a_loyalty_member() {
        // Arrange
        GuestProfile profile = Any.GuestProfile().WithLoyaltyNumber((string?)null).Generate();

        // Act / Assert
        Assert.False(profile.IsLoyaltyMember);
        Assert.Null(profile.LoyaltyNumber);
    }

    [Fact]
    public void A_missing_first_name_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.GuestProfile().WithFirstName((GuestName)null!).Generate());

        Assert.Equal("firstName", error.ParamName);
    }

    [Fact]
    public void A_missing_last_name_is_refused() {
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => Any.GuestProfile().WithLastName((GuestName)null!).Generate());

        Assert.Equal("lastName", error.ParamName);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData("a@b")]
    [InlineData("a@b.c")]
    [InlineData("a@b.abcdefg")]
    [InlineData("Marie@example.org")]
    [InlineData("marie@Example.org")]
    [InlineData("marie@example.ORG")]
    [InlineData("@example.org")]
    [InlineData("marie curie@example.org")]
    [InlineData("marie@exa_mple.org")]
    public void A_malformed_email_is_refused(string email) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithEmail(email).Generate());

        Assert.Equal("email", error.ParamName);
    }

    [Theory]
    [InlineData("a@b.co")]
    [InlineData("first.last-name_1@sub-domain.abcdef")]
    public void A_well_formed_email_is_accepted(string email) {
        GuestProfile profile = Any.GuestProfile().WithEmail(email).Generate();

        Assert.Equal(email, profile.Email);
    }

    [Fact]
    public void A_null_email_is_refused_as_a_null() {
        Assert.Throws<ArgumentNullException>(() => Any.GuestProfile().WithEmail((string)null!).Generate());
    }

    [Theory]
    [InlineData("33123456789")]
    [InlineData("+0123456789")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+33 1 23 45 67 89")]
    [InlineData("+33abc45678")]
    [InlineData("")]
    public void A_phone_that_is_not_e164_is_refused(string phone) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithPhone(phone).Generate());

        Assert.Equal("phone", error.ParamName);
    }

    [Theory]
    [InlineData("+12345678")]
    [InlineData("+123456789012345")]
    public void A_phone_at_the_edges_of_the_digit_count_is_accepted(string phone) {
        GuestProfile profile = Any.GuestProfile().WithPhone(phone).Generate();

        Assert.Equal(phone, profile.Phone);
    }

    [Fact]
    public void A_null_phone_is_refused_as_a_null() {
        Assert.Throws<ArgumentNullException>(() => Any.GuestProfile().WithPhone((string)null!).Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("É1")]
    public void A_nationality_that_is_not_two_upper_case_ascii_letters_is_refused(string nationality) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithNationality(nationality).Generate());

        Assert.Equal("nationality", error.ParamName);
    }

    [Fact]
    public void A_null_nationality_fails_with_a_null_reference() {
        Assert.Throws<NullReferenceException>(() => Any.GuestProfile().WithNationality((string)null!).Generate());
    }

    [Theory]
    [InlineData(1900, 1, 1)]
    [InlineData(2008, 1, 1)]
    [InlineData(1985, 6, 15)]
    public void A_date_of_birth_within_the_bounds_is_accepted(int year, int month, int day) {
        DateOnly birth = new DateOnly(year, month, day);

        GuestProfile profile = Any.GuestProfile().WithDateOfBirth(birth).Generate();

        Assert.Equal(birth, profile.DateOfBirth);
    }

    [Theory]
    [InlineData(1899, 12, 31)]
    [InlineData(2008, 1, 2)]
    [InlineData(2020, 6, 1)]
    public void A_date_of_birth_outside_the_bounds_is_refused(int year, int month, int day) {
        DateOnly birth = new DateOnly(year, month, day);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.GuestProfile().WithDateOfBirth(birth).Generate());

        Assert.Equal("dateOfBirth", error.ParamName);
    }

    [Theory]
    [InlineData("LY1234567")]
    [InlineData("LY123456789")]
    [InlineData("ly12345678")]
    [InlineData("XY12345678")]
    [InlineData("LY1234567A")]
    [InlineData("")]
    public void A_malformed_loyalty_number_is_refused(string loyaltyNumber) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithLoyaltyNumber(loyaltyNumber).Generate());

        Assert.Equal("loyaltyNumber", error.ParamName);
    }

    [Fact]
    public void Five_special_requests_are_accepted() {
        IReadOnlyList<string> requests = Any.ListOf(Any.String().Printable()).WithCount(5).Generate();

        GuestProfile profile = Any.GuestProfile().WithSpecialRequests(requests).Generate();

        Assert.Equal(5, profile.SpecialRequests.Count);
    }

    [Fact]
    public void Six_special_requests_are_refused() {
        IReadOnlyList<string> requests = Any.ListOf(Any.String().Printable()).WithCount(6).Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithSpecialRequests(requests).Generate());

        Assert.Equal("specialRequests", error.ParamName);
    }

    [Fact]
    public void No_special_request_is_fine() {
        GuestProfile profile = Any.GuestProfile().WithSpecialRequests(new List<string>()).Generate();

        Assert.Empty(profile.SpecialRequests);
    }

    [Fact]
    public void Special_requests_are_not_validated() {
        IReadOnlyList<string> requests = ["", "   ", "\n"];

        GuestProfile profile = Any.GuestProfile().WithSpecialRequests(requests).Generate();

        Assert.Equal(3, profile.SpecialRequests.Count);
    }

    [Fact]
    public void A_null_list_of_special_requests_fails_with_a_null_reference() {
        Assert.Throws<NullReferenceException>(() => Any.GuestProfile().WithSpecialRequests((IReadOnlyList<string>)null!).Generate());
    }

    [Fact]
    public void The_email_is_checked_before_the_phone() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithEmail("bad").WithPhone("bad").Generate());

        Assert.Equal("email", error.ParamName);
    }

    [Fact]
    public void The_phone_is_checked_before_the_nationality() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithPhone("bad").WithNationality("bad").Generate());

        Assert.Equal("phone", error.ParamName);
    }

    [Fact]
    public void The_nationality_is_checked_before_the_date_of_birth() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.GuestProfile().WithNationality("bad").WithDateOfBirth(new DateOnly(1800, 1, 1)).Generate());

        Assert.Equal("nationality", error.ParamName);
    }

    [Fact]
    public void The_date_of_birth_is_checked_before_the_loyalty_number() {
        ArgumentException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.GuestProfile().WithDateOfBirth(new DateOnly(1800, 1, 1)).WithLoyaltyNumber("bad").Generate());

        Assert.Equal("dateOfBirth", error.ParamName);
    }

    [Fact]
    public void The_preferred_room_type_and_the_marketing_consent_are_not_checked() {
        GuestProfile profile = Any.GuestProfile().WithPreferredRoomType((RoomType)99).WithMarketingConsent(false).Generate();

        Assert.Equal((RoomType)99, profile.PreferredRoomType);
        Assert.False(profile.MarketingConsent);
    }

}
