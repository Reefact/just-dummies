using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Rows;

namespace KnittingPatterns.Tests.Regular;

public sealed class KnittingPatternTests {

    /// <summary>
    ///     Cast on 12. Ribbing: row 1 (RS) *k2, p2; rep from * 3 times (12 sts), row 2 (WS) the reverse (12 sts).
    ///     Body: row 3 (RS) k5, kfb, k6 (13 sts), row 4 (WS) p13 (13 sts), row 5 (RS) k2tog 3 times, k7, yo (11 sts),
    ///     row 6 (WS) p8, cdd (9 sts).
    /// </summary>
    private static PatternSection[] ShapedHat() {
        PatternSection ribbing = Section(
            "Ribbing",
            Rs(1, 12, Rep(3, K(2), P(2))),
            Ws(2, 12, Rep(3, P(2), K(2))));

        PatternSection body = Section(
            "Body",
            Rs(3, 13, K(5), Of(StitchKind.KnitFrontAndBack), K(6)),
            Ws(4, 13, P(13)),
            Rs(5, 11, Of(StitchKind.KnitTwoTogether, 3), K(7), Of(StitchKind.YarnOver)),
            Ws(6, 9, P(8), Of(StitchKind.CentralDoubleDecrease)));

        return new[] { ribbing, body };
    }

    private static KnittingPattern PatternOf(int castOn, params PatternSection[] sections) {
        return new AnyKnittingPattern().WithCastOnCount(castOn).WithSections(sections).Generate();
    }

    [Fact]
    public void A_pattern_keeps_its_gauge_its_cast_on_and_its_sections_and_counts_its_rows() {
        // Arrange
        Gauge          anyGauge = new AnyGauge().Generate();
        PatternSection[] sections = ShapedHat();

        // Act
        KnittingPattern pattern = new("Beanie", anyGauge, 12, sections);

        // Assert
        Assert.Same(anyGauge, pattern.Gauge);
        Assert.Equal(12, pattern.CastOnCount);
        Assert.Equal(sections, pattern.Sections);
        Assert.Equal(6, pattern.RowCount);
    }

    [Fact]
    public void A_pattern_title_is_trimmed() {
        // Arrange
        Gauge anyGauge = new AnyGauge().Generate();

        // Act
        KnittingPattern pattern = new("  Beanie\t", anyGauge, 12, ShapedHat());

        // Assert
        Assert.Equal("Beanie", pattern.Title);
    }

    [Fact]
    public void A_pattern_does_not_follow_a_section_list_changed_after_it_was_written() {
        // Arrange
        List<PatternSection> sections = new(ShapedHat());
        KnittingPattern      pattern  = new AnyKnittingPattern().WithCastOnCount(12).WithSections(sections).Generate();

        // Act
        sections.RemoveAt(1);

        // Assert
        Assert.Equal(2, pattern.Sections.Count);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(KnittingPattern.MaxCastOn)]
    public void A_pattern_accepts_the_ends_of_the_cast_on_range(int castOn) {
        // Arrange
        AnyKnittingPattern generator = new AnyKnittingPattern().WithCastOnCount(castOn);

        // Act
        KnittingPattern pattern = generator.Generate();

        // Assert
        Assert.Equal(castOn, pattern.CastOnCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(KnittingPattern.MaxCastOn + 1)]
    public void A_pattern_refuses_a_cast_on_just_outside_its_range(int castOn) {
        // Arrange
        Gauge          anyGauge   = new AnyGauge().Generate();
        PatternSection anySection = new AnyPatternSection().WithFirstRow(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("castOnCount", () => new KnittingPattern("Beanie", anyGauge, castOn, new[] { anySection }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_pattern_refuses_a_blank_title(string blank) {
        // Arrange
        Gauge            anyGauge    = new AnyGauge().Generate();
        PatternSection[] anySections = new AnyContiguousSections().Generate().ToArray();
        int              anyCastOn   = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("title", () => new KnittingPattern(blank, anyGauge, anyCastOn, anySections));
    }

    [Fact]
    public void A_pattern_refuses_a_missing_gauge() {
        // Arrange
        PatternSection[] anySections = new AnyContiguousSections().Generate().ToArray();
        int              anyCastOn   = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("gauge", () => new KnittingPattern("Beanie", null!, anyCastOn, anySections));
    }

    [Fact]
    public void A_pattern_refuses_an_empty_list_of_sections() {
        // Arrange
        Gauge anyGauge  = new AnyGauge().Generate();
        int   anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("sections", () => new KnittingPattern("Beanie", anyGauge, anyCastOn, Array.Empty<PatternSection>()));
    }

    [Fact]
    public void A_pattern_refuses_a_missing_list_of_sections() {
        // Arrange
        Gauge anyGauge  = new AnyGauge().Generate();
        int   anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("sections", () => new KnittingPattern("Beanie", anyGauge, anyCastOn, null!));
    }

    [Fact]
    public void A_pattern_refuses_a_missing_section() {
        // Arrange
        Gauge          anyGauge   = new AnyGauge().Generate();
        PatternSection anySection = new AnyPatternSection().WithFirstRow(1).Generate();
        int            anyCastOn  = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("sections", () => new KnittingPattern("Beanie", anyGauge, anyCastOn, new[] { anySection, null! }));
    }

    [Fact]
    public void A_pattern_refuses_a_first_section_that_does_not_start_at_row_one() {
        // Arrange
        Gauge          anyGauge   = new AnyGauge().Generate();
        PatternSection notFirst   = new AnyPatternSection().WithFirstRow(3).Generate();
        int            anyCastOn  = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern("Beanie", anyGauge, anyCastOn, new[] { notFirst }));

        // Assert
        Assert.Contains("starts at row 3, not row 1", error.Message);
    }

    [Fact]
    public void A_pattern_refuses_a_gap_between_two_sections() {
        // Arrange: Ribbing ends at row 2, Body starts at row 4
        Gauge          anyGauge = new AnyGauge().Generate();
        PatternSection ribbing  = new AnyPatternSection().WithName("Ribbing").WithFirstRow(1).WithRowCount(2).Generate();
        PatternSection body     = new AnyPatternSection().WithName("Body").WithFirstRow(4).Generate();
        int            anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern("Beanie", anyGauge, anyCastOn, new[] { ribbing, body }));

        // Assert
        Assert.Contains("row 3 was expected", error.Message);
    }

    [Fact]
    public void A_pattern_refuses_two_sections_that_share_a_row() {
        // Arrange: Ribbing ends at row 2, Body starts again at row 2
        Gauge          anyGauge  = new AnyGauge().Generate();
        PatternSection ribbing   = new AnyPatternSection().WithName("Ribbing").WithFirstRow(1).WithRowCount(2).Generate();
        PatternSection body      = new AnyPatternSection().WithName("Body").WithFirstRow(2).Generate();
        int            anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("sections", () => new KnittingPattern("Beanie", anyGauge, anyCastOn, new[] { ribbing, body }));
    }

    [Theory]
    [InlineData("Body", "Body")]
    [InlineData("Body", "BODY")]
    [InlineData("Body", " body ")]
    public void A_pattern_refuses_two_sections_with_the_same_name_whatever_their_casing_or_padding(string first, string second) {
        // Arrange
        Gauge          anyGauge  = new AnyGauge().Generate();
        PatternSection one       = new AnyPatternSection().WithName(first).WithFirstRow(1).WithRowCount(2).Generate();
        PatternSection two       = new AnyPatternSection().WithName(second).WithFirstRow(3).Generate();
        int            anyCastOn = Any.Int32().Between(1, KnittingPattern.MaxCastOn).Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern("Beanie", anyGauge, anyCastOn, new[] { one, two }));

        // Assert
        Assert.Contains("Two sections are named", error.Message);
    }

    [Fact]
    public void A_generated_pattern_is_made_of_contiguous_sections_from_row_one() {
        // Arrange
        KnittingPattern anyPattern = new AnyKnittingPattern().Generate();

        // Act
        IReadOnlyList<PatternSection> sections = anyPattern.Sections;

        // Assert
        Assert.Equal(1, sections[0].FirstRow);
        for (int i = 1; i < sections.Count; i++) {
            Assert.Equal(sections[i - 1].LastRow + 1, sections[i].FirstRow);
        }
    }

    [Fact]
    public void A_pattern_expands_every_row_of_every_section_in_order() {
        // Arrange
        KnittingPattern pattern = PatternOf(12, ShapedHat());

        // Act
        IReadOnlyList<ExpandedRow> expanded = pattern.Expand();

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, expanded.Select(r => r.RowNumber));
        Assert.Equal(new[] { "Ribbing", "Ribbing", "Body", "Body", "Body", "Body" }, expanded.Select(r => r.SectionName));
        Assert.Equal(new[] { 12, 12, 12, 13, 13, 11 }, expanded.Select(r => r.StitchesBefore));
        Assert.Equal(new[] { 12, 12, 13, 13, 11, 9 }, expanded.Select(r => r.StitchesAfter));
        Assert.Equal(new[] { 0, 0, 1, 0, 1, 0 }, expanded.Select(r => r.Increases));
        Assert.Equal(new[] { 0, 0, 0, 0, 3, 1 }, expanded.Select(r => r.Decreases));
    }

    [Fact]
    public void A_pattern_expands_each_row_into_single_stitch_operations() {
        // Arrange
        KnittingPattern pattern = PatternOf(12, ShapedHat());

        // Act
        ExpandedRow first = pattern.Expand()[0];

        // Assert: *k2, p2; rep from * 3 times
        Assert.Equal(
            new[] {
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Purl,
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Purl,
                StitchKind.Knit, StitchKind.Knit, StitchKind.Purl, StitchKind.Purl,
            },
            first.Operations);
    }

    [Fact]
    public void Expanding_twice_starts_from_a_fresh_fabric_each_time() {
        // Arrange
        KnittingPattern pattern = PatternOf(12, ShapedHat());

        // Act
        IReadOnlyList<ExpandedRow> first  = pattern.Expand();
        IReadOnlyList<ExpandedRow> second = pattern.Expand();

        // Assert
        Assert.Equal(first.Select(r => r.StitchesAfter), second.Select(r => r.StitchesAfter));
    }

    [Fact]
    public void A_pattern_whose_row_does_not_fit_the_stitches_left_by_the_previous_one_cannot_be_expanded() {
        // Arrange: row 1 leaves 12 stitches, row 2 asks for 10
        PatternSection section = Section("Body", Rs(1, 12, K(12)), Ws(2, 10, P(10)));
        KnittingPattern pattern = PatternOf(12, section);

        // Act
        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());

        // Assert
        Assert.Equal(2, error.RowNumber);
        Assert.Equal(12, error.LiveStitches);
        Assert.Equal(10, error.RequiredStitches);
    }

    [Fact]
    public void A_pattern_whose_first_row_does_not_fit_the_cast_on_cannot_be_expanded() {
        // Arrange
        PatternSection  section = Section("Body", Rs(1, 20, K(20)));
        KnittingPattern pattern = PatternOf(18, section);

        // Act / Assert
        Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());
    }

    [Fact]
    public void A_pattern_whose_rows_do_not_present_the_sides_in_turn_cannot_be_expanded() {
        // Arrange: row 1 is written for the wrong side; the pattern itself is accepted
        PatternSection  section = Section("Body", Ws(1, 6, K(6)));
        KnittingPattern pattern = PatternOf(6, section);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => pattern.Expand());

        // Assert
        Assert.IsNotType<StitchCountMismatchException>(error);
    }

    [Fact]
    public void The_finished_width_is_the_last_rows_stitches_at_the_gauge() {
        // Arrange: 9 stitches at 18 stitches per 10 cm
        Gauge           gauge   = new AnyGauge().WithStitchesPer10Cm(18m).Generate();
        PatternSection[] sections = ShapedHat();
        KnittingPattern pattern = new AnyKnittingPattern().WithGauge(gauge).WithCastOnCount(12).WithSections(sections).Generate();

        // Act
        decimal width = pattern.FinishedWidthCm();

        // Assert
        Assert.Equal(5m, width);
    }

}
