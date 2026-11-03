namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class GaugeTests {

    [Theory]
    [InlineData(4, 4)]
    [InlineData(4, 8)]
    [InlineData(60, 30)]
    [InlineData(60, 80)]
    [InlineData(40, 80)]
    [InlineData(22.5, 30.5)]
    public void A_gauge_accepts_the_edges_of_the_stitch_range_the_row_range_and_the_plausible_ratio(double stitches, double rows) {
        // Act
        Gauge gauge = new((decimal)stitches, (decimal)rows);

        // Assert
        Assert.Equal((decimal)stitches, gauge.StitchesPer10Cm);
        Assert.Equal((decimal)rows, gauge.RowsPer10Cm);
    }

    [Theory]
    [InlineData(3.99, 4)]
    [InlineData(0, 4)]
    [InlineData(-10, 4)]
    [InlineData(60.01, 60)]
    public void A_stitch_gauge_lies_between_four_and_sixty_stitches_per_ten_centimetres(double stitches, double rows) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Gauge((decimal)stitches, (decimal)rows));

        Assert.Equal("stitchesPer10Cm", refusal.ParamName);
    }

    [Theory]
    [InlineData(4, 3.99)]
    [InlineData(10, 0)]
    [InlineData(10, -5)]
    [InlineData(60, 80.01)]
    public void A_row_gauge_lies_between_four_and_eighty_rows_per_ten_centimetres(double stitches, double rows) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Gauge((decimal)stitches, (decimal)rows));

        Assert.Equal("rowsPer10Cm", refusal.ParamName);
    }

    [Theory]
    [InlineData(20, 9.99)]
    [InlineData(20, 40.01)]
    [InlineData(10, 4.9)]
    public void A_row_gauge_stays_within_half_to_twice_the_stitch_gauge(double stitches, double rows) {
        // The row gauge is individually in range in each case: only its ratio to the stitch gauge is wrong.
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Gauge((decimal)stitches, (decimal)rows));

        Assert.Equal("rowsPer10Cm", refusal.ParamName);
    }

    [Theory]
    [InlineData(10, 22)]
    [InlineData(12.4, 27)]
    [InlineData(13.5, 30)]
    [InlineData(100, 220)]
    public void A_width_is_cast_on_at_the_stitch_gauge_rounded_to_the_nearest_stitch(double widthCm, int expectedStitches) {
        // Arrange
        Gauge gauge = new(22m, 30m);

        // Act + Assert
        Assert.Equal(expectedStitches, gauge.StitchesForWidth((decimal)widthCm));
    }

    [Fact]
    public void A_width_that_falls_exactly_between_two_stitch_counts_rounds_up() {
        // Arrange: 13.25 cm at 20 stitches per 10 cm is 26.5 stitches
        Gauge gauge = new(20m, 25m);

        // Act + Assert
        Assert.Equal(27, gauge.StitchesForWidth(13.25m));
    }

    [Fact]
    public void A_tiny_width_still_needs_one_stitch() {
        // Arrange: 0.01 cm is a fiftieth of a stitch
        Gauge gauge = new(20m, 25m);

        // Act + Assert
        Assert.Equal(1, gauge.StitchesForWidth(0.01m));
    }

    [Theory]
    [InlineData(10, 30)]
    [InlineData(25, 75)]
    [InlineData(10.6, 32)]
    public void A_length_is_worked_at_the_row_gauge_rounded_to_the_nearest_row(double lengthCm, int expectedRows) {
        // Arrange
        Gauge gauge = new(22m, 30m);

        // Act + Assert
        Assert.Equal(expectedRows, gauge.RowsForLength((decimal)lengthCm));
    }

    [Fact]
    public void A_length_that_falls_exactly_between_two_row_counts_rounds_up() {
        // Arrange: 10.6 cm at 25 rows per 10 cm is 26.5 rows
        Gauge gauge = new(20m, 25m);

        // Act + Assert
        Assert.Equal(27, gauge.RowsForLength(10.6m));
    }

    [Fact]
    public void A_tiny_length_still_needs_one_row() {
        Gauge gauge = new(20m, 25m);

        Assert.Equal(1, gauge.RowsForLength(0.01m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void A_width_must_be_positive(double widthCm) {
        // Arrange
        Gauge anyGauge = Any.Gauge().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.StitchesForWidth((decimal)widthCm));

        // Assert
        Assert.Equal("widthCm", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void A_length_must_be_positive(double lengthCm) {
        Gauge anyGauge = Any.Gauge().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.RowsForLength((decimal)lengthCm));

        Assert.Equal("lengthCm", refusal.ParamName);
    }

    [Theory]
    [InlineData(22, 10)]
    [InlineData(11, 5)]
    [InlineData(1, 0.4545454545454545454545454545)]
    public void The_width_of_some_stitches_is_ten_centimetres_times_the_stitches_over_the_stitch_gauge(int stitches, double expectedWidthCm) {
        // Arrange
        Gauge gauge = new(22m, 30m);

        // Act
        decimal width = gauge.WidthOf(stitches);

        // Assert
        Assert.Equal((decimal)expectedWidthCm, width, precision: 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void The_width_of_stitches_needs_at_least_one_stitch(int stitches) {
        Gauge anyGauge = Any.Gauge().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.WidthOf(stitches));

        Assert.Equal("stitches", refusal.ParamName);
    }

    [Fact]
    public void Casting_on_for_the_width_of_seventeen_stitches_gives_back_seventeen_stitches() {
        // Arrange: 17 stitches at 22 per 10 cm is 7.727... cm, a width that is not exact in decimals
        Gauge   gauge = new(22m, 30m);
        decimal width = gauge.WidthOf(17);

        // Act + Assert
        Assert.Equal(17, gauge.StitchesForWidth(width));
    }

}
