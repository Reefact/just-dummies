// Scaffolded by `any` (JustDummies), then reshaped by hand: the grid's cells are not an independent
// parameter. Their count is cellsX·cellsY·cellsZ and each coordinate appears exactly once, a rule tying
// four parameters together. So the dimensions are drawn first and one cell is drawn per coordinate,
// from a rock recipe that may depend on the position (a layered reservoir, a hot bottom layer...).
// Do NOT re-run `any generate GeologicalGrid --force` over this file.

using System;
using System.Collections.Generic;

using GeothermalReservoir.Domain;
using JustDummies;

namespace GeothermalReservoir.Tests.Expert.Arbitrary;

/// <summary>A generator of arbitrary, completely filled <see cref="GeologicalGrid" /> values.</summary>
public sealed partial class AnyGeologicalGrid : IAny<GeologicalGrid> {

    private readonly IAny<int>                                      _cellsX;
    private readonly IAny<int>                                      _cellsY;
    private readonly IAny<int>                                      _cellsZ;
    private readonly IAny<double>                                   _cellSizeMetres;
    private readonly Func<CellCoordinate, AnyRockCell, AnyRockCell> _rock;

    public AnyGeologicalGrid()
        : this(cellsX:         AnyValidCellsX(),
               cellsY:         AnyValidCellsY(),
               cellsZ:         AnyValidCellsZ(),
               cellSizeMetres: AnyValidCellSizeMetres(),
               rock:           (_, cell) => cell) { }

    // The domain accepts any positive dimension; a test grid stays small enough to be built in a blink.
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

    private AnyGeologicalGrid(IAny<int>                                      cellsX,
                              IAny<int>                                      cellsY,
                              IAny<int>                                      cellsZ,
                              IAny<double>                                   cellSizeMetres,
                              Func<CellCoordinate, AnyRockCell, AnyRockCell> rock) {
        _cellsX         = cellsX;
        _cellsY         = cellsY;
        _cellsZ         = cellsZ;
        _cellSizeMetres = cellSizeMetres;
        _rock           = rock;
    }

    public AnyGeologicalGrid WithDimensions(int cellsX, int cellsY, int cellsZ) {
        return new AnyGeologicalGrid(new FixedValue<int>(cellsX), new FixedValue<int>(cellsY), new FixedValue<int>(cellsZ), _cellSizeMetres, _rock);
    }

    public AnyGeologicalGrid WithCellsX(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(configure(AnyValidCellsX()), _cellsY, _cellsZ, _cellSizeMetres, _rock);
    }

    public AnyGeologicalGrid WithCellsY(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(_cellsX, configure(AnyValidCellsY()), _cellsZ, _cellSizeMetres, _rock);
    }

    public AnyGeologicalGrid WithCellsZ(int value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, new FixedValue<int>(value), _cellSizeMetres, _rock);
    }

    public AnyGeologicalGrid WithCellsZ(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, configure(AnyValidCellsZ()), _cellSizeMetres, _rock);
    }

    public AnyGeologicalGrid WithCellSizeMetres(double value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, new FixedValue<double>(value), _rock);
    }

    public AnyGeologicalGrid WithCellSizeMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, configure(AnyValidCellSizeMetres()), _rock);
    }

    /// <summary>Refines the rock of every cell the same way. The last call wins.</summary>
    public AnyGeologicalGrid WithRock(Func<AnyRockCell, AnyRockCell> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, _cellSizeMetres, (_, cell) => configure(cell));
    }

    /// <summary>Refines the rock of each cell from its position. The last call wins.</summary>
    public AnyGeologicalGrid WithRock(Func<CellCoordinate, AnyRockCell, AnyRockCell> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, _cellSizeMetres, configure);
    }

    public GeologicalGrid Generate() {
        int    cellsX         = _cellsX.Generate();
        int    cellsY         = _cellsY.Generate();
        int    cellsZ         = _cellsZ.Generate();
        double cellSizeMetres = _cellSizeMetres.Generate();

        List<RockCell> cells = new List<RockCell>(cellsX * cellsY * cellsZ);
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    CellCoordinate coordinate = new CellCoordinate(i, j, k);
                    cells.Add(_rock(coordinate, new AnyRockCell()).WithCoordinate(coordinate).Generate());
                }
            }
        }

        return new GeologicalGrid(cellsX, cellsY, cellsZ, cellSizeMetres, cells);
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
