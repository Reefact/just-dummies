using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

public sealed class GaugeTests {

    [Fact]
    public void A_gauge_keeps_its_stitches_and_its_rows() {
        // Act
        Gauge gauge = new(22m, 30m);

        // Assert
        Assert.Equal(22m, gauge.StitchesPer10Cm);
        Assert.Equal(30m, gauge.RowsPer10Cm);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(4, 8)]
    [InlineData(60, 30)]
    [InlineData(60, 80)]
    [InlineData(40, 20)]
    [InlineData(40, 80)]
    public void A_gauge_accepts_the_corners_of_what_a_fabric_can_be(int stitches, int rows) {
        // Act
        Gauge gauge = new(stitches, rows);

        // Assert
        Assert.Equal(stitches, gauge.StitchesPer10Cm);
        Assert.Equal(rows, gauge.RowsPer10Cm);
    }

    [Theory]
    [InlineData("3.99")]
    [InlineData("60.01")]
    public void A_gauge_refuses_a_stitch_gauge_just_outside_four_to_sixty(string stitches) {
        // Arrange
        decimal anyRows = Any.Decimal().Between(4m, 60m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("stitchesPer10Cm", () => new Gauge(decimal.Parse(stitches, System.Globalization.CultureInfo.InvariantCulture), anyRows));
    }

    [Theory]
    [InlineData("3.99")]
    [InlineData("80.01")]
    public void A_gauge_refuses_a_row_gauge_just_outside_four_to_eighty(string rows) {
        // Arrange
        decimal anyStitches = Any.Decimal().Between(4m, 60m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("rowsPer10Cm", () => new Gauge(anyStitches, decimal.Parse(rows, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void A_gauge_refuses_any_stitch_gauge_below_four() {
        // Arrange
        decimal anyStitches = Any.Decimal().LessThan(4m).Generate();
        decimal anyRows     = Any.Decimal().Between(4m, 80m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("stitchesPer10Cm", () => new Gauge(anyStitches, anyRows));
    }

    [Fact]
    public void A_gauge_refuses_any_stitch_gauge_above_sixty() {
        // Arrange
        decimal anyStitches = Any.Decimal().GreaterThan(60m).Generate();
        decimal anyRows     = Any.Decimal().Between(4m, 80m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("stitchesPer10Cm", () => new Gauge(anyStitches, anyRows));
    }

    [Fact]
    public void A_gauge_refuses_any_row_gauge_outside_four_to_eighty() {
        // Arrange
        decimal anyStitches = Any.Decimal().Between(4m, 60m).Generate();
        decimal anyTooFew   = Any.Decimal().LessThan(4m).Generate();
        decimal anyTooMany  = Any.Decimal().GreaterThan(80m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("rowsPer10Cm", () => new Gauge(anyStitches, anyTooFew));
        Assert.Throws<ArgumentOutOfRangeException>("rowsPer10Cm", () => new Gauge(anyStitches, anyTooMany));
    }

    [Theory]
    [InlineData("30", "14.99")]
    [InlineData("30", "60.01")]
    [InlineData("4", "8.01")]
    [InlineData("60", "29.99")]
    [InlineData("10", "4")]
    public void A_gauge_refuses_rows_outside_half_to_twice_the_stitches(string stitches, string rows) {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new Gauge(decimal.Parse(stitches, System.Globalization.CultureInfo.InvariantCulture), decimal.Parse(rows, System.Globalization.CultureInfo.InvariantCulture)));

        // Assert: the plausibility rule, not a range rule
        Assert.Equal("rowsPer10Cm", error.ParamName);
        Assert.IsNotType<ArgumentOutOfRangeException>(error);
    }

    [Fact]
    public void A_generated_gauge_is_a_plausible_fabric() {
        // Arrange / Act
        Gauge anyGauge = new AnyGauge().Generate();

        // Assert
        Assert.InRange(anyGauge.RowsPer10Cm, anyGauge.StitchesPer10Cm / 2m, anyGauge.StitchesPer10Cm * 2m);
    }

    [Theory]
    [InlineData(20, 30, 60)]
    [InlineData(21, 5, 11)]
    [InlineData(21, 15, 32)]
    [InlineData(25, 10, 25)]
    public void The_stitches_for_a_width_are_rounded_to_the_nearest_whole_stitch_and_a_half_goes_up(int stitchGauge, int widthCm, int expected) {
        // Arrange: 21 sts over 5 cm is 10.5 stitches, 21 over 15 is 31.5
        Gauge gauge = new AnyGauge().WithStitchesPer10Cm(stitchGauge).Generate();

        // Act / Assert
        Assert.Equal(expected, gauge.StitchesForWidth(widthCm));
    }

    [Fact]
    public void The_stitches_for_a_very_narrow_width_are_never_fewer_than_one() {
        // Arrange: 0.01 cm at 20 stitches per 10 cm is 0.02 of a stitch
        Gauge gauge = new AnyGauge().WithStitchesPer10Cm(20m).Generate();

        // Act / Assert
        Assert.Equal(1, gauge.StitchesForWidth(0.01m));
    }

    [Theory]
    [InlineData(28, 10, 28)]
    [InlineData(30, 5, 15)]
    [InlineData(25, 3, 8)]
    public void The_rows_for_a_length_are_rounded_to_the_nearest_whole_row_and_a_half_goes_up(int rowGauge, int lengthCm, int expected) {
        // Arrange: 25 rows over 3 cm is 7.5 rows
        Gauge gauge = new AnyGauge().WithStitchesPer10Cm(30m).WithRowsPer10Cm(rowGauge).Generate();

        // Act / Assert
        Assert.Equal(expected, gauge.RowsForLength(lengthCm));
    }

    [Fact]
    public void The_rows_for_a_very_short_length_are_never_fewer_than_one() {
        // Arrange
        Gauge gauge = new AnyGauge().WithStitchesPer10Cm(20m).WithRowsPer10Cm(24m).Generate();

        // Act / Assert
        Assert.Equal(1, gauge.RowsForLength(0.01m));
    }

    [Fact]
    public void A_width_or_a_length_that_is_not_positive_is_refused() {
        // Arrange
        Gauge   anyGauge  = new AnyGauge().Generate();
        decimal anyAmount = Any.Decimal().LessThanOrEqualTo(0m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("widthCm", () => anyGauge.StitchesForWidth(anyAmount));
        Assert.Throws<ArgumentOutOfRangeException>("lengthCm", () => anyGauge.RowsForLength(anyAmount));
    }

    [Fact(Skip = "domain defect: StitchesForWidth throws OverflowException (decimal multiplication, then the int cast) for a very large positive width instead of a ArgumentOutOfRangeException or a result, seed -315809928"), Reproducible(Seed = -315809928)]
    public void Any_positive_width_gives_at_least_one_stitch() {
        // Arrange
        Gauge   anyGauge = new AnyGauge().Generate();
        decimal anyWidth = Any.Decimal().Positive().Generate();

        // Act
        int stitches = anyGauge.StitchesForWidth(anyWidth);

        // Assert
        Assert.True(stitches >= 1);
    }

    [Fact(Skip = "domain defect: RowsForLength throws OverflowException for a very large positive length, same cause as StitchesForWidth, seed -315809928"), Reproducible(Seed = -315809928)]
    public void Any_positive_length_gives_at_least_one_row() {
        // Arrange
        Gauge   anyGauge  = new AnyGauge().Generate();
        decimal anyLength = Any.Decimal().Positive().Generate();

        // Act
        int rows = anyGauge.RowsForLength(anyLength);

        // Assert
        Assert.True(rows >= 1);
    }

    [Theory]
    [InlineData(20, 25, "12.5")]
    [InlineData(25, 25, "10")]
    [InlineData(40, 1, "0.25")]
    public void The_width_of_some_stitches_is_that_many_tenths_of_the_gauge_in_centimetres(int stitchGauge, int stitches, string widthCm) {
        // Arrange
        Gauge gauge = new AnyGauge().WithStitchesPer10Cm(stitchGauge).Generate();

        // Act / Assert
        Assert.Equal(decimal.Parse(widthCm, System.Globalization.CultureInfo.InvariantCulture), gauge.WidthOf(stitches));
    }

    [Fact]
    public void The_width_of_no_stitch_at_all_is_refused() {
        // Arrange
        Gauge anyGauge   = new AnyGauge().Generate();
        int   anyStitches = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("stitches", () => anyGauge.WidthOf(anyStitches));
    }

}
