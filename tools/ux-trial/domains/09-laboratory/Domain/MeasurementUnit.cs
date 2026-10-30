namespace Laboratory.Domain;

/// <summary>A UCUM-like unit symbol such as "mmol/L" or "g/dL".</summary>
public sealed class MeasurementUnit : IEquatable<MeasurementUnit> {

    public const int MaxLength = 16;

    public MeasurementUnit(string symbol) {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (symbol.Length > MaxLength) {
            throw new ArgumentOutOfRangeException(nameof(symbol), $"A unit symbol has at most {MaxLength} characters.");
        }
        Symbol = symbol;
    }

    public string Symbol { get; }

    public bool Equals(MeasurementUnit? other) => other is not null && string.Equals(other.Symbol, Symbol, StringComparison.Ordinal);
    public override bool Equals(object? obj) => Equals(obj as MeasurementUnit);
    public override int GetHashCode() => Symbol.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Symbol;

}
