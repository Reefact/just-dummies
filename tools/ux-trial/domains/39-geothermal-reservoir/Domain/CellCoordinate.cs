namespace GeothermalReservoir.Domain;

/// <summary>Position of a cell in the structured grid; K grows with depth.</summary>
public sealed class CellCoordinate : IEquatable<CellCoordinate>
{
    public CellCoordinate(int i, int j, int k)
    {
        if (i < 0) throw new ArgumentOutOfRangeException(nameof(i), i, "Grid indices are zero-based and non-negative.");
        if (j < 0) throw new ArgumentOutOfRangeException(nameof(j), j, "Grid indices are zero-based and non-negative.");
        if (k < 0) throw new ArgumentOutOfRangeException(nameof(k), k, "Grid indices are zero-based and non-negative.");
        I = i;
        J = j;
        K = k;
    }

    public int I { get; }
    public int J { get; }
    public int K { get; }

    /// <summary>True when the two cells share a face (six-point stencil).</summary>
    public bool IsFaceNeighbourOf(CellCoordinate other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Math.Abs(I - other.I) + Math.Abs(J - other.J) + Math.Abs(K - other.K) == 1;
    }

    public bool Equals(CellCoordinate? other) => other is not null && I == other.I && J == other.J && K == other.K;
    public override bool Equals(object? obj) => Equals(obj as CellCoordinate);
    public override int GetHashCode() => HashCode.Combine(I, J, K);
    public override string ToString() => $"({I},{J},{K})";
}
