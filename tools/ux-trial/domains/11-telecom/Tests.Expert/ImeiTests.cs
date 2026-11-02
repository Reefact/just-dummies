using Telecom.Domain;
using Xunit;

namespace Telecom.Tests.Expert;

public sealed class ImeiTests {

    // Check digits computed by hand, independently of the code under test.
    [Theory]
    [InlineData("49015420323751", 8)]
    [InlineData("35693803564380", 9)]
    [InlineData("12345678901234", 7)]
    [InlineData("00000000000000", 0)]   // a sum multiple of ten gives 0, not 10
    public void The_check_digit_is_the_luhn_digit_of_the_body(string body, int expected) {
        // Act
        int checkDigit = Imei.LuhnCheckDigit(body);

        // Assert
        Assert.Equal(expected, checkDigit);
    }

    [Fact]
    public void A_handset_identity_exposes_its_value_and_its_type_allocation_code() {
        // Act
        Imei handset = new Imei("490154203237518");

        // Assert
        Assert.Equal("490154203237518", handset.Value);
        Assert.Equal("49015420", handset.TypeAllocationCode);
    }

    [Theory]
    [InlineData("356938035643809")]
    [InlineData("000000000000000")]
    public void Another_valid_identity_is_accepted(string value) {
        // Act
        Imei handset = new Imei(value);

        // Assert
        Assert.Equal(value, handset.Value);
    }

    [Theory]
    [InlineData("490154203237510")]
    [InlineData("490154203237511")]
    [InlineData("490154203237512")]
    [InlineData("490154203237513")]
    [InlineData("490154203237514")]
    [InlineData("490154203237515")]
    [InlineData("490154203237516")]
    [InlineData("490154203237517")]
    [InlineData("490154203237519")]
    public void Any_check_digit_but_the_right_one_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Imei(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("49015420323751")]      // 14 digits
    [InlineData("4901542032375180")]    // 16 digits
    [InlineData("")]
    [InlineData("49015420323751a")]     // a letter in place of the check digit
    [InlineData("4901542032375 8")]     // a space
    [InlineData("٤٩٠١٥٤٢٠٣٢٣٧٥١٨")]     // 15 Arabic-Indic digits: digits for char.IsDigit, not ASCII
    public void A_value_that_is_not_15_ascii_digits_is_refused(string value) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Imei(value));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void No_value_at_all_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Imei(null!));
    }

    [Fact]
    public void Two_identities_with_the_same_value_are_equal() {
        // Arrange
        Imei first  = new Imei("490154203237518");
        Imei second = new Imei("490154203237518");

        // Act & Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_identities_with_different_values_are_not_equal() {
        // Arrange
        Imei first  = new Imei("490154203237518");
        Imei second = new Imei("356938035643809");

        // Act & Assert
        Assert.False(first.Equals(second));
    }

}
