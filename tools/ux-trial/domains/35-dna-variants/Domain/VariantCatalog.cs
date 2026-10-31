namespace DnaVariants.Domain;

/// <summary>
/// The set of distinct variants recorded against one reference, stored in canonical form so that two descriptions
/// of the same change count once. Overlapping changes are refused: the catalog describes one haplotype.
/// </summary>
public sealed class VariantCatalog
{
    public const int MaxVariants = 10_000;

    private readonly List<NormalizedVariant> _variants = [];

    public VariantCatalog(ReferenceSequence reference, NormalizationConvention convention)
    {
        Normalizer = new VariantNormalizer(reference, convention);
    }

    public VariantNormalizer Normalizer { get; }

    public ReferenceSequence Reference => Normalizer.Reference;

    public IReadOnlyList<NormalizedVariant> Variants => _variants.OrderBy(v => v.Variant.Position).ToList();

    public int Count => _variants.Count;

    /// <summary>Records the variant; returns <c>false</c> when an equivalent description is already recorded.</summary>
    /// <exception cref="ReferenceMismatchException">The variant does not agree with the reference.</exception>
    /// <exception cref="InvalidOperationException">The catalog is full, or the variant overlaps a different recorded one.</exception>
    public bool Record(Variant variant)
    {
        NormalizedVariant normalized = Normalizer.Normalize(variant);
        if (_variants.Contains(normalized))
        {
            return false;
        }

        if (_variants.Count >= MaxVariants)
        {
            throw new InvalidOperationException($"A catalog holds at most {MaxVariants} variants.");
        }

        NormalizedVariant? clash = _variants.FirstOrDefault(v => v.Variant.Overlaps(normalized.Variant));
        if (clash is not null)
        {
            throw new InvalidOperationException($"{normalized} overlaps {clash}, already recorded.");
        }

        _variants.Add(normalized);
        return true;
    }

    public bool Contains(Variant variant) => _variants.Contains(Normalizer.Normalize(variant));

    public bool Remove(Variant variant) => _variants.Remove(Normalizer.Normalize(variant));

    /// <summary>The sequence obtained by applying every recorded variant to the reference.</summary>
    /// <exception cref="InvalidOperationException">Applying the variants would delete the whole sequence.</exception>
    public NucleotideSequence ApplyAll()
    {
        string sequence = Reference.Sequence.Bases;
        foreach (NormalizedVariant normalized in _variants.OrderByDescending(v => v.Variant.Position))
        {
            Variant v = normalized.Variant;
            sequence = string.Concat(sequence.AsSpan(0, v.Position - 1), v.Alternate.Bases, sequence.AsSpan(v.Position - 1 + v.Reference.Length));
        }

        if (sequence.Length == 0)
        {
            throw new InvalidOperationException($"The recorded variants delete the whole of {Reference.Accession}.");
        }

        return new NucleotideSequence(sequence);
    }
}
