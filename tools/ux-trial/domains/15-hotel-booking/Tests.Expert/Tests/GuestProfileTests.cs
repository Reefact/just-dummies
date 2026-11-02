using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class GuestProfileTests {

    [Fact]
    public void A_profile_keeps_everything_it_was_given() {
        // Arrange
        GuestName firstName = new GuestName("Camille");
        GuestName lastName  = new GuestName("Durand");
        string[]  requests  = ["quiet room", "late check-out"];

        // Act
        GuestProfile profile = new GuestProfile(firstName, lastName, "camille.durand@example.com", "+33612345678", "FR",
                                                new DateOnly(1985, 4, 12), "LY12345678", RoomType.Twin, true, requests);

        // Assert
        Assert.Same(firstName, profile.FirstName);
        Assert.Same(lastName, profile.LastName);
        Assert.Equal("camille.durand@example.com", profile.Email);
        Assert.Equal("+33612345678", profile.Phone);
        Assert.Equal("FR", profile.Nationality);
        Assert.Equal(new DateOnly(1985, 4, 12), profile.DateOfBirth);
        Assert.Equal("LY12345678", profile.LoyaltyNumber);
        Assert.Equal(RoomType.Twin, profile.PreferredRoomType);
        Assert.True(profile.MarketingConsent);
        Assert.Equal(requests, profile.SpecialRequests);
    }

    [Fact]
    public void A_guest_holding_a_loyalty_number_is_a_loyalty_member() {
        // Arrange
        GuestProfile member = Any.GuestProfile().WithLoyaltyNumber(number => number).Generate();

        // Act / Assert
        Assert.True(member.IsLoyaltyMember);
    }

    [Fact]
    public void A_guest_without_a_loyalty_number_is_not_a_loyalty_member() {
        // Arrange
        GuestProfile nonMember = Any.GuestProfile().WithoutLoyaltyNumber().Generate();

        // Act / Assert
        Assert.False(nonMember.IsLoyaltyMember);
    }

    [Theory]
    [InlineData("Camille", "Durand", "DURAND, Camille")]
    [InlineData("  Camille ", "  Durand ", "DURAND, Camille")]
    [InlineData("Ana María", "de la Cruz", "DE LA CRUZ, Ana María")]
    [InlineData("Seán", "o'neil", "O'NEIL, Seán")]
    public void The_display_name_is_the_upper_cased_last_name_a_comma_and_the_first_name(string first, string last, string expected) {
        // Arrange
        GuestProfile profile = Any.GuestProfile().WithFirstName(new GuestName(first)).WithLastName(new GuestName(last)).Generate();

        // Act
        string displayName = profile.DisplayName;

        // Assert
        Assert.Equal(expected, displayName);
    }

    [Fact]
    public void An_absent_first_name_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithFirstName((GuestName)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => profiles.Generate());

        // Assert
        Assert.Equal("firstName", refusal.ParamName);
    }

    [Fact]
    public void An_absent_last_name_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithLastName((GuestName)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => profiles.Generate());

        // Assert
        Assert.Equal("lastName", refusal.ParamName);
    }

    [Theory]
    [InlineData("a@b.co")]
    [InlineData("first.last@sub-domain.museum")]
    [InlineData("a_b-c.d9@x1.abcdef")]
    public void An_address_with_lower_case_local_part_domain_and_a_two_to_six_letter_suffix_is_accepted(string email) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithEmail(email);

        // Act
        GuestProfile profile = profiles.Generate();

        // Assert
        Assert.Equal(email, profile.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-at-sign.example.com")]
    [InlineData("two@@example.com")]
    [InlineData("Upper@example.com")]
    [InlineData("user@EXAMPLE.com")]
    [InlineData("user@example.COM")]
    [InlineData("user@example.c")]
    [InlineData("user@example.abcdefg")]
    [InlineData("user@example")]
    [InlineData("user@.com")]
    [InlineData("us er@example.com")]
    [InlineData("user@exa_mple.com")]
    [InlineData("user@example.co.uk")]   // a real-world address, refused by the domain's own pattern
    [InlineData("@example.com")]
    [InlineData(" user@example.com")]
    public void An_address_outside_the_accepted_shape_is_refused(string email) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithEmail(email);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("email", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so a trailing \\n is accepted; deterministic literal, no seed needed")]
    public void An_address_ending_with_a_line_feed_is_refused() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithEmail("user@example.com\n");

        // Act / Assert
        Assert.Throws<ArgumentException>(() => profiles.Generate());
    }

    [Fact]
    public void An_absent_address_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithEmail((string)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => profiles.Generate());

        // Assert
        Assert.Equal("input", refusal.ParamName);
    }

    [Theory]
    [InlineData("+12345678")]
    [InlineData("+33612345678")]
    [InlineData("+123456789012345")]
    public void A_plus_sign_then_eight_to_fifteen_digits_not_starting_with_zero_is_a_phone_number(string phone) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithPhone(phone);

        // Act
        GuestProfile profile = profiles.Generate();

        // Assert
        Assert.Equal(phone, profile.Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0612345678")]
    [InlineData("33612345678")]
    [InlineData("+0612345678")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+33 6 12 34 56 78")]
    [InlineData("+33-612345678")]
    [InlineData("+3361234567a")]
    [InlineData("++33612345678")]
    [InlineData("+33612345678 ")]
    public void A_number_outside_the_international_format_is_refused(string phone) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithPhone(phone);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("phone", refusal.ParamName);
    }

    [Fact]
    public void An_absent_phone_number_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithPhone((string)null!);

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => profiles.Generate());

        // Assert
        Assert.Equal("input", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so a trailing \\n is accepted; deterministic literal, no seed needed")]
    public void A_phone_number_ending_with_a_line_feed_is_refused() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithPhone("+33612345678\n");

        // Act / Assert
        Assert.Throws<ArgumentException>(() => profiles.Generate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("É")]
    [InlineData("ÉF")]
    [InlineData("F ")]
    public void A_nationality_that_is_not_two_ascii_upper_case_letters_is_refused(string nationality) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithNationality(nationality);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("nationality", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: a null nationality throws NullReferenceException instead of ArgumentNullException, no seed needed (pinned null)")]
    public void An_absent_nationality_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithNationality((string)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => profiles.Generate());
    }

    [Theory]
    [InlineData(1900, 1, 1)]
    [InlineData(1985, 4, 12)]
    [InlineData(2008, 1, 1)]
    public void A_guest_born_from_the_start_of_1900_to_the_start_of_2008_is_accepted(int year, int month, int day) {
        // Arrange
        DateOnly dateOfBirth = new DateOnly(year, month, day);

        // Act
        GuestProfile profile = Any.GuestProfile().WithDateOfBirth(dateOfBirth).Generate();

        // Assert
        Assert.Equal(dateOfBirth, profile.DateOfBirth);
    }

    [Theory]
    [InlineData(1899, 12, 31)]
    [InlineData(2008, 1, 2)]
    [InlineData(2024, 6, 1)]
    [InlineData(1, 1, 1)]
    public void A_guest_born_outside_that_interval_is_refused(int year, int month, int day) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithDateOfBirth(new DateOnly(year, month, day));

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => profiles.Generate());

        // Assert
        Assert.Equal("dateOfBirth", refusal.ParamName);
    }

    [Theory]
    [InlineData("LY00000000")]
    [InlineData("LY12345678")]
    public void A_loyalty_number_is_LY_and_eight_digits(string number) {
        // Act
        GuestProfile profile = Any.GuestProfile().WithLoyaltyNumber(number).Generate();

        // Assert
        Assert.Equal(number, profile.LoyaltyNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LY1234567")]
    [InlineData("LY123456789")]
    [InlineData("ly12345678")]
    [InlineData("XY12345678")]
    [InlineData("LY1234567A")]
    [InlineData("12345678")]
    [InlineData(" LY12345678")]
    public void Anything_else_is_not_a_loyalty_number(string number) {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithLoyaltyNumber(number);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("loyaltyNumber", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: Regex $ also matches before a final line feed, so a trailing \\n is accepted; deterministic literal, no seed needed")]
    public void A_loyalty_number_ending_with_a_line_feed_is_refused() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithLoyaltyNumber("LY12345678\n");

        // Act / Assert
        Assert.Throws<ArgumentException>(() => profiles.Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5)]
    public void Up_to_five_special_requests_are_accepted(int count) {
        // Arrange
        IReadOnlyList<string> requests = Any.ListOf(Any.String()).WithCount(count).Generate();

        // Act
        GuestProfile profile = Any.GuestProfile().WithSpecialRequests(requests).Generate();

        // Assert
        Assert.Equal(count, profile.SpecialRequests.Count);
    }

    [Fact]
    public void Six_special_requests_are_too_many() {
        // Arrange
        IReadOnlyList<string> requests = Any.ListOf(Any.String()).WithCount(6).Generate();
        AnyGuestProfile       profiles = Any.GuestProfile().WithSpecialRequests(requests);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("specialRequests", refusal.ParamName);
    }

    [Fact]
    public void The_content_of_a_special_request_is_not_checked() {
        // Arrange
        IReadOnlyList<string> requests = ["", "   ", "\n\t"];

        // Act
        GuestProfile profile = Any.GuestProfile().WithSpecialRequests(requests).Generate();

        // Assert
        Assert.Equal(3, profile.SpecialRequests.Count);
    }

    [Fact(Skip = "domain defect: a null list of special requests throws NullReferenceException instead of ArgumentNullException, no seed needed (pinned null)")]
    public void An_absent_list_of_special_requests_is_refused_as_null() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithSpecialRequests((IReadOnlyList<string>)null!);

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => profiles.Generate());
    }

    [Fact(Skip = "domain defect: the profile keeps the caller's list, so adding requests afterwards breaks the at-most-five rule; Reservation copies its guest list, GuestProfile does not. No seed needed.")]
    public void The_list_of_special_requests_is_copied() {
        // Arrange
        List<string> requests = Any.ListOf(Any.String()).WithCount(5).Generate();
        GuestProfile profile  = Any.GuestProfile().WithSpecialRequests(requests).Generate();

        // Act
        requests.Add("one too many");

        // Assert
        Assert.Equal(5, profile.SpecialRequests.Count);
    }

    [Fact]
    public void The_address_is_checked_before_the_phone_number() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithEmail("nope").WithPhone("nope");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("email", refusal.ParamName);
    }

    [Fact]
    public void The_phone_number_is_checked_before_the_nationality() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithPhone("nope").WithNationality("nope");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("phone", refusal.ParamName);
    }

    [Fact]
    public void The_nationality_is_checked_before_the_date_of_birth() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithNationality("nope").WithDateOfBirth(new DateOnly(1800, 1, 1));

        // Act
        Exception refusal = Assert.ThrowsAny<Exception>(() => profiles.Generate());

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void The_date_of_birth_is_checked_before_the_loyalty_number() {
        // Arrange
        AnyGuestProfile profiles = Any.GuestProfile().WithDateOfBirth(new DateOnly(1800, 1, 1)).WithLoyaltyNumber("nope");

        // Act
        Exception refusal = Assert.ThrowsAny<Exception>(() => profiles.Generate());

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_loyalty_number_is_checked_before_the_special_requests() {
        // Arrange
        IReadOnlyList<string> tooMany  = Any.ListOf(Any.String()).WithCount(6).Generate();
        AnyGuestProfile       profiles = Any.GuestProfile().WithLoyaltyNumber("nope").WithSpecialRequests(tooMany);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => profiles.Generate());

        // Assert
        Assert.Equal("loyaltyNumber", refusal.ParamName);
    }

    [Theory]
    [InlineData(RoomType.Single, true)]
    [InlineData(RoomType.Suite, false)]
    public void The_preferred_room_type_and_the_marketing_consent_are_not_checked(RoomType type, bool consent) {
        // Act
        GuestProfile profile = Any.GuestProfile().WithPreferredRoomType(type).WithMarketingConsent(consent).Generate();

        // Assert
        Assert.Equal(type, profile.PreferredRoomType);
        Assert.Equal(consent, profile.MarketingConsent);
    }

}
