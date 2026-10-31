namespace DnaVariants.Domain;

/// <summary>A reference sequence of the reference genome, identified by its accession.</summary>
public sealed class ReferenceSequence
{
    public ReferenceSequence(Accession accession, string name, NucleotideSequence sequence)
    {
        ArgumentNullException.ThrowIfNull(accession);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(sequence);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A reference sequence needs a name (e.g. 'chr17').", nameof(name));
        }

        Accession = accession;
        Name = name;
        Sequence = sequence;
    }

    public Accession Accession { get; }

    public string Name { get; }

    public NucleotideSequence Sequence { get; }

    public int Length => Sequence.Length;

    public char BaseAt(int position) => Sequence.Slice(position, 1)[0];

    /// <summary>Checks that <paramref name="variant"/> lies on this reference and that its reference allele matches it.</summary>
    public void Verify(Variant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        if (!variant.Accession.Equals(Accession))
        {
            throw new ReferenceMismatchException($"Variant {variant} is on {variant.Accession}, not on {Accession}.");
        }

        if (variant.End > Length || variant.Position > Length + 1)
        {
            throw new ReferenceMismatchException($"Variant {variant} runs past the end of {Accession} ({Length} bases).");
        }

        string expected = Sequence.Slice(variant.Position, variant.Reference.Length);
        if (expected != variant.Reference.Bases)
        {
            throw new ReferenceMismatchException(
                $"Variant {variant} states reference '{variant.Reference}' but {Accession} reads '{expected}' at {variant.Position}.");
        }
    }

    public override bool Equals(object? obj) => obj is ReferenceSequence other && Accession.Equals(other.Accession);

    public override int GetHashCode() => Accession.GetHashCode();

    public override string ToString() => $"{Name} ({Accession}, {Length} bp)";
}
