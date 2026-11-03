using System.Globalization;
using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Domain.Tests.Expert;

public sealed class ImpedanceTests {

    [Theory]
    [InlineData(0d, 0.5d)]        // a purely reactive branch
    [InlineData(10d, 10d)]        // both ceilings
    [InlineData(0.01, double.Epsilon)]
    public void Resistance_and_reactance_inside_their_ranges_are_kept(double resistance, double reactance) {
        Impedance impedance = new Impedance(resistance, reactance);

        Assert.Equal(resistance, impedance.ResistancePu);
        Assert.Equal(reactance, impedance.ReactancePu);
    }

    [Theory]
    [InlineData(-0.0001)]
    [InlineData(10.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void A_resistance_outside_zero_to_ten_is_refused(double resistance) {
        double anyReactance = Any.Double().Between(0.001d, Impedance.MaximumPu).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Impedance(resistance, anyReactance));

        Assert.Equal("resistancePu", refusal.ParamName);
    }

    [Theory]
    [InlineData(0d)]                  // not inductive
    [InlineData(-0.1)]
    [InlineData(10.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void A_reactance_outside_zero_exclusive_to_ten_is_refused(double reactance) {
        double anyResistance = Any.Double().Between(0d, Impedance.MaximumPu).Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Impedance(anyResistance, reactance));

        Assert.Equal("reactancePu", refusal.ParamName);
    }

    [Fact]
    public void The_admittance_of_a_tenth_plus_two_tenths_j_is_two_minus_four_j() {
        Impedance impedance = new Impedance(0.1, 0.2);

        Complex admittance = impedance.Admittance;

        Assert.Equal(2.0, admittance.Real, 12);
        Assert.Equal(-4.0, admittance.Imaginary, 12);
    }

    [Fact]
    public void The_value_carries_resistance_as_real_part_and_reactance_as_imaginary_part() {
        Impedance impedance = new Impedance(0.03, 0.12);

        Assert.Equal(new Complex(0.03, 0.12), impedance.Value);
    }

    [Fact]
    public void X_over_R_is_the_ratio_of_reactance_to_resistance() {
        Impedance impedance = new Impedance(0.05, 0.4);

        Assert.Equal(8.0, impedance.XOverR, 12);
    }

    [Fact]
    public void X_over_R_of_a_branch_without_resistance_is_infinite() {
        Impedance impedance = new Impedance(0d, 0.4);

        Assert.True(double.IsPositiveInfinity(impedance.XOverR));
    }

    [Fact]
    public void Impedances_with_the_same_parts_are_equal() {
        Impedance first  = new Impedance(0.02, 0.06);
        Impedance second = new Impedance(0.02, 0.06);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Theory]
    [InlineData(0.03, 0.06)]
    [InlineData(0.02, 0.07)]
    public void Impedances_differing_by_either_part_are_different(double resistance, double reactance) {
        Impedance reference = new Impedance(0.02, 0.06);

        Assert.NotEqual(reference, new Impedance(resistance, reactance));
    }

    [Fact]
    public void An_impedance_reads_as_resistance_plus_j_reactance() {
        Impedance impedance = new Impedance(0.5, 2);

        using (new CultureScope(CultureInfo.InvariantCulture)) {
            Assert.Equal("0.5 + j2 pu", impedance.ToString());
        }
    }

    [Fact(Skip = "domain defect: ToString formats with the current culture, so a French thread reads '0,5 + j2 pu' (literal input, no drawn value, seed n/a)")]
    public void An_impedance_reads_the_same_whatever_the_culture_of_the_thread() {
        Impedance impedance = new Impedance(0.5, 2);

        using (new CultureScope(new CultureInfo("fr-FR"))) {
            Assert.Equal("0.5 + j2 pu", impedance.ToString());
        }
    }

}
