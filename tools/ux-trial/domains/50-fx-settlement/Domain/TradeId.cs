namespace FxSettlement.Domain;

/// <summary>The desk's trade reference: <c>FX</c> followed by ten digits, not all zero (e.g. FX0000048213).</summary>
public sealed class TradeId : IEquatable<TradeId>
{
    public TradeId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 12 || !value.StartsWith("FX", StringComparison.Ordinal) || !value[2..].All(char.IsAsciiDigit))
        {
            throw new FormatException($"'{value}' is not a trade reference (FX + 10 digits).");
        }

        if (value[2..].All(c => c == '0'))
        {
            throw new ArgumentException("Trade reference FX0000000000 is reserved.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public bool Equals(TradeId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as TradeId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
