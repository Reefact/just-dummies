namespace Warehouse.Domain;

/// <summary>The supplier's batch or lot number, free text up to 20 characters.</summary>
public sealed class LotNumber {

    public LotNumber(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 20) {
            throw new ArgumentException("A lot number is at most 20 characters.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

}
