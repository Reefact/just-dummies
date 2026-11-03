// Scaffolded by `any` (JustDummies), then completed by hand: the wells are not drawn as a free list. A simulation
// requires each well inside the grid, one per cell, with unique ids, so Generate() draws the grid first and
// then places 0..3 wells on distinct coordinates of the drawn grid.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed partial class AnyReservoirSimulation : IAny<ReservoirSimulation> {

    private IAny<GeologicalGrid>    _grid;
    private IAny<int>               _wellCount;
    private AnyWell                 _wellRecipe;
    private IAny<BoundaryCondition> _boundary;

    private AnyGeologicalGrid    _gridRecipe;
    private AnyInt32             _wellCountRecipe;
    private AnyBoundaryCondition _boundaryRecipe;

    public AnyReservoirSimulation()
        : this(grid:      new AnyGeologicalGrid(),
               wellCount: Any.Int32().Between(0, 3),
               well:      new AnyWell(),
               boundary:  new AnyBoundaryCondition()) { }

    private AnyReservoirSimulation(AnyGeologicalGrid grid, AnyInt32 wellCount, AnyWell well, AnyBoundaryCondition boundary) {
        _grid            = grid;
        _gridRecipe      = grid;
        _wellCount       = wellCount;
        _wellCountRecipe = wellCount;
        _wellRecipe      = well;
        _boundary        = boundary;
        _boundaryRecipe  = boundary;
    }

    private AnyReservoirSimulation(AnyReservoirSimulation from) {
        _grid            = from._grid;
        _gridRecipe      = from._gridRecipe;
        _wellCount       = from._wellCount;
        _wellCountRecipe = from._wellCountRecipe;
        _wellRecipe      = from._wellRecipe;
        _boundary        = from._boundary;
        _boundaryRecipe  = from._boundaryRecipe;
    }

    public AnyReservoirSimulation WithGrid(GeologicalGrid value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._grid = Any.Fixed(value);

        return copy;
    }

    public AnyReservoirSimulation WithGrid(Func<AnyGeologicalGrid, AnyGeologicalGrid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._gridRecipe = configure(_gridRecipe);
        copy._grid       = copy._gridRecipe;

        return copy;
    }

    /// <summary>Pins how many wells are placed (capped by the number of cells of the drawn grid).</summary>
    public AnyReservoirSimulation WithWellCount(int value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._wellCount = Any.Fixed(value);

        return copy;
    }

    public AnyReservoirSimulation WithoutWells() {
        return WithWellCount(0);
    }

    /// <summary>Refines every well placed; the completion and the number of the id are set by this generator.</summary>
    public AnyReservoirSimulation WithWells(Func<AnyWell, AnyWell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._wellRecipe = configure(_wellRecipe);

        return copy;
    }

    public AnyReservoirSimulation WithBoundary(BoundaryCondition value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._boundary = Any.Fixed(value);

        return copy;
    }

    public AnyReservoirSimulation WithBoundary(Func<AnyBoundaryCondition, AnyBoundaryCondition> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._boundaryRecipe = configure(_boundaryRecipe);
        copy._boundary       = copy._boundaryRecipe;

        return copy;
    }

    public ReservoirSimulation Generate() {
        GeologicalGrid grid = _grid.Generate();

        List<CellCoordinate> coordinates = grid.Cells.Select(c => c.Coordinate).ToList();
        List<CellCoordinate> shuffled    = Any.PermutationOf(coordinates).Generate();
        int                  wellCount   = Math.Min(_wellCount.Generate(), shuffled.Count);

        List<Well> wells = new List<Well>();
        for (int n = 0; n < wellCount; n++) {
            wells.Add(_wellRecipe.WithCompletion(shuffled[n]).WithId(n.ToString("00")).Generate());
        }

        return new ReservoirSimulation(grid, wells, _boundary.Generate());
    }

}
