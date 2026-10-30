using System.Text.RegularExpressions;

namespace RealEstate.Domain;

/// <summary>
/// A French cadastral parcel reference: INSEE commune code (5 digits, or 2A/2B + 3 digits for
/// Corsica), a 3-digit prefix, a 2-character section and a 4-digit plan number, space-separated.
/// Example: "75056 000 AB 0123".
/// </summary>
public sealed class CadastralReference : IEquatable<CadastralReference> {

    private static readonly Regex Format = new Regex(@"^(\d{5}|2[AB]\d{3}) \d{3} [A-Z0-9]{2} \d{4}$", RegexOptions.CultureInvariant);

    public CadastralReference(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a cadastral reference.", nameof(value));
        }
        if (value.EndsWith("0000", StringComparison.Ordinal)) {
            throw new ArgumentException("A plan number starts at 0001.", nameof(value));
        }
        Value = value;
    }

    public string Value { get; }

    public string CommuneCode => Value.Substring(0, 5);

    public string Section => Value.Substring(10, 2);

    public bool IsInCorsica => CommuneCode.StartsWith("2A", StringComparison.Ordinal) || CommuneCode.StartsWith("2B", StringComparison.Ordinal);

    public string Department => IsInCorsica ? CommuneCode.Substring(0, 2) : CommuneCode.StartsWith("97", StringComparison.Ordinal) ? CommuneCode.Substring(0, 3) : CommuneCode.Substring(0, 2);

    public bool Equals(CadastralReference? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => Equals(obj as CadastralReference);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
