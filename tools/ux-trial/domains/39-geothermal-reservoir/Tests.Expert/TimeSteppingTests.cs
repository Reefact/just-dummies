using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

using Xunit;

namespace GeothermalReservoir.Tests.Expert;

/// <summary>Advancing the explicit scheme: stability, physics and balances.</summary>
public sealed class TimeSteppingTests {

    private static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);

    [Fact]
    public void A_step_beyond_the_stability_limit_is_refused_and_nothing_moves() {
        // Arrange
        ReservoirSimulation anyField  = Reservoirs.ProductiveField().Generate();
        Pressure[]          before    = anyField.Grid.Cells.Select(cell => cell.Pressure).ToArray();
        TimeStep            tooLong   = new TimeStep(anyField.MaximumStableTimeStepSeconds() * 2);

        // Act
        Exception? refusal = Record.Exception(() => anyField.Advance(tooLong));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(0, anyField.StepsTaken);
        Assert.Equal(before, anyField.Grid.Cells.Select(cell => cell.Pressure));
    }

    [Fact]
    public void A_reservoir_at_rest_stays_at_rest() {
        // Arrange
        Pressure            anyPressure    = Any.Pressure().BetweenBars(100, 300).Generate();
        Temperature         anyTemperature = Any.Temperature().BetweenCelsius(60, 200).Generate();
        ReservoirSimulation anyRestingField = Reservoirs.ProductiveField()
                                                        .WithGrid(grid => Reservoirs.ProductiveGrid(grid)
                                                                              .WithRock(rock => Reservoirs.ProductiveRock(rock)
                                                                                                          .WithPressure(anyPressure)
                                                                                                          .WithTemperature(anyTemperature)))
                                                        .Generate();

        // Act
        anyRestingField.Advance(Reservoirs.StableStep(anyRestingField));

        // Assert
        Assert.All(anyRestingField.Grid.Cells, cell => {
            Assert.Equal(anyPressure.Pascals, cell.Pressure.Pascals, 1e-6);
            Assert.Equal(anyTemperature.Kelvin, cell.Temperature.Kelvin, 1e-9);
        });
    }

    [Fact]
    public void Injection_raises_the_pressure_of_the_completion_cell() {
        // Arrange
        Well                anyInjector = Any.Well().Injector().WithCompletion(Origin).WithRateCubicMetresPerSecond(Reservoirs.ModestRate).Generate();
        ReservoirSimulation anyField    = AtRest().WithWells(anyInjector).Generate();
        Pressure            initial     = anyField.Grid.CellAt(Origin).Pressure;

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.True(anyField.Grid.CellAt(Origin).Pressure.CompareTo(initial) > 0);
    }

    [Fact]
    public void Production_lowers_the_pressure_of_the_completion_cell() {
        // Arrange
        Well                anyProducer = Any.Well().Producer().WithCompletion(Origin).WithRateCubicMetresPerSecond(Reservoirs.ModestRate).Generate();
        ReservoirSimulation anyField    = AtRest().WithWells(anyProducer).Generate();
        Pressure            initial     = anyField.Grid.CellAt(Origin).Pressure;

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.True(anyField.Grid.CellAt(Origin).Pressure.CompareTo(initial) < 0);
    }

    [Fact]
    public void Cold_reinjection_cools_the_rock_around_the_injector() {
        // Arrange
        Well anyColdInjector = Any.Well()
                                  .Injector()
                                  .WithCompletion(Origin)
                                  .WithRateCubicMetresPerSecond(Reservoirs.ModestRate)
                                  .WithInjectionTemperature(Temperature.FromCelsius(30))
                                  .Generate();
        ReservoirSimulation anyField = AtRest().WithWells(anyColdInjector).Generate();
        Temperature         initial  = anyField.Grid.CellAt(Origin).Temperature;

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.True(anyField.Grid.CellAt(Origin).Temperature.CompareTo(initial) < 0);
    }

    [Fact]
    public void Heat_conducts_from_the_hot_cell_to_the_cold_one() {
        // Arrange
        ReservoirSimulation anyField = Reservoirs.ProductiveField()
                                                 .WithGrid(grid => Reservoirs.ProductiveGrid(grid)
                                                                       .WithDimensions(2, 1, 1)
                                                                       .WithRock((coordinate, rock) => Reservoirs.ProductiveRock(rock)
                                                                                                                 .WithPressure(Pressure.FromBars(200))
                                                                                                                 .WithTemperature(Temperature.FromCelsius(coordinate.I == 0 ? 180 : 80))))
                                                 .Generate();
        RockCell hot  = anyField.Grid.CellAt(Origin);
        RockCell cold = anyField.Grid.CellAt(new CellCoordinate(1, 0, 0));

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.True(hot.Temperature.Celsius < 180);
        Assert.True(cold.Temperature.Celsius > 80);
    }

    [Fact]
    public void A_shut_in_injector_injects_nothing() {
        // Arrange
        Well                anyInjector = Any.Well().Injector().WithCompletion(Origin).WithRateCubicMetresPerSecond(Reservoirs.ModestRate).Generate();
        ReservoirSimulation anyField    = AtRest().WithWells(anyInjector).Generate();
        anyField.ShutIn(anyInjector.Id);

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.Equal(0.0, anyField.CumulativeInjectedCubicMetres);
    }

    [Fact]
    public void The_injected_volume_is_the_rate_times_the_elapsed_time() {
        // Arrange
        Well                anyInjector = Any.Well().Injector().WithCompletion(Origin).WithRateCubicMetresPerSecond(0.01).Generate();
        ReservoirSimulation anyField    = AtRest().WithWells(anyInjector).Generate();
        TimeStep            anyStep     = Reservoirs.StableStep(anyField);

        // Act
        anyField.Advance(anyStep);
        anyField.Advance(anyStep);
        anyField.Advance(anyStep);

        // Assert
        Assert.Equal(3, anyField.StepsTaken);
        Assert.Equal(0.01 * anyField.ElapsedSeconds, anyField.CumulativeInjectedCubicMetres, 1e-12);
    }

    [Fact]
    public void A_drawn_down_reservoir_is_recharged_by_a_higher_pressure_aquifer() {
        // Arrange
        ReservoirSimulation anyField = Reservoirs.ProductiveField()
                                                 .WithGrid(grid => Reservoirs.ProductiveGrid(grid)
                                                                       .WithRock(rock => Reservoirs.ProductiveRock(rock).WithPressure(Pressure.FromBars(150))))
                                                 .WithBoundary(boundary => boundary.WithFarFieldPressure(Pressure.FromBars(200))
                                                                                   .WithTransmissibilityMultiplier(multiplier => multiplier.GreaterThan(0.0)))
                                                 .Generate();

        // Act
        anyField.Advance(Reservoirs.StableStep(anyField));

        // Assert
        Assert.True(anyField.CumulativeBoundaryInflowCubicMetres > 0);
    }

    [Fact]
    public void Every_cubic_metre_is_accounted_for_while_the_field_operates() {
        // Arrange
        List<CellCoordinate> anyTwoCells = Any.ListOf(Any.ElementOf(Coordinates(2, 2, 2))).WithCount(2).Distinct().Generate();
        Well anyInjector = Any.Well().Injector().WithSerial(1).WithCompletion(anyTwoCells[0]).WithRateCubicMetresPerSecond(Reservoirs.ModestRate).Generate();
        Well anyProducer = Any.Well().Producer().WithSerial(1).WithCompletion(anyTwoCells[1]).WithRateCubicMetresPerSecond(Reservoirs.ModestRate).Generate();
        ReservoirSimulation anyField = Reservoirs.ProductiveField()
                                                 .WithGrid(grid => Reservoirs.ProductiveGrid(grid).WithDimensions(2, 2, 2))
                                                 .WithWells(anyInjector, anyProducer)
                                                 .WithBoundary(boundary => boundary.WithFarFieldPressure(pressure => pressure.BetweenBars(100, 300))
                                                                                   .WithFarFieldTemperature(temperature => temperature.BetweenCelsius(60, 200)))
                                                 .Generate();

        // Act
        for (int step = 0; step < 5; step++) {
            anyField.Advance(Reservoirs.StableStep(anyField));
        }

        // Assert
        double throughput = anyField.CumulativeInjectedCubicMetres + anyField.CumulativeProducedCubicMetres;
        Assert.True(Math.Abs(anyField.MassBalanceErrorCubicMetres()) <= 1e-9 * throughput,
                    $"Mass balance error of {anyField.MassBalanceErrorCubicMetres()} m³ for {throughput} m³ moved.");
    }

    [Fact]
    public void A_producer_cannot_drain_a_sealed_pocket_below_zero_pressure_and_nothing_moves() {
        // Arrange
        Well anyProducer = Any.Well().Producer().WithCompletion(Origin).WithRateCubicMetresPerSecond(0.05).Generate();
        ReservoirSimulation anySealedPocket = Reservoirs.ProductiveField()
                                                        .WithGrid(grid => Reservoirs.ProductiveGrid(grid).WithDimensions(1, 1, 1))
                                                        .WithWells(anyProducer)
                                                        .Generate();
        Pressure initial = anySealedPocket.Grid.CellAt(Origin).Pressure;

        // Act
        Exception? refusal = Record.Exception(() => anySealedPocket.Advance(new TimeStep(TimeStep.MaximumSeconds)));

        // Assert
        Assert.IsType<InvalidOperationException>(refusal);
        Assert.Equal(initial, anySealedPocket.Grid.CellAt(Origin).Pressure);
        Assert.Equal(0.0, anySealedPocket.ElapsedSeconds);
    }

    /// <summary>A productive field whose cells all share one drawn pressure and temperature.</summary>
    private static AnyReservoirSimulation AtRest() {
        Pressure    anyPressure    = Any.Pressure().BetweenBars(100, 300).Generate();
        Temperature anyTemperature = Any.Temperature().BetweenCelsius(60, 200).Generate();

        return Reservoirs.ProductiveField()
                         .WithGrid(grid => Reservoirs.ProductiveGrid(grid)
                                               .WithRock(rock => Reservoirs.ProductiveRock(rock)
                                                                           .WithPressure(anyPressure)
                                                                           .WithTemperature(anyTemperature)));
    }

    private static List<CellCoordinate> Coordinates(int cellsX, int cellsY, int cellsZ) {
        List<CellCoordinate> coordinates = [];
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    coordinates.Add(new CellCoordinate(i, j, k));
                }
            }
        }

        return coordinates;
    }

}
