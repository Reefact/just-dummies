namespace BatchGenealogy.Domain;

/// <summary>The shape of a manufacturing step in the genealogy graph.</summary>
public enum TransformationKind
{
    /// <summary>Two or more input lots mixed into exactly one output lot.</summary>
    Blend,

    /// <summary>One input lot divided into two or more sub-lots of the same lot.</summary>
    Split,

    /// <summary>One or more inputs transformed (granulation, compression, filling...) into exactly one output lot.</summary>
    Process,
}
