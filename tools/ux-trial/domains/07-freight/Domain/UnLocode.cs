using System.Text.RegularExpressions;

namespace Freight.Domain;

/// <summary>A UN/LOCODE location: two-letter country, three-character location (letters or 2-9).</summary>
public sealed class UnLocode : IEquatable<UnLocode> {

    public UnLocode(string code) {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (!Regex.IsMatch(code, "^[A-Z]{2}[A-Z2-9]{3}$")) {
            throw new FormatException($"'{code}' is not a UN/LOCODE.");
        }

        Code = code;
    }

    public string Code { get; }

    public string Country => Code.Substring(0, 2);

    public bool Equals(UnLocode? other) => other is not null && Code == other.Code;

    public override bool Equals(object? obj) => Equals(obj as UnLocode);

    public override int GetHashCode() => Code.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Code;

}
