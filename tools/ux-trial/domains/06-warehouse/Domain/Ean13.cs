namespace Warehouse.Domain;

/// <summary>A GTIN-13 (EAN-13) trade item number.</summary>
public sealed class Ean13 {

    public Ean13(string code) {
        Validate(code);
        Code = code;
    }

    public string Code { get; }

    /// <summary>The GS1 prefix: 300-379 is France, for instance.</summary>
    public string CountryPrefix => Code[..3];

    private static void Validate(string code) {
        ArgumentNullException.ThrowIfNull(code);
        if (code.Length != 13 || !code.All(char.IsAsciiDigit)) {
            throw new ArgumentException("An EAN-13 is exactly 13 digits.", nameof(code));
        }
        if (!Gs1.HasValidCheckDigit(code)) {
            throw new ArgumentException($"'{code}' has a wrong check digit.", nameof(code));
        }
    }

    public override bool Equals(object? obj) => obj is Ean13 other && other.Code == Code;

    public override int GetHashCode() => Code.GetHashCode();

    public override string ToString() => Code;

}
