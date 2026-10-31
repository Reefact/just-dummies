namespace DnaVariants.Domain;

/// <summary>
/// The canonical form of a variant against a reference under a convention: minimal alleles, shifted through repeats.
/// Two descriptions of the same change normalise to equal <see cref="NormalizedVariant"/>s.
/// </summary>
public sealed class NormalizedVariant : IEquatable<NormalizedVariant>
{
    public NormalizedVariant(Variant variant, NormalizationConvention convention, VariantKind kind)
    {
        ArgumentNullException.ThrowIfNull(variant);
        if (!Enum.IsDefined(convention))
        {
            throw new ArgumentOutOfRangeException(nameof(convention), convention, "Unknown normalisation convention.");
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown variant kind.");
        }

        if (kind == VariantKind.Duplication && !variant.Reference.IsEmpty)
        {
            throw new ArgumentException("Only an insertion can be classified as a duplication.", nameof(kind));
        }

        if (kind != VariantKind.Duplication && kind != variant.Kind)
        {
            throw new ArgumentException($"Variant {variant} is a {variant.Kind}, not a {kind}.", nameof(kind));
        }

        Variant = variant;
        Convention = convention;
        Kind = kind;
    }

    public Variant Variant { get; }

    public NormalizationConvention Convention { get; }

    public VariantKind Kind { get; }

    /// <summary>A compact HGVS-like description, e.g. <c>NC_000001.11:g.12del</c>.</summary>
    public string ToHgvs()
    {
        Variant v = Variant;
        string prefix = $"{v.Accession}:g.";
        return Kind switch
        {
            VariantKind.Substitution => $"{prefix}{v.Position}{v.Reference.Bases}>{v.Alternate.Bases}",
            VariantKind.Deletion => $"{prefix}{Range(v.Position, v.End)}del",
            VariantKind.Duplication => $"{prefix}{Range(v.Position - v.Alternate.Length, v.Position - 1)}dup",
            VariantKind.Insertion => $"{prefix}{v.Position - 1}_{v.Position}ins{v.Alternate.Bases}",
            _ => $"{prefix}{Range(v.Position, v.End)}delins{v.Alternate.Bases}",
        };
    }

    private static string Range(int start, int end) => start == end ? $"{start}" : $"{start}_{end}";

    public bool Equals(NormalizedVariant? other) =>
        other is not null && Convention == other.Convention && Variant.Equals(other.Variant);

    public override bool Equals(object? obj) => Equals(obj as NormalizedVariant);

    public override int GetHashCode() => HashCode.Combine(Variant, Convention);

    public override string ToString() => ToHgvs();
}
