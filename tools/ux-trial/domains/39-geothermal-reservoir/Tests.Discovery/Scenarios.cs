using GeothermalReservoir.Domain;

using JustDummies;

namespace GeothermalReservoir.Tests.Discovery;

/// <summary>
/// Reservoirs a time step can be taken in. A pressure drawn over the whole (0, 100 MPa] range next to a
/// neighbour drawn independently is a valid cell but not a reservoir one can march in time, so the
/// scenarios below state what "a working reservoir" means once.
/// </summary>
internal static class Scenarios {

    public static readonly CellCoordinate Origin = new CellCoordinate(0, 0, 0);

    public static AnyRockCell WorkingRock(AnyRockCell rock) {
        return rock.WithPorosity(p => p.WithFraction(f => f.Between(0.05, 0.5)))
                  .WithPressure(p => p.WithPascals(pa => pa.Between(10e6, 50e6)))
                  .WithTemperature(t => t.WithKelvin(k => k.Between(330.0, 500.0)));
    }

    /// <summary>Wells small enough to march with, a boundary at a working pressure and temperature.</summary>
    public static AnyReservoirSimulation AnyWorkingReservoir() {
        return Any.ReservoirSimulation()
                  .WithGrid(g => g.WithCellsX(x => x.Between(1, 3))
                                  .WithCellsY(y => y.Between(1, 3))
                                  .WithCellsZ(z => z.Between(1, 3))
                                  .WithCellSizeMetres(s => s.Between(5.0, 100.0))
                                  .WithCells(WorkingRock))
                  .WithWells(w => w.WithRateCubicMetresPerSecond(r => r.Between(1e-4, 1e-2)))
                  .WithBoundary(b => b.WithFarFieldPressure(p => p.WithPascals(pa => pa.Between(10e6, 50e6)))
                                      .WithFarFieldTemperature(t => t.WithKelvin(k => k.Between(330.0, 500.0))));
    }

    /// <summary>Half of the stability limit, and never more than the longest step a TimeStep can express.</summary>
    public static TimeStep SafeStepFor(ReservoirSimulation simulation) {
        return new TimeStep(Math.Min(simulation.MaximumStableTimeStepSeconds() / 2.0, TimeStep.MaximumSeconds));
    }

    public static Well InjectorAtOrigin(double rate, double injectionKelvin) {
        return Any.Well().WithKind(WellKind.Injector).WithId("01").WithCompletion(Origin)
                  .WithRateCubicMetresPerSecond(rate).WithInjectionTemperature(new Temperature(injectionKelvin)).Generate();
    }

    public static Well ProducerAtOrigin(double rate) {
        return Any.Well().WithKind(WellKind.Producer).WithId("01").WithCompletion(Origin)
                  .WithRateCubicMetresPerSecond(rate).Generate();
    }

    public static BoundaryCondition SealedBoundary() {
        return Any.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();
    }

    /// <summary>
    /// One 10 m cell of 20 % porosity at 20 MPa and 400 K: its storage is 3e-7 m³/Pa and its heat capacity
    /// 2.836e9 J/K, which is why the expected figures in the tests are what they are. Permeability and
    /// conductivity are drawn, since a lone sealed cell does not care about them.
    /// </summary>
    public static ReservoirSimulation LoneCell(Well? well, BoundaryCondition? boundary = null) {
        RockCell cell = Any.RockCell()
                           .WithCoordinate(Origin)
                           .WithPorosity(new Porosity(0.2))
                           .WithPressure(new Pressure(20e6))
                           .WithTemperature(new Temperature(400.0))
                           .Generate();
        GeologicalGrid grid = new GeologicalGrid(1, 1, 1, 10.0, [cell]);

        return new ReservoirSimulation(grid, well is null ? [] : [well], boundary ?? SealedBoundary());
    }

}
