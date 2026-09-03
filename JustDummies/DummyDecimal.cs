#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="decimal" /> values — the same contract as <see cref="DummyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingDummyConstraintException" /> naming both
///     sides; instances are immutable recipes. Exclusive bounds are expressed as the inclusive bound plus a point
///     exclusion, since <see cref="decimal" /> has no next-representable-value ladder.
/// </summary>
public sealed class DummyDecimal : IDummy<decimal>, IHasRandomSource, ICardinalityHint<decimal>, IPoolInspection<decimal> {

    /// <summary>The fewest decimal places <see cref="WithScale" /> accepts — a whole number, with no fractional part.</summary>
    private const int MinScale = 0;

    #region Statics members declarations

    internal static DummyDecimal Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new DummyDecimal(source, DecimalIntervalSpec.Unconstrained("Decimal", V));
    }

    private static string V(decimal value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(decimal[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly DecimalIntervalSpec _spec;

    #endregion

    private DummyDecimal(RandomSource source, DecimalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<decimal>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<decimal>.Contains(decimal value) => _spec.Contains(value);

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067).
    bool IPoolInspection<decimal>.IsPooled => _spec.IsPooled;

    IReadOnlyList<decimal> IPoolInspection<decimal>.GetSurvivors() => _spec.GetSurvivors(value => value);

    IReadOnlyList<PoolRejection<decimal>> IPoolInspection<decimal>.GetRejections() => _spec.GetRejections(value => value);

    /// <summary>Requires a value strictly greater than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal Positive() {
        return new DummyDecimal(_source, _spec.WithMinimumAbove(0m, ConstraintCall.Of(nameof(Positive))));
    }

    /// <summary>Requires a value strictly less than zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal Negative() {
        return new DummyDecimal(_source, _spec.WithMaximumBelow(0m, ConstraintCall.Of(nameof(Negative))));
    }

    /// <summary>Pins the value to exactly zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal Zero() {
        return new DummyDecimal(_source, _spec.WithMinimum(0m, ConstraintCall.Of(nameof(Zero))).WithMaximum(0m, ConstraintCall.Of(nameof(Zero))));
    }

    /// <summary>Requires a value different from zero.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal NonZero() {
        return new DummyDecimal(_source, _spec.WithExcluded([0m], ConstraintCall.Of(nameof(NonZero))));
    }

    /// <summary>Requires a value strictly greater than <paramref name="value" /> — the inclusive bound plus a point exclusion.</summary>
    /// <param name="value">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal GreaterThan(decimal value) {
        return new DummyDecimal(_source, _spec.WithMinimumAbove(value, ConstraintCall.Of(nameof(GreaterThan), V(value))));
    }

    /// <summary>Requires a value greater than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal GreaterThanOrEqualTo(decimal value) {
        return new DummyDecimal(_source, _spec.WithMinimum(value, ConstraintCall.Of(nameof(GreaterThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value strictly less than <paramref name="value" /> — the inclusive bound plus a point exclusion.</summary>
    /// <param name="value">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal LessThan(decimal value) {
        return new DummyDecimal(_source, _spec.WithMaximumBelow(value, ConstraintCall.Of(nameof(LessThan), V(value))));
    }

    /// <summary>Requires a value less than or equal to <paramref name="value" />.</summary>
    /// <param name="value">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal LessThanOrEqualTo(decimal value) {
        return new DummyDecimal(_source, _spec.WithMaximum(value, ConstraintCall.Of(nameof(LessThanOrEqualTo), V(value))));
    }

    /// <summary>Requires a value within the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />].</summary>
    /// <param name="minimum">The inclusive lower bound.</param>
    /// <param name="maximum">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="minimum" /> is greater than <paramref name="maximum" />.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal Between(decimal minimum, decimal maximum) {
        if (minimum > maximum) { throw new ArgumentException($"The minimum ({V(minimum)}) must be less than or equal to the maximum ({V(maximum)}).", nameof(minimum)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(minimum), V(maximum));

        return new DummyDecimal(_source, _spec.WithMinimum(minimum, constraint).WithMaximum(maximum, constraint));
    }

    /// <summary>
    ///     Requires the value to be expressible in <paramref name="scale" /> decimal places — a multiple of
    ///     10^-<paramref name="scale" /> (a valid amount in cents is <c>WithScale(2)</c>), drawn directly on that grid.
    ///     A value lattice, not a representation contract: the drawn value lies on the grid but is not padded with
    ///     trailing zeros. Declared once per generator.
    /// </summary>
    /// <param name="scale">The number of decimal places; in the inclusive range [0, 28].</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="scale" /> is outside the range [0, 28].</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal WithScale(int scale) {
        if (scale < MinScale || scale > DecimalIntervalSpec.MaxScale) { throw new ArgumentOutOfRangeException(nameof(scale), scale, $"The scale must be in the inclusive range [{MinScale}, {DecimalIntervalSpec.MaxScale}]."); }

        return new DummyDecimal(_source, _spec.WithScale(scale, ConstraintCall.Of(nameof(WithScale), scale.ToString(CultureInfo.InvariantCulture))));
    }

    /// <summary>Requires the value to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal OneOf(params decimal[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new DummyDecimal(_source, _spec.WithAllowed(values, ConstraintCall.Of(nameof(OneOf), Join(values))));
    }

    /// <summary>Requires the value to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal Except(params decimal[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new DummyDecimal(_source, _spec.WithExcluded(values.ToArray(), ConstraintCall.Of(nameof(Except), Join(values))));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyDecimal DifferentFrom(decimal value) {
        return new DummyDecimal(_source, _spec.WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value))));
    }

    /// <inheritdoc />
    public decimal Generate() {
        return _spec.Generate(_source);
    }

}
