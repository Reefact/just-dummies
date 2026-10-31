using JustDummies;
using JustDummies.Xunit;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

[Reproducible]
public sealed class KnittingPatternTests {

    private static RowInstruction Row(int number, FabricSide side, params IRowElement[] elements) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side).WithElements(elements).Generate();
    }

    private static PatternSection Section(string name, params RowInstruction[] rows) {
        return Any.PatternSection().WithName(name).WithRows(rows).Generate();
    }

    [Fact]
    public void A_pattern_expands_every_row_of_every_section_in_order() {
        // Arrange
        // Cast on 20. Ribbing, rows 1-2: *k2, p2; rep 5 times. Lace, row 3 (RS): k2, *yo, k2tog; rep 8 times, k2. Row 4 (WS): p20.
        PatternSection ribbing = Section("Ribbing",
                                         Row(1, FabricSide.RightSide, new StitchRepeat([new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 2)], 5)),
                                         Row(2, FabricSide.WrongSide, new StitchRepeat([new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 2)], 5)));
        PatternSection lace = Section("Lace",
                                      Row(3, FabricSide.RightSide,
                                          new StitchRun(StitchKind.Knit, 2),
                                          new StitchRepeat([new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1)], 8),
                                          new StitchRun(StitchKind.Knit, 2)),
                                      Row(4, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 20)));
        KnittingPattern pattern = Any.KnittingPattern().WithCastOnCount(20).WithSections([ribbing, lace]).Generate();

        // Act
        IReadOnlyList<ExpandedRow> rows = pattern.Expand();

        // Assert
        Assert.Equal(new[] { "Ribbing", "Ribbing", "Lace", "Lace" }, rows.Select(r => r.SectionName));
        Assert.Equal(new[] { 1, 2, 3, 4 }, rows.Select(r => r.RowNumber));
        Assert.All(rows, r => Assert.Equal(20, r.StitchesAfter));
        Assert.Equal(20, rows[2].Operations.Count);
    }

    [Fact]
    public void Crown_decreases_bring_the_stitch_count_down_row_after_row() {
        // Arrange
        // Cast on 24. Row 1 (RS): *k2tog; rep 12 times (12 sts). Row 2 (WS): p12. Row 3 (RS): *cdd; rep 4 times (4 sts).
        PatternSection crown = Section("Crown decreases",
                                       Row(1, FabricSide.RightSide, new StitchRepeat([new StitchRun(StitchKind.KnitTwoTogether, 1)], 12)),
                                       Row(2, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 12)),
                                       Row(3, FabricSide.RightSide, new StitchRepeat([new StitchRun(StitchKind.CentralDoubleDecrease, 1)], 4)));
        KnittingPattern pattern = Any.KnittingPattern().WithCastOnCount(24).WithSections([crown]).Generate();

        // Act
        IReadOnlyList<ExpandedRow> rows = pattern.Expand();

        // Assert
        Assert.Equal(new[] { 12, 12, 4 }, rows.Select(r => r.StitchesAfter));
        Assert.Equal(new[] { 12, 0, 4 }, rows.Select(r => r.Decreases));
    }

    [Fact]
    public void Expanding_reports_the_first_row_that_does_not_fit_the_needle() {
        // Arrange
        // Row 1 increases 10 sts to 20, row 2 is written for the 10 sts of the cast-on.
        PatternSection body = Section("Body",
                                      Row(1, FabricSide.RightSide, new StitchRun(StitchKind.KnitFrontAndBack, 10)),
                                      Row(2, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 10)));
        KnittingPattern pattern = Any.KnittingPattern().WithCastOnCount(10).WithSections([body]).Generate();

        // Act
        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());

        // Assert
        Assert.Equal(2, error.RowNumber);
        Assert.Equal(20, error.LiveStitches);
        Assert.Equal(10, error.RequiredStitches);
    }

    [Fact]
    public void The_finished_width_is_measured_on_the_last_row() {
        // Arrange
        // Cast on 40, row 1 (RS): *k2tog; rep 20 times leaves 20 sts, at 20 sts per 10 cm.
        Gauge          gauge = Any.Gauge().WithStitchesPer10Cm(20m).Generate();
        PatternSection body  = Section("Body", Row(1, FabricSide.RightSide, new StitchRepeat([new StitchRun(StitchKind.KnitTwoTogether, 1)], 20)));
        KnittingPattern pattern = Any.KnittingPattern().WithGauge(gauge).WithCastOnCount(40).WithSections([body]).Generate();

        // Act
        decimal width = pattern.FinishedWidthCm();

        // Assert
        Assert.Equal(10m, width);
    }

    [Fact]
    public void A_pattern_whose_first_section_does_not_start_at_row_1_is_refused() {
        // Arrange
        string anyTitle   = Any.String().NotBlank().Generate();
        Gauge  anyGauge   = Any.Gauge().Generate();
        int    anyCastOn  = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();
        PatternSection anySectionFromRow3 = Any.PatternSection()
                                               .WithRows([Any.RowInstruction().WithRowNumber(3).Generate()])
                                               .Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern(anyTitle, anyGauge, anyCastOn, [anySectionFromRow3]));

        // Assert
        Assert.Equal("sections", error.ParamName);
        Assert.Contains("row 3", error.Message);
    }

    [Fact]
    public void Two_sections_named_alike_whatever_the_case_are_refused() {
        // Arrange
        PatternSection first  = Section("Body", Any.RowInstruction().WithRowNumber(1).WithSide(FabricSide.RightSide).Generate());
        PatternSection second = Section("BODY", Any.RowInstruction().WithRowNumber(2).WithSide(FabricSide.WrongSide).Generate());

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.KnittingPattern().WithSections([first, second]).Generate());

        // Assert
        Assert.Equal("sections", error.ParamName);
        Assert.Contains("BODY", error.Message);
    }

    [Fact]
    public void A_gap_between_two_sections_is_refused() {
        // Arrange
        PatternSection ribbing = Section("Ribbing", Any.RowInstruction().WithRowNumber(1).WithSide(FabricSide.RightSide).Generate());
        PatternSection body    = Section("Body", Any.RowInstruction().WithRowNumber(3).WithSide(FabricSide.RightSide).Generate());

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.KnittingPattern().WithSections([ribbing, body]).Generate());

        // Assert
        Assert.Contains("row 2 was expected", error.Message);
    }

    [Fact]
    public void A_pattern_title_is_kept_without_its_surrounding_spaces() {
        // Arrange
        // Act
        KnittingPattern pattern = Any.KnittingPattern().WithTitle("  Seafoam scarf ").Generate();

        // Assert
        Assert.Equal("Seafoam scarf", pattern.Title);
    }

}
