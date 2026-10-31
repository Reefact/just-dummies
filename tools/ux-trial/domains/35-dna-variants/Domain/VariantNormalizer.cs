namespace DnaVariants.Domain;

/// <summary>Brings variants on one reference to their canonical form under one convention.</summary>
public sealed class VariantNormalizer
{
    public VariantNormalizer(ReferenceSequence reference, NormalizationConvention convention)
    {
        ArgumentNullException.ThrowIfNull(reference);
        if (!Enum.IsDefined(convention))
        {
            throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown normalisation convention.");
        }

        Reference = reference;
        Convention = convention;
    }

    public ReferenceSequence Reference { get; }

    public NormalizationConvention Convention { get; }

    /// <summary>
    /// Verifies the variant against the reference, trims the bases both alleles share (suffix, then prefix) and, for a
    /// pure insertion or deletion, shifts it through the surrounding repeat in the direction of the convention.
    /// </summary>
    /// <exception cref="ReferenceMismatchException">The variant is not on, or does not agree with, the reference.</exception>
    public NormalizedVariant Normalize(Variant variant)
    {
        Reference.Verify(variant);

        string refBases = variant.Reference.Bases;
        string altBases = variant.Alternate.Bases;
        int position = variant.Position;

        int suffix = 0;
        while (suffix < refBases.Length && suffix < altBases.Length
               && refBases[refBases.Length - 1 - suffix] == altBases[altBases.Length - 1 - suffix])
        {
            suffix++;
        }

        refBases = refBases[..^suffix];
        altBases = altBases[..^suffix];

        int prefix = 0;
        while (prefix < refBases.Length && prefix < altBases.Length && refBases[prefix] == altBases[prefix])
        {
            prefix++;
        }

        refBases = refBases[prefix..];
        altBases = altBases[prefix..];
        position += prefix;

        if (refBases.Length == 0 || altBases.Length == 0)
        {
            bool isInsertion = refBases.Length == 0;
            string moving = isInsertion ? altBases : refBases;
            (position, moving) = Convention == NormalizationConvention.LeftAligned
                ? ShiftLeft(position, moving)
                : ShiftRight(position, moving, isInsertion ? 0 : moving.Length);
            if (isInsertion)
            {
                altBases = moving;
            }
            else
            {
                refBases = moving;
            }
        }

        Variant canonical = new(variant.Accession, position, new Allele(refBases), new Allele(altBases));
        return new NormalizedVariant(canonical, Convention, Classify(canonical));
    }

    /// <summary>Whether two descriptions denote the same change on the reference.</summary>
    public bool AreEquivalent(Variant first, Variant second) => Normalize(first).Equals(Normalize(second));

    private (int Position, string Moving) ShiftLeft(int position, string moving)
    {
        string sequence = Reference.Sequence.Bases;
        while (position > 1 && sequence[position - 2] == moving[^1])
        {
            moving = sequence[position - 2] + moving[..^1];
            position--;
        }

        return (position, moving);
    }

    private (int Position, string Moving) ShiftRight(int position, string moving, int referenceLength)
    {
        string sequence = Reference.Sequence.Bases;
        int next = position - 1 + referenceLength;
        while (next < sequence.Length && sequence[next] == moving[0])
        {
            moving = moving[1..] + sequence[next];
            position++;
            next++;
        }

        return (position, moving);
    }

    private VariantKind Classify(Variant canonical)
    {
        if (canonical.Kind != VariantKind.Insertion)
        {
            return canonical.Kind;
        }

        int length = canonical.Alternate.Length;
        int start = canonical.Position - length;
        bool duplicates = start >= 1 && Reference.Sequence.Slice(start, length) == canonical.Alternate.Bases;
        return duplicates ? VariantKind.Duplication : VariantKind.Insertion;
    }
}
