namespace JustDummies;

/// <summary>
///     What <see cref="Any.Fixed{T}" /> returns: a constant in the shape of a generator. It carries no random source,
///     because it draws nothing, and says so through <see cref="IReproducibilityHint" /> — a reproducible generator with
///     no source is one that takes no part in the draw, which is how a composition tells it from a foreign generator it
///     cannot replay (ADR-0118).
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class FixedAny<T> : IAny<T>, IReproducibilityHint, ICardinalityHint<T> {

    #region Fields declarations

    private readonly T _value;

    #endregion

    internal FixedAny(T value) {
        _value = value;
    }

    /// <summary>Always: a value drawn from no source is replayed by any seed.</summary>
    bool IReproducibilityHint.DrawsOnlyFromSource => true;

    /// <summary>One value, whatever the comparer: every call returns the same instance.</summary>
    long? ICardinalityHint<T>.DistinctCardinality => 1;

    /// <summary>Whether <paramref name="value" /> is the fixed value, under the default comparer.</summary>
    bool ICardinalityHint<T>.Contains(T value) {
        return EqualityComparer<T>.Default.Equals(_value, value);
    }

    /// <inheritdoc />
    public T Generate() {
        return _value;
    }

}
