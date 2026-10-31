using GridLoadFlow.Domain;

using JustDummies;

namespace GridLoadFlow.Tests.Expert;

public sealed class TransformerTests {

    [Fact]
    public void Raising_the_tap_moves_it_one_step_up() {
        // Arrange
        Transformer anyTransformerAtNominalTap = Any.Transformer().WithTapRatio(1.0).Generate();

        // Act
        anyTransformerAtNominalTap.RaiseTap();

        // Assert
        Assert.Equal(1.0125, anyTransformerAtNominalTap.TapRatio, 9);
    }

    [Fact]
    public void A_tap_at_its_upper_end_stop_cannot_be_raised() {
        // Arrange
        Transformer anyTransformerAtTopTap = Any.Transformer().WithTapRatio(1.1).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyTransformerAtTopTap.RaiseTap());

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(1.1, anyTransformerAtTopTap.TapRatio);
    }

    [Fact]
    public void A_tap_one_step_above_its_lower_end_stop_can_be_lowered_once() {
        // Arrange
        Transformer anyTransformer = Any.Transformer().WithTapRatio(0.9125).Generate();

        // Act
        anyTransformer.LowerTap();
        Exception? secondMove = Record.Exception(() => anyTransformer.LowerTap());

        // Assert
        Assert.Equal(0.9, anyTransformer.TapRatio, 9);
        Assert.IsType<InvalidOperationException>(secondMove);
    }

}
