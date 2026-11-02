namespace PassiveComponents.Domain;

/// <summary>One line of the bill of materials: a part and every designator it is fitted on.</summary>
public sealed class BomLine
{
    public BomLine(Part part, IEnumerable<ReferenceDesignator> designators)
    {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentNullException.ThrowIfNull(designators);
        ReferenceDesignator[] sorted = designators.ToArray();
        if (sorted.Length == 0)
        {
            throw new ArgumentException("A line lists at least one designator.", nameof(designators));
        }

        if (sorted.Any(d => d is null))
        {
            throw new ArgumentException("A line lists no null designator.", nameof(designators));
        }

        if (sorted.Distinct().Count() != sorted.Length)
        {
            throw new ArgumentException("A line lists each designator once.", nameof(designators));
        }

        ReferenceDesignator? mismatch = sorted.FirstOrDefault(d => d.Kind != part.Kind);
        if (mismatch is not null)
        {
            throw new ArgumentException($"{mismatch} cannot take {part.PartNumber}, a {Part.Describe(part.Kind)}.", nameof(designators));
        }

        Array.Sort(sorted);
        Part = part;
        Designators = Array.AsReadOnly(sorted);
    }

    public Part Part { get; }

    public IReadOnlyList<ReferenceDesignator> Designators { get; }

    public int Quantity => Designators.Count;

    public decimal ExtendedCost => Part.UnitCost * Quantity;

    public override string ToString() => $"{Quantity} × {Part.PartNumber}: {string.Join(", ", Designators)}";
}
