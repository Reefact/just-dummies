using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Regular;

public sealed class StitchCountMismatchExceptionTests {

    [Fact]
    public void The_exception_names_the_row_and_both_stitch_counts() {
        // Act
        StitchCountMismatchException error = new(7, 24, 26);

        // Assert
        Assert.Equal(7, error.RowNumber);
        Assert.Equal(24, error.LiveStitches);
        Assert.Equal(26, error.RequiredStitches);
        Assert.Equal("Row 7 needs 26 stitches but the needle holds 24.", error.Message);
    }

    [Fact]
    public void The_exception_is_an_invalid_operation() {
        // Arrange
        StitchCountMismatchException anyError = new AnyStitchCountMismatchException().Generate();

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(anyError);
    }

}
