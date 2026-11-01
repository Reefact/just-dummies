#if NET8_0_OR_GREATER
#region Usings declarations

using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="TimeOnly" /> values — the same contract as <see cref="AnyInt32" />:
///     constraints express what the surrounding code requires of the value, never what the test asserts;
///     contradictory constraints fail eagerly with a <see cref="ConflictingAnyConstraintException" /> naming both
///     sides; instances are immutable recipes, and each value is built to satisfy the constraints in one draw.
///     Available on the net8.0 target only, like the type itself. There is deliberately no clock-relative
///     constraint: a reproducible test pins its reference time of days explicitly with <see cref="After" /> and
///     <see cref="Before" />.
/// </summary>
public sealed class AnyTimeOnly : IAny<TimeOnly>, IHasRandomSource, ICardinalityHint<TimeOnly>, IPoolInspection<TimeOnly> {

    #region Statics members declarations

    internal static AnyTimeOnly Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyTimeOnly(source, Frame.Linear.Unconstrained(), Frame.Linear, []);
    }

    private static string V(TimeOnly value) {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }

    private static string Join(TimeOnly[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<Declaration> _declarations;
    private readonly Frame                      _frame;
    private readonly RandomSource               _source;
    private readonly OrdinalIntervalSpec        _spec;

    #endregion

    private AnyTimeOnly(RandomSource source, OrdinalIntervalSpec spec, Frame frame, IReadOnlyList<Declaration> declarations) {
        _source       = source;
        _spec         = spec;
        _frame        = frame;
        _declarations = declarations;
    }

    RandomSource? IHasRandomSource.Source => _source;

    long? ICardinalityHint<TimeOnly>.DistinctCardinality => _spec.Cardinality;

    bool ICardinalityHint<TimeOnly>.Contains(TimeOnly value) => _spec.Contains(_frame.Ord(value));

    // Explicit, like the cardinality hint above: an inspection answers a maintenance question and does not
    // belong in the completion list a caller writes constraints in (ADR-0067). Val projects the engine's
    // ordinal back to the caller's own type.
    bool IPoolInspection<TimeOnly>.IsPooled => _spec.IsPooled;

    IReadOnlyList<TimeOnly> IPoolInspection<TimeOnly>.GetSurvivors() => _spec.GetSurvivors(_frame.Val);

    IReadOnlyList<PoolRejection<TimeOnly>> IPoolInspection<TimeOnly>.GetRejections() => _spec.GetRejections(_frame.Val);

    /// <summary>Requires a time of day strictly after <paramref name="time" />.</summary>
    /// <param name="time">The exclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly After(TimeOnly time) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(After), V(time));

        return Declare(new Declaration(constraint, [time], (frame, spec) => spec.WithMinimumAbove(frame.Ord(time), constraint)));
    }

    /// <summary>Requires a time of day at or after <paramref name="time" />.</summary>
    /// <param name="time">The inclusive lower bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly AfterOrEqualTo(TimeOnly time) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(AfterOrEqualTo), V(time));

        return Declare(new Declaration(constraint, [time], (frame, spec) => spec.WithMinimum(frame.Ord(time), constraint)));
    }

    /// <summary>Requires a time of day strictly before <paramref name="time" />.</summary>
    /// <param name="time">The exclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Before(TimeOnly time) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(Before), V(time));

        return Declare(new Declaration(constraint, [time], (frame, spec) => spec.WithMaximumBelow(frame.Ord(time), constraint)));
    }

    /// <summary>Requires a time of day at or before <paramref name="time" />.</summary>
    /// <param name="time">The inclusive upper bound.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly BeforeOrEqualTo(TimeOnly time) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(BeforeOrEqualTo), V(time));

        return Declare(new Declaration(constraint, [time], (frame, spec) => spec.WithMaximum(frame.Ord(time), constraint)));
    }

    /// <summary>
    ///     Requires a time of day within the window from <paramref name="start" /> to <paramref name="end" />, both
    ///     included. A <paramref name="start" /> after the <paramref name="end" /> names a window that crosses midnight —
    ///     <c>Between(22:00, 06:00)</c> is the night, as <see cref="TimeOnly.IsBetween" /> reads it — drawn uniformly over
    ///     its two segments. <c>Between(t, t)</c> stays the single value <c>t</c>.
    /// </summary>
    /// <remarks>
    ///     Once a window crosses midnight, every bound is read along its direction, whatever the order the calls were
    ///     written in: <c>After(23:00)</c> moves its start, <c>Before(02:00)</c> its end, and a time outside the window
    ///     admits no value. A granularity that does not divide a day has no single grid across midnight and is refused
    ///     there (ADR-0125).
    /// </remarks>
    /// <param name="start">The inclusive start of the window.</param>
    /// <param name="end">The inclusive end of the window.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Between(TimeOnly start, TimeOnly end) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(Between), V(start), V(end));

        if (start > end && _frame.Window is null) { return CrossMidnight(start, end, constraint); }

        return Declare(new Declaration(constraint, [start, end], (frame, spec) => spec.WithMinimum(frame.Ord(start), constraint).WithMaximum(frame.Ord(end), constraint)));
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
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly WithGranularity(TimeSpan granularity) {
        if (granularity <= TimeSpan.Zero) { throw new ArgumentOutOfRangeException(nameof(granularity), granularity, "The granularity must be strictly positive."); }

        string         rendered   = granularity.ToString("c", CultureInfo.InvariantCulture);
        ConstraintCall constraint = ConstraintCall.Of(nameof(WithGranularity), rendered);
        bool           dividesDay = TimeSpan.TicksPerDay % granularity.Ticks == 0;

        return Declare(new Declaration(constraint, [], (frame, spec) => spec.WithStep((ulong)granularity.Ticks, frame.Ord(TimeOnly.MinValue), constraint), dividesDay));
    }

    /// <summary>Requires the time of day to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly OneOf(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));

        return Declare(new Declaration(constraint, [], (frame, spec) => spec.WithAllowed(values.Select(frame.Ord).ToArray(), constraint)));
    }

    /// <summary>
    ///     Requires the time of day to be one of the supplied values — the <see cref="IEnumerable{T}" /> counterpart of
    ///     <see cref="OneOf(TimeOnly[])" />, for a set already held as a sequence (a list, a LINQ result, values loaded at
    ///     test setup). Same contract in every other respect.
    /// </summary>
    /// <param name="values">The allowed values; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly OneOf(IEnumerable<TimeOnly> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return OneOf(values as TimeOnly[] ?? values.ToArray());
    }

    /// <summary>Requires the time of day to be none of the supplied values.</summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Except(params TimeOnly[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { return this; }

        ConstraintCall constraint = ConstraintCall.Of(nameof(Except), Join(values));

        return Declare(new Declaration(constraint, [], (frame, spec) => spec.WithExcluded(values.Select(frame.Ord).ToArray(), constraint)));
    }

    /// <summary>
    ///     Requires the value to be none of the supplied values — the sequence form of <see cref="Except(TimeOnly[])" />,
    ///     for a set the test keeps, such as the values already used. The sequence is materialized once, and an empty one
    ///     excludes nothing (ADR-0122).
    /// </summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly Except(IEnumerable<TimeOnly> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return Except(values as TimeOnly[] ?? values.ToArray());
    }

    /// <summary>
    ///     Requires the time of day to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except(TimeOnly[])" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated time of day must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyTimeOnly DifferentFrom(TimeOnly value) {
        ConstraintCall constraint = ConstraintCall.Of(nameof(DifferentFrom), V(value));

        return Declare(new Declaration(constraint, [], (frame, spec) => spec.WithExcluded([frame.Ord(value)], constraint)));
    }

    /// <inheritdoc />
    public TimeOnly Generate() {
        return _frame.Val(_spec.GenerateOrdinal(_source.Current));
    }

    /// <summary>
    ///     Applies <paramref name="declaration" /> in the current frame. Inside a window that crosses midnight, a time
    ///     outside it and a granularity with no grid across midnight are refused, naming the window.
    /// </summary>
    private AnyTimeOnly Declare(Declaration declaration) {
        if (_frame.Window is ConstraintCall window) { Check(declaration, _frame, window, false); }

        return new AnyTimeOnly(_source, declaration.Apply(_frame, _spec), _frame, [.. _declarations, declaration]);
    }

    /// <summary>
    ///     Declares the first window that crosses midnight: the generator moves to a frame that starts the day at
    ///     <paramref name="start" />, where the window is one interval, and every earlier declaration is applied again
    ///     there — so the order the calls were written in does not change what they mean (ADR-0125).
    /// </summary>
    private AnyTimeOnly CrossMidnight(TimeOnly start, TimeOnly end, ConstraintCall constraint) {
        Frame               frame = Frame.Crossing(start, end, constraint);
        OrdinalIntervalSpec spec  = frame.Unconstrained();
        foreach (Declaration earlier in _declarations) {
            Check(earlier, frame, constraint, true);
            spec = earlier.Apply(frame, spec);
        }

        Declaration window = new(constraint, [start, end], (rotated, current) => current.WithMinimum(rotated.Ord(start), constraint).WithMaximum(rotated.Ord(end), constraint));

        return new AnyTimeOnly(_source, window.Apply(frame, spec), frame, [.. _declarations, window]);
    }

    private static void Check(Declaration declaration, Frame frame, ConstraintCall window, bool applyingIsTheWindow) {
        ConstraintCall applying = applyingIsTheWindow ? window : declaration.Constraint;
        ConstraintCall other    = applyingIsTheWindow ? declaration.Constraint : window;

        TimeOnly[] outside = declaration.Times.Where(frame.IsOutsideTheWindow).ToArray();
        if (outside.Length > 0) { throw ConflictingAnyConstraintException.OutsideTheWindow(applying, other, V(outside[0]), applyingIsTheWindow); }

        // A window inside the crossing one whose end comes first along it would leave two pieces: refused here, naming
        // the crossing window, before the engine reads its two bounds as a contradiction of the call with itself.
        if (declaration.Times.Length == 2 && frame.Ord(declaration.Times[0]) > frame.Ord(declaration.Times[1])) {
            throw ConflictingAnyConstraintException.WindowInTwoPieces(applying, other, V(declaration.Times[0]), V(declaration.Times[1]), applyingIsTheWindow);
        }

        if (!declaration.HasAGridAcrossMidnight) { throw ConflictingAnyConstraintException.GranularityAcrossMidnight(applying, other, applyingIsTheWindow); }
    }

    /// <summary>
    ///     One declaration as it was written, kept so it can be applied again in another frame: its call, the times of
    ///     day it bounds the value with, and how it narrows a specification.
    /// </summary>
    private sealed class Declaration {

        private readonly Func<Frame, OrdinalIntervalSpec, OrdinalIntervalSpec> _apply;

        internal Declaration(ConstraintCall constraint, TimeOnly[] times, Func<Frame, OrdinalIntervalSpec, OrdinalIntervalSpec> apply, bool hasAGridAcrossMidnight = true) {
            Constraint             = constraint;
            Times                  = times;
            _apply                 = apply;
            HasAGridAcrossMidnight = hasAGridAcrossMidnight;
        }

        internal ConstraintCall Constraint { get; }

        /// <summary>The times of day the declaration bounds the value with — none for a set, an exclusion or a grid.</summary>
        internal TimeOnly[] Times { get; }

        /// <summary>Whether the declaration still means one thing across midnight: false for a granularity that does not divide a day.</summary>
        internal bool HasAGridAcrossMidnight { get; }

        internal OrdinalIntervalSpec Apply(Frame frame, OrdinalIntervalSpec spec) {
            return _apply(frame, spec);
        }

    }

    /// <summary>
    ///     Where the day starts for the ordinal engine. The linear frame starts it at midnight, as the type does; a
    ///     window that crosses midnight starts it at its own start, so the window is one interval of the engine and is
    ///     drawn uniformly by construction, and the frame remembers that window to read later bounds along it.
    /// </summary>
    private sealed class Frame {

        private const ulong Day = TimeSpan.TicksPerDay;

        internal static readonly Frame Linear = new(0UL, null, 0UL);

        private readonly ulong _length;
        private readonly ulong _start;

        private Frame(ulong start, ConstraintCall? window, ulong length) {
            _start  = start;
            Window  = window;
            _length = length;
        }

        /// <summary>The window crossing midnight this frame starts at, or <c>null</c> for the linear frame.</summary>
        internal ConstraintCall? Window { get; }

        internal static Frame Crossing(TimeOnly start, TimeOnly end, ConstraintCall window) {
            ulong origin = (ulong)start.Ticks;

            return new Frame(origin, window, ((ulong)end.Ticks + Day - origin) % Day);
        }

        internal ulong Ord(TimeOnly value) {
            return ((ulong)value.Ticks + Day - _start) % Day;
        }

        internal TimeOnly Val(ulong ordinal) {
            return new TimeOnly((long)((ordinal + _start) % Day));
        }

        internal bool IsOutsideTheWindow(TimeOnly value) {
            return Window is not null && Ord(value) > _length;
        }

        internal OrdinalIntervalSpec Unconstrained() {
            return OrdinalIntervalSpec.Unconstrained("TimeOnly", ordinal => V(Val(ordinal)), 0UL, Day - 1UL);
        }

    }

}
#endif
