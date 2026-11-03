// Scaffolded by `any` (JustDummies), then completed by hand: the wells must be completed inside the grid that
// is drawn, in distinct cells, with distinct ids. They cannot be drawn independently of the grid, so the
// generator draws the grid first and then places 0..3 wells in it.

using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

namespace GeothermalReservoir.Domain;

public sealed partial class AnyReservoirSimulation : IAny<ReservoirSimulation> {

    private IAny<GeologicalGrid>    _grid;
    private IAny<BoundaryCondition> _boundary;
    private IAny<int>               _wellCount;

    private AnyGeologicalGrid    _gridRecipe;
    private AnyBoundaryCondition _boundaryRecipe;
    private AnyInt32             _wellCountRecipe;

    private IReadOnlyCollection<Well>? _pinnedWells;
    private Func<AnyWell, AnyWell>     _wellRefinement = well => well;

    public AnyReservoirSimulation()
        : this(grid:      new AnyGeologicalGrid(),
               boundary:  new AnyBoundaryCondition(),
               wellCount: Any.Int32().Between(0, 3)) { }

    private AnyReservoirSimulation(AnyGeologicalGrid    grid,
                                   AnyBoundaryCondition boundary,
                                   AnyInt32             wellCount) {
        _grid            = grid;
        _gridRecipe      = grid;
        _boundary        = boundary;
        _boundaryRecipe  = boundary;
        _wellCount       = wellCount;
        _wellCountRecipe = wellCount;
    }

    private AnyReservoirSimulation(AnyReservoirSimulation from) {
        _grid            = from._grid;
        _gridRecipe      = from._gridRecipe;
        _boundary        = from._boundary;
        _boundaryRecipe  = from._boundaryRecipe;
        _wellCount       = from._wellCount;
        _wellCountRecipe = from._wellCountRecipe;
        _pinnedWells     = from._pinnedWells;
        _wellRefinement  = from._wellRefinement;
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

    /// <summary>Pins the wells; the caller is responsible for completing them inside the grid.</summary>
    public AnyReservoirSimulation WithWells(IReadOnlyCollection<Well> value) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._pinnedWells = value;

        return copy;
    }

    public AnyReservoirSimulation WithWellCount(int count) {
        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._wellCount = Any.Fixed(count);

        return copy;
    }

    public AnyReservoirSimulation WithWellCount(Func<AnyInt32, AnyInt32> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy = new AnyReservoirSimulation(this);

        copy._wellCountRecipe = configure(_wellCountRecipe);
        copy._wellCount       = copy._wellCountRecipe;

        return copy;
    }

    /// <summary>Refines every drawn well; id, kind and completion stay assigned by the simulation generator.</summary>
    public AnyReservoirSimulation WithWells(Func<AnyWell, AnyWell> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyReservoirSimulation copy     = new AnyReservoirSimulation(this);
        Func<AnyWell, AnyWell> previous = _wellRefinement;

        copy._wellRefinement = well => configure(previous(well));

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

        IReadOnlyCollection<Well> wells = _pinnedWells ?? DrawWellsIn(grid);

        return new ReservoirSimulation(grid, wells, _boundary.Generate());
    }

    private IReadOnlyCollection<Well> DrawWellsIn(GeologicalGrid grid) {
        List<CellCoordinate> coordinates = grid.Cells.Select(c => c.Coordinate).ToList();
        int                  count       = Math.Min(_wellCount.Generate(), coordinates.Count);
        List<CellCoordinate> shuffled    = Any.PermutationOf(coordinates).Generate();

        List<Well> wells = new List<Well>();
        for (int n = 0; n < count; n++) {
            string number = (n + 1).ToString("00");

            // The prefix follows whatever kind the well draws or a refinement pins; the number makes the id unique.
            wells.Add(_wellRefinement(new AnyWell().WithId(digits => digits.OneOf(number)).WithCompletion(shuffled[n])).Generate());
        }

        return wells;
    }

}
