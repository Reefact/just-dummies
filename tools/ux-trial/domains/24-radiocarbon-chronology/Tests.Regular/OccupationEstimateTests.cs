using JustDummies;
using RadiocarbonChronology.Domain;
using RadiocarbonChronology.Tests.Regular.Generators;
using Xunit;

namespace RadiocarbonChronology.Tests.Regular;

public sealed class OccupationEstimateTests {

    [Fact]
    public void An_estimate_keeps_the_layer_code_and_the_date() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US7", anyDate);

        // Assert
        Assert.Equal("US7", estimate.LayerCode);
        Assert.Same(anyDate, estimate.Date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void An_estimate_without_a_layer_code_is_refused(string? layerCode) {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new OccupationEstimate(layerCode!, anyDate));
    }

    [Fact]
    public void An_estimate_without_a_date_is_refused() {
        // Arrange
        string anyCode = Any.StringMatching("[A-Z]{1,3}[0-9]{1,4}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new OccupationEstimate(anyCode, null!));
    }

    [Fact]
    public void The_plausible_intervals_are_the_highest_density_region_at_95_4_percent() {
        // Arrange
        CalibratedDate anyDate = Any.CalibratedDate().Generate();

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", anyDate);

        // Assert
        Assert.Equal(0.954, OccupationEstimate.CredibleLevel);
        Assert.Equal(anyDate.HighestDensityRegion(0.954), estimate.PlausibleIntervals);
    }

    [Fact]
    public void A_date_with_a_single_peak_is_not_multimodal() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.1, 0.3, 0.4, 0.2 });

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.False(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(100, 103), estimate.Envelope);
    }

    [Fact]
    public void A_date_with_two_separate_peaks_is_multimodal_and_the_envelope_spans_both() {
        // Arrange
        CalibratedDate date = new CalibratedDate(100, new[] { 0.48, 0.01, 0d, 0d, 0d, 0.01, 0.5 });

        // Act
        OccupationEstimate estimate = new OccupationEstimate("US1", date);

        // Assert
        Assert.True(estimate.IsMultimodal);
        Assert.Equal(new CalendarInterval(100, 106), estimate.Envelope);
    }

    [Fact]
    public void The_envelope_of_an_estimate_covers_all_its_plausible_intervals() {
        // Arrange
        OccupationEstimate anyEstimate = Any.OccupationEstimate().Generate();

        // Act
        CalendarInterval envelope = anyEstimate.Envelope;

        // Assert
        Assert.All(anyEstimate.PlausibleIntervals, interval => {
            Assert.True(envelope.Contains(interval.StartYear));
            Assert.True(envelope.Contains(interval.EndYear));
        });
    }

}
