namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class KnittingPatternTests {

    private static string AnyTitle() {
        return AnyKnittingPattern.ValidTitle().Generate();
    }

    // Sections of a pattern that was drawn whole: contiguous from row 1, distinct names.
    private static IReadOnlyList<PatternSection> AnySections() {
        return Any.KnittingPattern().Generate().Sections;
    }

    private static PatternSection Section(string name, int firstRow, int rowCount) {
        return Any.PatternSection().WithName(name).StartingAtRow(firstRow).WithRowCount(rowCount).Generate();
    }

    private static RowInstruction Row(int rowNumber, FabricSide side, params IRowElement[] elements) {
        return new RowInstruction(rowNumber, side, elements, null);
    }

    // ------------------------------------------------------------------------------------------ construction

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_pattern_needs_a_title_with_something_in_it(string blankTitle) {
        Gauge anyGauge = Any.Gauge().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(blankTitle, anyGauge, 20, AnySections()));

        Assert.Equal("title", refusal.ParamName);
    }

    [Fact]
    public void A_pattern_without_a_title_is_refused() {
        Gauge anyGauge = Any.Gauge().Generate();

        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(null!, anyGauge, 20, AnySections()));
    }

    [Fact]
    public void A_pattern_keeps_its_title_without_the_spaces_around_it() {
        Gauge anyGauge = Any.Gauge().Generate();

        KnittingPattern pattern = new("  Cabled hat  ", anyGauge, 20, AnySections());

        Assert.Equal("Cabled hat", pattern.Title);
    }

    [Fact]
    public void A_pattern_without_a_gauge_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(AnyTitle(), null!, 20, AnySections()));
    }

    [Fact]
    public void A_pattern_keeps_the_gauge_it_was_given() {
        Gauge gauge = new(22m, 30m);

        KnittingPattern pattern = new(AnyTitle(), gauge, 20, AnySections());

        Assert.Same(gauge, pattern.Gauge);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(KnittingPattern.MaxCastOn + 1)]
    public void A_pattern_is_cast_on_between_one_and_two_thousand_stitches_and_refuses_any_other_count(int castOnCount) {
        Gauge anyGauge = Any.Gauge().Generate();

        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new KnittingPattern(AnyTitle(), anyGauge, castOnCount, AnySections()));

        Assert.Equal("castOnCount", refusal.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(KnittingPattern.MaxCastOn)]
    public void A_pattern_accepts_the_fewest_and_the_most_cast_on_stitches(int castOnCount) {
        Gauge anyGauge = Any.Gauge().Generate();

        KnittingPattern pattern = new(AnyTitle(), anyGauge, castOnCount, AnySections());

        Assert.Equal(castOnCount, pattern.CastOnCount);
    }

    [Fact]
    public void A_pattern_without_a_section_list_is_refused() {
        Gauge anyGauge = Any.Gauge().Generate();

        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, null!));
    }

    [Fact]
    public void A_pattern_needs_at_least_one_section() {
        Gauge anyGauge = Any.Gauge().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, []));

        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_pattern_cannot_hold_a_missing_section() {
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection ribbing  = Section("Ribbing", 1, 2);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, [ribbing, null!]));

        Assert.Equal("sections", refusal.ParamName);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void A_pattern_starts_at_row_one(int firstRow) {
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection section  = Section("Ribbing", firstRow, 3);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, [section]));

        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_pattern_refuses_a_gap_between_two_sections() {
        // Arrange: Ribbing ends on row 2, Body starts on row 4
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection ribbing  = Section("Ribbing", 1, 2);
        PatternSection body     = Section("Body", 4, 3);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, [ribbing, body]));

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_pattern_refuses_two_sections_that_share_a_row() {
        // Arrange: Ribbing ends on row 2, Body starts on row 2 again
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection ribbing  = Section("Ribbing", 1, 2);
        PatternSection body     = Section("Body", 2, 3);

        Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, [ribbing, body]));
    }

    [Theory]
    [InlineData("Body", "Body")]
    [InlineData("Body", "BODY")]
    [InlineData("Body", "  body  ")]
    public void A_pattern_refuses_two_sections_with_the_same_name_whatever_the_case_or_the_spaces(string firstName, string secondName) {
        // Arrange
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection first    = Section(firstName, 1, 2);
        PatternSection second   = Section(secondName, 3, 2);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new KnittingPattern(AnyTitle(), anyGauge, 20, [first, second]));

        // Assert
        Assert.Equal("sections", refusal.ParamName);
    }

    [Fact]
    public void A_pattern_reports_its_sections_in_order_and_ends_on_the_last_row_of_the_last_one() {
        // Arrange
        Gauge          anyGauge = Any.Gauge().Generate();
        PatternSection ribbing  = Section("Ribbing", 1, 2);
        PatternSection body     = Section("Body", 3, 4);
        PatternSection crown    = Section("Crown decreases", 7, 3);

        // Act
        KnittingPattern pattern = new(AnyTitle(), anyGauge, 20, [ribbing, body, crown]);

        // Assert
        Assert.Equal([ribbing, body, crown], pattern.Sections);
        Assert.Equal(9, pattern.RowCount);
    }

    [Fact]
    public void A_pattern_is_not_changed_by_the_list_of_sections_it_was_built_from() {
        // Arrange
        Gauge                anyGauge = Any.Gauge().Generate();
        List<PatternSection> sections = [Section("Ribbing", 1, 2)];
        KnittingPattern      pattern  = new(AnyTitle(), anyGauge, 20, sections);

        // Act
        sections.Add(Section("Body", 3, 2));

        // Assert
        Assert.Equal(2, pattern.RowCount);
    }

    // ------------------------------------------------------------------------------------------ expanding

    // Cast on 12; ribbing, then a decrease section, then an increase section.
    private static KnittingPattern HatPattern() {
        IRowElement[] ribbing = [new StitchRepeat([new StitchRun(StitchKind.Knit, 1), new StitchRun(StitchKind.Purl, 1)], 6)];

        PatternSection ribbingSection = new("Ribbing", [new RowInstruction(1, FabricSide.RightSide, ribbing, 12),
                                                        new RowInstruction(2, FabricSide.WrongSide, ribbing, 12)]);
        PatternSection decreaseSection = new("Decrease", [new RowInstruction(3, FabricSide.RightSide, [new StitchRun(StitchKind.KnitTwoTogether, 4), new StitchRun(StitchKind.Knit, 4)], 8),
                                                          new RowInstruction(4, FabricSide.WrongSide, [new StitchRun(StitchKind.Purl, 8)], 8)]);
        PatternSection increaseSection = new("Increase", [new RowInstruction(5, FabricSide.RightSide, [new StitchRun(StitchKind.KnitFrontAndBack, 2), new StitchRun(StitchKind.Knit, 6)], 10)]);

        return new KnittingPattern("Hat", new Gauge(20m, 28m), 12, [ribbingSection, decreaseSection, increaseSection]);
    }

    [Fact]
    public void Expanding_a_pattern_works_every_row_of_every_section_and_follows_the_stitch_count_row_after_row() {
        // Arrange
        KnittingPattern pattern = HatPattern();

        // Act
        IReadOnlyList<ExpandedRow> rows = pattern.Expand();

        // Assert
        Assert.Equal([1, 2, 3, 4, 5], rows.Select(r => r.RowNumber));
        Assert.Equal([12, 12, 12, 8, 8], rows.Select(r => r.StitchesBefore));
        Assert.Equal([12, 12, 8, 8, 10], rows.Select(r => r.StitchesAfter));
    }

    [Fact]
    public void Expanding_a_pattern_tells_each_row_which_section_it_belongs_to() {
        IReadOnlyList<ExpandedRow> rows = HatPattern().Expand();

        Assert.Equal(["Ribbing", "Ribbing", "Decrease", "Decrease", "Increase"], rows.Select(r => r.SectionName));
    }

    [Fact]
    public void Expanding_a_pattern_counts_the_increases_and_decreases_of_each_row() {
        IReadOnlyList<ExpandedRow> rows = HatPattern().Expand();

        Assert.Equal([0, 0, 4, 0, 0], rows.Select(r => r.Decreases));
        Assert.Equal([0, 0, 0, 0, 2], rows.Select(r => r.Increases));
    }

    [Fact]
    public void Expanding_a_pattern_unrolls_its_repeats_into_single_stitches() {
        // Arrange: row 1 is "*k1, p1; rep from * 6 times"
        KnittingPattern pattern = HatPattern();

        // Act
        ExpandedRow firstRow = pattern.Expand()[0];

        // Assert
        Assert.Equal(12, firstRow.Operations.Count);
        Assert.Equal([StitchKind.Knit, StitchKind.Purl, StitchKind.Knit, StitchKind.Purl], firstRow.Operations.Take(4));
    }

    [Fact]
    public void Expanding_a_pattern_twice_starts_again_from_the_cast_on_each_time() {
        // Arrange
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        // Act
        IReadOnlyList<ExpandedRow> first  = anyPattern.Expand();
        IReadOnlyList<ExpandedRow> second = anyPattern.Expand();

        // Assert
        Assert.Equal(first.Select(r => r.StitchesAfter), second.Select(r => r.StitchesAfter));
    }

    [Fact]
    public void Expanding_a_pattern_gives_one_expanded_row_per_row_of_the_pattern() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        IReadOnlyList<ExpandedRow> rows = anyPattern.Expand();

        Assert.Equal(anyPattern.RowCount, rows.Count);
    }

    [Fact]
    public void Expanding_a_pattern_whose_row_does_not_fit_the_stitches_left_by_the_previous_one_names_the_row() {
        // Arrange: cast on 12, row 1 leaves 10 stitches and row 2 purls 12
        PatternSection section = new("Body", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.KnitTwoTogether, 2), new StitchRun(StitchKind.Knit, 8)),
                                               Row(2, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 12))]);
        KnittingPattern pattern = new(AnyTitle(), Any.Gauge().Generate(), 12, [section]);

        // Act
        StitchCountMismatchException mismatch = Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());

        // Assert
        Assert.Equal(2, mismatch.RowNumber);
        Assert.Equal(10, mismatch.LiveStitches);
        Assert.Equal(12, mismatch.RequiredStitches);
    }

    [Fact]
    public void A_pattern_that_misses_the_cast_on_count_is_built_without_complaint_and_only_fails_when_expanded() {
        // Arrange: the first row knits 10 stitches, the pattern casts on 12
        PatternSection section = new("Body", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.Knit, 10))]);

        // Act
        KnittingPattern pattern = new(AnyTitle(), Any.Gauge().Generate(), 12, [section]);

        // Assert
        Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());
    }

    [Fact]
    public void A_pattern_whose_second_section_opens_on_the_side_the_first_one_ended_on_is_built_but_cannot_be_expanded() {
        // Arrange: both sections are fine on their own; row 3 is written wrong side, right after row 2 on the wrong side
        PatternSection ribbing = new("Ribbing", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.Knit, 12)),
                                                Row(2, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 12))]);
        PatternSection body    = new("Body", [Row(3, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 12))]);

        // Act
        KnittingPattern pattern = new(AnyTitle(), Any.Gauge().Generate(), 12, [ribbing, body]);

        // Assert: an exact InvalidOperationException, the side error, not a stitch count one
        Assert.Throws<InvalidOperationException>(() => pattern.Expand());
    }

    [Fact]
    public void A_pattern_whose_first_row_is_written_wrong_side_is_built_but_cannot_be_expanded() {
        PatternSection section = new("Body", [Row(1, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 12))]);

        KnittingPattern pattern = new(AnyTitle(), Any.Gauge().Generate(), 12, [section]);

        Assert.Throws<InvalidOperationException>(() => pattern.Expand());
    }

    // ------------------------------------------------------------------------------------------ finished width

    [Fact]
    public void The_finished_width_is_that_of_the_stitches_left_by_the_last_row() {
        // Arrange: cast on 20 at 20 stitches per 10 cm, one row of 4 decreases leaves 16 stitches
        PatternSection section = new("Body", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.KnitTwoTogether, 4), new StitchRun(StitchKind.Knit, 12))]);
        KnittingPattern pattern = new(AnyTitle(), new Gauge(20m, 28m), 20, [section]);

        // Act + Assert
        Assert.Equal(8m, pattern.FinishedWidthCm());
    }

    [Fact]
    public void The_finished_width_follows_the_last_row_up_when_the_pattern_increases() {
        // Arrange: cast on 20 at 20 stitches per 10 cm, one row of 4 increases leaves 24 stitches
        PatternSection section = new("Body", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.KnitFrontAndBack, 4), new StitchRun(StitchKind.Knit, 16))]);
        KnittingPattern pattern = new(AnyTitle(), new Gauge(20m, 28m), 20, [section]);

        // Act + Assert
        Assert.Equal(12m, pattern.FinishedWidthCm());
    }

    [Fact]
    public void The_finished_width_of_a_pattern_that_cannot_be_expanded_is_not_computed() {
        PatternSection section = new("Body", [Row(1, FabricSide.RightSide, new StitchRun(StitchKind.Knit, 10))]);
        KnittingPattern pattern = new(AnyTitle(), Any.Gauge().Generate(), 12, [section]);

        Assert.Throws<StitchCountMismatchException>(() => pattern.FinishedWidthCm());
    }

}
