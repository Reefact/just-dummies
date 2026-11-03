using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests.Regular;

public sealed class PolicyNumberTests {

    [Fact]
    public void A_policy_number_keeps_the_value_it_was_built_from() {
        // Arrange
        string anyValue = Any.StringMatching("POL-[0-9]{4}-[A-Z]{2}[0-9]{6}").Generate();

        // Act
        PolicyNumber number = new PolicyNumber(anyValue);

        // Assert
        Assert.Equal(anyValue, number.Value);
        Assert.Equal(anyValue, number.ToString());
    }

    [Fact]
    public void The_documented_example_is_a_policy_number() {
        Assert.Equal("POL-2026-FR123456", new PolicyNumber("POL-2026-FR123456").Value);
    }

    [Fact]
    public void The_year_is_not_validated() {
        Assert.Equal("POL-0000-AA000000", new PolicyNumber("POL-0000-AA000000").Value);
    }

    [Fact]
    public void A_null_value_is_refused_as_null() {
        Assert.Throws<ArgumentNullException>(() => new PolicyNumber(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("pol-2026-FR123456")]
    [InlineData("POL-202-FR123456")]
    [InlineData("POL-20266-FR123456")]
    [InlineData("POL-2026-fr123456")]
    [InlineData("POL-2026-F1123456")]
    [InlineData("POL-2026-FRA123456")]
    [InlineData("POL-2026-FR12345")]
    [InlineData("POL-2026-FR1234567")]
    [InlineData("POL2026-FR123456")]
    [InlineData("POL-2026FR123456")]
    [InlineData("XPOL-2026-FR123456")]
    [InlineData(" POL-2026-FR123456")]
    [InlineData("POL-2026-FR123456 ")]
    [InlineData("POL-2026-FR123456X")]
    public void A_value_off_the_format_is_refused(string value) {
        Assert.Throws<ArgumentException>(() => new PolicyNumber(value));
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final line feed, so 'POL-2026-FR123456\\n' is accepted as a policy number, seed n/a (literal)")]
    public void A_value_with_a_trailing_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new PolicyNumber("POL-2026-FR123456\n"));
    }

    [Fact(Skip = "domain defect: '\\d' matches any Unicode decimal digit in .NET, so Arabic-Indic digits are accepted in the year and the serial, seed n/a (literal)")]
    public void A_value_with_non_ascii_digits_is_refused() {
        Assert.Throws<ArgumentException>(() => new PolicyNumber("POL-٢٠٢٦-FR123456"));
    }

    [Fact]
    public void Two_numbers_with_the_same_value_are_equal() {
        // Arrange
        string       anyValue = Any.StringMatching("POL-[0-9]{4}-[A-Z]{2}[0-9]{6}").Generate();
        PolicyNumber first    = new PolicyNumber(anyValue);
        PolicyNumber second   = new PolicyNumber(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_numbers_with_different_values_are_not_equal() {
        // Arrange
        AnyPattern   generator = Any.StringMatching("POL-[0-9]{4}-[A-Z]{2}[0-9]{6}");
        string       anyValue  = generator.Generate();
        string       anyOther  = generator.DifferentFrom(anyValue).Generate();

        // Assert
        Assert.False(new PolicyNumber(anyValue).Equals(new PolicyNumber(anyOther)));
    }

    [Fact]
    public void A_number_is_not_equal_to_something_else() {
        PolicyNumber number = Any.PolicyNumber().Generate();

        Assert.False(number.Equals(null));
        Assert.False(number.Equals(number.Value));
    }

    [Fact]
    public void Every_generated_number_is_accepted_by_its_own_constructor() {
        // The generator is the test's own: this guards the recipe against drifting from the domain.
        PolicyNumber anyNumber = Any.PolicyNumber().Generate();

        Assert.Matches(@"^POL-[0-9]{4}-[A-Z]{2}[0-9]{6}$", anyNumber.Value);
    }

}
