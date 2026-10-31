namespace LiquidityGap.Domain;

/// <summary>An ISO 4217 currency code, such as EUR or USD.</summary>
public sealed class Currency : IEquatable<Currency>
{
    public Currency(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length != 3 || !code.All(static c => c is >= 'A' and <= 'Z'))
        {
            throw new ArgumentException($"A currency code is three uppercase ASCII letters, got '{code}'.", nameof(code));
        }

        Code = code;
    }

    public string Code { get; }

    public bool Equals(Currency? other) => other is not null && Code == other.Code;

    public override bool Equals(object? obj) => Equals(obj as Currency);

    public override int GetHashCode() => Code.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Code;
}
