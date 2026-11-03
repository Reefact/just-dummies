using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class KnittingPatternTests {

    private static RowInstruction Row(int number, FabricSide side, params IRowElement[] elements) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side).WithElements(elements).Generate();
    }

    private static RowInstruction Neutral(int number, FabricSide side, int stitches) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side).WithStitchNeutralElements(stitches).Generate();
    }

    private static PatternSection Section(string name, params RowInstruction[] rows) {
        return Any.PatternSection().WithName(name).WithRows(rows).Generate();
    }

    /// <summary>Cast on 20. Ribbing rows 1-2, Lace rows 3-4, Decrease rows 5-6, 20 stitches down to 10.</summary>
    private static KnittingPattern ScarfPattern(Gauge gauge) {
        IRowElement ribbing = new StitchRepeat(new[] { new StitchRun(StitchKind.Knit, 2), new StitchRun(StitchKind.Purl, 2) }, 5);
        IRowElement lace    = new StitchRepeat(new[] { new StitchRun(StitchKind.YarnOver, 1), new StitchRun(StitchKind.KnitTwoTogether, 1) }, 8);

        return new KnittingPattern("Scarf", gauge, 20, new[] {
            Section("Ribbing", Row(1, FabricSide.RightSide, ribbing), Row(2, FabricSide.WrongSide, ribbing)),
            Section("Lace", Row(3, FabricSide.RightSide, new StitchRun(StitchKind.Knit, 2), lace, new StitchRun(StitchKind.Knit, 2)),
                            Row(4, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 20))),
            Section("Decrease", Row(5, FabricSide.RightSide, new StitchRun(StitchKind.KnitTwoTogether, 10)),
                                Row(6, FabricSide.WrongSide, new StitchRun(StitchKind.Purl, 10)))
        });
    }

    [Fact]
    public void A_pattern_expands_every_row_of_every_section_keeping_the_stitch_count_in_step() {
        KnittingPattern pattern = ScarfPattern(Any.Gauge().Generate());

        IReadOnlyList<ExpandedRow> rows = pattern.Expand();

        Assert.Equal(6, rows.Count);
        Assert.Equal(new[] { "Ribbing", "Ribbing", "Lace", "Lace", "Decrease", "Decrease" }, rows.Select(r => r.SectionName));
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, rows.Select(r => r.RowNumber));
        Assert.Equal(new[] { 20, 20, 20, 20, 20, 10 }, rows.Select(r => r.StitchesBefore));
        Assert.Equal(new[] { 20, 20, 20, 20, 10, 10 }, rows.Select(r => r.StitchesAfter));
        Assert.Equal(8, rows[2].Increases);
        Assert.Equal(8, rows[2].Decreases);
        Assert.Equal(10, rows[4].Decreases);
    }

    [Fact]
    public void Expanding_twice_starts_from_a_fresh_fabric_each_time() {
        KnittingPattern pattern = ScarfPattern(Any.Gauge().Generate());

        IReadOnlyList<ExpandedRow> first  = pattern.Expand();
        IReadOnlyList<ExpandedRow> second = pattern.Expand();

        Assert.Equal(first.Select(r => r.StitchesAfter), second.Select(r => r.StitchesAfter));
    }

    [Fact]
    public void The_finished_width_is_the_width_of_the_stitches_left_by_the_last_row() {
        // 10 stitches left, at 25 stitches per 10 cm.
        KnittingPattern pattern = ScarfPattern(new Gauge(25m, 30m));

        Assert.Equal(4m, pattern.FinishedWidthCm());
    }

    [Fact]
    public void The_row_count_is_the_last_row_of_the_last_section() {
        KnittingPattern pattern = ScarfPattern(Any.Gauge().Generate());

        Assert.Equal(6, pattern.RowCount);
        Assert.Equal(3, pattern.Sections.Count);
    }

    [Fact]
    public void A_pattern_whose_second_row_needs_other_stitches_than_the_first_left_cannot_be_expanded() {
        Gauge anyGauge = Any.Gauge().Generate();
        KnittingPattern pattern = new KnittingPattern("Hat", anyGauge, 20, new[] {
            Section("Body", Neutral(1, FabricSide.RightSide, 20), Neutral(2, FabricSide.WrongSide, 19))
        });

        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());

        Assert.Equal(2, error.RowNumber);
        Assert.Equal(20, error.LiveStitches);
        Assert.Equal(19, error.RequiredStitches);
    }

    [Fact]
    public void A_first_row_that_does_not_consume_the_cast_on_cannot_be_expanded() {
        Gauge anyGauge = Any.Gauge().Generate();
        KnittingPattern pattern = new KnittingPattern("Hat", anyGauge, 20, new[] {
            Section("Body", Neutral(1, FabricSide.RightSide, 18))
        });

        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => pattern.Expand());

        Assert.Equal(1, error.RowNumber);
        Assert.Equal(20, error.LiveStitches);
    }

    [Fact]
    public void A_pattern_starting_on_the_wrong_side_is_accepted_but_cannot_be_expanded() {
        // Nothing at construction looks at which side row 1 is written for; the fabric does when the pattern is worked.
        Gauge anyGauge = Any.Gauge().Generate();
        KnittingPattern pattern = new KnittingPattern("Hat", anyGauge, 12, new[] {
            Section("Body", Neutral(1, FabricSide.WrongSide, 12))
        });

        Assert.Throws<InvalidOperationException>(() => pattern.Expand());
    }

    [Fact]
    public void A_title_is_trimmed() {
        KnittingPattern pattern = Any.KnittingPattern().WithTitle("  Cabled hat ").Generate();

        Assert.Equal("Cabled hat", pattern.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_pattern_needs_a_title_with_something_in_it(string blank) {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        Assert.Throws<ArgumentException>(() => new KnittingPattern(blank, anyPattern.Gauge, anyPattern.CastOnCount, anyPattern.Sections));
        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(null!, anyPattern.Gauge, anyPattern.CastOnCount, anyPattern.Sections));
    }

    [Fact]
    public void A_pattern_needs_a_gauge() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(anyPattern.Title, null!, anyPattern.CastOnCount, anyPattern.Sections));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2000)]
    public void A_cast_on_may_be_one_stitch_or_two_thousand(int castOn) {
        KnittingPattern pattern = Any.KnittingPattern().WithCastOnCount(castOn).Generate();

        Assert.Equal(castOn, pattern.CastOnCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2001)]
    public void A_cast_on_outside_1_to_2000_is_refused(int castOn) {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, castOn, anyPattern.Sections));

        Assert.Equal("castOnCount", error.ParamName);
    }

    [Fact]
    public void A_pattern_needs_sections() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        Assert.Throws<ArgumentNullException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, null!));
        Assert.Throws<ArgumentException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, Array.Empty<PatternSection>()));
    }

    [Fact]
    public void A_pattern_cannot_hold_a_null_section() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        Assert.Throws<ArgumentException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, new PatternSection?[] { anyPattern.Sections[0], null }!));
    }

    [Fact]
    public void The_first_section_starts_at_row_one() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, new[] {
            Section("Body", Neutral(2, FabricSide.WrongSide, 10))
        }));

        Assert.Equal("sections", error.ParamName);
        Assert.Contains("starts at row 2, not row 1", error.Message);
    }

    [Theory]
    [InlineData(4)]   // a gap after row 2
    [InlineData(2)]   // overlaps row 2
    public void Sections_follow_each_other_without_a_gap_or_an_overlap(int secondSectionStart) {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();
        FabricSide      side       = secondSectionStart % 2 == 1 ? FabricSide.RightSide : FabricSide.WrongSide;

        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, new[] {
            Section("Ribbing", Neutral(1, FabricSide.RightSide, 10), Neutral(2, FabricSide.WrongSide, 10)),
            Section("Body", Neutral(secondSectionStart, side, 10))
        }));

        Assert.Contains($"Section 'Body' starts at row {secondSectionStart}; row 3 was expected.", error.Message);
    }

    [Theory]
    [InlineData("Body", "Body")]
    [InlineData("Body", "BODY")]
    public void Section_names_are_unique_whatever_their_case(string first, string second) {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, new[] {
            Section(first, Neutral(1, FabricSide.RightSide, 10)),
            Section(second, Neutral(2, FabricSide.WrongSide, 10))
        }));

        Assert.Contains($"Two sections are named '{second}'", error.Message);
    }

    [Fact]
    public void A_pattern_keeps_its_own_copy_of_the_sections() {
        KnittingPattern    anyPattern = Any.KnittingPattern().Generate();
        List<PatternSection> sections = new List<PatternSection> { Section("Body", Neutral(1, FabricSide.RightSide, 10)) };
        KnittingPattern    pattern    = new KnittingPattern(anyPattern.Title, anyPattern.Gauge, anyPattern.CastOnCount, sections);

        sections.Add(Section("Border", Neutral(2, FabricSide.WrongSide, 10)));

        Assert.Single(pattern.Sections);
        Assert.Equal(1, pattern.RowCount);
    }

}
