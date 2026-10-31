using JustDummies;

using KnittingPatterns.Domain;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

public sealed class KnittingPatternTests {

    private static readonly AnyString AnySectionName = Any.String().Alpha().WithLengthBetween(3, PatternSection.MaxNameLength);

    [Fact]
    public void Expanding_a_pattern_works_every_row_of_every_section_on_the_stitches_the_previous_row_left() {
        // Arrange
        // Ribbing: rows 1-2 of k2, p2 over 40 sts. Body: an increase row, then a plain row over 42.
        PatternSection ribbing = new("Ribbing", [Written(1, Repeat(10, K(2), P(2))), Written(2, Repeat(10, K(2), P(2)))]);
        PatternSection body    = new("Body", [Written(3, K(1), Kfb(), K(36), Kfb(), K(1)), Plain(4, 42).Generate()]);
        KnittingPattern anyPattern = Any.KnittingPattern().WithCastOnCount(40).WithSections(ribbing, body).Generate();

        // Act
        IReadOnlyList<ExpandedRow> rows = anyPattern.Expand();

        // Assert
        Assert.Equal(
            [("Ribbing", 1, 40, 40), ("Ribbing", 2, 40, 40), ("Body", 3, 40, 42), ("Body", 4, 42, 42)],
            rows.Select(r => (r.SectionName, r.RowNumber, r.StitchesBefore, r.StitchesAfter)));
    }

    [Fact]
    public void A_section_written_for_the_count_before_the_previous_shaping_fails_at_its_first_row() {
        // Arrange
        // The border increases to 42, but the next section was written for the 40 cast on.
        PatternSection border = new("Border", [Written(1, K(1), Kfb(), K(36), Kfb(), K(1))]);
        PatternSection body   = new("Body", [Plain(2, 40).Generate(), Plain(3, 40).Generate()]);
        KnittingPattern anyPattern = Any.KnittingPattern().WithCastOnCount(40).WithSections(border, body).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyPattern.Expand());

        // Assert
        StitchCountMismatchException mismatch = Assert.IsType<StitchCountMismatchException>(refusal);
        Assert.Equal((2, 42, 40), (mismatch.RowNumber, mismatch.LiveStitches, mismatch.RequiredStitches));
    }

    [Fact]
    public void A_pattern_without_shaping_finishes_as_wide_as_its_cast_on_at_the_gauge() {
        // Arrange
        // Whatever its sections say, the default pattern only holds plain rows over its cast-on.
        KnittingPattern anyUnshapedPattern = Any.KnittingPattern()
                                                .WithCastOnCount(64)
                                                .WithGauge(g => g.WithStitchesPer10Cm(16m))
                                                .Generate();

        // Act
        decimal width = anyUnshapedPattern.FinishedWidthCm();

        // Assert
        Assert.Equal(40m, width);
    }

    [Fact]
    public void Expanding_a_pattern_twice_starts_again_from_the_cast_on() {
        // Arrange
        PatternSection crown = new("Crown", [Written(1, Repeat(8, K(1), K2Tog())), Written(2, P(16))]);
        KnittingPattern anyHatCrown = Any.KnittingPattern().WithCastOnCount(24).WithSections(crown).Generate();
        anyHatCrown.Expand();

        // Act
        IReadOnlyList<ExpandedRow> again = anyHatCrown.Expand();

        // Assert
        Assert.Equal((24, 16), (again[0].StitchesBefore, again[^1].StitchesAfter));
    }

    [Fact]
    public void Two_sections_named_alike_whatever_their_case_are_refused() {
        // Arrange
        string         anyName  = AnySectionName.Generate();
        PatternSection first    = Any.PatternSection().WithName(anyName.ToLowerInvariant()).Covering(1, 4).Generate();
        PatternSection second   = Any.PatternSection().WithName(anyName.ToUpperInvariant()).Covering(5, 6).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.KnittingPattern().WithSections(first, second).Generate());

        // Assert
        ArgumentException duplicate = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("Two sections are named", duplicate.Message);
    }

    [Fact]
    public void A_section_leaving_a_gap_after_the_previous_one_is_refused() {
        // Arrange
        PatternSection anyFirst  = Any.PatternSection().Covering(1, 4).Generate();
        PatternSection anyAfterAGap = Any.PatternSection().Covering(6, 9).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Any.KnittingPattern().WithSections(anyFirst, anyAfterAGap).Generate());

        // Assert
        ArgumentException gap = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("starts at row 6; row 5 was expected", gap.Message);
    }

}
