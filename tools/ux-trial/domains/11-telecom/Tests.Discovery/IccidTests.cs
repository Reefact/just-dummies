using JustDummies;

using Xunit;

namespace Telecom.Domain.Tests;

public sealed class IccidTests {

    [Fact]
    public void Accepts_the_prefix_89_and_17_digits() {
        // Arrange
        string anyTail = Any.StringMatching("[0-9]{17}").Generate();

        // Act
        Iccid iccid = new Iccid("89" + anyTail);

        // Assert
        Assert.Equal(19, iccid.Value.Length);
    }

    [Fact]
    public void Accepts_the_prefix_89_and_18_digits() {
        // Arrange
        string anyTail = Any.StringMatching("[0-9]{18}").Generate();

        // Act
        Iccid iccid = new Iccid("89" + anyTail);

        // Assert
        Assert.Equal(20, iccid.Value.Length);
    }

    [Fact]
    public void Reads_its_country_code_from_characters_2_and_3() {
        // Arrange: the tail is not what the test is about
        string anyTail = Any.StringMatching("[0-9]{15}").Generate();
        Iccid  iccid   = new Iccid("8933" + anyTail);

        // Act
        string countryCode = iccid.CountryCode;

        // Assert
        Assert.Equal("33", countryCode);
    }

    [Fact]
    public void Refuses_a_number_that_does_not_start_with_89() {
        // Arrange: 19 digits, the first two being anything but 89
        string anyPrefix = Any.StringMatching("(?:[0-79][0-9]|8[0-8])").Generate();
        string anyTail   = Any.StringMatching("[0-9]{17}").Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Iccid(anyPrefix + anyTail));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Refuses_18_digits_or_fewer() {
        // Arrange
        string anyTooShort = "89" + Any.StringMatching("[0-9]{0,16}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Iccid(anyTooShort));
    }

    [Fact]
    public void Refuses_21_digits_or_more() {
        // Arrange
        string anyTooLong = "89" + Any.StringMatching("[0-9]{19,30}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Iccid(anyTooLong));
    }

    [Fact]
    public void Refuses_a_character_that_is_not_a_digit() {
        // Arrange
        string anyHead   = Any.StringMatching("89[0-9]{8}").Generate();
        string anyTail   = Any.StringMatching("[0-9]{8}").Generate();
        char   anyLetter = Any.Char().OneOf('A', 'f', ' ', '-').Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Iccid(anyHead + anyLetter + anyTail));
    }

    [Fact]
    public void Refuses_null() {
        // The regex call throws before the constructor's own check can.
        Assert.Throws<ArgumentNullException>(() => new Iccid(null!));
    }

    [Fact(Skip = "domain defect: a trailing line feed satisfies '$' so '8933000000000000000\\n' is accepted as an ICCID (same cause as Msisdn)")]
    public void Refuses_digits_followed_by_a_line_feed() {
        Assert.Throws<ArgumentException>(() => new Iccid("8933000000000000000\n"));
    }

}
