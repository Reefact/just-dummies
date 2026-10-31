namespace BatchGenealogy.Domain;

/// <summary>
/// An edge of the genealogy graph: <see cref="Target"/> was made, through a transformation,
/// partly from <see cref="Source"/>, which makes up <see cref="Share"/> of it.
/// </summary>
public sealed class ProvenanceLink
{
    public ProvenanceLink(LotNumber source, LotNumber target, string transformationId, decimal share)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformationId);
        if (source.Equals(target))
        {
            throw new ArgumentException($"Lot {source} cannot be its own provenance.", nameof(target));
        }

        if (share <= 0m || share > 1m)
        {
            throw new ArgumentOutOfRangeException(nameof(share), share, "A share lies in (0, 1].");
        }

        Source = source;
        Target = target;
        TransformationId = transformationId;
        Share = share;
    }

    public LotNumber Source { get; }

    public LotNumber Target { get; }

    public string TransformationId { get; }

    public decimal Share { get; }

    public override string ToString() => $"{Source} -> {Target} ({Share:P1} via {TransformationId})";
}
