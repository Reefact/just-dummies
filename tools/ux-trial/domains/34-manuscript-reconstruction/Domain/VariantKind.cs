namespace ManuscriptReconstruction.Domain;

/// <summary>How a witness's reading at a passage was produced.</summary>
public enum VariantKind
{
    /// <summary>The scribe wrote the reading in the run of the text.</summary>
    Substitution,

    /// <summary>The witness has nothing at the passage where others have text.</summary>
    Omission,

    /// <summary>The witness adds words absent from the others.</summary>
    Insertion,

    /// <summary>A later hand altered what the first hand wrote.</summary>
    Correction,
}
