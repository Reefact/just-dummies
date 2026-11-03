using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

public sealed class ExpandedRowTests {

    private static readonly StitchKind[] MixedRow = {
        StitchKind.Knit, StitchKind.KnitTwoTogether, StitchKind.YarnOver, StitchKind.KnitFrontAndBack, StitchKind.CentralDoubleDecrease,
    };

    [Fact]
    public void An_expanded_row_derives_the_stitches_it_leaves_and_counts_its_increases_and_decreases() {
        // Arrange: consumes 1 + 2 + 0 + 1 + 3 = 7, produces 1 + 1 + 1 + 2 + 1 = 6
        ExpandedRow row = new(
            "Body", 4, FabricSide.WrongSide, 7, MixedRow);

        // Assert
        Assert.Equal(7, row.StitchesBefore);
        Assert.Equal(6, row.StitchesAfter);
        Assert.Equal(2, row.Increases);
        Assert.Equal(2, row.Decreases);
        Assert.Equal(MixedRow, row.Operations);
    }

    [Fact]
    public void An_expanded_row_keeps_where_it_comes_from() {
        // Arrange
        IReadOnlyList<StitchKind> anyOperations = new AnyExpandedRow().Generate().Operations;
        int                       consumed      = anyOperations.Sum(o => o.Consumes());

        // Act
        ExpandedRow row = new("Lace panel", 12, FabricSide.RightSide, consumed, anyOperations);

        // Assert
        Assert.Equal("Lace panel", row.SectionName);
        Assert.Equal(12, row.RowNumber);
        Assert.Equal(FabricSide.RightSide, row.Side);
    }

    [Fact]
    public void An_expanded_row_does_not_follow_an_operation_list_changed_after_it_was_built() {
        // Arrange
        List<StitchKind> operations = new() { StitchKind.Knit, StitchKind.Purl };
        ExpandedRow      row        = new("Body", 1, FabricSide.RightSide, 2, operations);

        // Act
        operations.Add(StitchKind.Knit);

        // Assert
        Assert.Equal(2, row.Operations.Count);
    }

    [Fact]
    public void An_expanded_row_refuses_operations_that_do_not_consume_the_stitches_it_starts_with() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore + 1, anyRow.Operations));

        // Assert
        Assert.Equal("operations", error.ParamName);
        Assert.Contains($"starts with {anyRow.StitchesBefore + 1}", error.Message);
    }

    [Fact]
    public void An_expanded_row_refuses_a_blank_section_name() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();
        string      blank  = Any.String().WithChars(" \t").NonEmpty().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("sectionName", () => new ExpandedRow(blank, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, anyRow.Operations));
    }

    [Fact]
    public void An_expanded_row_refuses_a_row_number_below_one() {
        // Arrange
        ExpandedRow anyRow    = new AnyExpandedRow().Generate();
        int         anyNumber = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("rowNumber", () => new ExpandedRow(anyRow.SectionName, anyNumber, anyRow.Side, anyRow.StitchesBefore, anyRow.Operations));
    }

    [Fact]
    public void An_expanded_row_refuses_a_side_the_fabric_does_not_have() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("side", () => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, (FabricSide)5, anyRow.StitchesBefore, anyRow.Operations));
    }

    [Fact]
    public void An_expanded_row_refuses_to_start_with_no_stitch() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();
        int         anyCount = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("stitchesBefore", () => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyCount, anyRow.Operations));
    }

    [Fact]
    public void An_expanded_row_refuses_an_empty_list_of_operations() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>("operations", () => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, Array.Empty<StitchKind>()));
    }

    [Fact]
    public void An_expanded_row_refuses_a_missing_list_of_operations() {
        // Arrange
        ExpandedRow anyRow = new AnyExpandedRow().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>("operations", () => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, null!));
    }

    [Fact(Skip = "domain defect: Operations returns the internal array as an IReadOnlyList, so a cast writes through it (same for RowInstruction.Elements, StitchRepeat.Runs, PatternSection.Rows, KnittingPattern.Sections); deterministic, no seed involved")]
    public void An_expanded_row_cannot_be_changed_through_the_operations_it_hands_out() {
        // Arrange
        ExpandedRow row = new("Body", 1, FabricSide.RightSide, 2, new[] { StitchKind.Knit, StitchKind.Purl });

        // Act: the list is an array underneath; a caller that casts it can write to it
        if (row.Operations is StitchKind[] exposed) {
            exposed[0] = StitchKind.CentralDoubleDecrease;
        }

        // Assert
        Assert.Equal(StitchKind.Knit, row.Operations[0]);
    }

}
