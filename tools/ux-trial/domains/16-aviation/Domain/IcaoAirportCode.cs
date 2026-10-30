namespace Aviation.Domain;

/// <summary>A four-letter ICAO location indicator, such as LFPG.</summary>
public sealed class IcaoAirportCode {

    public IcaoAirportCode(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != 4) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "An ICAO code has four letters.");
        }

        foreach (char letter in value) {
            if (!char.IsAsciiLetterUpper(letter)) {
                throw new ArgumentException($"'{value}' must be upper-case letters only.", nameof(value));
            }
        }

        Value = value;
    }

    public string Value { get; }

    /// <summary>The first letter names the ICAO region: E and L are Europe, K is the contiguous United States.</summary>
    public bool IsEuropean => Value[0] is 'E' or 'L';

    public override string ToString() => Value;

}
