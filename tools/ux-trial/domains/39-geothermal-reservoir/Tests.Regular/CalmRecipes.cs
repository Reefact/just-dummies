using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     Refinements that keep a drawn reservoir inside the physical envelope of the explicit scheme, so that a
///     test about stepping is not a test about an unphysical start. They narrow, never replace, the valid base.
/// </summary>
public static class CalmRecipes {

    public static AnyRockCell CalmRock(this AnyRockCell cell) {
        return cell.WithPorosity(p => p.WithFraction(f => f.Between(0.05, 0.3)))
                   .WithPermeability(k => k.WithSquareMetres(m => m.Between(1e-14, 1e-12)))
                   .WithPressure(p => p.WithPascals(pa => pa.Between(10e6, 30e6)))
                   .WithTemperature(t => t.WithKelvin(k => k.Between(350, 450)));
    }

    public static AnyGeologicalGrid Calm(this AnyGeologicalGrid grid) {
        return grid.WithCellSizeMetres(s => s.Between(20, 200)).WithCells(c => c.CalmRock());
    }

    public static AnyBoundaryCondition Calm(this AnyBoundaryCondition boundary) {
        return boundary.WithFarFieldPressure(p => p.WithPascals(pa => pa.Between(10e6, 30e6)))
                       .WithFarFieldTemperature(t => t.WithKelvin(k => k.Between(350, 450)));
    }

    /// <summary>A reservoir whose wells are weak enough for a handful of steps to stay inside the pressure range.</summary>
    public static AnyReservoirSimulation Calm(this AnyReservoirSimulation simulation) {
        return simulation.WithGrid(g => g.Calm())
                         .WithBoundary(b => b.Calm())
                         .WithWells(w => w.WithRateCubicMetresPerSecond(r => r.Between(1e-7, 1e-6))
                                          .WithInjectionTemperature(t => t.WithKelvin(k => k.Between(350, 450))));
    }

}
