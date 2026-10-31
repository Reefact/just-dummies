using GeothermalReservoir.Domain;
using GeothermalReservoir.Tests.Expert.Arbitrary;

using JustDummies;

namespace GeothermalReservoir.Tests.Expert;

/// <summary>
///     Recipes for the reservoirs this team simulates. They narrow the domain's ranges to the envelope of a
///     producing geothermal field, the precondition of every time-stepping test; none of them is chosen to
///     make an assertion pass.
/// </summary>
internal static class Reservoirs {

    /// <summary>Permeable, water-saturated, hot rock at a few hundred bars.</summary>
    public static AnyRockCell ProductiveRock(AnyRockCell rock) {
        return rock.WithPorosity(porosity => porosity.WithFraction(fraction => fraction.Between(0.05, 0.3)))
                   .WithPermeability(permeability => permeability.WithSquareMetres(k => k.Between(1e-12, 1e-11)))
                   .WithPressure(pressure => pressure.BetweenBars(100, 300))
                   .WithTemperature(temperature => temperature.BetweenCelsius(60, 200));
    }

    /// <summary>A few cubic hectometres of productive rock, at least two cells so that fluid can move.</summary>
    public static AnyGeologicalGrid ProductiveGrid(AnyGeologicalGrid grid) {
        return grid.WithCellsX(cellsX => cellsX.GreaterThanOrEqualTo(2))
                   .WithCellSizeMetres(size => size.Between(10, 100))
                   .WithRock(ProductiveRock);
    }

    /// <summary>A well rate a cell of productive rock sustains over a few steps.</summary>
    public static AnyDouble ModestRate(AnyDouble rate) {
        return rate.LessThanOrEqualTo(0.02);
    }

    /// <summary>A productive field with no wells and a sealed boundary, unless the test says otherwise.</summary>
    public static AnyReservoirSimulation ProductiveField() {
        return Any.ReservoirSimulation()
                  .WithGrid(ProductiveGrid)
                  .WithWells()
                  .WithBoundary(boundary => boundary.WithTransmissibilityMultiplier(0.0));
    }

    /// <summary>Half the explicit scheme's stability limit: a step the simulation must accept.</summary>
    public static TimeStep StableStep(ReservoirSimulation simulation) {
        return new TimeStep(simulation.MaximumStableTimeStepSeconds() / 2);
    }

}
