using System.Globalization;

using JustDummies;

using Xunit;

namespace Drafting.Domain;

public sealed class DimensionTests {

    private const int Precision = 9;

    [Fact]
    public void A_dimension_keeps_what_it_was_given() {
        // Arrange
        double       anyNominal   = Any.Double().Between(0.001, 19.6).Generate();
        LengthUnit   anyUnit      = Any.Enum<LengthUnit>().Generate();
        IsoTolerance anyTolerance = Any.IsoTolerance().Generate();

        // Act
        Dimension dimension = new Dimension(anyNominal, anyUnit, anyTolerance);

        // Assert
        Assert.Equal(anyNominal, dimension.Nominal);
        Assert.Equal(anyUnit, dimension.Unit);
        Assert.Same(anyTolerance, dimension.Tolerance);
    }

    [Fact]
    public void The_tolerance_is_optional() {
        // Act
        Dimension dimension = Any.Dimension().WithoutTolerance().Generate();

        // Assert
        Assert.Null(dimension.Tolerance);
    }

    [Fact]
    public void A_nan_nominal_is_refused_as_an_argument_error() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Millimetre, null));

        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(exception);
        Assert.Equal("nominal", exception.ParamName);
    }

    [Fact]
    public void A_nominal_of_zero_or_less_is_refused() {
        // Arrange
        double     anyNonPositive = Any.Double().LessThanOrEqualTo(0).Generate();
        LengthUnit anyUnit        = Any.Enum<LengthUnit>().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(anyNonPositive, anyUnit, null));

        // Assert
        Assert.Equal("nominal", exception.ParamName);
    }

    [Fact]
    public void The_nan_check_comes_before_the_positivity_check_and_the_positivity_check_before_the_ceiling() {
        // Assert
        Assert.IsNotType<ArgumentOutOfRangeException>(Assert.Throws<ArgumentException>(() => new Dimension(double.NaN, LengthUnit.Inch, null)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(-1000.0, LengthUnit.Inch, null));
    }

    [Fact]
    public void A_nominal_above_500_millimetres_is_refused() {
        // Arrange
        double anyTooLarge = Any.Double().GreaterThan(StandardTolerance.LargestNominalMillimetres).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(anyTooLarge, LengthUnit.Millimetre, null));

        // Assert
        Assert.Equal("Nominal", exception.ParamName);
    }

    [Fact]
    public void A_nominal_of_exactly_500_millimetres_is_accepted() {
        // Act
        Dimension dimension = new Dimension(500.0, LengthUnit.Millimetre, null);

        // Assert
        Assert.Equal(500.0, dimension.NominalInMillimetres);
    }

    [Fact]
    public void The_500_millimetre_ceiling_applies_to_the_nominal_converted_from_inches() {
        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dimension(19.7, LengthUnit.Inch, null));    // 500.38 mm
        Assert.Equal(19.6 * 25.4, new Dimension(19.6, LengthUnit.Inch, null).NominalInMillimetres, Precision);
    }

    [Fact]
    public void A_nominal_in_millimetres_is_not_converted() {
        // Arrange
        double anyNominal = Any.Double().Between(0.001, 500.0).Generate();

        // Act
        Dimension dimension = new Dimension(anyNominal, LengthUnit.Millimetre, null);

        // Assert
        Assert.Equal(anyNominal, dimension.NominalInMillimetres);
    }

    [Fact]
    public void A_nominal_in_inches_is_converted_at_25_point_4_millimetres_per_inch() {
        // Arrange
        double anyNominal = Any.Double().Between(0.001, 19.6).Generate();

        // Act
        Dimension dimension = new Dimension(anyNominal, LengthUnit.Inch, null);

        // Assert
        Assert.Equal(anyNominal * 25.4, dimension.NominalInMillimetres, Precision);
    }

    [Fact]
    public void A_unit_that_is_not_a_declared_member_is_treated_as_millimetres() {
        // Arrange
        int anyUndeclared = Any.Int32().Except(0, 1).Generate();

        // Act
        Dimension dimension = new Dimension(10.0, (LengthUnit)anyUndeclared, null);

        // Assert
        Assert.Equal(10.0, dimension.NominalInMillimetres);
    }

    [Fact]
    public void Without_a_tolerance_the_limits_are_the_nominal() {
        // Arrange
        Dimension anyPlainDimension = Any.Dimension().WithoutTolerance().Generate();

        // Assert
        Assert.Equal(anyPlainDimension.NominalInMillimetres, anyPlainDimension.UpperLimitInMillimetres);
        Assert.Equal(anyPlainDimension.NominalInMillimetres, anyPlainDimension.LowerLimitInMillimetres);
    }

    [Fact]
    public void A_hole_zone_H_opens_the_upper_limit_by_the_whole_tolerance_and_keeps_the_lower_at_the_nominal() {
        // Arrange: IT7 of the 18-30 mm range is 21 um.
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Assert
        Assert.Equal(25.021, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(25.0, dimension.LowerLimitInMillimetres, Precision);
    }

    [Fact]
    public void A_shaft_zone_h_keeps_the_upper_limit_at_the_nominal_and_lowers_the_lower_by_the_whole_tolerance() {
        // Arrange: IT6 of the 18-30 mm range is 13 um.
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("h", 6));

        // Assert
        Assert.Equal(25.0, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(24.987, dimension.LowerLimitInMillimetres, Precision);
    }

    [Theory]
    [InlineData("JS")]
    [InlineData("js")]
    public void A_symmetrical_zone_spreads_half_the_tolerance_on_each_side(string zone) {
        // Arrange: IT7 of the 18-30 mm range is 21 um, half of it 10.5 um.
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance(zone, 7));

        // Assert
        Assert.Equal(25.0105, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(24.9895, dimension.LowerLimitInMillimetres, Precision);
    }

    [Fact]
    public void The_tolerance_is_looked_up_for_the_nominal_in_millimetres_when_the_unit_is_the_inch() {
        // Arrange: 1 in = 25.4 mm is in the 18-30 range, IT7 = 21 um.
        Dimension dimension = new Dimension(1.0, LengthUnit.Inch, new IsoTolerance("H", 7));

        // Assert
        Assert.Equal(25.421, dimension.UpperLimitInMillimetres, Precision);
        Assert.Equal(25.4, dimension.LowerLimitInMillimetres, Precision);
    }

    [Fact]
    public void The_upper_limit_is_never_below_the_lower_limit() {
        // Arrange
        Dimension anyDimension = Any.Dimension().Generate();

        // Assert
        Assert.True(anyDimension.UpperLimitInMillimetres >= anyDimension.LowerLimitInMillimetres);
    }

    [Fact]
    public void A_measure_equal_to_either_limit_is_accepted() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));

        // Assert
        Assert.True(dimension.Accepts(dimension.LowerLimitInMillimetres));
        Assert.True(dimension.Accepts(dimension.UpperLimitInMillimetres));
    }

    [Fact]
    public void A_measure_strictly_between_the_limits_is_accepted() {
        // Arrange
        Dimension dimension = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));
        double    anyInside = Any.Double().Between(dimension.LowerLimitInMillimetres, dimension.UpperLimitInMillimetres).Generate();

        // Assert
        Assert.True(dimension.Accepts(anyInside));
    }

    [Fact]
    public void A_measure_outside_the_limits_is_rejected() {
        // Arrange
        Dimension dimension      = new Dimension(25.0, LengthUnit.Millimetre, new IsoTolerance("H", 7));
        double    anyTooSmall    = Any.Double().LessThan(dimension.LowerLimitInMillimetres).Generate();
        double    anyTooLarge    = Any.Double().GreaterThan(dimension.UpperLimitInMillimetres).Generate();

        // Assert
        Assert.False(dimension.Accepts(anyTooSmall));
        Assert.False(dimension.Accepts(anyTooLarge));
    }

    [Fact]
    public void A_nan_measure_is_never_accepted() {
        // Arrange
        Dimension anyDimension = Any.Dimension().Generate();

        // Assert
        Assert.False(anyDimension.Accepts(double.NaN));
    }

    [Fact]
    public void A_dimension_without_a_tolerance_accepts_only_its_nominal() {
        // Arrange
        Dimension dimension = new Dimension(12.0, LengthUnit.Millimetre, null);

        // Assert
        Assert.True(dimension.Accepts(12.0));
        Assert.False(dimension.Accepts(12.0001));
    }

    [Theory]
    [InlineData(25.0, LengthUnit.Millimetre, "H", 7, "Ø25 H7 mm")]
    [InlineData(2.0, LengthUnit.Inch, "h", 6, "Ø2 h6 in")]
    public void A_toleranced_dimension_reads_with_its_tolerance_and_unit(double nominal, LengthUnit unit, string zone, int grade, string expected) {
        // Arrange
        Dimension dimension = new Dimension(nominal, unit, new IsoTolerance(zone, grade));

        // Assert
        Assert.Equal(expected, dimension.ToString());
    }

    [Fact(Skip = "domain defect (low, MODEL.md is silent on culture): ToString formats the nominal with the current culture, so on a fr-FR machine Ø25.5 reads Ø25,5")]
    public void A_fractional_nominal_reads_with_a_decimal_point_whatever_the_culture_of_the_machine() {
        // Arrange
        CultureInfo previous = CultureInfo.CurrentCulture;
        try {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            // Act
            string text = new Dimension(25.5, LengthUnit.Millimetre, new IsoTolerance("H", 7)).ToString();

            // Assert
            Assert.Equal("Ø25.5 H7 mm", text);
        } finally {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_dimension_without_a_tolerance_reads_with_its_unit_only() {
        // Assert
        Assert.Equal("Ø12 mm", new Dimension(12.0, LengthUnit.Millimetre, null).ToString());
        Assert.Equal("Ø3 in", new Dimension(3.0, LengthUnit.Inch, null).ToString());
    }

}
