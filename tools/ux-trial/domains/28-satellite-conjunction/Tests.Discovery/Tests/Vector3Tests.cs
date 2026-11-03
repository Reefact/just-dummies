using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Discovery;

public sealed class Vector3Tests {

    [Fact]
    public void A_vector_keeps_its_components() {
        // Act
        Vector3 vector = new Vector3(1.5, -2.5, 3.5);

        // Assert
        Assert.Equal(1.5, vector.X);
        Assert.Equal(-2.5, vector.Y);
        Assert.Equal(3.5, vector.Z);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_vector_refuses_a_non_finite_x(double notFinite) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Vector3().WithX(notFinite).Generate());

        // Assert
        Assert.Equal("x", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_vector_refuses_a_non_finite_y(double notFinite) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Vector3().WithY(notFinite).Generate());

        // Assert
        Assert.Equal("y", refusal.ParamName);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_vector_refuses_a_non_finite_z(double notFinite) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Vector3().WithZ(notFinite).Generate());

        // Assert
        Assert.Equal("z", refusal.ParamName);
    }

    [Fact]
    public void The_zero_vector_is_the_origin() {
        // Act / Assert
        Assert.Equal(new Vector3(0, 0, 0), Vector3.Zero);
        Assert.Equal(0, Vector3.Zero.Magnitude);
    }

    [Fact]
    public void The_magnitude_is_the_euclidean_norm() {
        // Arrange
        Vector3 vector = new Vector3(3, 4, 12);

        // Act / Assert
        Assert.Equal(13, vector.Magnitude);
    }

    [Fact]
    public void Plus_adds_component_by_component() {
        // Arrange
        Vector3 left  = new Vector3(1, 2, 3);
        Vector3 right = new Vector3(10, 20, 30);

        // Act / Assert
        Assert.Equal(new Vector3(11, 22, 33), left.Plus(right));
    }

    [Fact]
    public void Minus_subtracts_component_by_component() {
        // Arrange
        Vector3 left  = new Vector3(10, 20, 30);
        Vector3 right = new Vector3(1, 2, 3);

        // Act / Assert
        Assert.Equal(new Vector3(9, 18, 27), left.Minus(right));
    }

    [Fact]
    public void Scale_multiplies_every_component() {
        // Arrange
        Vector3 vector = new Vector3(1, -2, 3);

        // Act / Assert
        Assert.Equal(new Vector3(2.5, -5, 7.5), vector.Scale(2.5));
    }

    [Fact]
    public void Scaling_by_a_non_finite_factor_is_refused() {
        // Arrange
        Vector3 anyVector = Any.Vector3().WithX(1).WithY(1).WithZ(1).Generate();

        // Act
        Action scale = () => anyVector.Scale(double.PositiveInfinity);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(scale);
    }

    [Fact]
    public void The_dot_product_sums_the_products_of_the_components() {
        // Arrange
        Vector3 left  = new Vector3(1, 2, 3);
        Vector3 right = new Vector3(4, -5, 6);

        // Act / Assert
        Assert.Equal(12, left.Dot(right));
    }

    [Fact]
    public void The_cross_product_of_two_axes_is_the_third() {
        // Arrange
        Vector3 x = new Vector3(1, 0, 0);
        Vector3 y = new Vector3(0, 1, 0);

        // Act / Assert
        Assert.Equal(new Vector3(0, 0, 1), x.Cross(y));
        Assert.Equal(new Vector3(0, 0, -1), y.Cross(x));
    }

    [Fact]
    public void Normalizing_a_vector_gives_it_unit_length_in_the_same_direction() {
        // Arrange
        Vector3 vector = new Vector3(0, 3, 4);

        // Act
        Vector3 unit = vector.Normalized();

        // Assert
        Assert.Equal(0, unit.X);
        Assert.Equal(0.6, unit.Y, 12);
        Assert.Equal(0.8, unit.Z, 12);
    }

    [Fact(Skip = "domain defect: Normalized() of (1e200, 0, 0) returns the zero vector because Magnitude overflows to Infinity; literal values, no seed")]
    public void Normalizing_a_vector_with_very_large_components_gives_it_unit_length() {
        // Arrange
        Vector3 vector = new Vector3(1e200, 0, 0);

        // Act
        Vector3 unit = vector.Normalized();

        // Assert
        Assert.Equal(1, unit.X, 9);
    }

    [Fact(Skip = "domain defect: Normalized() of (1e-200, 0, 0), which is not the zero vector, throws 'The zero vector has no direction' because Magnitude underflows to 0; literal values, no seed")]
    public void Normalizing_a_vector_with_very_small_components_gives_it_unit_length() {
        // Arrange
        Vector3 vector = new Vector3(1e-200, 0, 0);

        // Act
        Vector3 unit = vector.Normalized();

        // Assert
        Assert.Equal(1, unit.X, 9);
    }

    [Fact(Skip = "domain defect: Normalized() returns the zero vector when a drawn component is huge (Magnitude overflows to Infinity), seed 305883612")]
    public void Normalizing_a_drawn_non_zero_vector_gives_it_unit_length() {
        // Arrange
        Vector3 anyVector = Any.Vector3().Generate();
        Assert.NotEqual(Vector3.Zero, anyVector);

        // Act
        Vector3 unit = anyVector.Normalized();

        // Assert
        Assert.Equal(1, unit.Magnitude, 9);
    }

    [Fact]
    public void The_zero_vector_has_no_direction() {
        // Act
        Action normalize = () => Vector3.Zero.Normalized();

        // Assert
        Assert.Throws<InvalidOperationException>(normalize);
    }

    [Fact]
    public void Two_vectors_with_the_same_components_are_equal_and_hash_alike() {
        // Arrange
        Vector3 anyVector = Any.Vector3().Generate();
        Vector3 twin      = new Vector3(anyVector.X, anyVector.Y, anyVector.Z);

        // Act / Assert
        Assert.Equal(anyVector, twin);
        Assert.Equal(anyVector.GetHashCode(), twin.GetHashCode());
        Assert.True(anyVector.Equals((object)twin));
        Assert.False(anyVector.Equals(null));
    }

    [Fact]
    public void Vectors_differing_by_one_component_are_different() {
        // Arrange
        Vector3 vector = new Vector3(1, 2, 3);

        // Act / Assert
        Assert.NotEqual(vector, new Vector3(1, 2, 4));
        Assert.NotEqual(vector, new Vector3(1, 0, 3));
        Assert.NotEqual(vector, new Vector3(0, 2, 3));
    }

    [Fact]
    public void A_vector_is_written_with_its_components_in_the_invariant_culture() {
        // Arrange
        Vector3 vector = new Vector3(1.5, -2, 0.25);

        // Act / Assert
        Assert.Equal("(1.5, -2, 0.25)", vector.ToString());
    }

}
