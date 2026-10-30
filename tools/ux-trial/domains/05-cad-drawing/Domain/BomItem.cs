namespace Drafting.Domain;

/// <summary>A node of a bill of materials: a part, how many per parent, and the parts it is made of.</summary>
public sealed class BomItem {

    public const int MaximumDescriptionLength = 80;
    public const int MaximumQuantity          = 9_999;

    public BomItem(PartNumber part, string description, int quantity, IReadOnlyList<BomItem> children) {
        ArgumentNullException.ThrowIfNull(part);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        if (description.Length > MaximumDescriptionLength) {
            throw new ArgumentException($"A BOM description holds at most {MaximumDescriptionLength} characters.", nameof(description));
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, MaximumQuantity);
        ArgumentNullException.ThrowIfNull(children);
        if (children.Any(child => child.Contains(part))) {
            throw new ArgumentException($"{part} cannot be one of its own components.", nameof(children));
        }

        Part        = part;
        Description = description;
        Quantity    = quantity;
        Children    = children;
    }

    public PartNumber             Part        { get; }
    public string                 Description { get; }
    public int                    Quantity    { get; }
    public IReadOnlyList<BomItem> Children    { get; }

    public bool IsLeaf => Children.Count == 0;

    public int Depth => IsLeaf ? 1 : 1 + Children.Max(child => child.Depth);

    public bool Contains(PartNumber part) {
        return Part.Equals(part) || Children.Any(child => child.Contains(part));
    }

    /// <summary>How many of <paramref name="part"/> one build of this item consumes, quantities multiplied down the tree.</summary>
    public int TotalQuantityOf(PartNumber part) {
        if (Part.Equals(part)) {
            return Quantity;
        }

        return Quantity * Children.Sum(child => child.TotalQuantityOf(part));
    }

    /// <summary>Leaf parts with their total quantity for one build of this item.</summary>
    public IReadOnlyDictionary<PartNumber, int> FlattenToLeaves() {
        Dictionary<PartNumber, int> totals = new();
        Accumulate(this, 1, totals);
        return totals;
    }

    private static void Accumulate(BomItem item, int multiplier, Dictionary<PartNumber, int> totals) {
        int quantity = multiplier * item.Quantity;
        if (item.IsLeaf) {
            totals[item.Part] = totals.GetValueOrDefault(item.Part) + quantity;
            return;
        }
        foreach (BomItem child in item.Children) {
            Accumulate(child, quantity, totals);
        }
    }

}
