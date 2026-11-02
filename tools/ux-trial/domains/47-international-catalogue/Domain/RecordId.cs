namespace InternationalCatalogue.Domain;

/// <summary>
/// The identifier of a record in the union catalogue: the contributing agency's two upper-case letters and
/// eight digits, not all zero (e.g. <c>FR00412873</c>).
/// </summary>
public sealed class RecordId : IEquatable<RecordId>
{
    public RecordId(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 10)
        {
            throw new FormatException($"A record identifier has 10 characters, not {value.Length}.");
        }

        if (!char.IsAsciiLetterUpper(value[0]) || !char.IsAsciiLetterUpper(value[1]))
        {
            throw new FormatException("A record identifier starts with a two-letter upper-case agency code.");
        }

        if (value.AsSpan(2).ContainsAnyExcept("0123456789".AsSpan()))
        {
            throw new FormatException("A record identifier ends with eight ASCII digits.");
        }

        if (value.AsSpan(2).TrimStart('0').IsEmpty)
        {
            throw new FormatException("The record number cannot be zero.");
        }

        Value = value;
    }

    public string Value { get; }

    public string Agency => Value[..2];

    public bool Equals(RecordId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as RecordId);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
