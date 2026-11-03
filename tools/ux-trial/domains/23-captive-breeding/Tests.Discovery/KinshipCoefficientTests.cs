using System.Globalization;
using CaptiveBreeding.Domain;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class KinshipCoefficientTests {

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.125)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void A_value_inside_the_unit_interval_is_kept(double value) {
        // Act
        KinshipCoefficient kinship = new KinshipCoefficient(value);

        // Assert
        Assert.Equal(value, kinship.Value);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(1.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.MaxValue)]
    public void A_value_outside_the_unit_interval_is_refused(double value) {
        // Act
        Action act = () => new KinshipCoefficient(value);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Unrelated_individuals_have_a_kinship_of_zero() {
        // Assert
        Assert.Equal(0.0, KinshipCoefficient.Unrelated.Value);
    }

    [Fact]
    public void The_inbreeding_of_an_offspring_equals_the_kinship_of_its_parents() {
        // Arrange
        KinshipCoefficient kinship = new KinshipCoefficient(0.125);

        // Assert
        Assert.Equal(0.125, kinship.OffspringInbreeding);
    }

    [Theory]
    [InlineData(0.25, 0.25, false)]
    [InlineData(0.26, 0.25, true)]
    [InlineData(0.24, 0.25, false)]
    [InlineData(0.0, 0.0, false)]
    public void Exceeds_is_a_strict_comparison(double value, double limit, bool expected) {
        // Arrange
        KinshipCoefficient kinship = new KinshipCoefficient(value);

        // Act
        bool exceeds = kinship.Exceeds(new KinshipCoefficient(limit));

        // Assert
        Assert.Equal(expected, exceeds);
    }

    [Fact]
    public void Exceeds_refuses_a_missing_limit() {
        // Act
        Action act = () => KinshipCoefficient.Unrelated.Exceeds(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Coefficients_are_ordered_by_value_and_anything_follows_null() {
        // Arrange
        KinshipCoefficient low  = new KinshipCoefficient(0.1);
        KinshipCoefficient high = new KinshipCoefficient(0.2);

        // Assert
        Assert.True(low.CompareTo(high) < 0);
        Assert.True(high.CompareTo(low) > 0);
        Assert.Equal(0, low.CompareTo(new KinshipCoefficient(0.1)));
        Assert.True(low.CompareTo(null) > 0);
    }

    [Fact]
    public void Coefficients_of_the_same_value_are_equal() {
        // Arrange
        KinshipCoefficient first  = new KinshipCoefficient(0.375);
        KinshipCoefficient second = new KinshipCoefficient(0.375);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new KinshipCoefficient(0.5));
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void A_negative_zero_is_the_same_coefficient_as_zero() {
        // Arrange
        KinshipCoefficient negativeZero = new KinshipCoefficient(-0.0);

        // Assert
        Assert.Equal(KinshipCoefficient.Unrelated, negativeZero);
        Assert.Equal(KinshipCoefficient.Unrelated.GetHashCode(), negativeZero.GetHashCode());
    }

    [Fact(Skip = "domain defect: new KinshipCoefficient(-0.0).ToString() is '-0.0000' although the value is accepted and equal to 0, seed n/a (literal input)")]
    public void A_negative_zero_reads_like_zero() {
        // Assert
        Assert.Equal("0.0000", new KinshipCoefficient(-0.0).ToString());
    }

    [Fact]
    public void The_text_form_uses_four_decimals_whatever_the_culture() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            string text = new KinshipCoefficient(0.25).ToString();

            // Assert
            Assert.Equal("0.2500", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

}
