using System.Text.RegularExpressions;

namespace Payroll.Domain;

/// <summary>An employee identifier such as <c>EMP-004217</c>.</summary>
public sealed class EmployeeId {

    private static readonly Regex Format = new Regex(@"^EMP-\d{6}$", RegexOptions.Compiled);

    public EmployeeId(string value) {
        if (value is null) {
            throw new ArgumentNullException(nameof(value));
        }
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not an employee identifier (EMP-nnnnnn).", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override bool Equals(object? obj) => obj is EmployeeId other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;

}
