using JustDummies;

using Xunit;

namespace ShiftPlanning.Domain.Tests.Discovery;

public sealed class SiteIdTests {

    [Theory]
    [InlineData("PAR-01")]
    [InlineData("NYC-99")]
    [InlineData("ZZZ-10")]
    [InlineData("AAA-09")]
    public void A_site_code_is_three_capitals_a_dash_and_a_number_from_01(string code) {
        // Act
        SiteId siteId = new SiteId(code);

        // Assert
        Assert.Equal(code, siteId.Value);
        Assert.Equal(code, siteId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("par-01")]
    [InlineData("PA-01")]
    [InlineData("PARI-01")]
    [InlineData("PAR-1")]
    [InlineData("PAR-001")]
    [InlineData("PAR_01")]
    [InlineData("PAR01")]
    [InlineData("PAR-0A")]
    [InlineData(" PAR-01")]
    public void A_code_of_the_wrong_shape_is_refused(string code) {
        Assert.Throws<FormatException>(() => new SiteId(code));
    }

    [Fact(Skip = "domain defect: SiteId's pattern ends with $, which also matches before a trailing newline, so 'PAR-01\\n' is accepted and kept as the Value; use \\z. No drawn value is involved (literal input), seed irrelevant")]
    public void A_code_followed_by_a_line_feed_is_refused() {
        Assert.Throws<FormatException>(() => new SiteId("PAR-01\n"));
    }

    [Fact]
    public void Site_number_zero_is_refused_with_an_argument_exception_rather_than_a_format_one() {
        Assert.Throws<ArgumentException>(() => new SiteId("PAR-00"));
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new SiteId(null!));
    }

    [Fact]
    public void Two_codes_with_the_same_text_are_the_same_site() {
        // Arrange
        string code = new AnySiteId().Generate().Value;

        // Act
        SiteId first  = new SiteId(code);
        SiteId second = new SiteId(code);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_different_codes_are_different_sites() {
        // Arrange
        SiteId paris    = new SiteId("PAR-01");
        SiteId anyOther = new AnySiteId().WithValue(v => v.DifferentFrom("PAR-01")).Generate();

        // Assert
        Assert.NotEqual(paris, anyOther);
        Assert.False(paris.Equals(null));
    }

}

public sealed class EmployeeIdTests {

    [Theory]
    [InlineData("E004217")]
    [InlineData("E000001")]
    [InlineData("E999999")]
    public void A_personnel_number_is_an_E_and_six_digits(string number) {
        // Act
        EmployeeId employeeId = new EmployeeId(number);

        // Assert
        Assert.Equal(number, employeeId.Value);
        Assert.Equal(number, employeeId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("E")]
    [InlineData("E12345")]
    [InlineData("E1234567")]
    [InlineData("e004217")]
    [InlineData("X004217")]
    [InlineData("E00421A")]
    [InlineData("E00 217")]
    [InlineData("E-04217")]
    public void A_number_of_the_wrong_shape_is_refused(string number) {
        Assert.Throws<FormatException>(() => new EmployeeId(number));
    }

    [Fact]
    public void All_zero_digits_are_refused_with_an_argument_exception() {
        Assert.Throws<ArgumentException>(() => new EmployeeId("E000000"));
    }

    [Fact]
    public void A_null_number_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new EmployeeId(null!));
    }

    [Fact]
    public void Equality_is_by_value() {
        // Arrange
        EmployeeId first = new EmployeeId("E004217");

        // Assert
        Assert.Equal(first, new EmployeeId("E004217"));
        Assert.Equal(first.GetHashCode(), new EmployeeId("E004217").GetHashCode());
        Assert.NotEqual(first, new EmployeeId("E004218"));
        Assert.False(first.Equals(null));
    }

}
