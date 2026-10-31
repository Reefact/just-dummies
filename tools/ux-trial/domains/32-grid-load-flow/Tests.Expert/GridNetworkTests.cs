using GridLoadFlow.Domain;

using GridLoadFlow.Tests.Expert.Recipes;

using JustDummies;

namespace GridLoadFlow.Tests.Expert;

public sealed class GridNetworkTests {

    [Fact]
    public void A_network_with_two_slack_buses_is_refused() {
        // Arrange
        IReadOnlyList<BusId> anyIds      = Grid.AnyDistinctBusIds(2);
        string               anyLineId   = AnyLine.Ids().Generate();
        double               anyKv       = Grid.AnyBaseKv();
        Bus                  firstSlack  = Grid.AnySlack(anyIds[0], anyKv).Generate();
        Bus                  secondSlack = Grid.AnySlack(anyIds[1], anyKv).Generate();
        Line                 anyLine     = Grid.AnyLineBetween(anyLineId, anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grid.AnyNetwork([firstSlack, secondSlack], [anyLine]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("exactly one slack bus; found 2", argument.Message);
    }

    [Fact]
    public void A_line_cannot_join_two_voltage_levels() {
        // Arrange
        IReadOnlyList<BusId> anyIds    = Grid.AnyDistinctBusIds(2);
        string               anyLineId = AnyLine.Ids().Generate();
        (double anyHighKv, double anyLowKv) = Grid.AnyTwoVoltageLevels();

        Bus  anySlack   = Grid.AnySlack(anyIds[0], anyHighKv).Generate();
        Bus  anyLoadBus = Grid.AnyLoadBus(anyIds[1], anyLowKv).Generate();
        Line anyLine    = Grid.AnyLineBetween(anyLineId, anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grid.AnyNetwork([anySlack, anyLoadBus], [anyLine]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("needs a transformer", argument.Message);
    }

    [Fact]
    public void A_transformer_mounted_upside_down_is_refused() {
        // Arrange
        IReadOnlyList<BusId> anyIds           = Grid.AnyDistinctBusIds(2);
        string               anyTransformerId = AnyLine.Ids().Generate();
        (double anyHighKv, double anyLowKv) = Grid.AnyTwoVoltageLevels();

        Bus         anySlackOnHighSide = Grid.AnySlack(anyIds[0], anyHighKv).Generate();
        Bus         anyLoadOnLowSide   = Grid.AnyLoadBus(anyIds[1], anyLowKv).Generate();
        Transformer upsideDown         = Grid.AnyTransformerBetween(anyTransformerId, highVoltage: anyIds[1], lowVoltage: anyIds[0]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grid.AnyNetwork([anySlackOnHighSide, anyLoadOnLowSide], [], [upsideDown]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("must be above its low-voltage side", argument.Message);
    }

    [Fact]
    public void A_bus_left_unconnected_islands_the_network() {
        // Arrange
        IReadOnlyList<BusId> anyIds    = Grid.AnyDistinctBusIds(3);
        string               anyLineId = AnyLine.Ids().Generate();
        double               anyKv     = Grid.AnyBaseKv();

        Bus  anySlack      = Grid.AnySlack(anyIds[0], anyKv).Generate();
        Bus  anyConnected  = Grid.AnyLoadBus(anyIds[1], anyKv).Generate();
        Bus  anyIsolated   = Grid.AnyLoadBus(anyIds[2], anyKv).Generate();
        Line anyOnlyLine   = Grid.AnyLineBetween(anyLineId, anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grid.AnyNetwork([anySlack, anyConnected, anyIsolated], [anyOnlyLine]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("islanded", argument.Message);
    }

    [Fact]
    public void A_line_and_a_transformer_cannot_share_a_branch_id() {
        // Arrange
        IReadOnlyList<BusId> anyIds         = Grid.AnyDistinctBusIds(3);
        string               anySharedId    = AnyLine.Ids().Generate();
        (double anyHighKv, double anyLowKv) = Grid.AnyTwoVoltageLevels();

        Bus         anySlack     = Grid.AnySlack(anyIds[0], anyHighKv).Generate();
        Bus         anyHighBus   = Grid.AnyLoadBus(anyIds[1], anyHighKv).Generate();
        Bus         anyLowBus    = Grid.AnyLoadBus(anyIds[2], anyLowKv).Generate();
        Line        anyLine      = Grid.AnyLineBetween(anySharedId, anyIds[0], anyIds[1]).Generate();
        Transformer anyTransformer = Grid.AnyTransformerBetween(anySharedId, anyIds[1], anyIds[2]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grid.AnyNetwork([anySlack, anyHighBus, anyLowBus], [anyLine], [anyTransformer]));

        // Assert
        ArgumentException argument = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("transformers", argument.ParamName);
        Assert.Contains("appears twice", argument.Message);
    }

    [Fact]
    public void Opening_the_only_line_to_a_bus_is_refused_and_leaves_it_in_service() {
        // Arrange
        IReadOnlyList<BusId> anyIds = Grid.AnyDistinctBusIds(2);
        double               anyKv  = Grid.AnyBaseKv();
        GridNetwork radial = Grid.AnyNetwork([Grid.AnySlack(anyIds[0], anyKv).Generate(), Grid.AnyLoadBus(anyIds[1], anyKv).Generate()],
                                             [Grid.AnyLineBetween("RADIAL", anyIds[0], anyIds[1]).Generate()]);

        // Act
        Exception? refusal = Record.Exception(() => radial.TakeLineOutOfService("RADIAL"));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.True(radial.GetLine("RADIAL").InService);
    }

    [Fact]
    public void One_of_two_parallel_lines_can_be_opened_and_then_returned_to_service() {
        // Arrange
        IReadOnlyList<BusId> anyIds = Grid.AnyDistinctBusIds(2);
        double               anyKv  = Grid.AnyBaseKv();
        GridNetwork doubleCircuit = Grid.AnyNetwork([Grid.AnySlack(anyIds[0], anyKv).Generate(), Grid.AnyLoadBus(anyIds[1], anyKv).Generate()],
                                                    [Grid.AnyLineBetween("CIRCUIT-1", anyIds[0], anyIds[1]).Generate(),
                                                     Grid.AnyLineBetween("CIRCUIT-2", anyIds[0], anyIds[1]).Generate()]);

        // Act
        doubleCircuit.TakeLineOutOfService("CIRCUIT-1");
        bool openedWhileOut = !doubleCircuit.GetLine("CIRCUIT-1").InService;
        doubleCircuit.ReturnLineToService("CIRCUIT-1");

        // Assert
        Assert.True(openedWhileOut);
        Assert.True(doubleCircuit.GetLine("CIRCUIT-1").InService);
    }

    [Fact]
    public void A_line_already_in_service_cannot_be_returned_to_service() {
        // Arrange
        IReadOnlyList<BusId> anyIds = Grid.AnyDistinctBusIds(2);
        double               anyKv  = Grid.AnyBaseKv();
        GridNetwork anyNetwork = Grid.AnyNetwork([Grid.AnySlack(anyIds[0], anyKv).Generate(), Grid.AnyLoadBus(anyIds[1], anyKv).Generate()],
                                                 [Grid.AnyLineBetween("IN-SERVICE", anyIds[0], anyIds[1]).Generate()]);

        // Act
        Exception? refusal = Record.Exception(() => anyNetwork.ReturnLineToService("IN-SERVICE"));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void Asking_for_a_bus_the_network_does_not_have_fails_with_key_not_found() {
        // Arrange
        BusId                missing   = new("MISSING");
        IReadOnlyList<BusId> anyIds    = Any.ListOf(Any.BusId().WithValue(v => v.DifferentFrom(missing.Value))).WithCount(2).Distinct().Generate();
        string               anyLineId = AnyLine.Ids().Generate();
        double               anyKv     = Grid.AnyBaseKv();
        GridNetwork anyNetwork = Grid.AnyNetwork([Grid.AnySlack(anyIds[0], anyKv).Generate(), Grid.AnyLoadBus(anyIds[1], anyKv).Generate()],
                                                 [Grid.AnyLineBetween(anyLineId, anyIds[0], anyIds[1]).Generate()]);

        // Act
        Exception? refusal = Record.Exception(() => anyNetwork.GetBus(missing));

        // Assert
        Assert.IsType<KeyNotFoundException>(refusal);
    }

    [Fact]
    public void The_slack_must_cover_all_load_less_what_the_other_generators_are_scheduled_to_produce() {
        // Arrange
        IReadOnlyList<BusId> anyIds     = Grid.AnyDistinctBusIds(3);
        IReadOnlyList<string> anyLineIds = Grid.AnyDistinctBranchIds(2);
        double               anyKv      = Grid.AnyBaseKv();

        // The slack's own generation is drawn: the demand it must meet does not depend on it.
        Bus slack     = Grid.AnySlack(anyIds[0], anyKv).WithLoad(new PowerInjection(10.0, 0.0)).Generate();
        Bus generator = Any.Bus().WithId(anyIds[1]).WithBaseKv(anyKv).WithType(BusType.Pv)
                                 .WithGeneration(new PowerInjection(20.0, 0.0))
                                 .WithLoad(new PowerInjection(30.0, 0.0))
                                 .Generate();
        Bus consumer  = Grid.AnyLoadBus(anyIds[2], anyKv).WithLoad(new PowerInjection(50.0, 0.0)).Generate();

        GridNetwork network = Grid.AnyNetwork([slack, generator, consumer],
                                              [Grid.AnyLineBetween(anyLineIds[0], anyIds[0], anyIds[1]).Generate(),
                                               Grid.AnyLineBetween(anyLineIds[1], anyIds[1], anyIds[2]).Generate()]);

        // Act
        double demand = network.SlackActiveDemandMw;

        // Assert
        Assert.Equal(70.0, demand, 9);
    }

}
