namespace AdaptiveVideo.Domain;

/// <summary>The encoding variants offered for one piece of content, ordered by ascending bitrate.</summary>
public sealed class BitrateLadder
{
    private readonly List<EncodingVariant> _variants;

    public BitrateLadder(IEnumerable<EncodingVariant> variants)
    {
        ArgumentNullException.ThrowIfNull(variants);
        List<EncodingVariant> list = variants.ToList();
        if (list.Count == 0)
        {
            throw new ArgumentException("A bitrate ladder needs at least one variant.", nameof(variants));
        }

        if (list.Any(v => v is null))
        {
            throw new ArgumentException("A bitrate ladder cannot contain a null variant.", nameof(variants));
        }

        if (list.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != list.Count)
        {
            throw new ArgumentException("Variant ids must be unique within a ladder.", nameof(variants));
        }

        if (list.Select(v => v.Bitrate.Kbps).Distinct().Count() != list.Count)
        {
            throw new ArgumentException("Variant bitrates must be distinct within a ladder.", nameof(variants));
        }

        list.Sort((a, b) => a.Bitrate.CompareTo(b.Bitrate));
        for (int i = 1; i < list.Count; i++)
        {
            if (list[i].Height < list[i - 1].Height)
            {
                throw new ArgumentException(
                    $"Variant {list[i].Id} has a higher bitrate but a lower resolution than {list[i - 1].Id}.", nameof(variants));
            }
        }

        _variants = list;
    }

    public IReadOnlyList<EncodingVariant> Variants => _variants;

    public EncodingVariant Lowest => _variants[0];

    public EncodingVariant Highest => _variants[^1];

    public EncodingVariant Find(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _variants.FirstOrDefault(v => v.Id == id)
               ?? throw new KeyNotFoundException($"The ladder has no variant '{id}'.");
    }

    /// <summary>The highest variant whose bitrate does not exceed <paramref name="budgetKbps"/>, or the lowest one.</summary>
    public EncodingVariant HighestWithin(double budgetKbps) =>
        _variants.LastOrDefault(v => v.Bitrate.Kbps <= budgetKbps) ?? Lowest;

    public EncodingVariant StepDown(EncodingVariant current) => _variants[Math.Max(0, IndexOf(current) - 1)];

    public EncodingVariant StepUp(EncodingVariant current) => _variants[Math.Min(_variants.Count - 1, IndexOf(current) + 1)];

    public int IndexOf(EncodingVariant variant)
    {
        ArgumentNullException.ThrowIfNull(variant);
        int index = _variants.FindIndex(v => v.Id == variant.Id);
        if (index < 0)
        {
            throw new ArgumentException($"Variant {variant.Id} is not part of this ladder.", nameof(variant));
        }

        return index;
    }
}
