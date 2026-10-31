using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class LineTests {

    [Fact]
    public void A_line_cannot_connect_a_bus_to_itself() {
        // Arrange
        BusId   anyBus  = Arbitrary.BusId().Generate();
        AnyLine anyLoop = Arbitrary.Line().WithFromBus(anyBus).WithToBus(anyBus);

        // Act
        Exception? refusal = Record.Exception(() => anyLoop.Generate());

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("toBus", argumentException.ParamName);
    }
}
