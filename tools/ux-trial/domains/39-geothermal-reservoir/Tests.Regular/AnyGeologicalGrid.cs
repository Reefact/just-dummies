// Scaffolded by `any` (JustDummies), then completed by hand: the constructor ties the cell list to the
// three dimensions (cells.Count == cellsX * cellsY * cellsZ, each coordinate present once), which no
// per-parameter recipe can express. The cells are therefore no longer a parameter of the generator: they
// are derived from the dimensions, one RockCell per coordinate.

using System;
using System.Collections.Generic;

using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed partial class AnyGeologicalGrid : IAny<GeologicalGrid> {

    private IAny<int>    _cellsX;
    private IAny<int>    _cellsY;
    private IAny<int>    _cellsZ;
    private IAny<double> _cellSizeMetres;

    private AnyInt32  _cellsXRecipe;
    private AnyInt32  _cellsYRecipe;
    private AnyInt32  _cellsZRecipe;
    private AnyDouble _cellSizeMetresRecipe;

    private Func<CellCoordinate, AnyRockCell, AnyRockCell> _cellRefinement = (_, cell) => cell;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeologicalGrid()
        : this(cellsX:         AnyValidCellsX(),
               cellsY:         AnyValidCellsY(),
               cellsZ:         AnyValidCellsZ(),
               cellSizeMetres: AnyValidCellSizeMetres()) { }

    // Small grids: every test walks every cell, and a dummy grid has no reason to be big.
    private static AnyInt32 AnyValidCellsX() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsY() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsZ() {
        return Any.Int32().Between(1, 4);
    }

    private static AnyDouble AnyValidCellSizeMetres() {
        return Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres);
    }

    private AnyGeologicalGrid(AnyInt32  cellsX,
                              AnyInt32  cellsY,
                              AnyInt32  cellsZ,
                              AnyDouble cellSizeMetres) {
        _cellsX               = cellsX;
        _cellsXRecipe         = cellsX;
        _cellsY               = cellsY;
        _cellsYRecipe         = cellsY;
        _cellsZ               = cellsZ;
        _cellsZRecipe         = cellsZ;
        _cellSizeMetres       = cellSizeMetres;
        _cellSizeMetresRecipe = cellSizeMetres;
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
        _cellRefinement       = from._cellRefinement;
    }

    public AnyGeologicalGrid WithCellsX(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsX = Any.Fixed(value);

        return copy;
    }

    public AnyGeologicalGrid WithCellsX(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsXRecipe = configure(_cellsXRecipe);
        copy._cellsX       = copy._cellsXRecipe;

        return copy;
    }

    public AnyGeologicalGrid WithCellsY(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsY = Any.Fixed(value);

        return copy;
    }

    public AnyGeologicalGrid WithCellsY(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsYRecipe = configure(_cellsYRecipe);
        copy._cellsY       = copy._cellsYRecipe;

        return copy;
    }

    public AnyGeologicalGrid WithCellsZ(int value) {
        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsZ = Any.Fixed(value);

        return copy;
    }

    public AnyGeologicalGrid WithCellsZ(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid copy = new AnyGeologicalGrid(this);

        copy._cellsZRecipe = configure(_cellsZRecipe);
        copy._cellsZ       = copy._cellsZRecipe;

        return copy;
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

    /// <summary>Refines every cell the same way; the coordinate stays the one the grid assigns.</summary>
    public AnyGeologicalGrid WithCells(Func<AnyRockCell, AnyRockCell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        return WithCells((_, cell) => configure(cell));
    }

    /// <summary>Refines each cell knowing where it sits, e.g. to give the grid a thermal gradient.</summary>
    public AnyGeologicalGrid WithCells(Func<CellCoordinate, AnyRockCell, AnyRockCell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyGeologicalGrid                                   copy     = new AnyGeologicalGrid(this);
        Func<CellCoordinate, AnyRockCell, AnyRockCell> previous = _cellRefinement;

        copy._cellRefinement = (coordinate, cell) => configure(coordinate, previous(coordinate, cell));

        return copy;
    }

    public GeologicalGrid Generate() {
        int nx = _cellsX.Generate();
        int ny = _cellsY.Generate();
        int nz = _cellsZ.Generate();

        List<RockCell> cells = new List<RockCell>(nx * ny * nz);
        for (int i = 0; i < nx; i++) {
            for (int j = 0; j < ny; j++) {
                for (int k = 0; k < nz; k++) {
                    CellCoordinate coordinate = new CellCoordinate(i, j, k);
                    cells.Add(_cellRefinement(coordinate, new AnyRockCell().WithCoordinate(coordinate)).Generate());
                }
            }
        }

        return new GeologicalGrid(nx, ny, nz, _cellSizeMetres.Generate(), cells);
    }

}
