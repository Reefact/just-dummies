using System.Text.RegularExpressions;

namespace Education.Domain;

// A student number such as STU-2025-00042: the intake year, then a five-digit sequence.
public sealed class StudentNumber {

    private static readonly Regex Format = new Regex(@"^STU-(19|20)\d{2}-\d{5}$", RegexOptions.Compiled);

    public StudentNumber(string value) {
        ArgumentNullException.ThrowIfNull(value);
        if (!Format.IsMatch(value)) {
            throw new ArgumentException($"'{value}' is not a student number (STU-yyyy-nnnnn).", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public int IntakeYear => int.Parse(Value.Substring(4, 4));

    public override bool Equals(object? obj) => obj is StudentNumber other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}
