using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class ImpedanceTests {

    [Fact]
    public void Accepts_a_resistance_from_zero_to_ten_and_a_reactance_above_zero_up_to_ten() {
        // Arrange
        Impedance lossless = new Impedance(0.0, 10.0);
        Impedance resistive = new Impedance(10.0, double.Epsilon);

        // Assert
        Assert.Equal(0.0, lossless.ResistancePu);
        Assert.Equal(10.0, lossless.ReactancePu);
        Assert.Equal(10.0, resistive.ResistancePu);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(10.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Refuses_a_resistance_outside_zero_to_ten(double resistancePu) {
        // Arrange
        double anyReactancePu = Any.Double().Positive().LessThanOrEqualTo(Impedance.MaximumPu).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Impedance(resistancePu, anyReactancePu));

        // Assert
        Assert.Equal("resistancePu", exception.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(10.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_reactance_that_is_not_inductive_or_exceeds_ten(double reactancePu) {
        // Arrange
        double anyResistancePu = Any.Double().Between(0.0, Impedance.MaximumPu).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Impedance(anyResistancePu, reactancePu));

        // Assert
        Assert.Equal("reactancePu", exception.ParamName);
    }

    [Fact]
    public void Exposes_its_value_as_a_complex_number() {
        // Arrange
        Impedance impedance = new Impedance(0.02, 0.1);

        // Assert
        Assert.Equal(new Complex(0.02, 0.1), impedance.Value);
    }

    [Fact]
    public void Admittance_is_the_reciprocal_of_the_impedance() {
        // Arrange
        Impedance anyImpedance = Any.Impedance().Generate();

        // Act
        Complex product = anyImpedance.Value * anyImpedance.Admittance;

        // Assert
        Assert.Equal(1.0, product.Real, 9);
        Assert.Equal(0.0, product.Imaginary, 9);
    }

    [Fact]
    public void Admittance_of_a_purely_reactive_branch_is_negative_imaginary() {
        // Arrange
        double anyReactancePu = Any.Double().Positive().LessThanOrEqualTo(Impedance.MaximumPu).Generate();
        Impedance impedance   = new Impedance(0.0, anyReactancePu);

        // Assert
        Assert.Equal(0.0, impedance.Admittance.Real, 12);
        Assert.True(impedance.Admittance.Imaginary < 0.0);
    }

    [Fact]
    public void X_over_R_is_the_reactance_divided_by_the_resistance() {
        Assert.Equal(5.0, new Impedance(0.02, 0.1).XOverR, 12);
    }

    [Fact]
    public void X_over_R_of_a_lossless_branch_is_infinite() {
        // Arrange
        double anyReactancePu = Any.Double().Positive().LessThanOrEqualTo(Impedance.MaximumPu).Generate();

        // Assert
        Assert.Equal(double.PositiveInfinity, new Impedance(0.0, anyReactancePu).XOverR);
    }

    [Fact]
    public void Two_impedances_with_the_same_parts_are_equal() {
        // Arrange
        Impedance anyImpedance = Any.Impedance().Generate();
        Impedance copy         = new Impedance(anyImpedance.ResistancePu, anyImpedance.ReactancePu);

        // Assert
        Assert.Equal(anyImpedance, copy);
        Assert.Equal(anyImpedance.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Impedances_differing_by_either_part_are_different() {
        // Arrange
        Impedance reference = new Impedance(0.02, 0.1);

        // Assert
        Assert.NotEqual(reference, new Impedance(0.03, 0.1));
        Assert.NotEqual(reference, new Impedance(0.02, 0.2));
        Assert.False(reference.Equals(null));
    }

    [Fact]
    public void Prints_both_parts() {
        Assert.Equal("0.02 + j0.1 pu", new Impedance(0.02, 0.1).ToString());
    }

}
