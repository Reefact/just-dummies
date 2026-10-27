namespace Shop.Domain;

/// <summary>A percentage taken off an order: 0 to 100, at most one decimal place (e.g. 12.5).</summary>
public sealed class DiscountPercentage {

    public DiscountPercentage(decimal value) {
        Validate(value);

        Value = value;
    }

    public decimal Value { get; }

    public static DiscountPercentage None => new DiscountPercentage(0m);

    public bool IsFull => Value == 100m;

    private static void Validate(decimal value) {
        if (value < 0m) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A discount cannot be negative.");
        }
        if (value > 100m) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A discount cannot exceed 100 %.");
        }
        if (decimal.Round(value, 1) != value) {
            throw new ArgumentException("A discount carries at most one decimal place.", nameof(value));
        }
    }

}
