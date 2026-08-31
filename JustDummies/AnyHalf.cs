#if NET8_0_OR_GREATER
#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="Half" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes. NaN and the infinities are never generated nor accepted. Available on
///     the net8.0 target only, like the type itself.
/// </summary>
/// <remarks>
///     <para>
///         The refusal covers <b>arguments</b> too, not only draws: <c>Except(Half.NaN)</c> and a non-finite bound
///         are rejected with an <see cref="System.ArgumentException" />. A value that cannot be compared is not a
///         constraint — every comparison with NaN is false — and a NaN drawn into an arrangement the test never meant
///         to exercise fails an assertion nobody wrote.
///     </para>
///     <para>
///         When a non-finite value is genuinely part of the domain under test, draw it from an explicit pool instead:
///         <c>Any.OneOf(Half.NaN, (Half)1, (Half)2)</c>. The generic entry points carry no finiteness rule, by construction.
///         When the test <i>asserts on</i> the non-finite path, write the literal at the call site — it is the subject
///         of the test, not a dummy.
///     </para>
/// </remarks>
public sealed class AnyHalf : IAny<Half>, IHasRandomSource, ICardinalityHint<Half>, IPoolInspection<Half> {

    #region Statics members declarations

    internal static AnyHalf Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyHalf(source, ContinuousIntervalSpec.Unconstrained("Half", value => V((Half)value), value => (double)(Half)value, value => NextUp((Half)value), -(double)Half.MaxValue, (double)Half.MaxValue));
    }

    private static string V(Half value) {
        return value.ToString(null, CultureInfo.InvariantCulture);
    }

    private static string Join(Half[] values) {
        return string.Join(", ", values.Select(V));
    }

    /// <summary>The next representable half above <paramref name="value" /> — the exclusive-bound arithmetic.</summary>
    private static double NextUp(Half value) {
        short bits = BitConverter.HalfToInt16Bits(value);
        if (bits >= 0) { bits++; } else if (bits == short.MinValue) { bits = 1; } else { bits--; }

        Half next = BitConverter.Int16BitsToHalf(bits);

        return Half.IsInfinity(next) ? double.PositiveInfinity : (double)next;
    }

    /// <summary>
    ///     The position of a finite half on a ladder running from the lowest to the highest. The two zeros share a
    ///     rung because they compare equal, so the ladder counts what a <see cref="System.Collections.Generic.HashSet{T}" />
    ///     of halves would keep apart rather than what the bit patterns would.
    /// </summary>
    private static long Rung(Half value) {
        short bits      = BitConverter.HalfToInt16Bits(value);
        int   magnitude = bits & 0x7FFF;

        return bits < 0 ? -magnitude : magnitude;
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource           _source;
    private readonly ContinuousIntervalSpec _spec;

    #endregion

    private AnyHalf(RandomSource source, ContinuousIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <remarks>
    ///     <para>
    ///         The shared interval specification answers <c>null</c> for a floating-point range, counting representable
    ///         values being a type-specific concern it does not carry — so this row carries it. Sixteen bits hold
    ///         63 487 distinct finite values, which is under every cap the collections apply, so the count is
    ///         observable here in a way it is not for <see cref="AnyDouble" /> or <see cref="AnySingle" />: without it
    ///         a distinct set over halves accepts a floor no draw could ever reach, and only says so after exhausting
    ///         a budget sized from the ask rather than from the domain.
    ///     </para>
    ///     <para>
    ///         Exclusions are not subtracted. A cardinality is read as an upper bound — it bounds a redraw budget and
    ///         refuses an impossible count — and both uses stay sound when the bound is generous, while an
    ///         under-count would refuse a set the row can actually produce.
    ///     </para>
    /// </remarks>
    long? ICardinalityHint<Half>.DistinctCardinality => _spec.Cardinality ?? FiniteValuesInSpan();

    // The allow-list holds the doubles the supplied halves widen to, so membership tests the same widening.
    bool ICardinalityHint<Half>.Contains(Half value) => _spec.Contains((double)value);

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067).
    bool IPoolInspection<Half>.IsPooled => _spec.IsPooled;

    IReadOnlyList<Half> IPoolInspection<Half>.GetSurvivors() => _spec.GetSurvivors(value => (Half)value);

    IReadOnlyList<PoolRejection<Half>> IPoolInspection<Half>.GetRejections() => _spec.GetRejections(value => (Half)value);

    /// <summary>Requires a value strictly greater than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf Positive() {
        return new AnyHalf(_source, _spec.WithMinimumAbove(0d, ConstraintCall.Of(nameof(Positive))));
    }

    /// <summary>Requires a value strictly less than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf Negative() {
        return new AnyHalf(_source, _spec.WithMaximumBelow(0d, ConstraintCall.Of(nameof(Negative))));
    }

    /// <summary>Pins the value to exactly zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf Zero() {
        return new AnyHalf(_source, _spec.WithMinimum(0d, ConstraintCall.Of(nameof(Zero))).WithMaximum(0d, ConstraintCall.Of(nameof(Zero))));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf NonZero() {
        return new AnyHalf(_source, _spec.WithExcluded([0d], ConstraintCall.Of(nameof(NonZero))));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf GreaterThan(Half value) {
        ContinuousIntervalSpec.EnsureFinite((double)value, nameof(value));

        return new AnyHalf(_source, _spec.WithMinimumAbove((double)value, ConstraintCall.Of(nameof(GreaterThan), V(value))));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf GreaterThanOrEqualTo(Half value) {
        ContinuousIntervalSpec.EnsureFinite((double)value, nameof(value));

        return new AnyHalf(_source, _spec.WithMinimum((double)value, ConstraintCall.Of(nameof(GreaterThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" />.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf LessThan(Half value) {
        ContinuousIntervalSpec.EnsureFinite((double)value, nameof(value));

        return new AnyHalf(_source, _spec.WithMaximumBelow((double)value, ConstraintCall.Of(nameof(LessThan), V(value))));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf LessThanOrEqualTo(Half value) {
        ContinuousIntervalSpec.EnsureFinite((double)value, nameof(value));

        return new AnyHalf(_source, _spec.WithMaximum((double)value, ConstraintCall.Of(nameof(LessThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when a bound is not finite or <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf Between(Half minimum, Half maximum) {
        ContinuousIntervalSpec.EnsureFinite((double)minimum, nameof(minimum));
        ContinuousIntervalSpec.EnsureFinite((double)maximum, nameof(maximum));
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(minimum), V(maximum));

        return new AnyHalf(_source, _spec.WithMinimum((double)minimum, constraint).WithMaximum((double)maximum, constraint));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf OneOf(params Half[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (Half value in values) { ContinuousIntervalSpec.EnsureFinite((double)value, nameof(values)); }

        return new AnyHalf(_source, _spec.WithAllowed(values.Select(value => (double)value).ToArray(), ConstraintCall.Of(nameof(OneOf), Join(values))));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a non-finite value.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf Except(params Half[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (Half value in values) { ContinuousIntervalSpec.EnsureFinite((double)value, nameof(values)); }

        return new AnyHalf(_source, _spec.WithExcluded(values.Select(value => (double)value).ToArray(), ConstraintCall.Of(nameof(Except), Join(values))));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is not finite.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyHalf DifferentFrom(Half value) {
        ContinuousIntervalSpec.EnsureFinite((double)value, nameof(value));

        return new AnyHalf(_source, _spec.WithExcluded([(double)value], ConstraintCall.Of(nameof(DifferentFrom), V(value))));
    }

    /// <inheritdoc />
    public Half Generate() {
        return (Half)_spec.Generate(_source);
    }

    /// <summary>How many distinct finite halves the declared interval holds, counted on the ladder of <see cref="Rung" />.</summary>
    private long FiniteValuesInSpan() {
        double lower = Math.Max(_spec.Min, -(double)Half.MaxValue);
        double upper = Math.Min(_spec.Max, (double)Half.MaxValue);

        if (lower > upper) { return 0; }

        // A bound lands between two halves as often as on one, and the conversion rounds to the nearest rather than
        // inwards, so a single step on the type's own ladder puts an out-of-interval rounding back inside it.
        Half first = (Half)lower;
        if ((double)first < lower) { first = Half.BitIncrement(first); }

        Half last = (Half)upper;
        if ((double)last > upper) { last = Half.BitDecrement(last); }

        return first > last ? 0 : Rung(last) - Rung(first) + 1;
    }

}
#endif
