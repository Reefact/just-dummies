using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

public sealed class ExpandedRowTests {

    [Fact]
    public void An_expanded_row_derives_the_stitches_it_leaves_and_its_increases_and_decreases() {
        int        anyRowNumber = Any.Int32().Positive().Generate();
        FabricSide anySide      = Any.Enum<FabricSide>().Generate();

        // k, yo, kfb, ssk: consumes 1+0+1+2 = 4, produces 1+1+2+1 = 5
        ExpandedRow row = new ExpandedRow("Lace", anyRowNumber, anySide, 4,
                                          new[] { StitchKind.Knit, StitchKind.YarnOver, StitchKind.KnitFrontAndBack, StitchKind.SlipSlipKnit });

        Assert.Equal("Lace", row.SectionName);
        Assert.Equal(4, row.StitchesBefore);
        Assert.Equal(5, row.StitchesAfter);
        Assert.Equal(2, row.Increases);
        Assert.Equal(1, row.Decreases);
    }

    [Fact]
    public void Plain_stitches_are_neither_increases_nor_decreases() {
        ExpandedRow row = new ExpandedRow("Body", 1, FabricSide.RightSide, 3, new[] { StitchKind.Knit, StitchKind.Purl, StitchKind.Slip });

        Assert.Equal(0, row.Increases);
        Assert.Equal(0, row.Decreases);
        Assert.Equal(3, row.StitchesAfter);
    }

    [Fact]
    public void A_central_double_decrease_counts_as_one_decrease() {
        ExpandedRow row = new ExpandedRow("Crown", 1, FabricSide.RightSide, 6, new[] { StitchKind.CentralDoubleDecrease, StitchKind.KnitTwoTogether, StitchKind.Knit });

        Assert.Equal(2, row.Decreases);
        Assert.Equal(3, row.StitchesAfter);
    }

    [Fact]
    public void An_expanded_row_keeps_its_operations_in_working_order() {
        ExpandedRow row = new ExpandedRow("Body", 1, FabricSide.RightSide, 2, new[] { StitchKind.Purl, StitchKind.Knit });

        Assert.Equal(new[] { StitchKind.Purl, StitchKind.Knit }, row.Operations);
    }

    [Fact]
    public void An_expanded_row_refuses_operations_that_do_not_consume_the_stitches_it_starts_with() {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        ArgumentException error = Assert.Throws<ArgumentException>(() => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore + 1, anyRow.Operations));

        Assert.Equal("operations", error.ParamName);
    }

    [Fact]
    public void An_expanded_row_needs_a_section_name() {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        Assert.Throws<ArgumentNullException>(() => new ExpandedRow(null!, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, anyRow.Operations));
        Assert.Throws<ArgumentException>(() => new ExpandedRow("   ", anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, anyRow.Operations));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void An_expanded_row_is_numbered_from_one(int rowNumber) {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new ExpandedRow(anyRow.SectionName, rowNumber, anyRow.Side, anyRow.StitchesBefore, anyRow.Operations));

        Assert.Equal("rowNumber", error.ParamName);
    }

    [Fact]
    public void An_expanded_row_refuses_a_side_the_enum_does_not_declare() {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, (FabricSide)5, anyRow.StitchesBefore, anyRow.Operations));

        Assert.Equal("side", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void An_expanded_row_starts_with_at_least_one_stitch(int stitchesBefore) {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, stitchesBefore, anyRow.Operations));

        Assert.Equal("stitchesBefore", error.ParamName);
    }

    [Fact]
    public void An_expanded_row_needs_operations() {
        ExpandedRow anyRow = Any.ExpandedRow().Generate();

        Assert.Throws<ArgumentNullException>(() => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, null!));
        Assert.Throws<ArgumentException>(() => new ExpandedRow(anyRow.SectionName, anyRow.RowNumber, anyRow.Side, anyRow.StitchesBefore, Array.Empty<StitchKind>()));
    }

    [Fact]
    public void An_expanded_row_keeps_its_own_copy_of_the_operations() {
        List<StitchKind> operations = new List<StitchKind> { StitchKind.Knit, StitchKind.Purl };
        ExpandedRow      row        = new ExpandedRow("Body", 1, FabricSide.RightSide, 2, operations);

        operations.Add(StitchKind.Knit);

        Assert.Equal(2, row.Operations.Count);
        Assert.Equal(2, row.StitchesAfter);
    }

}
