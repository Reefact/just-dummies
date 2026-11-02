namespace ClinicalTrial.Domain;

/// <summary>
/// A resolved field: either an observed value or the reason it is missing — never both, never neither.
/// A field that has not been entered yet is represented by a <see langword="null"/> <c>Observed&lt;T&gt;?</c>, not by this type.
/// </summary>
public sealed class Observed<T> : IEquatable<Observed<T>>
    where T : struct
{
    public Observed(T? value, MissingReason? reason)
    {
        if (value.HasValue && reason.HasValue)
        {
            throw new ArgumentException($"A field is either observed or missing, not both (value {value.Value}, reason {reason.Value}).", nameof(reason));
        }

        if (!value.HasValue && !reason.HasValue)
        {
            throw new ArgumentException("A resolved field carries a value or a missing reason.", nameof(reason));
        }

        if (reason.HasValue && !Enum.IsDefined(reason.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown missing reason.");
        }

        Value = value;
        Reason = reason;
    }

    public static Observed<T> Of(T value) => new(value, null);

    public static Observed<T> Missing(MissingReason reason) => new(null, reason);

    public T? Value { get; }

    public MissingReason? Reason { get; }

    public bool IsObserved => Value.HasValue;

    public bool IsMissing(MissingReason reason) => Reason == reason;

    public bool Equals(Observed<T>? other) =>
        other is not null && Nullable.Equals(Value, other.Value) && Reason == other.Reason;

    public override bool Equals(object? obj) => Equals(obj as Observed<T>);

    public override int GetHashCode() => HashCode.Combine(Value, Reason);

    public override string ToString() => IsObserved ? $"{Value}" : $"<{Reason}>";
}
