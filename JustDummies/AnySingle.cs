#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="float" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes. NaN and the infinities are never generated nor accepted.
/// </summary>
/// <remarks>
///     <para>
///         The refusal covers <b>arguments</b> too, not only draws: <c>Except(float.NaN)</c> and a non-finite bound
///         are rejected with an <see cref="System.ArgumentException" />. A value that cannot be compared is not a
///         constraint — every comparison with NaN is false — and a NaN drawn into an arrangement the test never meant
///         to exercise fails an assertion nobody wrote.
///     </para>
///     <para>
///         When a non-finite value is genuinely part of the domain under test, draw it from an explicit pool instead:
///         <c>Any.OneOf(float.NaN, 1.0f, 2.0f)</c>. The generic entry points carry no finiteness rule, by construction.
///         When the test <i>asserts on</i> the non-finite path, write the literal at the call site — it is the subject
///         of the test, not a dummy.
///     </para>
/// </remarks>
public sealed class AnySingle : IAny<float>, IHasRandomSource, ICardinalityHint<float>, IPoolInspection<float> {

    #region Statics members declarations

    internal static AnySingle Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnySingle(source, ContinuousIntervalSpec.Unconstrained("Single", value => V((float)value), value => (float)value, value => NextUp((float)value), -float.MaxValue, float.MaxValue));
    }

    private static string V(float value) {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string Join(float[] values) {
        return string.Join(", ", values.Select(V));
    }

    /// <summary>The next representable float above <paramref name="value" /> — the exclusive-bound arithmetic.</summary>
    private static double NextUp(float value) {
        int bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        if (bits >= 0) { bits++; } else if (bits == int.MinValue) { bits = 1; } else { bits--; }

        float next = BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);

        return float.IsInfinity(next) ? double.PositiveInfinity : next;
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource           _source;
    private readonly ContinuousIntervalSpec _spec;

    #endregion

    private AnySingle(RandomSource source, ContinuousIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<float>.DistinctCardinality => _spec.Cardinality;

    // The allow-list holds the doubles the supplied floats widen to, so membership tests the same widening.
    bool ICardinalityHint<float>.Contains(float value) => _spec.Contains((double)value);

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067).
    bool IPoolInspection<float>.IsPooled => _spec.IsPooled;

    IReadOnlyList<float> IPoolInspection<float>.GetSurvivors() => _spec.GetSurvivors(value => (float)value);

    IReadOnlyList<PoolRejection<float>> IPoolInspection<float>.GetRejections() => _spec.GetRejections(value => (float)value);

    /// <summary>Requires a value strictly greater than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Positive() {
        return new AnySingle(_source, _spec.WithMinimumAbove(0d, ConstraintCall.Of(nameof(Positive))));
    }

    /// <summary>Requires a value strictly less than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Negative() {
        return new AnySingle(_source, _spec.WithMaximumBelow(0d, ConstraintCall.Of(nameof(Negative))));
    }

    /// <summary>Pins the value to exactly zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Zero() {
        return new AnySingle(_source, _spec.WithMinimum(0d, ConstraintCall.Of(nameof(Zero))).WithMaximum(0d, ConstraintCall.Of(nameof(Zero))));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle NonZero() {
        return new AnySingle(_source, _spec.WithExcluded([0d], ConstraintCall.Of(nameof(NonZero))));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle GreaterThan(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMinimumAbove(value, ConstraintCall.Of(nameof(GreaterThan), V(value))));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle GreaterThanOrEqualTo(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMinimum((double)value, ConstraintCall.Of(nameof(GreaterThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle LessThan(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMaximumBelow(value, ConstraintCall.Of(nameof(LessThan), V(value))));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle LessThanOrEqualTo(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithMaximum((double)value, ConstraintCall.Of(nameof(LessThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when a bound is not finite or <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Between(float minimum, float maximum) {
        ContinuousIntervalSpec.EnsureFinite(minimum, nameof(minimum));
        ContinuousIntervalSpec.EnsureFinite(maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(minimum), V(maximum));

        return new AnySingle(_source, _spec.WithMinimum((double)minimum, constraint).WithMaximum((double)maximum, constraint));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle OneOf(params float[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (float value in values) { ContinuousIntervalSpec.EnsureFinite(value, nameof(values)); }

        return new AnySingle(_source, _spec.WithAllowed(values.Select(value => (double)value).ToArray(), ConstraintCall.Of(nameof(OneOf), Join(values))));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle Except(params float[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (float value in values) { ContinuousIntervalSpec.EnsureFinite(value, nameof(values)); }

        return new AnySingle(_source, _spec.WithExcluded(values.Select(value => (double)value).ToArray(), ConstraintCall.Of(nameof(Except), Join(values))));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnySingle DifferentFrom(float value) {
        ContinuousIntervalSpec.EnsureFinite(value, nameof(value));
        return new AnySingle(_source, _spec.WithExcluded([(double)value], ConstraintCall.Of(nameof(DifferentFrom), V(value))));
    }

    /// <inheritdoc />
    public float Generate() {
        return (float)_spec.Generate(_source);
    }

}
