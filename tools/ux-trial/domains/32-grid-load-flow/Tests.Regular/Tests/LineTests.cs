using System.Numerics;

using GridLoadFlow.Domain;

using JustDummies;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class LineTests {

    [Fact]
    public void Starts_in_service() {
        // Arrange
        Line anyLine = Any.Line().Generate();

        // Assert
        Assert.True(anyLine.InService);
    }

    [Fact]
    public void Joins_two_distinct_buses() {
        // Arrange
        Line anyLine = Any.Line().Generate();

        // Assert
        Assert.NotEqual(anyLine.FromBus, anyLine.ToBus);
    }

    [Fact]
    public void Refuses_a_line_from_a_bus_to_itself() {
        // Arrange
        BusId anyBus  = Any.BusId().Generate();
        Line  anyLine = Any.Line().Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new Line(anyLine.Id, anyBus, anyBus, anyLine.Impedance, anyLine.ChargingSusceptancePu, anyLine.RatingMva));

        // Assert
        Assert.Equal("toBus", exception.ParamName);
        Assert.Contains(anyBus.Value, exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Refuses_a_blank_id(string id) {
        Assert.Throws<ArgumentException>(() => Any.Line().WithId(id).Generate());
    }

    [Fact]
    public void Refuses_a_null_id() {
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithId((string)null!).Generate());
    }

    [Fact]
    public void An_id_of_thirty_two_characters_is_the_longest_accepted() {
        // Arrange
        string anyLongestId = Any.String().Alpha().WithLength(32).Generate();
        string anyTooLongId = Any.String().Alpha().WithLength(33).Generate();

        // Act
        Line       line    = Any.Line().WithId(anyLongestId).Generate();
        Exception? refusal = Record.Exception(() => Any.Line().WithId(anyTooLongId).Generate());

        // Assert
        Assert.Equal(anyLongestId, line.Id);
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Refuses_a_missing_bus_or_impedance() {
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithFromBus((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithToBus((BusId)null!).Generate());
        Assert.Throws<ArgumentNullException>(() => Any.Line().WithImpedance((Impedance)null!).Generate());
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(5.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_charging_susceptance_outside_zero_to_five(double chargingSusceptancePu) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.Line().WithChargingSusceptancePu(chargingSusceptancePu).Generate());

        // Assert
        Assert.Equal("chargingSusceptancePu", exception.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(10_000.001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Refuses_a_rating_outside_zero_exclusive_to_ten_thousand(double ratingMva) {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Line().WithRatingMva(ratingMva).Generate());

        // Assert
        Assert.Equal("ratingMva", exception.ParamName);
    }

    [Fact]
    public void Accepts_a_lossless_line_with_no_charging_and_a_rating_of_ten_thousand() {
        // Act
        Line line = Any.Line().WithChargingSusceptancePu(0.0).WithRatingMva(10_000.0).Generate();

        // Assert
        Assert.Equal(0.0, line.ChargingSusceptancePu);
        Assert.Equal(10_000.0, line.RatingMva);
    }

    [Fact]
    public void Stamp_puts_the_series_admittance_and_half_the_charging_at_each_end() {
        // Arrange
        Impedance impedance = new Impedance(0.02, 0.1);
        Line      line      = Any.Line().WithImpedance(impedance).WithChargingSusceptancePu(0.4).Generate();
        Complex   y         = impedance.Admittance;
        Complex   halfB     = new Complex(0.0, 0.2);

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = line.Stamp();

        // Assert
        Assert.Equal(y + halfB, ff);
        Assert.Equal(y + halfB, tt);
        Assert.Equal(-y, ft);
        Assert.Equal(-y, tf);
    }

    [Fact]
    public void Stamp_of_a_line_without_charging_has_equal_diagonal_and_opposite_off_diagonal() {
        // Arrange
        Line anyChargelessLine = Any.Line().WithChargingSusceptancePu(0.0).Generate();

        // Act
        (Complex ff, Complex ft, Complex tf, Complex tt) = anyChargelessLine.Stamp();

        // Assert
        Assert.Equal(ff, tt);
        Assert.Equal(-ff, ft);
        Assert.Equal(ft, tf);
    }

}
