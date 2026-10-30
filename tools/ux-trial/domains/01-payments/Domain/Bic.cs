using System.Text.RegularExpressions;

namespace Payments.Domain;

/// <summary>A Business Identifier Code (ISO 9362): 8 or 11 characters.</summary>
public sealed class Bic : IEquatable<Bic> {

    public Bic(string code) {
        if (code is null || !Regex.IsMatch(code, "^[A-Z]{6}[A-Z0-9]{2}([A-Z0-9]{3})?$")) {
            throw new ArgumentException("A BIC is 4 letters (bank), 2 letters (country), 2 alphanumerics (location) and an optional 3-character branch.", nameof(code));
        }
        Code = code;
    }

    public string Code { get; }

    public string CountryCode => Code.Substring(4, 2);

    public bool IsPrimaryOffice => Code.Length == 8 || Code.EndsWith("XXX", StringComparison.Ordinal);

    public bool Equals(Bic? other) => other is not null && Code == other.Code;
    public override bool Equals(object? obj) => Equals(obj as Bic);
    public override int GetHashCode() => Code.GetHashCode();
    public override string ToString() => Code;

}
