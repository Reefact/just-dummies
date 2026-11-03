using GeothermalReservoir.Domain;

using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Support;

internal static class Scenarios {

    /// <summary>
    ///     A reservoir in its comfort zone: cells of 100 m or more, porosity of 5 % or more, pore pressure between
    ///     200 and 700 bar, wells of at most 0.05 m3/s. Explicit steps of at most an hour then never push a cell out of
    ///     the pressure range: the premise of a test about what a step conserves, not about what it refuses.
    /// </summary>
    public static AnyReservoirSimulation Gentle() {
        return Any.ReservoirSimulation()
                  .WithGrid(grid => grid.WithCellSizeMetres(size => size.Between(100.0, 1000.0))
                                        .WithCell(cell => cell.WithPorosity(porosity => porosity.WithFraction(fraction => fraction.GreaterThanOrEqualTo(0.05)))
                                                              .WithPressure(pressure => pressure.WithPascals(pascals => pascals.Between(20e6, 70e6)))))
                  .WithWell(well => well.WithRate(rate => rate.GreaterThanOrEqualTo(0.001).LessThanOrEqualTo(0.05)));
    }

    /// <summary>Half of the stable step, but never more than an hour: stable, and gentle on the pressure range.</summary>
    public static TimeStep SafeStepFor(ReservoirSimulation simulation) {
        return new TimeStep(Math.Min(simulation.MaximumStableTimeStepSeconds() / 2.0, 3600.0));
    }

    public static ReservoirSimulation SimulationOver(GeologicalGrid grid, params Well[] wells) {
        return new ReservoirSimulation(grid, wells, Any.BoundaryCondition().Generate());
    }

}
