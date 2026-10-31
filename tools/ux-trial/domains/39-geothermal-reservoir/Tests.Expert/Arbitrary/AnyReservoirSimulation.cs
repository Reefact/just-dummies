// Scaffolded by `any` (JustDummies), then reshaped by hand: the wells are not independent of the grid.
// Each must be completed inside it, in a cell of its own, under an id of its own. So the grid is drawn
// first and the wells are drawn from its coordinates. Do NOT re-run `any generate ReservoirSimulation
// --force` over this file.

using System;
using System.Collections.Generic;
using System.Linq;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>A generator of arbitrary, valid <see cref="ReservoirSimulation" /> values, not yet advanced.</summary>
public sealed partial class AnyReservoirSimulation : IAny<ReservoirSimulation> {

    private readonly IAny<GeologicalGrid>                                  _grid;
    private readonly Func<GeologicalGrid, IAny<IReadOnlyCollection<Well>>> _wells;
    private readonly IAny<BoundaryCondition>                               _boundary;

    public AnyReservoirSimulation()
        : this(grid:     AnyValidGrid(),
               wells:    AnyValidWells,
               boundary: AnyValidBoundary()) { }

    private static AnyGeologicalGrid AnyValidGrid() {
        return new AnyGeologicalGrid();
    }

    /// <summary>Up to three wells, each in a distinct cell of the grid and under a distinct id.</summary>
    private static IAny<IReadOnlyCollection<Well>> AnyValidWells(GeologicalGrid grid) {
        List<CellCoordinate> coordinates = grid.Cells.Select(cell => cell.Coordinate).ToList();
        int                  most        = Math.Min(3, coordinates.Count);

        return Any.Combine(
            Any.ListOf(Any.ElementOf(coordinates)).WithCountBetween(0, most).Distinct(),
            Any.ListOf(Any.Int32().Between(0, 99)).WithCount(most).Distinct(),
            (completions, serials) => (IReadOnlyCollection<Well>)completions
                                                                 .Select((completion, index) => new AnyWell().WithCompletion(completion)
                                                                                                             .WithSerial(serials[index])
                                                                                                             .Generate())
                                                                 .ToList());
    }

    private static AnyBoundaryCondition AnyValidBoundary() {
        return new AnyBoundaryCondition();
    }

    private AnyReservoirSimulation(IAny<GeologicalGrid>                                  grid,
                                   Func<GeologicalGrid, IAny<IReadOnlyCollection<Well>>> wells,
                                   IAny<BoundaryCondition>                               boundary) {
        _grid     = grid;
        _wells    = wells;
        _boundary = boundary;
    }

    public AnyReservoirSimulation WithGrid(GeologicalGrid value) {
        return new AnyReservoirSimulation(new FixedValue<GeologicalGrid>(value), _wells, _boundary);
    }

    public AnyReservoirSimulation WithGrid(Func<AnyGeologicalGrid, AnyGeologicalGrid> configure) {
        return new AnyReservoirSimulation(configure(AnyValidGrid()), _wells, _boundary);
    }

    /// <summary>Exactly these wells, whatever grid is drawn: their completions are the test's business.</summary>
    public AnyReservoirSimulation WithWells(params Well[] wells) {
        return new AnyReservoirSimulation(_grid, _ => new FixedValue<IReadOnlyCollection<Well>>(wells), _boundary);
    }

    public AnyReservoirSimulation WithBoundary(BoundaryCondition value) {
        return new AnyReservoirSimulation(_grid, _wells, new FixedValue<BoundaryCondition>(value));
    }

    public AnyReservoirSimulation WithBoundary(Func<AnyBoundaryCondition, AnyBoundaryCondition> configure) {
        return new AnyReservoirSimulation(_grid, _wells, configure(AnyValidBoundary()));
    }

    public ReservoirSimulation Generate() {
        GeologicalGrid grid = _grid.Generate();

        return new ReservoirSimulation(grid, _wells(grid).Generate(), _boundary.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
