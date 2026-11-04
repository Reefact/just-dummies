using System.Globalization;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Domain.Tests.Regular;

public sealed class KinshipCoefficientTests {

    [Fact]
    public void A_value_within_zero_and_one_is_kept() {
        // Arrange
        double anyValue = Any.Double().Between(0d, 1d).Generate();

        // Act
        KinshipCoefficient kinship = new KinshipCoefficient(anyValue);

        // Assert
        Assert.Equal(anyValue, kinship.Value);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(1d)]
    public void Both_ends_of_the_interval_are_accepted(double value) {
        Assert.Equal(value, new KinshipCoefficient(value).Value);
    }

    [Fact]
    public void A_negative_value_is_refused() {
        // Arrange
        double anyNegative = Any.Double().Negative().Generate();

        // Act
        Action construction = () => new KinshipCoefficient(anyNegative);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Fact]
    public void A_value_above_one_is_refused() {
        // Arrange
        double anyTooLarge = Any.Double().GreaterThan(1d).Generate();

        // Act
        Action construction = () => new KinshipCoefficient(anyTooLarge);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construction);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_value_that_is_not_a_finite_number_is_refused(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new KinshipCoefficient(value));
    }

    [Fact]
    public void Unrelated_is_a_kinship_of_zero() {
        Assert.Equal(0d, KinshipCoefficient.Unrelated.Value);
    }

    [Fact]
    public void The_offspring_inbreeding_equals_the_kinship() {
        // Arrange
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().Generate();

        // Act
        double inbreeding = anyKinship.OffspringInbreeding;

        // Assert
        Assert.Equal(anyKinship.Value, inbreeding);
    }

    [Fact]
    public void A_kinship_above_the_limit_exceeds_it() {
        // Arrange
        double anyLimit = Any.Double().Between(0d, 0.9d).Generate();
        double anyAbove = Any.Double().GreaterThan(anyLimit).LessThanOrEqualTo(1d).Generate();

        // Act
        bool exceeds = new KinshipCoefficient(anyAbove).Exceeds(new KinshipCoefficient(anyLimit));

        // Assert
        Assert.True(exceeds);
    }

    [Fact]
    public void A_kinship_at_or_below_the_limit_does_not_exceed_it() {
        // Arrange
        double anyLimit = Any.Double().Between(0d, 1d).Generate();
        double anyBelow = Any.Double().Between(0d, anyLimit).Generate();

        // Act
        bool exceeds = new KinshipCoefficient(anyBelow).Exceeds(new KinshipCoefficient(anyLimit));

        // Assert
        Assert.False(exceeds);
    }

    [Fact]
    public void A_kinship_equal_to_the_limit_does_not_exceed_it() {
        // Arrange
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().Generate();

        // Act / Assert
        Assert.False(anyKinship.Exceeds(anyKinship));
    }

    [Fact]
    public void Exceeds_refuses_a_missing_limit() {
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().Generate();

        Assert.Throws<ArgumentNullException>(() => anyKinship.Exceeds(null!));
    }

    [Fact]
    public void Kinships_compare_by_value() {
        // Arrange
        (double anyLow, double anyHigh) = Any.Pair(Any.Double().Between(0d, 1d), Any.Double().Between(0d, 1d)).Having.Item1LessThanItem2().Generate();

        KinshipCoefficient low  = new KinshipCoefficient(anyLow);
        KinshipCoefficient high = new KinshipCoefficient(anyHigh);

        // Act / Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new KinshipCoefficient(anyLow)));
    }

    [Fact]
    public void Any_kinship_is_greater_than_nothing() {
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().Generate();

        Assert.True(anyKinship.CompareTo(null) > 0);
    }

    [Fact]
    public void Kinships_with_the_same_value_are_equal() {
        // Arrange
        double anyValue = Any.Double().Between(0d, 1d).Generate();

        // Act
        KinshipCoefficient first  = new KinshipCoefficient(anyValue);
        KinshipCoefficient second = new KinshipCoefficient(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Kinships_with_different_values_are_not_equal() {
        // Arrange
        (double anyFirst, double anySecond) = Any.Pair(Any.Double().Between(0d, 1d), Any.Double().Between(0d, 1d)).Having.Item1LessThanItem2().Generate();

        // Act / Assert
        Assert.False(new KinshipCoefficient(anyFirst).Equals(new KinshipCoefficient(anySecond)));
    }

    [Fact]
    public void A_kinship_is_never_equal_to_null_or_to_a_number() {
        KinshipCoefficient anyKinship = Any.KinshipCoefficient().Generate();

        Assert.False(anyKinship.Equals((KinshipCoefficient?)null));
        Assert.False(anyKinship.Equals((object?)null));
        Assert.False(anyKinship.Equals(anyKinship.Value));
    }

    [Theory]
    [InlineData(0d, "0.0000")]
    [InlineData(0.25d, "0.2500")]
    [InlineData(0.125d, "0.1250")]
    [InlineData(1d, "1.0000")]
    [InlineData(0.0625d, "0.0625")]
    public void The_text_has_four_decimals(double value, string expected) {
        Assert.Equal(expected, new KinshipCoefficient(value).ToString());
    }

    [Fact]
    public void The_text_does_not_depend_on_the_current_culture() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try {
            // Act
            string text = new KinshipCoefficient(0.25d).ToString();

            // Assert
            Assert.Equal("0.2500", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
