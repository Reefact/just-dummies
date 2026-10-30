using JustDummies;
using JustDummies.Xunit;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

[Reproducible]
public sealed class NumberingTests {

    [Fact]
    public void A_number_in_the_French_mobile_ranges_is_a_French_mobile() {
        // Arrange: the 336/337 prefix is what the rule is about; the eight subscriber digits are not.
        Msisdn anyFrenchMobile = new Msisdn(Any.StringMatching(@"33[67][0-9]{8}").Generate());

        // Act
        bool isFrenchMobile = anyFrenchMobile.IsFrenchMobile;

        // Assert
        Assert.True(isFrenchMobile);
    }

    [Fact]
    public void A_UK_number_is_not_a_French_mobile() {
        // Arrange
        Msisdn anyUkNumber = new Msisdn(Any.StringMatching(@"44[0-9]{10}").Generate());

        // Act
        bool isFrenchMobile = anyUkNumber.IsFrenchMobile;

        // Assert
        Assert.False(isFrenchMobile);
    }

    [Fact]
    public void An_MSISDN_is_written_with_a_leading_plus() {
        // Arrange
        Msisdn number = new Msisdn("33612345678");

        // Act
        string written = number.ToString();

        // Assert
        Assert.Equal("+33612345678", written);
    }

    [Fact]
    public void The_type_allocation_code_is_the_first_eight_digits_of_the_IMEI() {
        // Arrange: the TAC is significant; the six-digit serial is arbitrary, and the check digit follows from both.
        const string tac          = "35332509";
        string       anySerial    = Any.String().Numeric().WithLength(6).Generate();
        Imei         anyIphoneImei = new Imei(tac + anySerial + Imei.LuhnCheckDigit(tac + anySerial));

        // Act
        string typeAllocationCode = anyIphoneImei.TypeAllocationCode;

        // Assert
        Assert.Equal(tac, typeAllocationCode);
    }

    [Fact]
    public void An_IMEI_whose_check_digit_is_off_by_one_is_refused() {
        // Arrange
        Imei   anyImei   = Arbitrary.Imei().Generate();
        char   wrong     = (char)('0' + (anyImei.Value[14] - '0' + 1) % 10);
        string corrupted = anyImei.Value.Substring(0, 14) + wrong;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Imei(corrupted));

        // Assert
        Assert.Contains("Luhn", refusal.Message);
    }

    [Fact]
    public void The_country_code_of_an_ICCID_follows_the_industry_prefix() {
        // Arrange: country 33 is the point; the issuer and serial digits are not.
        Iccid anyFrenchSim = new Iccid("8933" + Any.String().Numeric().WithLengthBetween(15, 16).Generate());

        // Act
        string countryCode = anyFrenchSim.CountryCode;

        // Assert
        Assert.Equal("33", countryCode);
    }

    [Fact]
    public void The_Luhn_check_digit_of_a_known_IMEI_body_is_computed() {
        // Assert: the published example IMEI 490154203237518.
        Assert.Equal(8, Imei.LuhnCheckDigit("49015420323751"));
    }

}
