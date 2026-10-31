// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate GeologicalGrid --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

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

    private readonly IAny<int>                           _cellsX;
    private readonly IAny<int>                           _cellsY;
    private readonly IAny<int>                           _cellsZ;
    private readonly IAny<double>                        _cellSizeMetres;
    private readonly AnyRockCell                         _cells;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyGeologicalGrid()
        : this(cellsX:         AnyValidCellsX(),
               cellsY:         AnyValidCellsY(),
               cellsZ:         AnyValidCellsZ(),
               cellSizeMetres: AnyValidCellSizeMetres(),
               cells:          AnyValidCells()) { }

    private static AnyInt32 AnyValidCellsX() {
        // At least one cell, as the constructor requires; a handful at most keeps a unit test fast.
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsY() {
        // At least one cell, as the constructor requires; a handful at most keeps a unit test fast.
        return Any.Int32().Between(1, 4);
    }

    private static AnyInt32 AnyValidCellsZ() {
        // At least one cell, as the constructor requires; a handful at most keeps a unit test fast.
        return Any.Int32().Between(1, 4);
    }

    private static AnyDouble AnyValidCellSizeMetres() {
        return Any.Double().Between(GeologicalGrid.MinimumCellSizeMetres, GeologicalGrid.MaximumCellSizeMetres);
    }

    private static AnyRockCell AnyValidCells() {
        // The recipe of one cell. Generate() draws exactly one per coordinate of the grid, since the
        // constructor wants cellsX * cellsY * cellsZ cells, each inside the grid and present once.
        return new AnyRockCell();
    }

    private AnyGeologicalGrid(IAny<int>                           cellsX,
                              IAny<int>                           cellsY,
                              IAny<int>                           cellsZ,
                              IAny<double>                        cellSizeMetres,
                              AnyRockCell                         cells) {
        _cellsX         = cellsX;
        _cellsY         = cellsY;
        _cellsZ         = cellsZ;
        _cellSizeMetres = cellSizeMetres;
        _cells          = cells;
    }

    /// <summary>Pins <c>cellsX</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsX(int value) {
        return new AnyGeologicalGrid(new FixedValue<int>(value), _cellsY, _cellsZ, _cellSizeMetres, _cells);
    }

    /// <summary>Draws <c>cellsX</c> from <c>AnyValidCellsX()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCellsX(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(configure(AnyValidCellsX()), _cellsY, _cellsZ, _cellSizeMetres, _cells);
    }

    /// <summary>Pins <c>cellsY</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsY(int value) {
        return new AnyGeologicalGrid(_cellsX, new FixedValue<int>(value), _cellsZ, _cellSizeMetres, _cells);
    }

    /// <summary>Draws <c>cellsY</c> from <c>AnyValidCellsY()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCellsY(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(_cellsX, configure(AnyValidCellsY()), _cellsZ, _cellSizeMetres, _cells);
    }

    /// <summary>Pins <c>cellsZ</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellsZ(int value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, new FixedValue<int>(value), _cellSizeMetres, _cells);
    }

    /// <summary>Draws <c>cellsZ</c> from <c>AnyValidCellsZ()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCellsZ(Func<AnyInt32, AnyInt32> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, configure(AnyValidCellsZ()), _cellSizeMetres, _cells);
    }

    /// <summary>Pins <c>cellSizeMetres</c> to a fixed value.</summary>
    public AnyGeologicalGrid WithCellSizeMetres(double value) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, new FixedValue<double>(value), _cells);
    }

    /// <summary>Draws <c>cellSizeMetres</c> from <c>AnyValidCellSizeMetres()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCellSizeMetres(Func<AnyDouble, AnyDouble> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, configure(AnyValidCellSizeMetres()), _cells);
    }

    /// <summary>Draws <c>cells</c> from <c>AnyValidCells()</c>, refined by <paramref name="configure" />.</summary>
    public AnyGeologicalGrid WithCells(Func<AnyRockCell, AnyRockCell> configure) {
        return new AnyGeologicalGrid(_cellsX, _cellsY, _cellsZ, _cellSizeMetres, configure(AnyValidCells()));
    }

    /// <summary>Produces one arbitrary <see cref="GeologicalGrid" />.</summary>
    public GeologicalGrid Generate() {
        int            cellsX = _cellsX.Generate();
        int            cellsY = _cellsY.Generate();
        int            cellsZ = _cellsZ.Generate();
        List<RockCell> cells  = [];
        for (int i = 0; i < cellsX; i++) {
            for (int j = 0; j < cellsY; j++) {
                for (int k = 0; k < cellsZ; k++) {
                    cells.Add(_cells.WithCoordinate(new CellCoordinate(i, j, k)).Generate());
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
