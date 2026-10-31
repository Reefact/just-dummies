namespace DnaVariants.Domain;

/// <summary>Where an indel inside a repeat is placed once its alleles are trimmed.</summary>
public enum NormalizationConvention
{
    /// <summary>Shifted to the leftmost equivalent position (VCF practice).</summary>
    LeftAligned,

    /// <summary>Shifted to the most 3' equivalent position (HGVS rule).</summary>
    RightAligned,
}
