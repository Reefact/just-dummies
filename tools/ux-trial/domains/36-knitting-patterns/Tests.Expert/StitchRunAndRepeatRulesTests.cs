using JustDummies;

using KnittingPatterns.Domain;

using static KnittingPatterns.Tests.Expert.Recipes.Rows;

namespace KnittingPatterns.Tests.Expert;

public sealed class StitchRunAndRepeatRulesTests {

    [Fact]
    public void A_run_of_no_stitch_is_refused() {
        // Arrange
        StitchKind anyKind = Any.Enum<StitchKind>().Generate();

        // Act
        Exception? refusal = Record.Exception(() => new StitchRun(anyKind, 0));

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal("count", outOfRange.ParamName);
    }

    [Fact]
    public void A_repeat_worked_only_once_is_refused_since_it_is_no_repeat() {
        // Arrange
        List<StitchRun> anyMotif = Any.ListOf(Any.StitchRun()).WithCountBetween(1, 4).Generate();

        // Act
        Exception? refusal = Record.Exception(() => new StitchRepeat(anyMotif, times: 1));

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refusal);
        Assert.Equal("times", outOfRange.ParamName);
    }

    [Fact]
    public void A_repeat_expanding_past_ten_thousand_operations_is_refused() {
        // Arrange
        StitchRun k100 = K(100);

        // Act
        Exception? refusal = Record.Exception(() => new StitchRepeat([k100], times: 101));

        // Assert
        ArgumentException tooLong = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("10100 operations", tooLong.Message);
    }

    [Fact]
    public void A_compact_repeat_expands_to_hundreds_of_operations() {
        // Arrange
        // "*k2, p2; rep from * 50 times": one line of a ribbing, two hundred stitches worked.
        StitchRepeat ribbing = Repeat(50, K(2), P(2));

        // Act
        int operations = ribbing.Expand().Count();

        // Assert
        Assert.Equal(200, operations);
        Assert.Equal(ribbing.OperationCount, operations);
    }

}
