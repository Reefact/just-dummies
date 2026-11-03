using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests;

public sealed class DimensionTests {

    [Fact]
    public void A_nominal_that_is_not_a_number_is_refused_as_an_argument_error() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Millimetre, null));

        Assert.IsNotType<ArgumentOutOfRangeException>(error);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(-500.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_nominal_that_is_not_strictly_positive_is_refused(double nominal) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(nominal, LengthUnit.Millimetre, null));

        Assert.Equal("nominal", error.ParamName);
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(25.0)]
    [InlineData(499.999)]
    [InlineData(500.0)]
    public void A_nominal_up_to_500_millimetres_is_accepted(double nominal) {
        // Act
        Dimension dimension = new Dimension(nominal, LengthUnit.Millimetre, null);

        // Assert
        Assert.Equal(nominal, dimension.Nominal);
        Assert.Equal(nominal, dimension.NominalInMillimetres);
    }

    [Theory]
    [InlineData(500.001)]
    [InlineData(1000.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.MaxValue)]
    public void A_nominal_above_500_millimetres_is_refused(double nominal) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(nominal, LengthUnit.Millimetre, null));

        Assert.Equal("Nominal", error.ParamName);
    }

    [Fact]
    public void The_500_millimetre_limit_applies_after_converting_inches() {
        // 19.685 in = 499.999 mm, 19.69 in = 500.126 mm
        Assert.Equal(499.999, new Dimension(19.685, LengthUnit.Inch, null).NominalInMillimetres, 9);

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(19.69, LengthUnit.Inch, null));
        Assert.Equal("Nominal", error.ParamName);
    }

    [Fact]
    public void A_size_that_fits_in_inches_but_would_not_in_millimetres_is_accepted() {
        // Act
        Dimension dimension = new Dimension(19.0, LengthUnit.Inch, null);

        // Assert
        Assert.Equal(482.6, dimension.NominalInMillimetres, 9);
    }

    [Fact]
    public void An_inch_nominal_is_converted_with_25_point_4() {
        Assert.Equal(25.4, new Dimension(1.0, LengthUnit.Inch, null).NominalInMillimetres, 12);
        Assert.Equal(Dimension.MillimetresPerInch, 25.4);
    }

    [Fact]
    public void A_unit_that_is_not_a_defined_member_is_treated_as_millimetres() {
        // Act
        Dimension dimension = new Dimension(25.0, (LengthUnit)99, null);

        // Assert
        Assert.Equal(25.0, dimension.NominalInMillimetres);
    }

    [Fact]
    public void The_nominal_is_checked_for_a_number_before_its_sign() {
        Assert.IsNotType<ArgumentOutOfRangeException>(Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Millimetre, null)));
    }

    [Fact]
    public void Without_a_tolerance_the_limits_are_the_nominal() {
        // Act
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, null);

        // Assert
        Assert.Null(dimension.Tolerance);
        Assert.Equal(25.0, dimension.UpperLimitInMillimetres);
        Assert.Equal(25.0, dimension.LowerLimitInMillimetres);
    }

    [Theory]
    // nominal (mm), zone, grade, expected lower limit, expected upper limit
    [InlineData(25.0, "H", 7, 25.000, 25.021)]
    [InlineData(25.0, "h", 6, 24.987, 25.000)]
    [InlineData(25.0, "JS", 7, 24.9895, 25.0105)]
    [InlineData(25.0, "js", 7, 24.9895, 25.0105)]
    [InlineData(25.0, "js", 6, 24.9935, 25.0065)]
    [InlineData(10.0, "H", 8, 10.000, 10.022)]        // 10 mm closes the 6..10 range
    [InlineData(10.001, "H", 8, 10.001, 10.028)]      // just above it opens 10..18
    [InlineData(3.0, "h", 11, 2.94, 3.0)]
    [InlineData(500.0, "H", 11, 500.0, 500.4)]
    public void The_deviations_follow_the_zone(double nominal, string zone, int grade, double lower, double upper) {
        // Arrange
        Dimension dimension = new Dimension(nominal, LengthUnit.Millimetre, new IsoTolerance(zone, grade));

        // Assert
        Assert.Equal(lower, dimension.LowerLimitInMillimetres, 9);
        Assert.Equal(upper, dimension.UpperLimitInMillimetres, 9);
    }

    [Fact]
    public void The_limits_of_an_inch_dimension_follow_its_size_in_millimetres() {
        // 1 in = 25.4 mm, in the 18..30 mm range: IT7 = 21 um
        Dimension dimension = new Dimension(1.0, LengthUnit.Inch, new IsoTolerance("H", 7));

        // Assert
        Assert.Equal(25.4, dimension.LowerLimitInMillimetres, 9);
        Assert.Equal(25.421, dimension.UpperLimitInMillimetres, 9);
    }

    [Fact]
    public void The_limits_are_given_in_millimetres_whatever_the_unit() {
        // Arrange
        IsoTolerance tolerance = new IsoTolerance("h", 6);
        Dimension    inInches  = new Dimension(1.0, LengthUnit.Inch, tolerance);
        Dimension    inMm      = new Dimension(25.4, LengthUnit.Millimetre, tolerance);

        // Assert
        Assert.Equal(inMm.UpperLimitInMillimetres, inInches.UpperLimitInMillimetres, 9);
        Assert.Equal(inMm.LowerLimitInMillimetres, inInches.LowerLimitInMillimetres, 9);
    }

    [Fact]
    public void A_measure_between_the_limits_is_accepted() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Assert
        Assert.True(dimension.Accepts(25.010));
    }

    [Fact]
    public void The_limits_themselves_are_accepted() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Assert
        Assert.True(dimension.Accepts(dimension.LowerLimitInMillimetres));
        Assert.True(dimension.Accepts(dimension.UpperLimitInMillimetres));
    }

    [Fact]
    public void A_measure_just_outside_the_limits_is_refused() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Assert
        Assert.False(dimension.Accepts(dimension.UpperLimitInMillimetres + 1e-6));
        Assert.False(dimension.Accepts(dimension.LowerLimitInMillimetres - 1e-6));
    }

    [Fact]
    public void Without_a_tolerance_only_the_exact_nominal_is_accepted() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, null);

        // Assert
        Assert.True(dimension.Accepts(25.0));
        Assert.False(dimension.Accepts(25.0000001));
    }

    [Fact]
    public void A_measure_that_is_not_a_number_is_never_accepted() {
        // Arrange
        Dimension anyDimension = Any.Dimension().Generate();

        // Assert
        Assert.False(anyDimension.Accepts(double.NaN));
    }

    [Theory]
    [InlineData("H", 7, "Ø25 H7 mm")]
    [InlineData("h", 6, "Ø25 h6 mm")]
    public void ToString_shows_the_nominal_the_class_and_the_unit(string zone, int grade, string expected) {
        Assert.Equal(expected, new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance(zone, grade)).ToString());
    }

    [Fact]
    public void ToString_omits_the_class_when_there_is_no_tolerance() {
        Assert.Equal("Ø25 mm", new Dimension(25.0, LengthUnit.Millimetre, null).ToString());
        Assert.Equal("Ø1 in", new Dimension(1.0, LengthUnit.Inch, null).ToString());
    }

}
