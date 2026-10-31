namespace DnaVariants.Domain;

/// <summary>
/// The bases one side of a variant carries. Unlike a <see cref="NucleotideSequence"/>, it may be empty
/// (the reference side of an insertion, the alternate side of a deletion); N is not allowed: an allele is a called base.
/// </summary>
public sealed class Allele : IEquatable<Allele>
{
    public const int MaxLength = 10_000;

    public static readonly Allele Empty = new(string.Empty);

    public Allele(string bases)
    {
        ArgumentNullException.ThrowIfNull(bases);
        if (bases.Length > MaxLength)
        {
            throw new ArgumentOutOfRangeException(nameof(bases), $"An allele is at most {MaxLength} bases long, got {bases.Length}.");
        }

        string upper = bases.ToUpperInvariant();
        int invalid = upper.AsSpan().IndexOfAnyExcept("ACGT");
        if (invalid >= 0)
        {
            throw new ArgumentException($"'{bases[invalid]}' at offset {invalid} is not a called base (A, C, G, T).", nameof(bases));
        }

        Bases = upper;
    }

    public string Bases { get; }

    public int Length => Bases.Length;

    public bool IsEmpty => Bases.Length == 0;

    public bool Equals(Allele? other) => other is not null && Bases == other.Bases;

    public override bool Equals(object? obj) => Equals(obj as Allele);

    public override int GetHashCode() => Bases.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => IsEmpty ? "-" : Bases;
}
