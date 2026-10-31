namespace RadiocarbonChronology.Domain;

/// <summary>A stratigraphic succession: the older layer was deposited before the younger one (it lies beneath it).</summary>
public sealed class PrecedenceRelation : IEquatable<PrecedenceRelation>
{
    public PrecedenceRelation(string olderLayerCode, string youngerLayerCode)
    {
        if (string.IsNullOrWhiteSpace(olderLayerCode))
        {
            throw new ArgumentException("The older layer code is required.", nameof(olderLayerCode));
        }

        if (string.IsNullOrWhiteSpace(youngerLayerCode))
        {
            throw new ArgumentException("The younger layer code is required.", nameof(youngerLayerCode));
        }

        if (string.Equals(olderLayerCode, youngerLayerCode, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Layer '{olderLayerCode}' cannot precede itself.", nameof(youngerLayerCode));
        }

        OlderLayerCode = olderLayerCode;
        YoungerLayerCode = youngerLayerCode;
    }

    public string OlderLayerCode { get; }

    public string YoungerLayerCode { get; }

    public bool Equals(PrecedenceRelation? other) =>
        other is not null
        && string.Equals(OlderLayerCode, other.OlderLayerCode, StringComparison.Ordinal)
        && string.Equals(YoungerLayerCode, other.YoungerLayerCode, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as PrecedenceRelation);

    public override int GetHashCode() => HashCode.Combine(OlderLayerCode, YoungerLayerCode);

    public override string ToString() => $"{OlderLayerCode} < {YoungerLayerCode}";
}
