using GeothermalReservoir.Domain;

namespace GeothermalReservoir.Tests.Regular;

/// <summary>
///     Recipes for a permeable sandstone reservoir of hectometric cells: the regime this team simulates,
///     in which one hour is a stable explicit step and the wells' rates move pressures by a few bars.
/// </summary>
internal static class Sandstone {

    public const double OneHour = 3600.0;

    public static readonly Func<AnyRockCell, AnyRockCell> Cell =
        c => c.WithPorosity(p => p.WithFraction(f => f.Between(0.1, 0.3)))
              .WithPermeability(k => k.WithSquareMetres(m => m.Between(1e-15, 5e-15)))
              .WithPressure(p => p.WithPascals(pa => pa.Between(150e5, 250e5)))
              .WithTemperature(t => t.WithKelvin(k => k.Between(333.15, 473.15)));

    /// <summary>A grid of the given shape whose cells all start at the same pressure and temperature.</summary>
    public static AnyGeologicalGrid UniformGrid(int cellsX, int cellsY, int cellsZ, Pressure pressure, Temperature temperature) {
        return Grid(cellsX, cellsY, cellsZ).WithCells(c => Cell(c).WithPressure(pressure).WithTemperature(temperature));
    }

    public static AnyGeologicalGrid Grid(int cellsX, int cellsY, int cellsZ) {
        return Arbitrary.GeologicalGrid()
                        .WithCellsX(cellsX)
                        .WithCellsY(cellsY)
                        .WithCellsZ(cellsZ)
                        .WithCellSizeMetres(s => s.Between(100.0, 200.0))
                        .WithCells(Cell);
    }

    public static BoundaryCondition Sealed() {
        return Arbitrary.BoundaryCondition().WithTransmissibilityMultiplier(0.0).Generate();
    }

}
