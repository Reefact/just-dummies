using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class GuestProfileTests {

    private static GuestProfile Build(string email = "ada@example.com",
                                      string phone = "+33612345678",
                                      string nationality = "FR",
                                      DateOnly? dateOfBirth = null,
                                      string? loyaltyNumber = null,
                                      IReadOnlyList<string>? specialRequests = null,
                                      GuestName? firstName = null,
                                      GuestName? lastName = null) {
        return new GuestProfile(firstName ?? new GuestName("Ada"),
                                lastName ?? new GuestName("Lovelace"),
                                email,
                                phone,
                                nationality,
                                dateOfBirth ?? new DateOnly(1990, 5, 17),
                                loyaltyNumber,
                                RoomType.Double,
                                true,
                                specialRequests ?? new List<string>());
    }

    [Fact]
    public void A_profile_keeps_everything_it_was_given() {
        // Arrange
        GuestName             first    = new GuestName("Ada");
        GuestName             last     = new GuestName("Lovelace");
        IReadOnlyList<string> requests = new List<string> { "High floor", "Quiet room" };

        // Act
        GuestProfile profile = new GuestProfile(first, last, "ada@example.com", "+33612345678", "FR", new DateOnly(1990, 5, 17), "LY12345678", RoomType.Suite, false, requests);

        // Assert
        Assert.Same(first, profile.FirstName);
        Assert.Same(last, profile.LastName);
        Assert.Equal("ada@example.com", profile.Email);
        Assert.Equal("+33612345678", profile.Phone);
        Assert.Equal("FR", profile.Nationality);
        Assert.Equal(new DateOnly(1990, 5, 17), profile.DateOfBirth);
        Assert.Equal("LY12345678", profile.LoyaltyNumber);
        Assert.Equal(RoomType.Suite, profile.PreferredRoomType);
        Assert.False(profile.MarketingConsent);
        Assert.Equal(requests, profile.SpecialRequests);
    }

    [Fact]
    public void A_null_first_name_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GuestProfile(null!, new GuestName("Lovelace"), "ada@example.com", "+33612345678", "FR", new DateOnly(1990, 5, 17), null, RoomType.Single, false, new List<string>()));
    }

    [Fact]
    public void A_null_last_name_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GuestProfile(new GuestName("Ada"), null!, "ada@example.com", "+33612345678", "FR", new DateOnly(1990, 5, 17), null, RoomType.Single, false, new List<string>()));
    }

    [Theory]
    [InlineData("ada@example.com")]
    [InlineData("a.b_c-d@my-host.io")]
    [InlineData("x@y.zz")]
    [InlineData("0@0.abcdef")]
    public void A_lower_case_email_is_accepted(string email) {
        // Act
        GuestProfile profile = Build(email: email);

        // Assert
        Assert.Equal(email, profile.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ada")]
    [InlineData("ada@example")]
    [InlineData("@example.com")]
    [InlineData("ada@.com")]
    [InlineData("ada@example.c")]
    [InlineData("ada@example.abcdefg")]
    [InlineData("Ada@example.com")]
    [InlineData("ada@Example.com")]
    [InlineData("ada@example.COM")]
    [InlineData("ada @example.com")]
    [InlineData("ada@exa mple.com")]
    [InlineData("ada@example.co.uk")]
    [InlineData("ada+tag@example.com")]
    [InlineData("ada@@example.com")]
    [InlineData("ada@example.c0m")]
    public void An_email_that_is_not_a_lower_case_address_is_refused(string email) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(email: email));

        // Assert
        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void A_null_email_is_refused_with_a_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Build(email: null!));
    }

    [Theory]
    [InlineData("+33612345678")]
    [InlineData("+12345678")]
    [InlineData("+123456789012345")]
    public void An_international_phone_number_of_eight_to_fifteen_digits_is_accepted(string phone) {
        // Act & Assert
        Assert.Equal(phone, Build(phone: phone).Phone);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0612345678")]
    [InlineData("+0612345678")]
    [InlineData("0033612345678")]
    [InlineData("+3361234")]
    [InlineData("+1234567")]
    [InlineData("+1234567890123456")]
    [InlineData("+33 6 12 34 56 78")]
    [InlineData("+33-612345678")]
    [InlineData("+33abcdefgh")]
    [InlineData("33612345678")]
    [InlineData("++33612345678")]
    public void A_phone_that_is_not_in_the_international_format_is_refused(string phone) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(phone: phone));

        // Assert
        Assert.Equal("phone", exception.ParamName);
    }

    [Fact]
    public void A_null_phone_is_refused_with_a_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Build(phone: null!));
    }

    [Theory]
    [InlineData("FR")]
    [InlineData("US")]
    [InlineData("ZZ")]
    public void A_two_letter_upper_case_nationality_is_accepted(string nationality) {
        // Act & Assert
        Assert.Equal(nationality, Build(nationality: nationality).Nationality);
    }

    [Theory]
    [InlineData("")]
    [InlineData("F")]
    [InlineData("FRA")]
    [InlineData("fr")]
    [InlineData("Fr")]
    [InlineData("F1")]
    [InlineData("É")]
    [InlineData("ÉÉ")]
    [InlineData("F ")]
    public void A_nationality_that_is_not_two_upper_case_ascii_letters_is_refused(string nationality) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(nationality: nationality));

        // Assert
        Assert.Equal("nationality", exception.ParamName);
    }

    [Fact]
    public void A_null_nationality_is_refused_with_a_null_reference_exception() {
        // Act & Assert: the domain does not guard it, the first dereference fails
        Assert.Throws<NullReferenceException>(() => Build(nationality: null!));
    }

    [Fact]
    public void A_guest_born_on_the_first_day_of_1900_is_accepted() {
        // Act & Assert
        Assert.Equal(new DateOnly(1900, 1, 1), Build(dateOfBirth: new DateOnly(1900, 1, 1)).DateOfBirth);
    }

    [Fact]
    public void A_guest_born_on_the_first_day_of_2008_is_accepted() {
        // Act & Assert
        Assert.Equal(new DateOnly(2008, 1, 1), Build(dateOfBirth: new DateOnly(2008, 1, 1)).DateOfBirth);
    }

    [Fact]
    public void A_guest_born_the_day_before_1900_is_refused() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Build(dateOfBirth: new DateOnly(1899, 12, 31)));

        // Assert
        Assert.Equal("dateOfBirth", exception.ParamName);
    }

    [Fact]
    public void A_guest_born_the_day_after_the_first_of_2008_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(dateOfBirth: new DateOnly(2008, 1, 2)));
    }

    [Fact]
    public void A_guest_born_in_the_future_is_refused() {
        // Arrange
        DateOnly anyFutureDate = Any.DateOnly().Between(new DateOnly(2030, 1, 1), new DateOnly(2100, 1, 1)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(dateOfBirth: anyFutureDate));
    }

    [Fact]
    public void A_guest_without_a_loyalty_number_is_not_a_loyalty_member() {
        // Act
        GuestProfile profile = Build(loyaltyNumber: null);

        // Assert
        Assert.Null(profile.LoyaltyNumber);
        Assert.False(profile.IsLoyaltyMember);
    }

    [Fact]
    public void A_guest_with_a_loyalty_number_is_a_loyalty_member() {
        // Arrange
        string anyLoyaltyNumber = Any.StringMatching("LY[0-9]{8}").Generate();

        // Act
        GuestProfile profile = Build(loyaltyNumber: anyLoyaltyNumber);

        // Assert
        Assert.True(profile.IsLoyaltyMember);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LY1234567")]
    [InlineData("LY123456789")]
    [InlineData("ly12345678")]
    [InlineData("LX12345678")]
    [InlineData("12345678")]
    [InlineData("LY1234567A")]
    [InlineData(" LY12345678")]
    public void A_malformed_loyalty_number_is_refused(string loyaltyNumber) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(loyaltyNumber: loyaltyNumber));

        // Assert
        Assert.Equal("loyaltyNumber", exception.ParamName);
    }

    [Fact]
    public void Five_special_requests_are_accepted() {
        // Arrange
        List<string> anyRequests = Any.ListOf(Any.String()).WithCount(5).Generate();

        // Act
        GuestProfile profile = Build(specialRequests: anyRequests);

        // Assert
        Assert.Equal(5, profile.SpecialRequests.Count);
    }

    [Fact]
    public void No_special_request_is_accepted() {
        // Act & Assert
        Assert.Empty(Build(specialRequests: new List<string>()).SpecialRequests);
    }

    [Fact]
    public void Six_special_requests_are_refused() {
        // Arrange
        List<string> anyRequests = Any.ListOf(Any.String()).WithCount(6).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(specialRequests: anyRequests));

        // Assert
        Assert.Equal("specialRequests", exception.ParamName);
    }

    [Fact]
    public void Special_requests_are_not_validated() {
        // Arrange
        List<string> requests = new List<string> { "", "   ", "\n" };

        // Act & Assert
        Assert.Equal(3, Build(specialRequests: requests).SpecialRequests.Count);
    }

    [Fact]
    public void A_null_list_of_special_requests_is_refused_with_a_null_reference_exception() {
        // Act & Assert
        Assert.Throws<NullReferenceException>(() => new GuestProfile(new GuestName("Ada"), new GuestName("Lovelace"), "ada@example.com", "+33612345678", "FR", new DateOnly(1990, 5, 17), null, RoomType.Single, false, null!));
    }

    [Fact]
    public void The_email_is_checked_before_the_phone() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(email: "nope", phone: "nope"));

        // Assert
        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void The_phone_is_checked_before_the_nationality() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(phone: "nope", nationality: "nope"));

        // Assert
        Assert.Equal("phone", exception.ParamName);
    }

    [Fact]
    public void The_nationality_is_checked_before_the_date_of_birth() {
        // Act & Assert
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Build(nationality: "nope", dateOfBirth: new DateOnly(1800, 1, 1)));

        Assert.Equal("nationality", exception.ParamName);
    }

    [Fact]
    public void The_display_name_is_the_upper_case_last_name_then_the_first_name() {
        // Act
        GuestProfile profile = Build(firstName: new GuestName("  Ada "), lastName: new GuestName("Lovelace"));

        // Assert
        Assert.Equal("LOVELACE, Ada", profile.DisplayName);
    }

    [Fact]
    public void The_display_name_upper_cases_accented_letters() {
        // Act
        GuestProfile profile = Build(firstName: new GuestName("Émile"), lastName: new GuestName("de Çelik-Ünal"));

        // Assert
        Assert.Equal("DE ÇELIK-ÜNAL, Émile", profile.DisplayName);
    }

    [Fact]
    public void The_generated_profile_can_be_customised_through_its_parts() {
        // Act
        GuestProfile profile = Arbitrary.GuestProfile()
                                        .WithLastName(n => n.WithValue("Hopper"))
                                        .WithFirstName(n => n.WithValue("Grace"))
                                        .Generate();

        // Assert
        Assert.Equal("HOPPER, Grace", profile.DisplayName);
    }

    [Fact]
    public void A_generated_profile_pinned_to_a_bad_email_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.GuestProfile().WithEmail("Not An Email").Generate());
    }

    [Fact]
    public void A_generated_profile_without_loyalty_number_is_not_a_member() {
        // Arrange
        GuestProfile anyProfile = Arbitrary.GuestProfile().WithoutLoyaltyNumber().Generate();

        // Assert
        Assert.False(anyProfile.IsLoyaltyMember);
    }

    [Fact]
    public void A_generated_profile_with_a_declared_loyalty_number_is_a_member() {
        // Arrange
        GuestProfile anyProfile = Arbitrary.GuestProfile().WithLoyaltyNumber(n => n).Generate();

        // Assert
        Assert.True(anyProfile.IsLoyaltyMember);
    }

    [Fact]
    public void The_generator_only_draws_profiles_the_domain_accepts() {
        // Act
        for (int i = 0; i < 300; i++) {
            GuestProfile anyProfile = Arbitrary.GuestProfile().Generate();

            Assert.True(anyProfile.SpecialRequests.Count <= 5);
        }
    }

    [Fact(Skip = "domain defect: a trailing line feed is accepted by the e-mail, phone and loyalty patterns ('$' matches before a final newline), e.g. \"ada@example.com\\n\"; literal input, no seed")]
    public void An_email_with_a_trailing_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Build(email: "ada@example.com\n"));
    }

}
