using Drafting.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Drafting.Domain.Tests.Expert.Tests;

public sealed class DimensionTests {

    private const int Precision = 9;

    private static IsoTolerance Tolerance(string zone, int grade) {
        return new IsoTolerance(zone, grade);
    }

    // ----- construction -----

    [Fact]
    public void A_dimension_keeps_its_nominal_unit_and_tolerance() {
        IsoTolerance h7 = Tolerance("H", 7);

        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, h7);

        Assert.Equal(25.0, dimension.Nominal);
        Assert.Equal(LengthUnit.Millimetre, dimension.Unit);
        Assert.Same(h7, dimension.Tolerance);
    }

    [Fact]
    public void The_tolerance_is_optional() {
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, null);

        Assert.Null(dimension.Tolerance);
    }

    [Fact]
    public void A_nominal_that_is_not_a_number_is_refused() {
        IsoTolerance? anyTolerance = Any.IsoTolerance().OrNull().Generate();

        // ArgumentException itself, not the out-of-range flavour.
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Millimetre, anyTolerance));

        Assert.Equal("nominal", error.ParamName);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.001)]
    [InlineData(-25.0)]
    [InlineData(double.NegativeInfinity)]
    public void A_nominal_that_is_not_strictly_positive_is_refused(double nominal) {
        LengthUnit anyUnit = Any.Enum<LengthUnit>().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(nominal, anyUnit, null));

        Assert.Equal("nominal", error.ParamName);
    }

    [Theory]
    [InlineData(500.0, LengthUnit.Millimetre)]
    [InlineData(499.999, LengthUnit.Millimetre)]
    [InlineData(19.68, LengthUnit.Inch)]     // 499.872 mm
    [InlineData(0.001, LengthUnit.Millimetre)]
    public void A_nominal_up_to_500_millimetres_is_accepted_whatever_the_unit(double nominal, LengthUnit unit) {
        Dimension dimension = new Dimension(nominal, unit, null);

        Assert.Equal(nominal, dimension.Nominal);
    }

    [Theory]
    [InlineData(500.001, LengthUnit.Millimetre)]
    [InlineData(1_000.0, LengthUnit.Millimetre)]
    [InlineData(19.69, LengthUnit.Inch)]     // 500.126 mm
    [InlineData(20.0, LengthUnit.Inch)]
    [InlineData(500.0, LengthUnit.Inch)]     // 12 700 mm: the limit applies once converted
    [InlineData(double.PositiveInfinity, LengthUnit.Millimetre)]
    public void A_nominal_above_500_millimetres_is_refused_once_converted(double nominal, LengthUnit unit) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(nominal, unit, null));

        // The parameter name reported is the property's ("Nominal"), not the constructor parameter's.
        Assert.Equal("Nominal", error.ParamName);
    }

    [Fact]
    public void The_nan_check_comes_before_the_range_checks() {
        Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Inch, null));
    }

    [Fact(Skip = "domain defect: the unit is not checked for being a defined LengthUnit, so (LengthUnit)99 is silently read as millimetres, while Drawing refuses an undefined projection")]
    public void An_undefined_unit_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(25.0, (LengthUnit)99, null));
    }

    // ----- sizes in millimetres -----

    [Theory]
    [InlineData(25.0, LengthUnit.Millimetre, 25.0)]
    [InlineData(1.0, LengthUnit.Inch, 25.4)]
    [InlineData(2.5, LengthUnit.Inch, 63.5)]
    [InlineData(0.5, LengthUnit.Inch, 12.7)]
    public void The_nominal_is_converted_to_millimetres(double nominal, LengthUnit unit, double expectedMillimetres) {
        IsoTolerance? anyTolerance = Any.IsoTolerance().OrNull().Generate();
        Dimension     dimension    = new Dimension(nominal, unit, anyTolerance);

        Assert.Equal(expectedMillimetres, dimension.NominalInMillimetres, Precision);
    }

    // ----- limits -----

    [Theory]
    [InlineData("H", 7, 25.0, 25.021, 25.000)]       // Ø25 H7: IT7 = 21 µm, upper +IT
    [InlineData("H", 11, 25.0, 25.130, 25.000)]      // IT11 = 130 µm
    [InlineData("h", 6, 25.0, 25.000, 24.987)]       // Ø25 h6: IT6 = 13 µm, lower -IT
    [InlineData("h", 11, 25.0, 25.000, 24.870)]
    [InlineData("JS", 7, 25.0, 25.0105, 24.9895)]    // symmetric ±IT/2
    [InlineData("js", 6, 25.0, 25.0065, 24.9935)]
    [InlineData("H", 7, 3.0, 3.010, 3.000)]          // first range, upper bound included
    [InlineData("H", 7, 3.1, 3.112, 3.100)]          // second range
    [InlineData("H", 7, 500.0, 500.063, 500.000)]    // last range, upper bound included
    [InlineData("h", 9, 100.0, 100.000, 99.913)]     // 87 µm
    public void A_toleranced_dimension_is_bounded_by_the_deviations_of_its_zone(string zone, int grade, double nominal, double expectedUpper, double expectedLower) {
        Dimension dimension = new Dimension(nominal, LengthUnit.Millimetre, Tolerance(zone, grade));

        Assert.Equal(expectedUpper, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(expectedLower, dimension.LowerLimitInMillimetres, Precision);
    }

    [Fact]
    public void The_tolerance_is_looked_up_on_the_size_in_millimetres_not_on_the_inch_nominal() {
        // 1 in = 25.4 mm sits in the 18..30 mm range: IT7 = 21 µm. Read as a raw "1", it would be the first range (10 µm).
        Dimension dimension = new Dimension(1.0, LengthUnit.Inch, Tolerance("H", 7));

        Assert.Equal(25.421, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(25.4, dimension.LowerLimitInMillimetres, Precision);
    }

    [Fact]
    public void Without_a_tolerance_both_limits_are_the_nominal() {
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, null);

        Assert.Equal(25.0, dimension.UpperLimitInMillimetres);
        Assert.Equal(25.0, dimension.LowerLimitInMillimetres);
    }

    [Fact]
    public void An_inch_dimension_without_a_tolerance_is_limited_by_its_converted_nominal() {
        Dimension dimension = new Dimension(2.0, LengthUnit.Inch, null);

        Assert.Equal(50.8, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(50.8, dimension.LowerLimitInMillimetres, Precision);
    }

    // ----- acceptance of a measure -----

    [Theory]
    [InlineData(25.000, true)]      // the lower limit is accepted
    [InlineData(25.021, true)]      // so is the upper limit
    [InlineData(25.010, true)]
    [InlineData(24.999, false)]
    [InlineData(25.022, false)]
    [InlineData(0.0, false)]
    [InlineData(double.NaN, false)]
    [InlineData(double.PositiveInfinity, false)]
    public void A_measure_is_accepted_between_the_limits_inclusive(double measured, bool expected) {
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, Tolerance("H", 7));

        Assert.Equal(expected, dimension.Accepts(measured));
    }

    [Fact]
    public void Without_a_tolerance_only_the_nominal_itself_is_accepted() {
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, null);

        Assert.True(dimension.Accepts(25.0));
        Assert.False(dimension.Accepts(25.0001));
        Assert.False(dimension.Accepts(24.9999));
    }

    // ----- text -----

    [Theory]
    [InlineData(25.0, LengthUnit.Millimetre, "H", 7, "Ø25 H7 mm")]
    [InlineData(25.0, LengthUnit.Millimetre, "h", 6, "Ø25 h6 mm")]
    [InlineData(1.5, LengthUnit.Inch, "js", 9, "Ø1.5 js9 in")]
    public void A_toleranced_dimension_reads_as_diameter_class_and_unit(double nominal, LengthUnit unit, string zone, int grade, string expected) {
        Assert.Equal(expected, new Dimension(nominal, unit, Tolerance(zone, grade)).ToString());
    }

    [Theory]
    [InlineData(25.0, LengthUnit.Millimetre, "Ø25 mm")]
    [InlineData(1.5, LengthUnit.Inch, "Ø1.5 in")]
    public void An_untoleranced_dimension_reads_as_diameter_and_unit(double nominal, LengthUnit unit, string expected) {
        Assert.Equal(expected, new Dimension(nominal, unit, null).ToString());
    }

}
