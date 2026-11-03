using JustDummies;

using SatelliteConjunction.Domain;

using Xunit;

namespace SatelliteConjunction.Tests.Regular;

public sealed class Vector3Tests {

    /// <summary>A vector of ordinary size: the arithmetic under test never has to wonder about overflow.</summary>
    private static AnyVector3 AnyOrdinaryVector() {
        return Any.Vector3()
                  .WithX(x => x.Between(-1_000, 1_000))
                  .WithY(y => y.Between(-1_000, 1_000))
                  .WithZ(z => z.Between(-1_000, 1_000));
    }

    [Fact]
    public void A_vector_keeps_its_three_components() {
        // Arrange
        double anyX = Any.Double().Between(-1e6, 1e6).Generate();
        double anyY = Any.Double().Between(-1e6, 1e6).Generate();
        double anyZ = Any.Double().Between(-1e6, 1e6).Generate();

        // Act
        Vector3 vector = new(anyX, anyY, anyZ);

        // Assert
        Assert.Equal(anyX, vector.X);
        Assert.Equal(anyY, vector.Y);
        Assert.Equal(anyZ, vector.Z);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void A_component_that_is_not_finite_is_refused(int component) {
        // Arrange
        double[] components = [Any.Double().Between(-1e6, 1e6).Generate(), Any.Double().Between(-1e6, 1e6).Generate(), Any.Double().Between(-1e6, 1e6).Generate()];
        components[component] = Any.OneOf(double.NaN, double.PositiveInfinity, double.NegativeInfinity).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new Vector3(components[0], components[1], components[2]));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(refusal);
    }

    [Fact]
    public void The_zero_vector_has_all_its_components_at_zero() {
        // Arrange / Act
        Vector3 zero = Vector3.Zero;

        // Assert
        Assert.Equal(0d, zero.X);
        Assert.Equal(0d, zero.Y);
        Assert.Equal(0d, zero.Z);
        Assert.Equal(0d, zero.Magnitude);
    }

    [Fact]
    public void The_magnitude_of_a_3_4_12_vector_is_13() {
        // Arrange
        Vector3 vector = new(3, 4, 12);

        // Act
        double magnitude = vector.Magnitude;

        // Assert
        Assert.Equal(13d, magnitude, 12);
    }

    [Fact]
    public void Plus_adds_component_by_component() {
        // Arrange
        Vector3 anyLeft  = AnyOrdinaryVector().Generate();
        Vector3 anyRight = AnyOrdinaryVector().Generate();

        // Act
        Vector3 sum = anyLeft.Plus(anyRight);

        // Assert
        Assert.Equal(anyLeft.X + anyRight.X, sum.X);
        Assert.Equal(anyLeft.Y + anyRight.Y, sum.Y);
        Assert.Equal(anyLeft.Z + anyRight.Z, sum.Z);
    }

    [Fact]
    public void Minus_subtracts_component_by_component() {
        // Arrange
        Vector3 anyLeft  = AnyOrdinaryVector().Generate();
        Vector3 anyRight = AnyOrdinaryVector().Generate();

        // Act
        Vector3 difference = anyLeft.Minus(anyRight);

        // Assert
        Assert.Equal(anyLeft.X - anyRight.X, difference.X);
        Assert.Equal(anyLeft.Y - anyRight.Y, difference.Y);
        Assert.Equal(anyLeft.Z - anyRight.Z, difference.Z);
    }

    [Fact]
    public void Subtracting_a_vector_from_itself_gives_the_zero_vector() {
        // Arrange
        Vector3 anyVector = AnyOrdinaryVector().Generate();

        // Act
        Vector3 difference = anyVector.Minus(anyVector);

        // Assert
        Assert.Equal(Vector3.Zero, difference);
    }

    [Fact]
    public void Scale_multiplies_every_component_by_the_factor() {
        // Arrange
        Vector3 anyVector = AnyOrdinaryVector().Generate();
        double  anyFactor = Any.Double().Between(-100, 100).Generate();

        // Act
        Vector3 scaled = anyVector.Scale(anyFactor);

        // Assert
        Assert.Equal(anyVector.X * anyFactor, scaled.X);
        Assert.Equal(anyVector.Y * anyFactor, scaled.Y);
        Assert.Equal(anyVector.Z * anyFactor, scaled.Z);
    }

    [Fact]
    public void Scaling_multiplies_the_magnitude_by_the_absolute_factor() {
        // Arrange
        Vector3 anyVector = AnyOrdinaryVector().Generate();
        double  anyFactor = Any.Double().Between(-100, 100).Generate();

        // Act
        double scaledMagnitude = anyVector.Scale(anyFactor).Magnitude;

        // Assert
        Assert.Equal(Math.Abs(anyFactor) * anyVector.Magnitude, scaledMagnitude, 1e-9 * (1 + scaledMagnitude));
    }

    [Fact]
    public void Dot_multiplies_component_by_component_and_sums() {
        // Arrange
        Vector3 left  = new(1, 2, 3);
        Vector3 right = new(4, -5, 6);

        // Act
        double dot = left.Dot(right);

        // Assert
        Assert.Equal(12d, dot);
    }

    [Fact]
    public void Dot_is_symmetric() {
        // Arrange
        Vector3 anyLeft  = AnyOrdinaryVector().Generate();
        Vector3 anyRight = AnyOrdinaryVector().Generate();

        // Act
        double leftDotRight = anyLeft.Dot(anyRight);
        double rightDotLeft = anyRight.Dot(anyLeft);

        // Assert
        Assert.Equal(leftDotRight, rightDotLeft);
    }

    [Fact]
    public void The_cross_product_of_the_x_and_y_axes_is_the_z_axis() {
        // Arrange
        Vector3 x = new(1, 0, 0);
        Vector3 y = new(0, 1, 0);

        // Act
        Vector3 z = x.Cross(y);

        // Assert
        Assert.Equal(new Vector3(0, 0, 1), z);
    }

    [Fact]
    public void The_cross_product_is_anticommutative() {
        // Arrange
        Vector3 anyLeft  = AnyOrdinaryVector().Generate();
        Vector3 anyRight = AnyOrdinaryVector().Generate();

        // Act
        Vector3 leftCrossRight = anyLeft.Cross(anyRight);
        Vector3 rightCrossLeft = anyRight.Cross(anyLeft);

        // Assert
        Assert.Equal(leftCrossRight, rightCrossLeft.Scale(-1));
    }

    [Fact]
    public void The_cross_product_is_orthogonal_to_both_operands() {
        // Arrange
        Vector3 anyLeft  = AnyOrdinaryVector().Generate();
        Vector3 anyRight = AnyOrdinaryVector().Generate();

        // Act
        Vector3 cross = anyLeft.Cross(anyRight);

        // Assert
        double tolerance = 1e-9 * (1 + cross.Magnitude * Math.Max(anyLeft.Magnitude, anyRight.Magnitude));
        Assert.Equal(0d, cross.Dot(anyLeft), tolerance);
        Assert.Equal(0d, cross.Dot(anyRight), tolerance);
    }

    [Fact]
    public void A_normalized_vector_has_a_magnitude_of_one_and_the_same_direction() {
        // Arrange
        Vector3 anyVector = Any.Vector3()
                               .WithX(x => x.Between(1, 1_000))
                               .WithY(y => y.Between(1, 1_000))
                               .WithZ(z => z.Between(1, 1_000))
                               .Generate();

        // Act
        Vector3 unit = anyVector.Normalized();

        // Assert
        Assert.Equal(1d, unit.Magnitude, 12);
        Assert.Equal(anyVector.Magnitude, anyVector.Dot(unit), 1e-9);
    }

    [Fact]
    public void The_zero_vector_cannot_be_normalized() {
        // Arrange
        Vector3 zero = Vector3.Zero;

        // Act
        Exception? refusal = Record.Exception(() => zero.Normalized());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Two_vectors_with_the_same_components_are_equal_and_hash_alike() {
        // Arrange
        Vector3 anyVector = Any.Vector3().Generate();
        Vector3 copy      = new(anyVector.X, anyVector.Y, anyVector.Z);

        // Act
        bool equal         = anyVector.Equals(copy);
        bool equalAsObject = anyVector.Equals((object)copy);

        // Assert
        Assert.True(equal);
        Assert.True(equalAsObject);
        Assert.Equal(anyVector.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Two_vectors_differing_by_one_component_are_not_equal() {
        // Arrange
        Vector3 anyVector = AnyOrdinaryVector().Generate();
        Vector3 other     = new(anyVector.X, anyVector.Y, Any.Double().Between(-1_000, 1_000).DifferentFrom(anyVector.Z).Generate());

        // Act
        bool equal       = anyVector.Equals(other);
        bool equalToNull = anyVector.Equals(null);

        // Assert
        Assert.False(equal);
        Assert.False(equalToNull);
    }

    [Fact]
    public void A_vector_is_written_with_the_invariant_culture() {
        // Arrange
        Vector3 vector = new(1.5, -2, 0.25);

        // Act
        string text = vector.ToString();

        // Assert
        Assert.Equal("(1.5, -2, 0.25)", text);
    }

}
