using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

/// <summary>
///     The generators of this project are code we own, with rules written by hand where the scaffolder could not
///     read them. These tests draw many values to make sure none of them can hand a constructor something it refuses:
///     if one does, every other test drawing from it would fail for the wrong reason.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Calibrated_dates_are_always_ones_the_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => Any.ListOf(Any.CalibratedDate()).WithCount(Draws).Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void Calibration_curves_are_always_ones_the_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => Any.ListOf(Any.CalibrationCurve()).WithCount(Draws).Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void Samples_are_always_ones_the_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => Any.ListOf(Any.Sample()).WithCount(Draws).Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void Relations_are_always_ones_the_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => Any.ListOf(Any.PrecedenceRelation()).WithCount(Draws).Generate());

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void Intervals_layers_points_ages_and_sites_are_always_ones_the_constructor_accepts() {
        // Act
        Exception? refusal = Record.Exception(() => {
            Any.ListOf(Any.CalendarInterval()).WithCount(Draws).Generate();
            Any.ListOf(Any.StratigraphicLayer()).WithCount(Draws).Generate();
            Any.ListOf(Any.CalibrationPoint()).WithCount(Draws).Generate();
            Any.ListOf(Any.RadiocarbonAge()).WithCount(Draws).Generate();
            Any.ListOf(Any.SiteChronology()).WithCount(Draws).Generate();
            Any.ListOf(Any.OccupationEstimate()).WithCount(Draws).Generate();
        });

        // Assert
        Assert.Null(refusal);
    }

}
