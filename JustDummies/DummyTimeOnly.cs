#if NET8_0_OR_GREATER
#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="TimeOnly" /> values — the same contract as <see cref="DummyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingDummyConstraintException" /> naming both
///     sides; instances are immutable recipes, and each value is built to satisfy the constraints in one draw.
///     Available on the net8.0 target only, like the type itself. There is deliberately no clock-relative
///     constraint: a reproducible test pins its reference time of days explicitly with <see cref="After" /> and
///     <see cref="Before" />.
/// </summary>
public sealed class DummyTimeOnly : IDummy<TimeOnly>, IHasRandomSource, ICardinalityHint<TimeOnly>, IPoolInspection<TimeOnly> {

    #region Statics members declarations

    internal static DummyTimeOnly Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new DummyTimeOnly(source, OrdinalIntervalSpec.Unconstrained("TimeOnly", ordinal => V(Val(ordinal)), Ord(TimeOnly.MinValue), Ord(TimeOnly.MaxValue)));
    }

    private static ulong Ord(TimeOnly value) {
        return (ulong)value.Ticks;
    }

    private static TimeOnly Val(ulong ordinal) {
        return new TimeOnly((long)ordinal);
    }

    private static string V(TimeOnly value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join(TimeOnly[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly RandomSource        _source;
    private readonly OrdinalIntervalSpec _spec;

    #endregion

    private DummyTimeOnly(RandomSource source, OrdinalIntervalSpec spec) {
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<TimeOnly>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<TimeOnly>.Contains(TimeOnly value) => _spec.Contains(Ord(value));

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067). Val projects the engine's
    // ordinal back to the caller's own type.
    bool IPoolInspection<TimeOnly>.IsPooled => _spec.IsPooled;

    IReadOnlyList<TimeOnly> IPoolInspection<TimeOnly>.GetSurvivors() => _spec.GetSurvivors(Val);

    IReadOnlyList<PoolRejection<TimeOnly>> IPoolInspection<TimeOnly>.GetRejections() => _spec.GetRejections(Val);

    /// <summary>Requires a time of day strictly after <paramref name="time" />.</summary>
    /// <param name="time">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly After(TimeOnly time) {
        return new DummyTimeOnly(_source, _spec.WithMinimumAbove(Ord(time), ConstraintCall.Of(nameof(After), V(time))));
    }

    /// <summary>Requires a time of day at or after <paramref name="time" />.</summary>
    /// <param name="time">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly AfterOrEqualTo(TimeOnly time) {
        return new DummyTimeOnly(_source, _spec.WithMinimum(Ord(time), ConstraintCall.Of(nameof(AfterOrEqualTo), V(time))));
    }

    /// <summary>Requires a time of day strictly before <paramref name="time" />.</summary>
    /// <param name="time">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly Before(TimeOnly time) {
        return new DummyTimeOnly(_source, _spec.WithMaximumBelow(Ord(time), ConstraintCall.Of(nameof(Before), V(time))));
    }

    /// <summary>Requires a time of day at or before <paramref name="time" />.</summary>
    /// <param name="time">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly BeforeOrEqualTo(TimeOnly time) {
        return new DummyTimeOnly(_source, _spec.WithMaximum(Ord(time), ConstraintCall.Of(nameof(BeforeOrEqualTo), V(time))));
    }

    /// <summary>Requires a time of day within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly Between(TimeOnly start, TimeOnly end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(start), V(end));

        return new DummyTimeOnly(_source, _spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint));
    }

    /// <summary>
    ///     Requires the time of day to fall on a lattice of <paramref name="granularity" /> from
    ///     <see cref="TimeOnly.MinValue" /> — a round time of day (a whole second, a quarter-hour), built on the grid
    ///     rather than snapped after the fact, so tick-precision values never surprise a serialization round-trip.
    ///     Declared once per generator.
    /// </summary>
    /// <param name="granularity">The lattice step; must be strictly positive. A granularity of one tick adds no constraint.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="granularity" /> is not strictly positive.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly WithGranularity(TimeSpan granularity) {
        if (granularity <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "The granularity must be strictly positive."); }

        string rendered = granularity.ToString("c", CultureInfo.InvariantCulture);

        return new DummyTimeOnly(_source, _spec.WithStep((ulong)granularity.Ticks, Ord(TimeOnly.MinValue), ConstraintCall.Of(nameof(WithGranularity), rendered)));
    }

    /// <summary>Requires the time of day to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly OneOf(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new DummyTimeOnly(_source, _spec.WithAllowed(values.Select(Ord).ToArray(), ConstraintCall.Of(nameof(OneOf), Join(values))));
    }

    /// <summary>Requires the time of day to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly Except(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return new DummyTimeOnly(_source, _spec.WithExcluded(values.Select(Ord).ToArray(), ConstraintCall.Of(nameof(Except), Join(values))));
    }

    /// <summary>
    ///     Requires the time of day to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated time of day must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyTimeOnly DifferentFrom(TimeOnly value) {
        return new DummyTimeOnly(_source, _spec.WithExcluded([Ord(value)], ConstraintCall.Of(nameof(DifferentFrom), V(value))));
    }

    /// <inheritdoc />
    public TimeOnly Generate() {
        return Val(_spec.GenerateOrdinal(_source.Current));
    }

}
#endif
