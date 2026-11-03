// Scaffolded by `any`, then rewritten by hand: the scaffold drew `cells` independently of the dimensions,
// which the constructor refuses on almost every draw. This generator draws the dimensions first and then
// one cell per coordinate. It is ours; do not re-scaffold it with --force.

using System;
using System.Collections.Generic;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeologicalGrid" /> values: 1 to 4 cells along each axis,
///     completely filled, every cell drawn from one recipe and placed at its own coordinate.
/// </summary>
public sealed partial class AnyGeologicalGrid : IAny<GeologicalGrid> {

    private IAny<int>    _cellsX;
    private IAny<int>    _cellsY;
    private IAny<int>    _cellsZ;
    private IAny<double> _cellSizeMetres;

    private AnyInt32   _cellsXRecipe;
    private AnyInt32   _cellsYRecipe;
    private AnyInt32   _cellsZRecipe;
    private AnyDouble  _cellSizeMetresRecipe;
    private AnyRockCell _cellRecipe;

    public AnyGeologicalGrid()
        : this(Any.Int32().Between(1, 4), Any.Int32().Between(1, 4), Any.Int32().Between(1, 4),
               Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres),
               new AnyRockCell()) { }

    private AnyGeologicalGrid(AnyInt32 cellsX, AnyInt32 cellsY, AnyInt32 cellsZ, AnyDouble cellSizeMetres, AnyRockCell cell) {
        _cellsX               = cellsX;
        _cellsXRecipe         = cellsX;
        _cellsY               = cellsY;
        _cellsYRecipe         = cellsY;
        _cellsZ               = cellsZ;
        _cellsZRecipe         = cellsZ;
        _cellSizeMetres       = cellSizeMetres;
        _cellSizeMetresRecipe = cellSizeMetres;
        _cellRecipe           = cell;
    }

    private AnyGeologicalGrid(AnyGeologicalGrid from) {
        _cellsX               = from._cellsX;
        _cellsXRecipe         = from._cellsXRecipe;
        _cellsY               = from._cellsY;
        _cellsYRecipe         = from._cellsYRecipe;
        _cellsZ               = from._cellsZ;
        _cellsZRecipe         = from._cellsZRecipe;
        _cellSizeMetres       = from._cellSizeMetres;
        _cellSizeMetresRecipe = from._cellSizeMetresRecipe;
        _cellRecipe           = from._cellRecipe;
    }

    public AnyGeologicalGrid WithCellsX(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellsX = Any.Fixed(value);
        return copy;
    }

    public AnyGeologicalGrid WithCellsY(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellsY = Any.Fixed(value);
        return copy;
    }

    public AnyGeologicalGrid WithCellsZ(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellsZ = Any.Fixed(value);
        return copy;
    }

    /// <summary>Pins the three dimensions at once.</summary>
    public AnyGeologicalGrid WithDimensions(int cellsX, int cellsY, int cellsZ) {
        return WithCellsX(cellsX).WithCellsY(cellsY).WithCellsZ(cellsZ);
    }

    public AnyGeologicalGrid WithCellSizeMetres(double value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellSizeMetres = Any.Fixed(value);
        return copy;
    }

    public AnyGeologicalGrid WithCellSizeMetres(Func<AnyDouble, AnyDouble> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellSizeMetresRecipe = configure(_cellSizeMetresRecipe);
        copy._cellSizeMetres       = copy._cellSizeMetresRecipe;
        return copy;
    }

    /// <summary>Refines the recipe EVERY cell is drawn from. Its coordinate is not ours to refine: the grid sets it.</summary>
    public AnyGeologicalGrid WithCell(Func<AnyRockCell, AnyRockCell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);
        copy._cellRecipe = configure(_cellRecipe);
        return copy;
    }

    public GeologicalGrid Generate() {
        int    cellsX = _cellsX.Generate();
        int    cellsY = _cellsY.Generate();
        int    cellsZ = _cellsZ.Generate();
        double size   = _cellSizeMetres.Generate();

        List<RockCell> cells = new List<RockCell>(cellsX * cellsY * cellsZ);
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    cells.Add(_cellRecipe.WithCoordinate(new CellCoordinate(i, j, k)).Generate());
                }
            }
        }

        return new GeologicalGrid(cellsX, cellsY, cellsZ, size, cells);
    }

}
