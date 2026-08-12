#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="DateTimeOffset" /> values — the same contract as
///     <see cref="AnyInt32" />: constraints express what the surrounding code requires of the value, never what the
///     test asserts; contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" />
///     naming both sides; instances are immutable recipes, and each value is built to satisfy the constraints in one
///     draw.
/// </summary>
/// <remarks>
///     Constraints compare by <see cref="DateTimeOffset.UtcTicks" /> — the instant, not the local rendering — exactly
///     as <see cref="DateTimeOffset" />'s own comparison operators do. Unconstrained, generated values carry offset
///     <see cref="TimeSpan.Zero" /> (UTC); <see cref="WithOffset" /> / <see cref="WithOffsetBetween" /> opt the offset
///     dimension into a fixed or bounded whole-minute value so offset-sensitive code can be exercised. Values supplied
///     to <see cref="OneOf" /> are returned as given, offset included. There is deliberately no clock-relative
///     constraint (no "in the past/future"): a reproducible test pins its reference instants explicitly with
///     <see cref="After" /> and <see cref="Before" />.
/// </remarks>
public sealed class AnyDateTimeOffset : IAny<DateTimeOffset>, IHasRandomSource, IComparerSensitiveCardinality<DateTimeOffset>, IPoolInspection<DateTimeOffset> {

    // DateTimeOffset admits an offset in whole minutes within ±14:00.
    private const int MaxOffsetMinutes = 14 * 60;

    #region Statics members declarations

    internal static AnyDateTimeOffset Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyDateTimeOffset(source, OrdinalIntervalSpec.Unconstrained("DateTimeOffset", ordinal => V(Val(ordinal)), Ord(DateTimeOffset.MinValue), Ord(DateTimeOffset.MaxValue)), null, null, 0, 0);
    }

    private static ulong Ord(DateTimeOffset value) {
        return (ulong)value.UtcTicks;
    }

    private static DateTimeOffset Val(ulong ordinal) {
        return new DateTimeOffset((long)ordinal, TimeSpan.Zero);
    }

    private static string V(DateTimeOffset value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Render(TimeSpan offset) {
        return offset.ToString("c", CultureInfo.InvariantCulture);
    }

    private static string Join(DateTimeOffset[] values) {
        return string.Join(", ", values.Select(V));
    }

    /// <summary>Validates a supplied offset (whole minutes, within ±14:00) and returns it in whole minutes.</summary>
    private static int ValidateOffset(TimeSpan offset, string parameterName) {
        if (offset.Ticks % TimeSpan.TicksPerMinute != 0) { throw new ArgumentException("The offset must be a whole number of minutes.", parameterName); }
        if (offset < TimeSpan.FromMinutes(-MaxOffsetMinutes) || offset > TimeSpan.FromMinutes(MaxOffsetMinutes)) {
            throw new ArgumentOutOfRangeException(parameterName, offset, "The offset must be within ±14:00.");
        }

        return (int)(offset.Ticks / TimeSpan.TicksPerMinute);
    }

    #endregion

    #region Fields declarations

    // EVERY spelling the caller supplied, grouped by instant and in supplied order -- never filtered. An ordinal
    // carries the instant alone, so this is where the offset a caller wrote survives; and because the offset
    // dimension may be declared before or after the pool, whichever comes second has to be able to judge the WHOLE
    // supplied set. Filtering here is what used to make the two declaration orders disagree: the earlier one
    // collapsed same-instant spellings to whichever was written first, and the later one then judged that
    // arbitrary survivor -- refusing a pool that demonstrably held an admissible value.
    private readonly IReadOnlyDictionary<ulong, DateTimeOffset[]>? _allowedOriginals;
    private readonly ConstraintCall?                               _offsetConstraint;
    private readonly int                                           _offsetMaxMinutes;
    private readonly int                                           _offsetMinMinutes;
    private readonly RandomSource                                  _source;
    private readonly OrdinalIntervalSpec                           _spec;

    #endregion

    private AnyDateTimeOffset(RandomSource source, OrdinalIntervalSpec spec, IReadOnlyDictionary<ulong, DateTimeOffset[]>? allowedOriginals,
                             ConstraintCall? offsetConstraint, int offsetMinMinutes, int offsetMaxMinutes) {
        _source           = source;
        _spec             = spec;
        _allowedOriginals = allowedOriginals;
        _offsetConstraint = offsetConstraint;
        _offsetMinMinutes = offsetMinMinutes;
        _offsetMaxMinutes = offsetMaxMinutes;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<DateTimeOffset>.DistinctCardinality => _spec.Cardinality;

    // The instants are the bound only while one instant has one spelling. A declared offset RANGE breaks that:
    // Generate draws a minute inside it, so the same instant comes back as any of (max - min + 1) DateTimeOffset
    // values -- equal to each other under the default comparer, which compares instants, and distinct under a finer
    // one. A pool short-circuits the draw before the offset is chosen, picking one spelling per instant, so it
    // keeps the bound whatever range is declared. Refusing to count is the honest answer, not a guess at the
    // product: a coarser comparer would make it wrong in the other direction.
    long? IComparerSensitiveCardinality<DateTimeOffset>.CardinalityUnderACustomComparer =>
        _allowedOriginals is null && _offsetMinMinutes != _offsetMaxMinutes ? null : _spec.Cardinality;

    bool ICardinalityHint<DateTimeOffset>.Contains(DateTimeOffset value) => _spec.Contains(Ord(value));

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067). Val projects the engine's
    // ordinal back to the caller's own type.
    bool IPoolInspection<DateTimeOffset>.IsPooled => _spec.IsPooled;

    // No offset special case on either side. The offset dimension is carried into the engine as an exclusion over
    // the instants no supplied spelling can satisfy, so the engine's own report already knows about it and names
    // it -- alongside every other constraint refusing the same instant, which is what a rejection owes its reader.
    IReadOnlyList<DateTimeOffset> IPoolInspection<DateTimeOffset>.GetSurvivors() => _spec.GetSurvivors(Supplied);

    IReadOnlyList<PoolRejection<DateTimeOffset>> IPoolInspection<DateTimeOffset>.GetRejections() => _spec.GetRejections(Supplied);

    /// <summary>
    ///     The value as the caller supplied it, recovered from the ordinal — the same projection
    ///     <see cref="Generate" /> uses, so a survivor is what a draw actually yields. An ordinal carries only the
    ///     instant, so rebuilding from it would report a value whose offset the draw never returns. Of the spellings
    ///     the caller wrote for one instant, the first the declared offset admits: it is the one the draw returns,
    ///     and choosing it here rather than when the pool was declared is what lets the offset be declared on either
    ///     side of it.
    /// </summary>
    private DateTimeOffset Supplied(ulong ordinal) {
        if (_allowedOriginals is null) { return Val(ordinal); }
        if (!_allowedOriginals.TryGetValue(ordinal, out DateTimeOffset[]? spellings) || spellings is null) { return Val(ordinal); }

        int admitted = Array.FindIndex(spellings, SatisfiesDeclaredOffset);

        // A negative index is reached only for an instant the offset refuses outright, which the engine reports as
        // a rejection: the caller is owed a value they wrote, so the first spelling is the honest one to hand back.
        return admitted >= 0 ? spellings[admitted] : spellings[0];
    }

    /// <summary>Requires an instant strictly after <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset After(DateTimeOffset instant) {
        return With(_spec.WithMinimumAbove(Ord(instant), ConstraintCall.Of(nameof(After), V(instant))));
    }

    /// <summary>Requires an instant at or after <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset AfterOrEqualTo(DateTimeOffset instant) {
        return With(_spec.WithMinimum(Ord(instant), ConstraintCall.Of(nameof(AfterOrEqualTo), V(instant))));
    }

    /// <summary>Requires an instant strictly before <paramref name="instant" />.</summary>
    /// <param name="instant">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Before(DateTimeOffset instant) {
        return With(_spec.WithMaximumBelow(Ord(instant), ConstraintCall.Of(nameof(Before), V(instant))));
    }

    /// <summary>Requires an instant at or before <paramref name="instant" />.</summary>
    /// <param name="instant">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset BeforeOrEqualTo(DateTimeOffset instant) {
        return With(_spec.WithMaximum(Ord(instant), ConstraintCall.Of(nameof(BeforeOrEqualTo), V(instant))));
    }

    /// <summary>Requires an instant within the inclusive range [<paramref name="start" />, <paramref name="end" />].</summary>
    /// <param name="start">The inclusive lower bound.</param>
    /// <param name="end">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="start" /> is after <paramref name="end" />.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Between(DateTimeOffset start, DateTimeOffset end) {
        if (start > end) { throw new ArgumentException($"The start ({V(start)}) must be at or before the end ({V(end)}).", nameof(start)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(start), V(end));

        return With(_spec.WithMinimum(Ord(start), constraint).WithMaximum(Ord(end), constraint));
    }

    /// <summary>
    ///     Requires the instant to fall on a lattice of <paramref name="granularity" /> from
    ///     <see cref="DateTimeOffset.MinValue" /> — a round instant (a whole second, a quarter-hour, a whole day),
    ///     built on the grid rather than snapped after the fact, so tick-precision values never surprise a
    ///     serialization round-trip. Declared once per generator.
    /// </summary>
    /// <param name="granularity">The lattice step; must be strictly positive. A granularity of one tick adds no constraint.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="granularity" /> is not strictly positive.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithGranularity(TimeSpan granularity) {
        if (granularity <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "The granularity must be strictly positive."); }

        string rendered = granularity.ToString("c", CultureInfo.InvariantCulture);

        return With(_spec.WithStep((ulong)granularity.Ticks, Ord(DateTimeOffset.MinValue), ConstraintCall.Of(nameof(WithGranularity), rendered)));
    }

    /// <summary>
    ///     Pins the offset dimension to <paramref name="offset" /> — every generated value carries exactly that offset,
    ///     rather than the default <see cref="TimeSpan.Zero" />. The instant is tightened so the value stays valid at
    ///     the domain edges. Declared once per generator.
    /// </summary>
    /// <param name="offset">The offset to pin; a whole number of minutes, within ±14:00.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="offset" /> is not a whole number of minutes.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="offset" /> is outside ±14:00.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithOffset(TimeSpan offset) {
        int minutes = ValidateOffset(offset, nameof(offset));

        return WithOffsetRange(minutes, minutes, ConstraintCall.Of(nameof(WithOffset), Render(offset)));
    }

    /// <summary>
    ///     Draws the offset dimension from the inclusive range [<paramref name="minimum" />, <paramref name="maximum" />] —
    ///     a bounded, whole-minute offset — so a test can exercise offset-sensitive logic while staying valid. The
    ///     instant is tightened so every offset in the range stays valid. Declared once per generator.
    /// </summary>
    /// <param name="minimum">The inclusive lower offset; a whole number of minutes, within ±14:00.</param>
    /// <param name="maximum">The inclusive upper offset; a whole number of minutes, within ±14:00.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentException">Thrown when an offset is not a whole number of minutes, or <paramref name="minimum" /> is after <paramref name="maximum" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an offset is outside ±14:00.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset WithOffsetBetween(TimeSpan minimum, TimeSpan maximum) {
        int min = ValidateOffset(minimum, nameof(minimum));
        int max = ValidateOffset(maximum, nameof(maximum));
        if (min > max) { throw new ArgumentException($"The minimum offset ({Render(minimum)}) must be at or before the maximum ({Render(maximum)}).", nameof(minimum)); }

        return WithOffsetRange(min, max, ConstraintCall.Of(nameof(WithOffsetBetween), Render(minimum), Render(maximum)));
    }

    /// <summary>
    ///     Requires the instant to be one of the supplied values — returned as given, offset included. Declared once
    ///     per generator.
    /// </summary>
    /// <param name="values">The allowed values; duplicates (same instant) are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    [SuppressMessage(SonarRule.S3267.Category, SonarRule.S3267.Id, Justification = SuppressionJustification.S3267.ConditionReadsMutatedCollection)]
    public AnyDateTimeOffset OneOf(params DateTimeOffset[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        ConstraintCall applying = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME pool is a no-op in the engine, which returns itself rather than conflicting, so it
        // must be one here too: rebuilding would repeat whatever this method records beside the spec.
        OrdinalIntervalSpec allowed = _spec.WithAllowed(values.Select(Ord).ToArray(), applying);
        if (ReferenceEquals(allowed, _spec)) { return this; }

        // Every spelling the caller wrote, grouped by instant and in supplied order. Nothing is dropped for
        // carrying the wrong offset: that judgement belongs to the offset dimension, which is applied below and
        // may equally be applied later, and it needs the whole set to reach the same verdict either way.
        Dictionary<ulong, List<DateTimeOffset>> originals = [];
        foreach (DateTimeOffset value in values) {
            ulong ordinal = Ord(value);
            if (!originals.ContainsKey(ordinal)) { originals.Add(ordinal, []); }

            originals[ordinal].Add(value);
        }

        Dictionary<ulong, DateTimeOffset[]> supplied = originals.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray());

        return new AnyDateTimeOffset(_source, NarrowedToTheDeclaredOffset(allowed, supplied, applying, _offsetMinMinutes, _offsetMaxMinutes, _offsetConstraint),
                                     supplied, _offsetConstraint, _offsetMinMinutes, _offsetMaxMinutes);
    }

    /// <summary>Requires the instant to be none of the supplied values (compared by instant).</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset Except(params DateTimeOffset[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return With(_spec.WithExcluded(values.Select(Ord).ToArray(), ConstraintCall.Of(nameof(Except), Join(values))));
    }

    /// <summary>
    ///     Requires the instant to differ from <paramref name="value" /> (compared by instant) — typically an existing
    ///     value the test already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent
    ///     at the call site.
    /// </summary>
    /// <param name="value">The value the generated instant must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyDateTimeOffset DifferentFrom(DateTimeOffset value) {
        return With(_spec.WithExcluded([Ord(value)], ConstraintCall.Of(nameof(DifferentFrom), V(value))));
    }

    /// <inheritdoc />
    public DateTimeOffset Generate() {
        SeededRandom random  = _source.Current;
        ulong        ordinal = _spec.GenerateOrdinal(random);
        // Supplied picks among the spellings without drawing: the choice is decided by the declared offset, not by
        // chance, so a pooled generator consumes exactly one random number whatever the pool holds (ADR-0049).
        if (_allowedOriginals is not null) { return Supplied(ordinal); }
        if (_offsetConstraint is null) { return Val(ordinal); }

        int minutes = _offsetMinMinutes == _offsetMaxMinutes
                          ? _offsetMinMinutes
                          : _offsetMinMinutes + random.Next(_offsetMaxMinutes - _offsetMinMinutes + 1);
        TimeSpan offset = TimeSpan.FromMinutes(minutes);

        // The instant domain was tightened when the offset was declared, so the local ticks stay valid here.
        return new DateTimeOffset((long)ordinal + offset.Ticks, offset);
    }

    /// <summary>Carries the offset state forward onto a new spec — every instant constraint routes through here.</summary>
    private AnyDateTimeOffset With(OrdinalIntervalSpec spec) {
        return new AnyDateTimeOffset(_source, spec, _allowedOriginals, _offsetConstraint, _offsetMinMinutes, _offsetMaxMinutes);
    }

    /// <summary>
    ///     <paramref name="spec" /> carrying the declared offset dimension, as an <b>exclusion</b> over the instants
    ///     no supplied spelling can satisfy.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The offset is a dimension the ordinal space does not carry, so it cannot be a bound the engine tests.
    ///         Recording it as an exclusion is what puts it inside the engine anyway: the excluded ordinals are gone
    ///         from the draw, and — because an exclusion is one of the engine's own declarations — the offset is
    ///         named as a culprit by the same report that names the bounds, together with anything else refusing the
    ///         same instant. That is <see cref="Except" />'s mechanism, reused rather than mirrored.
    ///     </para>
    ///     <para>
    ///         Called from both sides — from <c>OneOf</c> when the offset came first, and from
    ///         <see cref="WithOffsetRange" /> when the pool did — over the same unfiltered set of spellings. That is
    ///         what makes the two declaration orders reach one verdict (ADR-0030), and it holds only because nothing
    ///         upstream of here has already dropped a spelling.
    ///     </para>
    /// </remarks>
    private static OrdinalIntervalSpec NarrowedToTheDeclaredOffset(OrdinalIntervalSpec spec, IReadOnlyDictionary<ulong, DateTimeOffset[]> supplied,
                                                                   ConstraintCall applying, int minMinutes, int maxMinutes, ConstraintCall? offsetConstraint) {
        if (offsetConstraint is null) { return spec; }

        ulong[] refused = supplied.Where(entry => !entry.Value.Any(value => SatisfiesOffset(value, minMinutes, maxMinutes)))
                                  .Select(entry => entry.Key)
                                  .ToArray();
        if (refused.Length == 0) { return spec; }

        // Kept ahead of the engine's own emptiness check so the message names the offset dimension rather than
        // reporting a generic exhaustion: an offset no supplied spelling carries is the one thing worth saying.
        if (refused.Length == supplied.Count) { throw OffsetExcludesEveryPooledValue(applying, minMinutes, maxMinutes); }

        // The offset tags the exclusion -- it is what a reader must loosen -- but the conflict it may raise belongs
        // to `applying`, which from OneOf is the pool being written and not the offset accepted on an earlier line.
        return spec.WithExcluded(refused, offsetConstraint, applying);
    }

    [SuppressMessage(SonarRule.S125.Category, SonarRule.S125.Id, Justification = SuppressionJustification.S125.ProseNotDisabledCode)]
    private AnyDateTimeOffset WithOffsetRange(int minMinutes, int maxMinutes, ConstraintCall applying) {
        if (_offsetConstraint is not null) {
            if (_offsetMinMinutes == minMinutes && _offsetMaxMinutes == maxMinutes) { return this; }

            throw ConflictingAnyConstraintException.AlreadyDefined(applying, _offsetConstraint);
        }

        // Tighten the instant so local ticks = UtcTicks + offset stay in [0, MaxTicks] for every offset in the range;
        // the offset can then be drawn independently, never producing an out-of-range DateTimeOffset.
        long  minOffsetTicks = minMinutes * TimeSpan.TicksPerMinute;
        long  maxOffsetTicks = maxMinutes * TimeSpan.TicksPerMinute;
        ulong lowerUtc       = (ulong)Math.Max(0L, -minOffsetTicks);
        ulong upperUtc       = (ulong)(DateTimeOffset.MaxValue.UtcTicks - Math.Max(0L, maxOffsetTicks));

        OrdinalIntervalSpec spec = _spec.WithMinimum(lowerUtc, applying).WithMaximum(upperUtc, applying);

        // The same derivation OneOf runs, over the same unfiltered spellings, so declaring the offset after the pool
        // reaches the verdict declaring it before does (ADR-0030). It used to be a mirrored second filter here, over
        // a table OneOf had already collapsed -- which is exactly how the two orders came to disagree.
        if (_allowedOriginals is not null) {
            return new AnyDateTimeOffset(_source, NarrowedToTheDeclaredOffset(spec, _allowedOriginals, applying, minMinutes, maxMinutes, applying),
                                         _allowedOriginals, applying, minMinutes, maxMinutes);
        }

        return new AnyDateTimeOffset(_source, spec, _allowedOriginals, applying, minMinutes, maxMinutes);
    }

    /// <summary>Whether <paramref name="value" /> carries an offset the declared offset dimension admits.</summary>
    private bool SatisfiesDeclaredOffset(DateTimeOffset value) {
        return _offsetConstraint is null || SatisfiesOffset(value, _offsetMinMinutes, _offsetMaxMinutes);
    }

    private static bool SatisfiesOffset(DateTimeOffset value, int minMinutes, int maxMinutes) {
        double minutes = value.Offset.TotalMinutes;

        return minutes >= minMinutes && minutes <= maxMinutes;
    }

    // The range is passed in rather than read off the fields: when an offset is declared AFTER a pool, the fields
    // still hold the previous (undeclared) state at the point the contradiction is detected.
    private static ConflictingAnyConstraintException OffsetExcludesEveryPooledValue(ConstraintCall applying, int minMinutes, int maxMinutes) {
        string admitted = minMinutes == maxMinutes
                              ? Render(TimeSpan.FromMinutes(minMinutes))
                              : $"{Render(TimeSpan.FromMinutes(minMinutes))} to {Render(TimeSpan.FromMinutes(maxMinutes))}";

        return new ConflictingAnyConstraintException($"Cannot apply {applying} because no pooled value carries an offset it admits ({admitted}).");
    }

}
