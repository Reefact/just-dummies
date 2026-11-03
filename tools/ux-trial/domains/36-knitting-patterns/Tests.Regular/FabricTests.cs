using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

using static KnittingPatterns.Tests.Regular.Rows;

namespace KnittingPatterns.Tests.Regular;

public sealed class FabricTests {

    [Fact]
    public void A_fabric_starts_on_the_cast_on_with_no_row_worked_and_its_right_side_up() {
        // Act
        Fabric fabric = new(40);

        // Assert
        Assert.Equal(40, fabric.LiveStitches);
        Assert.Equal(0, fabric.RowsWorked);
        Assert.Equal(FabricSide.RightSide, fabric.NextSide);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(KnittingPattern.MaxCastOn)]
    public void A_fabric_accepts_the_ends_of_the_cast_on_range(int castOn) {
        Assert.Equal(castOn, new Fabric(castOn).LiveStitches);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(KnittingPattern.MaxCastOn + 1)]
    public void A_fabric_refuses_a_cast_on_just_outside_its_range(int castOn) {
        Assert.Throws<ArgumentOutOfRangeException>("castOnCount", () => new Fabric(castOn));
    }

    [Fact]
    public void A_fabric_refuses_any_cast_on_below_one() {
        // Arrange
        int anyCastOn = Any.Int32().LessThan(1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("castOnCount", () => new Fabric(anyCastOn));
    }

    [Fact]
    public void A_fabric_refuses_any_cast_on_above_the_most_a_needle_takes() {
        // Arrange
        int anyCastOn = Any.Int32().GreaterThan(KnittingPattern.MaxCastOn).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>("castOnCount", () => new Fabric(anyCastOn));
    }

    [Fact]
    public void Working_a_row_returns_it_expanded_and_moves_the_fabric_to_the_stitches_it_leaves() {
        // Arrange: "Row 1 (RS): k5, kfb, k6 (13 sts)" on 12 stitches
        Fabric         fabric = new(12);
        RowInstruction row    = Rs(1, 13, K(5), Of(StitchKind.KnitFrontAndBack), K(6));

        // Act
        ExpandedRow worked = fabric.Work("Body", row);

        // Assert
        Assert.Equal("Body", worked.SectionName);
        Assert.Equal(1, worked.RowNumber);
        Assert.Equal(FabricSide.RightSide, worked.Side);
        Assert.Equal(12, worked.StitchesBefore);
        Assert.Equal(13, worked.StitchesAfter);
        Assert.Equal(1, worked.Increases);
        Assert.Equal(13, fabric.LiveStitches);
        Assert.Equal(1, fabric.RowsWorked);
        Assert.Equal(FabricSide.WrongSide, fabric.NextSide);
    }

    [Fact]
    public void Working_rows_in_turn_alternates_the_side_and_follows_the_live_count() {
        // Arrange
        Fabric fabric = new(12);

        // Act
        fabric.Work("Body", Rs(1, 6, Of(StitchKind.KnitTwoTogether, 6)));
        FabricSide afterOne = fabric.NextSide;
        fabric.Work("Body", Ws(2, 6, P(6)));
        FabricSide afterTwo = fabric.NextSide;
        fabric.Work("Body", Rs(3, 12, Of(StitchKind.KnitFrontAndBack, 6)));

        // Assert
        Assert.Equal(FabricSide.WrongSide, afterOne);
        Assert.Equal(FabricSide.RightSide, afterTwo);
        Assert.Equal(12, fabric.LiveStitches);
        Assert.Equal(3, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_may_start_a_new_section_name_without_changing_the_fabric() {
        // Arrange
        Fabric fabric = new(4);
        fabric.Work("Ribbing", Rs(1, 4, K(4)));

        // Act
        ExpandedRow worked = fabric.Work("Body", Ws(2, 4, P(4)));

        // Assert
        Assert.Equal("Body", worked.SectionName);
        Assert.Equal(4, fabric.LiveStitches);
    }

    [Fact]
    public void A_row_out_of_sequence_is_refused_and_the_fabric_is_left_as_it_was() {
        // Arrange: the fabric awaits row 1; the stitch count of the row is not the point
        int    anyCastOn = Any.Int32().Between(1, StitchRun.MaxCount).Generate();
        Fabric fabric    = new(anyCastOn);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", Ws(2, anyCastOn, K(anyCastOn))));

        // Assert
        Assert.Contains("row 1 is next", error.Message);
        Assert.Equal(anyCastOn, fabric.LiveStitches);
        Assert.Equal(0, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_worked_twice_is_refused() {
        // Arrange
        Fabric         fabric = new(6);
        RowInstruction row    = Rs(1, 6, K(6));
        fabric.Work("Body", row);

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", row));
    }

    [Fact]
    public void A_row_written_for_the_wrong_side_is_refused_and_the_fabric_is_left_as_it_was() {
        // Arrange: row 1 faces the right side
        int    anyCastOn = Any.Int32().Between(1, StitchRun.MaxCount).Generate();
        Fabric fabric    = new(anyCastOn);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", Ws(1, anyCastOn, K(anyCastOn))));

        // Assert
        Assert.IsNotType<StitchCountMismatchException>(error);
        Assert.Contains("WrongSide", error.Message);
        Assert.Equal(0, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_out_of_sequence_is_reported_before_a_wrong_stitch_count() {
        // Arrange: row 2 is neither next nor fitting
        Fabric fabric = new(10);

        // Act
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", Ws(2, 3, K(3))));

        // Assert
        Assert.IsNotType<StitchCountMismatchException>(error);
    }

    [Fact]
    public void A_row_that_consumes_other_stitches_than_are_live_is_refused_with_both_counts() {
        // Arrange
        Fabric fabric = new(12);

        // Act
        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Rs(1, 10, K(10))));

        // Assert
        Assert.Equal(1, error.RowNumber);
        Assert.Equal(12, error.LiveStitches);
        Assert.Equal(10, error.RequiredStitches);
        Assert.Equal(12, fabric.LiveStitches);
        Assert.Equal(0, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_that_only_makes_yarn_overs_consumes_nothing_and_cannot_be_worked() {
        // Arrange
        int    anyCastOn = Any.Int32().Between(1, 100).Generate();
        Fabric fabric    = new(anyCastOn);

        // Act
        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Rs(1, 4, Of(StitchKind.YarnOver, 4))));

        // Assert
        Assert.Equal(0, error.RequiredStitches);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Working_a_row_for_a_blank_section_is_refused(string blank) {
        // Arrange
        Fabric fabric = new(3);

        // Act / Assert
        Assert.Throws<ArgumentException>("sectionName", () => fabric.Work(blank, Rs(1, 3, K(3))));
    }

    [Fact]
    public void Working_a_missing_row_is_refused() {
        // Arrange
        Fabric fabric = new(3);

        // Act / Assert
        Assert.Throws<ArgumentNullException>("row", () => fabric.Work("Body", null!));
    }

}
