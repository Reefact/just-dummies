using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Tests.Rows;

namespace KnittingPatterns.Tests.Regular.Tests;

public sealed class KnittingPatternTests {

    // A small hat crown: 8 stitches of 1x1 rib, a stockinette band, then every stitch decreased in pairs.
    private static readonly PatternSection Ribbing = new("Ribbing", [Row(1, Rep(4, K(1), P(1))), Row(2, Rep(4, K(1), P(1)))]);
    private static readonly PatternSection Body    = new("Body", [Row(3, K(8)), Row(4, P(8))]);
    private static readonly PatternSection Crown   = new("Crown", [Row(5, K2Tog(4)), Row(6, P(4))]);

    [Fact]
    public void Expanding_a_pattern_works_every_row_of_every_section_in_order() {
        // Arrange
        KnittingPattern anyHat = Arbitrary.KnittingPattern().WithCastOnCount(8).WithSections([Ribbing, Body, Crown]).Generate();

        // Act
        IReadOnlyList<ExpandedRow> expanded = anyHat.Expand();

        // Assert
        Assert.Equal(
            ["Ribbing:1:8>8", "Ribbing:2:8>8", "Body:3:8>8", "Body:4:8>8", "Crown:5:8>4", "Crown:6:4>4"],
            expanded.Select(r => $"{r.SectionName}:{r.RowNumber}:{r.StitchesBefore}>{r.StitchesAfter}"));
    }

    [Fact]
    public void A_compact_repeat_expands_into_one_operation_per_stitch() {
        // Arrange — "*k2, p2; rep from * 50 times" on 200 stitches
        PatternSection  rib       = new("Ribbing", [Row(1, Rep(50, K(2), P(2)))]);
        KnittingPattern anyBlanket = Arbitrary.KnittingPattern().WithCastOnCount(200).WithSections([rib]).Generate();

        // Act
        ExpandedRow row1 = anyBlanket.Expand().Single();

        // Assert
        Assert.Equal(200, row1.Operations.Count);
    }

    [Fact]
    public void Expanding_stops_at_the_first_row_that_does_not_fit_the_stitches_left_by_the_previous_one() {
        // Arrange
        PatternSection  badCrown   = new("Crown", [Row(5, K2Tog(4)), Row(6, P(8))]);
        KnittingPattern anyBadHat  = Arbitrary.KnittingPattern().WithCastOnCount(8).WithSections([Ribbing, Body, badCrown]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyBadHat.Expand());

        // Assert
        StitchCountMismatchException mismatch = Assert.IsType<StitchCountMismatchException>(refusal);
        Assert.Equal((6, 4, 8), (mismatch.RowNumber, mismatch.LiveStitches, mismatch.RequiredStitches));
    }

    [Fact]
    public void A_cast_on_that_does_not_match_the_first_row_is_reported_on_row_one() {
        // Arrange
        KnittingPattern anyHatCastOnTen = Arbitrary.KnittingPattern().WithCastOnCount(10).WithSections([Ribbing, Body, Crown]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyHatCastOnTen.Expand());

        // Assert
        StitchCountMismatchException mismatch = Assert.IsType<StitchCountMismatchException>(refusal);
        Assert.Equal((1, 10, 8), (mismatch.RowNumber, mismatch.LiveStitches, mismatch.RequiredStitches));
    }

    [Fact]
    public void The_finished_width_is_measured_on_the_last_row_at_the_stitch_gauge() {
        // Arrange
        Gauge           anyGaugeAt20Stitches = Arbitrary.Gauge().WithStitchesPer10Cm(20m).Generate();
        PatternSection  flare                = new("Flare", [Row(1, K(20), Kfb(20)), Row(2, P(60))]);
        KnittingPattern anyFlaredHem         = Arbitrary.KnittingPattern().WithGauge(anyGaugeAt20Stitches).WithCastOnCount(40).WithSections([flare]).Generate();

        // Act
        decimal width = anyFlaredHem.FinishedWidthCm();

        // Assert
        Assert.Equal(30m, width);
    }

    [Fact]
    public void A_pattern_whose_first_section_does_not_start_at_row_one_is_refused() {
        // Arrange
        AnyKnittingPattern anyPattern = Arbitrary.KnittingPattern().WithSections([Body, Crown]);

        // Act
        Exception? refusal = Record.Exception(() => anyPattern.Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("starts at row 3, not row 1", argumentException.Message);
    }

    [Fact]
    public void A_gap_between_two_sections_is_refused() {
        // Arrange
        AnyKnittingPattern anyPattern = Arbitrary.KnittingPattern().WithSections([Ribbing, Crown]);

        // Act
        Exception? refusal = Record.Exception(() => anyPattern.Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("Section 'Crown' starts at row 5; row 3 was expected", argumentException.Message);
    }

    [Fact]
    public void Two_sections_named_alike_regardless_of_case_are_refused() {
        // Arrange
        PatternSection anyFirst  = Arbitrary.PatternSection().WithName("Body").StartingAtRow(1).Generate();
        PatternSection anySecond = Arbitrary.PatternSection().WithName("BODY").StartingAtRow(anyFirst.LastRow + 1).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Arbitrary.KnittingPattern().WithSections([anyFirst, anySecond]).Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("Two sections are named 'BODY'", argumentException.Message);
    }

}
