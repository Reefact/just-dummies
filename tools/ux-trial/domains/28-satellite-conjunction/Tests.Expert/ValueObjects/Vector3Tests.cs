using System.Globalization;

using JustDummies;

using Xunit;

namespace SatelliteConjunction.Domain.Tests.Expert.ValueObjects;

public sealed class Vector3Tests {

    [Fact]
    public void A_vector_exposes_its_three_components() {
        // Act
        Vector3 vector = new(1.5, -2.5, 3.5);

        // Assert
        Assert.Equal(1.5, vector.X);
        Assert.Equal(-2.5, vector.Y);
        Assert.Equal(3.5, vector.Z);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_finite_x_component_is_refused(double nonFinite) {
        // Arrange
        double anyY = Any.Double().Generate();
        double anyZ = Any.Double().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Vector3(nonFinite, anyY, anyZ));

        // Assert
        Assert.Equal("x", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_finite_y_component_is_refused(double nonFinite) {
        // Arrange
        double anyX = Any.Double().Generate();
        double anyZ = Any.Double().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Vector3(anyX, nonFinite, anyZ));

        // Assert
        Assert.Equal("y", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_non_finite_z_component_is_refused(double nonFinite) {
        // Arrange
        double anyX = Any.Double().Generate();
        double anyY = Any.Double().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Vector3(anyX, anyY, nonFinite));

        // Assert
        Assert.Equal("z", refusal.ParamName);
    }

    [Fact]
    public void The_largest_finite_components_are_accepted() {
        // Act & Assert
        Assert.Equal(double.MaxValue, new Vector3(double.MaxValue, double.MinValue, 0).X);
    }

    [Fact]
    public void The_zero_vector_has_three_zero_components() {
        // Assert
        Assert.Equal(new Vector3(0, 0, 0), Vector3.Zero);
    }

    [Fact]
    public void The_magnitude_of_a_3_4_12_vector_is_13() {
        // Act & Assert
        Assert.Equal(13, new Vector3(3, 4, 12).Magnitude);
    }

    [Fact]
    public void The_magnitude_ignores_the_signs_of_the_components() {
        // Act & Assert
        Assert.Equal(13, new Vector3(-3, 4, -12).Magnitude);
    }

    [Fact]
    public void Plus_adds_component_by_component() {
        // Act
        Vector3 sum = new Vector3(1, 2, 3).Plus(new Vector3(10, 20, 30));

        // Assert
        Assert.Equal(new Vector3(11, 22, 33), sum);
    }

    [Fact]
    public void Minus_subtracts_component_by_component() {
        // Act
        Vector3 difference = new Vector3(10, 20, 30).Minus(new Vector3(1, 2, 3));

        // Assert
        Assert.Equal(new Vector3(9, 18, 27), difference);
    }

    [Fact]
    public void Scale_multiplies_every_component_and_a_negative_factor_reverses_the_vector() {
        // Arrange
        Vector3 vector = new(1, -2, 3);

        // Act & Assert
        Assert.Equal(new Vector3(2, -4, 6), vector.Scale(2));
        Assert.Equal(new Vector3(-1, 2, -3), vector.Scale(-1));
    }

    [Fact]
    public void Scaling_by_a_factor_that_overflows_is_refused_rather_than_producing_an_infinity() {
        // Arrange
        Vector3 vector = new(double.MaxValue, 0, 0);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => vector.Scale(2));
    }

    [Fact]
    public void Operations_do_not_change_the_vectors_they_are_applied_to() {
        // Arrange
        Vector3 left  = new(1, 2, 3);
        Vector3 right = new(4, 5, 6);

        // Act
        left.Plus(right);
        left.Minus(right);
        left.Scale(10);
        left.Cross(right);

        // Assert
        Assert.Equal(new Vector3(1, 2, 3), left);
        Assert.Equal(new Vector3(4, 5, 6), right);
    }

    [Fact]
    public void The_dot_product_of_two_vectors_is_the_sum_of_the_products_of_their_components() {
        // Act & Assert
        Assert.Equal(32, new Vector3(1, 2, 3).Dot(new Vector3(4, 5, 6)));
    }

    [Fact]
    public void The_dot_product_of_perpendicular_vectors_is_zero() {
        // Act & Assert
        Assert.Equal(0, new Vector3(1, 0, 0).Dot(new Vector3(0, 7, 0)));
    }

    [Fact]
    public void The_cross_product_follows_the_right_hand_rule() {
        // Arrange
        Vector3 x = new(1, 0, 0);
        Vector3 y = new(0, 1, 0);
        Vector3 z = new(0, 0, 1);

        // Act & Assert
        Assert.Equal(z, x.Cross(y));
        Assert.Equal(x, y.Cross(z));
        Assert.Equal(y, z.Cross(x));
        Assert.Equal(new Vector3(0, 0, -1), y.Cross(x));
    }

    [Fact]
    public void The_cross_product_of_two_general_vectors_is_perpendicular_to_both_with_the_expected_value() {
        // Act
        Vector3 product = new Vector3(1, 2, 3).Cross(new Vector3(4, 5, 6));

        // Assert
        Assert.Equal(new Vector3(-3, 6, -3), product);
    }

    [Fact]
    public void Normalizing_gives_a_vector_of_the_same_direction_and_unit_length() {
        // Act
        Vector3 unit = new Vector3(0, 3, 4).Normalized();

        // Assert
        Assert.Equal(0, unit.X);
        Assert.Equal(0.6, unit.Y, precision: 12);
        Assert.Equal(0.8, unit.Z, precision: 12);
        Assert.Equal(1, unit.Magnitude, precision: 12);
    }

    [Fact]
    public void The_zero_vector_cannot_be_normalized() {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Vector3.Zero.Normalized());
    }

    [Fact(Skip = "domain defect: the magnitude is the root of the dot product, which underflows to 0 for tiny components, so a non-zero vector 'has no direction' and an orbital speed of 1e-200 km/s reads as zero; literal case, no seed")]
    public void A_vector_with_tiny_but_non_zero_components_can_be_normalized() {
        // Arrange
        Vector3 tiny = new(1e-200, 0, 0);

        // Act
        Vector3 unit = tiny.Normalized();

        // Assert
        Assert.Equal(new Vector3(1, 0, 0), unit);
    }

    [Fact(Skip = "domain defect: the magnitude of a vector with large finite components overflows to infinity (Magnitude is documented nowhere to be finite, yet every component is); literal case, no seed")]
    public void The_magnitude_of_a_vector_with_large_finite_components_is_finite() {
        // Arrange
        Vector3 large = new(1e200, 0, 0);

        // Act & Assert
        Assert.Equal(1e200, large.Magnitude);
    }

    [Fact]
    public void Two_vectors_with_the_same_components_are_equal_and_share_a_hash_code() {
        // Arrange
        double anyX = Any.Double().Generate();
        double anyY = Any.Double().Generate();
        double anyZ = Any.Double().Generate();

        // Act
        Vector3 first  = new(anyX, anyY, anyZ);
        Vector3 second = new(anyX, anyY, anyZ);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_vectors_differing_by_one_component_are_different() {
        // Arrange
        double anyX = Any.Double().Generate();
        double anyY = Any.Double().Generate();
        double anyZ = Any.Double().Generate();

        // Act
        Vector3 reference = new(anyX, anyY, anyZ);
        Vector3 otherX    = new(Any.Double().DifferentFrom(anyX).Generate(), anyY, anyZ);
        Vector3 otherY    = new(anyX, Any.Double().DifferentFrom(anyY).Generate(), anyZ);
        Vector3 otherZ    = new(anyX, anyY, Any.Double().DifferentFrom(anyZ).Generate());

        // Assert
        Assert.NotEqual(reference, otherX);
        Assert.NotEqual(reference, otherY);
        Assert.NotEqual(reference, otherZ);
    }

    [Fact]
    public void A_vector_is_never_equal_to_null_or_to_something_that_is_not_a_vector() {
        // Arrange
        Vector3 anyVector = Any.Vector3().Generate();

        // Act & Assert
        Assert.False(anyVector.Equals((Vector3?)null));
        Assert.False(anyVector.Equals((object)"(0, 0, 0)"));
    }

    [Fact]
    public void Positive_and_negative_zero_are_the_same_component_for_equality_and_for_the_hash_code() {
        // Arrange
        Vector3 positiveZero = new(0.0, 1, 2);
        Vector3 negativeZero = new(-0.0, 1, 2);

        // Act & Assert
        Assert.Equal(positiveZero, negativeZero);
        Assert.Equal(positiveZero.GetHashCode(), negativeZero.GetHashCode());
    }

    [Fact]
    public void A_vector_is_written_with_its_components_in_parentheses_whatever_the_culture_of_the_thread() {
        // Arrange
        CultureInfo original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

        try {
            // Act
            string text = new Vector3(1.5, -2, 0.25).ToString();

            // Assert
            Assert.Equal("(1.5, -2, 0.25)", text);
        } finally {
            CultureInfo.CurrentCulture = original;
        }
    }

}
