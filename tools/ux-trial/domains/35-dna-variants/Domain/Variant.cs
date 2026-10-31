namespace DnaVariants.Domain;

/// <summary>
/// One sequence change, described as it was written: on <see cref="Accession"/>, the reference allele starting at the
/// 1-based <see cref="Position"/> is replaced by the alternate allele. For an insertion (empty reference allele) the
/// alternate bases go immediately before <see cref="Position"/>. Equality is textual; use
/// <see cref="VariantNormalizer.AreEquivalent"/> to compare effects on the reference.
/// </summary>
public sealed class Variant : IEquatable<Variant>
{
    public Variant(Accession accession, int position, Allele reference, Allele alternate)
    {
        ArgumentNullException.ThrowIfNull(accession);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(alternate);
        ArgumentOutOfRangeException.ThrowIfLessThan(position, 1);
        if (reference.Equals(alternate))
        {
            throw new ArgumentException($"Reference and alternate alleles are both '{reference}': that is no change.", nameof(alternate));
        }

        Accession = accession;
        Position = position;
        Reference = reference;
        Alternate = alternate;
    }

    public Accession Accession { get; }

    public int Position { get; }

    public Allele Reference { get; }

    public Allele Alternate { get; }

    /// <summary>The last reference base the variant covers; <c>Position - 1</c> for an insertion.</summary>
    public int End => Position + Reference.Length - 1;

    /// <summary>Net length change of the sequence: positive for a gain, negative for a loss.</summary>
    public int LengthChange => Alternate.Length - Reference.Length;

    public VariantKind Kind => (Reference.Length, Alternate.Length) switch
    {
        (1, 1) => VariantKind.Substitution,
        (0, _) => VariantKind.Insertion,
        (_, 0) => VariantKind.Deletion,
        _ => VariantKind.DeletionInsertion,
    };

    public bool Overlaps(Variant other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!Accession.Equals(other.Accession))
        {
            return false;
        }

        if (Reference.IsEmpty && other.Reference.IsEmpty)
        {
            return Position == other.Position;
        }

        if (Reference.IsEmpty)
        {
            return other.Position < Position && Position <= other.End;
        }

        if (other.Reference.IsEmpty)
        {
            return Position < other.Position && other.Position <= End;
        }

        return Position <= other.End && other.Position <= End;
    }

    public bool Equals(Variant? other) =>
        other is not null
        && Accession.Equals(other.Accession)
        && Position == other.Position
        && Reference.Equals(other.Reference)
        && Alternate.Equals(other.Alternate);

    public override bool Equals(object? obj) => Equals(obj as Variant);

    public override int GetHashCode() => HashCode.Combine(Accession, Position, Reference, Alternate);

    public override string ToString() => $"{Accession}:{Position}:{Reference}>{Alternate}";
}
