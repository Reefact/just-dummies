using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class GaugeTests {

    [Fact]
    public void A_gauge_exposes_the_swatch_it_was_measured_from() {
        Gauge gauge = new Gauge(22m, 30m);

        Assert.Equal(22m, gauge.StitchesPer10Cm);
        Assert.Equal(30m, gauge.RowsPer10Cm);
    }

    [Theory]
    [InlineData(4, 4)]
    [InlineData(8, 4)]
    [InlineData(60, 80)]
    [InlineData(40, 80)]
    [InlineData(60, 30)]
    public void A_gauge_accepts_the_edges_of_its_ranges(int stitches, int rows) {
        Gauge gauge = new Gauge(stitches, rows);

        Assert.Equal(stitches, gauge.StitchesPer10Cm);
    }

    [Theory]
    [InlineData("3.99")]
    [InlineData("60.01")]
    [InlineData("0")]
    public void A_gauge_refuses_a_stitch_count_outside_4_to_60(string stitches) {
        // The rows are fine and the stitch count is checked first: they play no part.
        decimal anyRows = Any.Decimal().Between(4m, 80m).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Gauge(decimal.Parse(stitches, System.Globalization.CultureInfo.InvariantCulture), anyRows));

        Assert.Equal("stitchesPer10Cm", error.ParamName);
    }

    [Theory]
    [InlineData("3.99")]
    [InlineData("80.01")]
    public void A_gauge_refuses_a_row_count_outside_4_to_80(string rows) {
        decimal anyStitches = Any.Decimal().Between(4m, 60m).Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Gauge(anyStitches, decimal.Parse(rows, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal("rowsPer10Cm", error.ParamName);
    }

    [Theory]
    [InlineData("10")]
    [InlineData("40")]
    public void A_gauge_accepts_rows_from_half_to_twice_the_stitches(string rows) {
        Gauge gauge = new Gauge(20m, decimal.Parse(rows, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(20m, gauge.StitchesPer10Cm);
    }

    [Theory]
    [InlineData("9.99")]
    [InlineData("40.01")]
    public void A_gauge_refuses_a_fabric_that_is_not_plausible(string rows) {
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Gauge(20m, decimal.Parse(rows, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal("rowsPer10Cm", error.ParamName);
    }

    [Theory]
    [InlineData("10", 20)]
    [InlineData("2.25", 5)]   // 4.5 stitches rounds away from zero
    [InlineData("2.2", 4)]
    [InlineData("0.01", 1)]   // never fewer than one stitch
    public void The_stitches_to_cast_on_for_a_width_are_rounded_to_the_nearest_whole_stitch(string widthCm, int expected) {
        Gauge gauge = new Gauge(20m, 28m);

        Assert.Equal(expected, gauge.StitchesForWidth(decimal.Parse(widthCm, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("10", 28)]
    [InlineData("1.25", 4)]   // 3.5 rows rounds away from zero
    [InlineData("1.2", 3)]
    [InlineData("0.01", 1)]
    public void The_rows_to_work_for_a_length_are_rounded_to_the_nearest_whole_row(string lengthCm, int expected) {
        Gauge gauge = new Gauge(20m, 28m);

        Assert.Equal(expected, gauge.RowsForLength(decimal.Parse(lengthCm, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void The_width_of_some_stitches_follows_the_gauge() {
        Gauge gauge = new Gauge(25m, 30m);

        Assert.Equal(20m, gauge.WidthOf(50));
        Assert.Equal(0.4m, gauge.WidthOf(1));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void A_width_or_a_length_that_is_not_positive_is_refused(string measure) {
        Gauge   anyGauge = Any.Gauge().Generate();
        decimal value    = decimal.Parse(measure, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.StitchesForWidth(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.RowsForLength(value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void The_width_of_fewer_than_one_stitch_is_refused(int stitches) {
        Gauge anyGauge = Any.Gauge().Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => anyGauge.WidthOf(stitches));
    }

}
