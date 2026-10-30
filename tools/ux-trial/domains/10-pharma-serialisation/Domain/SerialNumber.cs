using System.Text.RegularExpressions;

namespace Serialisation.Domain;

/// <summary>A randomised pack serial number (GS1 AI 21): 8 to 20 alphanumeric characters.</summary>
public sealed class SerialNumber {

    private static readonly Regex Alphabet = new Regex("^[A-Za-z0-9]*$", RegexOptions.Compiled);

    public SerialNumber(string value) {
        Value = value switch {
            null                         => throw new ArgumentNullException(nameof(value)),
            { Length: < 8 or > 20 }      => throw new ArgumentOutOfRangeException(nameof(value), "A serial number holds 8 to 20 characters."),
            _ when !Alphabet.IsMatch(value) => throw new ArgumentException("A serial number is alphanumeric.", nameof(value)),
            _                            => value
        };
    }

    public string Value { get; }

    public override bool Equals(object? obj) => obj is SerialNumber other && other.Value == Value;
    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    public override string ToString() => Value;

}
