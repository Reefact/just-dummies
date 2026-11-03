using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class FabricTests {

    private static RowInstruction Decreasing(int number, FabricSide side, int pairs) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side)
                  .WithElements(new IRowElement[] { new StitchRun(StitchKind.KnitTwoTogether, pairs) })
                  .Generate();
    }

    private static RowInstruction Increasing(int number, FabricSide side, int stitches) {
        return Any.RowInstruction().WithRowNumber(number).WithSide(side)
                  .WithElements(new IRowElement[] { new StitchRun(StitchKind.KnitFrontAndBack, stitches) })
                  .Generate();
    }

    [Fact]
    public void A_fresh_fabric_holds_the_cast_on_and_presents_its_right_side() {
        Fabric fabric = new Fabric(36);

        Assert.Equal(36, fabric.LiveStitches);
        Assert.Equal(0, fabric.RowsWorked);
        Assert.Equal(FabricSide.RightSide, fabric.NextSide);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2000)]
    public void A_cast_on_may_be_one_stitch_or_two_thousand(int castOn) {
        Assert.Equal(castOn, new Fabric(castOn).LiveStitches);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(2001)]
    public void A_cast_on_outside_1_to_2000_is_refused(int castOn) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Fabric(castOn));

        Assert.Equal("castOnCount", error.ParamName);
    }

    [Fact]
    public void Working_a_row_moves_the_fabric_to_the_stitches_the_row_leaves() {
        Fabric fabric = new Fabric(10);

        ExpandedRow worked = fabric.Work("Crown", Decreasing(1, FabricSide.RightSide, 5));

        Assert.Equal("Crown", worked.SectionName);
        Assert.Equal(1, worked.RowNumber);
        Assert.Equal(10, worked.StitchesBefore);
        Assert.Equal(5, worked.StitchesAfter);
        Assert.Equal(5, fabric.LiveStitches);
        Assert.Equal(1, fabric.RowsWorked);
    }

    [Fact]
    public void The_side_presented_alternates_with_every_row_worked() {
        Fabric fabric = new Fabric(10);

        fabric.Work("Crown", Decreasing(1, FabricSide.RightSide, 5));
        Assert.Equal(FabricSide.WrongSide, fabric.NextSide);

        fabric.Work("Crown", Increasing(2, FabricSide.WrongSide, 5));
        Assert.Equal(FabricSide.RightSide, fabric.NextSide);
        Assert.Equal(10, fabric.LiveStitches);
        Assert.Equal(2, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_that_consumes_other_stitches_than_the_needle_holds_is_a_stitch_count_mismatch() {
        Fabric fabric = new Fabric(10);

        StitchCountMismatchException error = Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Decreasing(1, FabricSide.RightSide, 4)));

        Assert.Equal(1, error.RowNumber);
        Assert.Equal(10, error.LiveStitches);
        Assert.Equal(8, error.RequiredStitches);
        Assert.Equal("Row 1 needs 8 stitches but the needle holds 10.", error.Message);
    }

    [Fact]
    public void A_refused_row_leaves_the_fabric_as_it_was() {
        Fabric fabric = new Fabric(10);
        Assert.Throws<StitchCountMismatchException>(() => fabric.Work("Body", Decreasing(1, FabricSide.RightSide, 4)));

        ExpandedRow worked = fabric.Work("Body", Decreasing(1, FabricSide.RightSide, 5));

        Assert.Equal(5, worked.StitchesAfter);
        Assert.Equal(1, fabric.RowsWorked);
    }

    [Fact]
    public void A_row_must_be_the_next_one() {
        Fabric fabric = new Fabric(10);
        RowInstruction skipping = Decreasing(2, FabricSide.WrongSide, 5);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", skipping));

        Assert.Equal("Row 2 cannot be worked: row 1 is next.", error.Message);
    }

    [Fact]
    public void A_row_cannot_be_worked_twice() {
        Fabric fabric = new Fabric(10);
        RowInstruction first = Any.RowInstruction().WithRowNumber(1).WithSide(FabricSide.RightSide).WithStitchNeutralElements(10).Generate();
        fabric.Work("Body", first);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", first));

        Assert.Equal("Row 1 cannot be worked: row 2 is next.", error.Message);
    }

    [Fact]
    public void A_row_written_for_the_wrong_side_is_refused() {
        Fabric fabric = new Fabric(10);
        RowInstruction wrongSide = Any.RowInstruction().WithRowNumber(1).WithSide(FabricSide.WrongSide).WithStitchNeutralElements(10).Generate();

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", wrongSide));

        Assert.Equal("Row 1 is written WrongSide but the fabric presents its RightSide.", error.Message);
    }

    [Fact]
    public void An_out_of_sequence_row_is_reported_before_a_stitch_count_mismatch() {
        Fabric fabric = new Fabric(10);

        // Row 2 is both out of sequence and consumes the wrong number of stitches.
        Assert.Throws<InvalidOperationException>(() => fabric.Work("Body", Decreasing(2, FabricSide.WrongSide, 4)));
    }

    [Fact]
    public void Working_a_row_needs_a_section_name_and_a_row() {
        Fabric         fabric = new Fabric(10);
        RowInstruction anyRow = Any.RowInstruction().WithRowNumber(1).WithSide(FabricSide.RightSide).WithStitchNeutralElements(10).Generate();

        Assert.Throws<ArgumentNullException>(() => fabric.Work(null!, anyRow));
        Assert.Throws<ArgumentException>(() => fabric.Work("  ", anyRow));
        Assert.Throws<ArgumentNullException>(() => fabric.Work("Body", null!));
    }

}
