namespace GeothermalReservoir.Domain;

/// <summary>A structured NX × NY × NZ grid of cubic rock cells, filled completely and exactly once.</summary>
public sealed class GeologicalGrid
{
    public const double MinimumCellSizeMetres = 1.0;
    public const double MaximumCellSizeMetres = 1000.0;

    private readonly RockCell[,,] _cells;

    public GeologicalGrid(int cellsX, int cellsY, int cellsZ, double cellSizeMetres, IReadOnlyCollection<RockCell> cells)
    {
        if (cellsX < 1) throw new ArgumentOutOfRangeException(nameof(cellsX), cellsX, "A grid needs at least one cell along X.");
        if (cellsY < 1) throw new ArgumentOutOfRangeException(nameof(cellsY), cellsY, "A grid needs at least one cell along Y.");
        if (cellsZ < 1) throw new ArgumentOutOfRangeException(nameof(cellsZ), cellsZ, "A grid needs at least one cell along Z.");
        if (double.IsNaN(cellSizeMetres) || cellSizeMetres < MinimumCellSizeMetres || cellSizeMetres > MaximumCellSizeMetres)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSizeMetres), cellSizeMetres,
                $"Cell size must be in [{MinimumCellSizeMetres}, {MaximumCellSizeMetres}] m.");
        }

        ArgumentNullException.ThrowIfNull(cells);
        if (cells.Count != cellsX * cellsY * cellsZ)
        {
            throw new ArgumentException(
                $"A {cellsX}x{cellsY}x{cellsZ} grid needs {cellsX * cellsY * cellsZ} cells, got {cells.Count}.", nameof(cells));
        }

        _cells = new RockCell[cellsX, cellsY, cellsZ];
        foreach (RockCell cell in cells)
        {
            if (cell is null) throw new ArgumentException("The grid cannot contain a null cell.", nameof(cells));
            CellCoordinate c = cell.Coordinate;
            if (c.I >= cellsX || c.J >= cellsY || c.K >= cellsZ)
            {
                throw new ArgumentException($"Cell {c} lies outside the {cellsX}x{cellsY}x{cellsZ} grid.", nameof(cells));
            }

            if (_cells[c.I, c.J, c.K] is not null)
            {
                throw new ArgumentException($"Cell {c} appears twice.", nameof(cells));
            }

            _cells[c.I, c.J, c.K] = cell;
        }

        CellsX = cellsX;
        CellsY = cellsY;
        CellsZ = cellsZ;
        CellSizeMetres = cellSizeMetres;
    }

    public int CellsX { get; }
    public int CellsY { get; }
    public int CellsZ { get; }
    public double CellSizeMetres { get; }
    public double CellVolumeCubicMetres => CellSizeMetres * CellSizeMetres * CellSizeMetres;
    public double FaceAreaSquareMetres => CellSizeMetres * CellSizeMetres;

    public IEnumerable<RockCell> Cells => _cells.Cast<RockCell>();

    public bool Contains(CellCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        return coordinate.I < CellsX && coordinate.J < CellsY && coordinate.K < CellsZ;
    }

    public RockCell CellAt(CellCoordinate coordinate)
    {
        if (!Contains(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "The coordinate lies outside the grid.");
        }

        return _cells[coordinate.I, coordinate.J, coordinate.K];
    }

    /// <summary>The face neighbours of a cell (up to six).</summary>
    public IEnumerable<RockCell> NeighboursOf(CellCoordinate coordinate)
    {
        RockCell centre = CellAt(coordinate);
        (int di, int dj, int dk)[] offsets = [(-1, 0, 0), (1, 0, 0), (0, -1, 0), (0, 1, 0), (0, 0, -1), (0, 0, 1)];
        foreach ((int di, int dj, int dk) in offsets)
        {
            int i = centre.Coordinate.I + di;
            int j = centre.Coordinate.J + dj;
            int k = centre.Coordinate.K + dk;
            if (i >= 0 && j >= 0 && k >= 0 && i < CellsX && j < CellsY && k < CellsZ)
            {
                yield return _cells[i, j, k];
            }
        }
    }

    /// <summary>Number of lateral faces (X or Y) of the cell that lie on the grid's outer boundary.</summary>
    public int LateralBoundaryFaceCount(CellCoordinate coordinate)
    {
        RockCell cell = CellAt(coordinate);
        int count = 0;
        if (cell.Coordinate.I == 0) count++;
        if (cell.Coordinate.I == CellsX - 1) count++;
        if (cell.Coordinate.J == 0) count++;
        if (cell.Coordinate.J == CellsY - 1) count++;
        return count;
    }

    /// <summary>Mean vertical thermal gradient in K/m, from the top layer to the bottom layer (0 for a single layer).</summary>
    public double AverageThermalGradient()
    {
        if (CellsZ == 1) return 0.0;
        double top = Cells.Where(c => c.Coordinate.K == 0).Average(c => c.Temperature.Kelvin);
        double bottom = Cells.Where(c => c.Coordinate.K == CellsZ - 1).Average(c => c.Temperature.Kelvin);
        return (bottom - top) / ((CellsZ - 1) * CellSizeMetres);
    }
}
