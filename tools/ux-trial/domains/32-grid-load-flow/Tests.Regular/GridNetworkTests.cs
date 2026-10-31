using System.Collections.Generic;

using GridLoadFlow.Domain;

using Xunit;

namespace GridLoadFlow.Tests.Regular;

public sealed class GridNetworkTests {

    [Fact]
    public void A_network_needs_exactly_one_slack_bus() {
        // Arrange
        List<BusId>  anyIds      = Grids.AnyDistinctBusIds(2);
        double       anyBaseKv   = Grids.AnyBaseKv();
        List<string> anyLineIds  = Grids.AnyDistinctBranchIds(1);
        Bus          anySlack    = Grids.Slack(anyIds[0], anyBaseKv).Generate();
        Bus          anyOtherSlack = Grids.Slack(anyIds[1], anyBaseKv).Generate();
        Line         anyLine     = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grids.Network(Grids.AnyBaseMva(), [anySlack, anyOtherSlack], [anyLine]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("exactly one slack bus; found 2", argumentException.Message);
    }

    [Fact]
    public void A_line_cannot_join_two_voltage_levels() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(2);
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Bus          anySlack   = Grids.Slack(anyIds[0], 225.0).Generate();
        Bus          anyLoad    = Grids.Load(anyIds[1], 63.0).Generate();
        Line         anyLine    = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grids.Network(Grids.AnyBaseMva(), [anySlack, anyLoad], [anyLine]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("needs a transformer", argumentException.Message);
    }

    [Fact]
    public void A_transformer_high_voltage_side_must_be_the_higher_level() {
        // Arrange
        List<BusId>  anyIds        = Grids.AnyDistinctBusIds(2);
        List<string> anyBranchIds  = Grids.AnyDistinctBranchIds(1);
        Bus          anySlack      = Grids.Slack(anyIds[0], 63.0).Generate();
        Bus          anyLoad       = Grids.Load(anyIds[1], 225.0).Generate();
        Transformer  anyTransformer = Arbitrary.Transformer().WithId(anyBranchIds[0]).WithHighVoltageBus(anyIds[0]).WithLowVoltageBus(anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grids.Network(Grids.AnyBaseMva(), [anySlack, anyLoad], [], [anyTransformer]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("must be above its low-voltage side", argumentException.Message);
    }

    [Fact]
    public void A_bus_unreachable_from_the_slack_makes_the_network_islanded() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(3);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(1);
        Bus          anySlack   = Grids.Slack(anyIds[0], anyBaseKv).Generate();
        Bus          anyFed     = Grids.Load(anyIds[1], anyBaseKv).Generate();
        Bus          anyIsolated = Grids.Load(anyIds[2], anyBaseKv).Generate();
        Line         anyLine    = Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate();

        // Act
        Exception? refusal = Record.Exception(() => Grids.Network(Grids.AnyBaseMva(), [anySlack, anyFed, anyIsolated], [anyLine]));

        // Assert
        ArgumentException argumentException = Assert.IsType<ArgumentException>(refusal);
        Assert.Contains("islanded", argumentException.Message);
    }

    [Fact]
    public void A_radial_line_cannot_be_taken_out_of_service() {
        // Arrange
        RadialPair  anyGrid = RadialPair.Build(parallelLines: 1);

        // Act
        Exception? refusal = Record.Exception(() => anyGrid.Network.TakeLineOutOfService(anyGrid.LineIds[0]));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.True(anyGrid.Network.GetLine(anyGrid.LineIds[0]).InService);
    }

    [Fact]
    public void One_of_two_parallel_lines_can_be_taken_out_of_service() {
        // Arrange
        RadialPair anyGrid = RadialPair.Build(parallelLines: 2);

        // Act
        anyGrid.Network.TakeLineOutOfService(anyGrid.LineIds[0]);

        // Assert
        Assert.False(anyGrid.Network.GetLine(anyGrid.LineIds[0]).InService);
        Assert.True(anyGrid.Network.GetLine(anyGrid.LineIds[1]).InService);
    }

    [Fact]
    public void A_line_already_in_service_cannot_be_returned_to_service() {
        // Arrange
        RadialPair anyGrid = RadialPair.Build(parallelLines: 2);

        // Act
        Exception? refusal = Record.Exception(() => anyGrid.Network.ReturnLineToService(anyGrid.LineIds[0]));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
    }

    [Fact]
    public void An_unknown_bus_is_reported_as_missing() {
        // Arrange
        RadialPair anyGrid    = RadialPair.Build(parallelLines: 1);
        BusId      anyUnknown = Arbitrary.BusId().WithValue(v => v.Except(anyGrid.SlackId.Value, anyGrid.LoadId.Value)).Generate();

        // Act
        Exception? refusal = Record.Exception(() => anyGrid.Network.GetBus(anyUnknown));

        // Assert
        Assert.IsType<KeyNotFoundException>(refusal);
    }

    [Fact]
    public void The_slack_must_cover_all_load_less_the_other_scheduled_generation() {
        // Arrange
        List<BusId>  anyIds     = Grids.AnyDistinctBusIds(3);
        double       anyBaseKv  = Grids.AnyBaseKv();
        List<string> anyLineIds = Grids.AnyDistinctBranchIds(2);
        Bus slack = Grids.Slack(anyIds[0], anyBaseKv).WithGeneration(new PowerInjection(500.0, 0.0)).WithLoad(new PowerInjection(10.0, 0.0)).Generate();
        Bus generator = Arbitrary.Bus().WithId(anyIds[1]).WithType(BusType.Pv).WithBaseKv(anyBaseKv)
                                 .WithGeneration(new PowerInjection(30.0, 0.0)).WithLoad(new PowerInjection(5.0, 0.0)).Generate();
        Bus load = Grids.Load(anyIds[2], anyBaseKv).WithLoad(new PowerInjection(60.0, 25.0)).Generate();
        GridNetwork network = Grids.Network(Grids.AnyBaseMva(),
                                            [slack, generator, load],
                                            [Grids.Line(anyLineIds[0], anyIds[0], anyIds[1]).Generate(), Grids.Line(anyLineIds[1], anyIds[0], anyIds[2]).Generate()]);

        // Act
        double slackDemandMw = network.SlackActiveDemandMw;

        // Assert
        Assert.Equal(45.0, slackDemandMw, 9);
    }
}
