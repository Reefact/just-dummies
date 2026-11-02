namespace PassiveComponents.Domain;

/// <summary>
/// A part placed on a reference designator. The designed part is the one the schematic calls for; the fitted
/// part is the one the board is built with, which a substitution may change.
/// </summary>
public sealed class Placement
{
    public Placement(ReferenceDesignator designator, Part part)
    {
        ArgumentNullException.ThrowIfNull(designator);
        ArgumentNullException.ThrowIfNull(part);
        if (designator.Kind != part.Kind)
        {
            throw new ArgumentException($"{designator} takes a {Part.Describe(designator.Kind)}; {part.PartNumber} is a {Part.Describe(part.Kind)}.", nameof(part));
        }

        Designator = designator;
        DesignedPart = part;
        FittedPart = part;
    }

    public ReferenceDesignator Designator { get; }

    public Part DesignedPart { get; }

    public Part FittedPart { get; private set; }

    public bool IsSubstituted => !FittedPart.Equals(DesignedPart);

    internal void Fit(Part part) => FittedPart = part;

    public override string ToString() => IsSubstituted ? $"{Designator}: {FittedPart.PartNumber} (for {DesignedPart.PartNumber})" : $"{Designator}: {FittedPart.PartNumber}";
}
