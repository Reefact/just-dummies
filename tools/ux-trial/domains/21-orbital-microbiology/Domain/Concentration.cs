namespace OrbitalMicrobiology.Domain;

/// <summary>A cell concentration, in cells per millilitre.</summary>
public sealed class Concentration : IEquatable<Concentration>, IComparable<Concentration>
{
    public const double MaxCellsPerMillilitre = 1e13;

    public Concentration(double cellsPerMillilitre)
    {
        if (!double.IsFinite(cellsPerMillilitre) || cellsPerMillilitre < 0 || cellsPerMillilitre > MaxCellsPerMillilitre)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cellsPerMillilitre),
                cellsPerMillilitre,
                $"A concentration lies in [0, {MaxCellsPerMillilitre:0e0}] cells/mL.");
        }

        CellsPerMillilitre = cellsPerMillilitre;
    }

    public static Concentration Zero { get; } = new(0);

    public double CellsPerMillilitre { get; }

    /// <summary>log10 of the concentration; refused for zero.</summary>
    public double Log10 =>
        CellsPerMillilitre > 0
            ? Math.Log10(CellsPerMillilitre)
            : throw new InvalidOperationException("An empty culture has no logarithm.");

    /// <summary>Generations needed to grow from this concentration to the target: log2(target / this).</summary>
    public double GenerationsTo(Concentration target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (CellsPerMillilitre == 0 || target.CellsPerMillilitre == 0)
        {
            throw new InvalidOperationException("Generations are counted between two non-empty concentrations.");
        }

        return Math.Log2(target.CellsPerMillilitre / CellsPerMillilitre);
    }

    public int CompareTo(Concentration? other) => other is null ? 1 : CellsPerMillilitre.CompareTo(other.CellsPerMillilitre);

    public bool Equals(Concentration? other) => other is not null && CellsPerMillilitre.Equals(other.CellsPerMillilitre);

    public override bool Equals(object? obj) => Equals(obj as Concentration);

    public override int GetHashCode() => CellsPerMillilitre.GetHashCode();

    public override string ToString() => $"{CellsPerMillilitre:0.###e0} cells/mL";
}
