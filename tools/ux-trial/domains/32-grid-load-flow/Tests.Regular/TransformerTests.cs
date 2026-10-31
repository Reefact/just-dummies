using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class TransformerTests {

    [Fact]
    public void Raising_the_tap_moves_it_one_step_up() {
        // Arrange
        Transformer anyTransformer = Arbitrary.Transformer().WithTapRatio(1.0).Generate();

        // Act
        anyTransformer.RaiseTap();

        // Assert
        Assert.Equal(1.0125, anyTransformer.TapRatio);
    }

    [Fact]
    public void The_tap_cannot_be_raised_past_its_upper_end_stop() {
        // Arrange
        Transformer anyTransformer = Arbitrary.Transformer().WithTapRatio(Transformer.MaximumTap).Generate();

        // Act
        Exception? refusal = Record.Exception(anyTransformer.RaiseTap);

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(Transformer.MaximumTap, anyTransformer.TapRatio);
    }
}
