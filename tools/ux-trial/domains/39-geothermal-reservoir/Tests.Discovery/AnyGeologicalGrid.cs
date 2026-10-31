// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeologicalGrid --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
//
// Hand-edited after scaffolding: the cell list is not an independent parameter. A grid holds exactly
// one cell per coordinate, so Generate() draws the dimensions first and then one cell per coordinate.

using System;
using System.Collections.Generic;

using JustDummies;

namespace GeothermalReservoir.Domain;

/// <summary>
///     A generator of arbitrary <see cref="GeologicalGrid" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyGeologicalGrid : IAny<GeologicalGrid> {

    private readonly IAny<int>    _cellsX;
    private readonly IAny<int>    _cellsY;
    private readonly IAny<int>    _cellsZ;
    private readonly IAny<double> _cellSizeMetres;
    private readonly AnyRockCell  _cell;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeologicalGrid()
        : this(cellsX:         AnyValidCellsX(),
               cellsY:         AnyValidCellsY(),
               cellsZ:         AnyValidCellsZ(),
               cellSizeMetres: AnyValidCellSizeMetres(),
               cell:           AnyValidCell()) { }

    // The domain only requires at least one cell per axis. The upper bound is a test-suite choice:
    // a handful of cells per axis keeps every grid small enough to simulate in a unit test.
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

    // The recipe of every cell; its coordinate is set by Generate().
    private static AnyRockCell AnyValidCell() {
        return new AnyRockCell();
    }

    private AnyGeologicalGrid(IAny<int>    cellsX,
                              IAny<int>    cellsY,
                              IAny<int>    cellsZ,
                              IAny<double> cellSizeMetres,
                              AnyRockCell  cell) {
        _cellsX         = cellsX;
        _cellsY         = cellsY;
        _cellsZ         = cellsZ;
        _cellSizeMetres = cellSizeMetres;
        _cell           = cell;
    }

    /// <summary>Pins <c>cellsX</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsX(int value) {
        return new AnyGeologicalGrid(new FixedValue<int>(value), _cellsY, _cellsZ, _cellSizeMetres, _cell);
    }

    /// <summary>Pins <c>cellsY</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsY(int value) {
        return new AnyGeologicalGrid(_cellsX, new FixedValue<int>(value), _cellsZ, _cellSizeMetres, _cell);
    }

    /// <summary>Pins <c>cellsZ</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsZ(int value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, new FixedValue<int>(value), _cellSizeMetres, _cell);
    }

    /// <summary>Pins the three dimensions at once.</summary>
    public AnyGeologicalGrid WithDimensions(int cellsX, int cellsY, int cellsZ) {
        return new AnyGeologicalGrid(new FixedValue<int>(cellsX), new FixedValue<int>(cellsY), new FixedValue<int>(cellsZ), _cellSizeMetres, _cell);
    }

    /// <summary>Pins <c>cellSizeMetres</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellSizeMetres(double value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, new FixedValue<double>(value), _cell);
    }

    /// <summary>Draws <c>cellSizeMetres</c> from <c>AnyValidCellSizeMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCellSizeMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, configure(AnyValidCellSizeMetres()), _cell);
    }

    /// <summary>Refines the recipe every cell is drawn from (its coordinate is always set by the grid).</summary>
    public AnyGeologicalGrid WithCells(Func<AnyRockCell, AnyRockCell> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, _cellSizeMetres, configure(AnyValidCell()));
    }

    /// <summary>Produces one arbitrary <see cref="GeologicalGrid" />.</summary>
    public GeologicalGrid Generate() {
        int cellsX = _cellsX.Generate();
        int cellsY = _cellsY.Generate();
        int cellsZ = _cellsZ.Generate();

        List<RockCell> cells = new List<RockCell>(cellsX * cellsY * cellsZ);
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    cells.Add(_cell.WithCoordinate(new CellCoordinate(i, j, k)).Generate());
                }
            }
        }

        return new GeologicalGrid(cellsX, cellsY, cellsZ, _cellSizeMetres.Generate(), cells);
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
