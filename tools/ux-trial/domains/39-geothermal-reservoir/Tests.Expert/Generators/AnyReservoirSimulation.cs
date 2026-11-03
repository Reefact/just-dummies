// Scaffolded by `any`, then rewritten by hand: wells must be completed inside THE grid that was drawn,
// one per cell, with distinct ids; nothing per-parameter can say that. The grid is drawn first, the wells
// are placed in it afterwards. It is ours; do not re-scaffold it with --force.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>A generator of arbitrary <see cref="ReservoirSimulation" /> values: 0 to 3 wells, each in a cell of its own.</summary>
public sealed partial class AnyReservoirSimulation : IAny<ReservoirSimulation> {

    private IAny<GeologicalGrid>    _grid;
    private IAny<BoundaryCondition> _boundary;
    private IAny<int>               _wellCount;
    private AnyWell                 _wellRecipe;
    private AnyGeologicalGrid       _gridRecipe;
    private AnyBoundaryCondition    _boundaryRecipe;
    private AnyInt32                _wellCountRecipe;

    public AnyReservoirSimulation()
        : this(new AnyGeologicalGrid(), new AnyBoundaryCondition(), Any.Int32().Between(0, 3), new AnyWell()) { }

    private AnyReservoirSimulation(AnyGeologicalGrid grid, AnyBoundaryCondition boundary, AnyInt32 wellCount, AnyWell well) {
        _grid            = grid;
        _gridRecipe      = grid;
        _boundary        = boundary;
        _boundaryRecipe  = boundary;
        _wellCount       = wellCount;
        _wellCountRecipe = wellCount;
        _wellRecipe      = well;
    }

    private AnyReservoirSimulation(AnyReservoirSimulation from) {
        _grid            = from._grid;
        _gridRecipe      = from._gridRecipe;
        _boundary        = from._boundary;
        _boundaryRecipe  = from._boundaryRecipe;
        _wellCount       = from._wellCount;
        _wellCountRecipe = from._wellCountRecipe;
        _wellRecipe      = from._wellRecipe;
    }

    public AnyReservoirSimulation WithGrid(Func<AnyGeologicalGrid, AnyGeologicalGrid> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._gridRecipe = configure(_gridRecipe);
        copy._grid       = copy._gridRecipe;
        return copy;
    }

    public AnyReservoirSimulation WithBoundary(Func<AnyBoundaryCondition, AnyBoundaryCondition> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._boundaryRecipe = configure(_boundaryRecipe);
        copy._boundary       = copy._boundaryRecipe;
        return copy;
    }

    public AnyReservoirSimulation WithBoundary(BoundaryCondition value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._boundary = Any.Fixed(value);
        return copy;
    }

    /// <summary>How many wells are completed; capped by the number of cells, since each takes a cell of its own.</summary>
    public AnyReservoirSimulation WithWellCount(int value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._wellCount = Any.Fixed(value);
        return copy;
    }

    public AnyReservoirSimulation WithWellCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._wellCountRecipe = configure(_wellCountRecipe);
        copy._wellCount       = copy._wellCountRecipe;
        return copy;
    }

    /// <summary>Refines the recipe EVERY well is drawn from. Its completion and its number are placed by the simulation.</summary>
    public AnyReservoirSimulation WithWell(Func<AnyWell, AnyWell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);
        copy._wellRecipe = configure(_wellRecipe);
        return copy;
    }

    public ReservoirSimulation Generate() {
        GeologicalGrid grid      = _grid.Generate();
        int            cellCount = grid.CellsX * grid.CellsY * grid.CellsZ;
        int            wellCount = Math.Min(_wellCount.Generate(), cellCount);

        List<int> cellIndexes = wellCount == 0
            ? new List<int>()
            : Any.ListOf(Any.Int32().Between(0, cellCount - 1)).WithCount(wellCount).Distinct().Generate();

        List<Well> wells = cellIndexes
                          .Select((cellIndex, number) => _wellRecipe
                                                        .WithCompletion(new CellCoordinate(cellIndex % grid.CellsX,
                                                                                           cellIndex / grid.CellsX % grid.CellsY,
                                                                                           cellIndex / (grid.CellsX * grid.CellsY)))
                                                        .WithNumber(number)
                                                        .Generate())
                          .ToList();

        return new ReservoirSimulation(grid, wells, _boundary.Generate());
    }

}
