namespace KnittingPatterns.Tests.Expert.Tests;

public sealed class StitchCountMismatchExceptionTests {

    [Fact]
    public void The_mismatch_says_which_row_needs_how_many_stitches_while_the_needle_holds_how_many() {
        // Act
        StitchCountMismatchException mismatch = new(7, 12, 10);

        // Assert
        Assert.Equal(7, mismatch.RowNumber);
        Assert.Equal(12, mismatch.LiveStitches);
        Assert.Equal(10, mismatch.RequiredStitches);
        Assert.Equal("Row 7 needs 10 stitches but the needle holds 12.", mismatch.Message);
    }

    [Fact]
    public void A_mismatch_is_an_invalid_operation() {
        Assert.IsAssignableFrom<InvalidOperationException>(new StitchCountMismatchException(1, 2, 3));
    }

}
