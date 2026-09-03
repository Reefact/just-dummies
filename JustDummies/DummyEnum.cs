#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <typeparamref name="TEnum" /> values, drawn uniformly from the enum's
///     <b>declared</b> members — never from undeclared numeric values. Constraints narrow the pool
///     (<see cref="OneOf" />, <see cref="Except" />, <see cref="DifferentFrom" />), and a combination that empties it
///     fails with a <see cref="ConflictingDummyConstraintException" /> naming both sides — before any value is drawn,
///     and whichever order the chain was written in, since <see cref="AllowingCombinations" /> widens the universe
///     the pool is cut from wherever it appears.
/// </summary>
/// <remarks>
///     A <see cref="FlagsAttribute">[Flags]</see> enum declares bits meant to be combined, so its <b>valid</b> values
///     are the combinations, not only the declared members: <c>Read | Write</c> is a legitimate value the type never
///     declares. The declared-members default holds for those enums too — it is the only default valid for both enum
///     families, and switching on the attribute would make the draw depend on a type's metadata rather than on what
///     the test wrote. Opt in explicitly with <see cref="AllowingCombinations" /> to widen the draw to every
///     combination — <see cref="OneOf" /> asks for nothing so wide, since an allow-list is the pool itself, so
///     naming a combination there needs no opt-in.
/// </remarks>
/// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
public sealed class DummyEnum<TEnum> : IDummy<TEnum>, IHasRandomSource, ICardinalityHint<TEnum>, IPoolInspection<TEnum>
    where TEnum : struct, Enum {

    // The ceiling on the number of non-zero declared members AllowingCombinations() will enumerate. The universe is
    // materialized so the draw is exactly uniform over the DISTINCT values and the cardinality hint stays exact
    // (a per-member coin flip is neither: with a declared composite such as ReadWrite = Read | Write, several
    // subsets collapse onto the same value). Enumeration is 2^k, so it needs a bound; beyond it the constraint is
    // refused by name rather than silently degraded into a second, non-uniform regime.
    private const int MaxCombinableMembers = 20;

    #region Statics members declarations

    // The declared-members set and the [Flags] marking of an enum type are process constants; cached once per closed
    // generic type instead of reflecting on every Dummy.Enum<T>() call.
    private static readonly TEnum[] Declared = ((TEnum[])Enum.GetValues(typeof(TEnum))).Distinct().ToArray();
    private static readonly bool    IsFlags  = typeof(TEnum).IsDefined(typeof(FlagsAttribute), false);

    // The combination universe is a process constant too, but far more expensive than Declared, so it is built on
    // first use instead of on every closed generic type. A race computes it twice and stores the same set: benign.
    private static TEnum[]? _combinations;

    internal static DummyEnum<TEnum> Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (Declared.Length == 0) {
            throw DummyGenerationException.EnumDeclaresNoMembers(typeof(TEnum).Name);
        }

        return new DummyEnum<TEnum>(source, Declared, false, null, null, [], []);
    }

    /// <summary>
    ///     Every value obtained by OR-ing a non-empty subset of the declared members, plus the zero value when a zero
    ///     member is declared. Taking the declared members as the generating set — rather than the individual bits —
    ///     absorbs declared composites (<c>ReadWrite = Read | Write</c> contributes nothing new) without having to
    ///     decide which members "are" bits, and never invents the zero value for an enum that deliberately declares no
    ///     <c>None</c>.
    /// </summary>
    private static TEnum[] Combinations {
        get {
            if (_combinations is not null) { return _combinations; }

            ulong[]        generators = Declared.Select(ToUInt64).Where(bits => bits != 0UL).ToArray();
            HashSet<ulong> reachable  = [];
            foreach (ulong generator in generators) {
                // Union of what was reachable, what becomes reachable by adding this generator to it, and the
                // generator alone — the OR-closure, built without enumerating the 2^k subsets that collapse.
                foreach (ulong existing in reachable.ToArray()) { reachable.Add(existing | generator); }
                reachable.Add(generator);
            }

            // The empty subset ORs to zero, but that value belongs to the universe only when the enum defines it.
            if (Declared.Any(value => ToUInt64(value) == 0UL)) { reachable.Add(0UL); }

            _combinations = reachable.OrderBy(bits => bits).Select(ToEnum).ToArray();

            return _combinations;
        }
    }

    /// <summary>The value's underlying bits, whatever the enum's underlying type — signed members included.</summary>
    private static ulong ToUInt64(TEnum value) {
        // Convert.ToUInt64 throws on a negative signed member, so each signed width is read at its own size and
        // reinterpreted, exactly as the runtime stores it.
        return Type.GetTypeCode(typeof(TEnum)) switch {
            TypeCode.SByte => unchecked((ulong)Convert.ToSByte(value, CultureInfo.InvariantCulture)),
            TypeCode.Int16 => unchecked((ulong)Convert.ToInt16(value, CultureInfo.InvariantCulture)),
            TypeCode.Int32 => unchecked((ulong)Convert.ToInt32(value, CultureInfo.InvariantCulture)),
            TypeCode.Int64 => unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture)),
            _              => Convert.ToUInt64(value, CultureInfo.InvariantCulture)
        };
    }

    private static TEnum ToEnum(ulong bits) {
        return (TEnum)Enum.ToObject(typeof(TEnum), bits);
    }

    /// <summary>
    ///     Whether <paramref name="value" /> can be obtained by OR-ing declared members of a
    ///     <see cref="FlagsAttribute">[Flags]</see> enum — <c>Read | Write</c> yes, <c>(Access)99</c> no.
    /// </summary>
    /// <remarks>
    ///     Decided arithmetically rather than by searching <see cref="Combinations" />: OR-ing every declared member
    ///     whose bits the value already carries gives the largest reachable value within it, so the value is
    ///     reachable exactly when that OR is the value itself. Linear in the declared members, which is what lets
    ///     <see cref="OneOf" /> accept a combination on an enum too wide for the universe to be enumerated at all.
    /// </remarks>
    private static bool IsCombinationOfDeclaredMembers(TEnum value) {
        if (!IsFlags) { return false; }

        ulong bits = ToUInt64(value);
        // The empty combination is a value only where the enum declares it, exactly as Combinations decides.
        if (bits == 0UL) { return Declared.Any(member => ToUInt64(member) == 0UL); }

        ulong reachable = 0UL;
        foreach (TEnum member in Declared) {
            ulong memberBits = ToUInt64(member);
            if (memberBits != 0UL && (memberBits & bits) == memberBits) { reachable |= memberBits; }
        }

        return reachable == bits;
    }

    private static string V(TEnum value) {
        return value.ToString();
    }

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(TEnum[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<TEnum>? _allowed;
    private readonly ConstraintCall?       _allowedConstraint;
    private readonly bool                  _combinable;
    private readonly IReadOnlyList<TEnum>  _excluded;
    // Provenance for the diagnostic path only: _excluded drives every draw decision, while this records WHICH
    // constraint contributed which values, so an exhausted pool can name the exclusion that emptied it instead of
    // the allow-list it emptied. Same split as the interval engines (OrdinalIntervalSpec._exclusions).
    private readonly IReadOnlyList<(ConstraintCall Constraint, TEnum[] Values)> _exclusions;
    private readonly List<TEnum>           _pool;
    private readonly RandomSource          _source;
    private readonly IReadOnlyList<TEnum>  _universe;

    #endregion

    private DummyEnum(RandomSource source, IReadOnlyList<TEnum> universe, bool combinable,
                    IReadOnlyList<TEnum>? allowed, ConstraintCall? allowedConstraint, IReadOnlyList<TEnum> excluded,
                    IReadOnlyList<(ConstraintCall Constraint, TEnum[] Values)> exclusions) {
        _source            = source;
        _universe          = universe;
        _combinable        = combinable;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        _exclusions        = exclusions;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool.
        _pool = (allowed ?? universe).Where(value => !excluded.Contains(value)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is materialized once at construction, so its size is the exact number of values drawable.
    long? ICardinalityHint<TEnum>.DistinctCardinality => _pool.Count;

    // The pool is the exact draw set, so membership is a direct pool lookup.
    bool ICardinalityHint<TEnum>.Contains(TEnum value) => _pool.Contains(value);

    // Explicit, like the cardinality hint above (ADR-0067). Without OneOf the pool is the enum's own universe,
    // which the caller did not supply and has nothing to audit -- so IsPooled answers on the allow-list alone.
    bool IPoolInspection<TEnum>.IsPooled => _allowed is not null;

    IReadOnlyList<TEnum> IPoolInspection<TEnum>.GetSurvivors() {
        return _allowed is null ? Array.Empty<TEnum>() : new ReadOnlyCollection<TEnum>(_pool.ToArray());
    }

    IReadOnlyList<PoolRejection<TEnum>> IPoolInspection<TEnum>.GetRejections() {
        if (_allowed is null) { return Array.Empty<PoolRejection<TEnum>>(); }

        List<PoolRejection<TEnum>> rejections = [];
        foreach (TEnum value in _allowed) {
            if (!_excluded.Contains(value)) { continue; }

            List<DeclaredConstraint> culprits = _exclusions
                                                .Where(entry => entry.Values.Contains(value))
                                                .Select(entry => entry.Constraint.ToDeclaredConstraint())
                                                .ToList();

            rejections.Add(new PoolRejection<TEnum>(value, culprits));
        }

        return new ReadOnlyCollection<PoolRejection<TEnum>>(rejections);
    }

    /// <summary>
    ///     Widens the draw from the declared members to every <b>combination</b> of them — the values a
    ///     <see cref="FlagsAttribute">[Flags]</see> enum is designed to hold. Without it, a flags dummy carries at
    ///     most one bit and a branch reading two never runs.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The universe is every value obtained by OR-ing a non-empty subset of the declared members, plus the
    ///         zero value when a zero member is declared: <c>{ None = 0, Read = 1, Write = 2, Exec = 4 }</c> yields the
    ///         eight values <c>0</c>–<c>7</c>, while <c>{ Left = 1, Right = 2 }</c> yields only <c>1</c>, <c>2</c> and
    ///         <c>3</c> — never <c>0</c>, which that enum does not define. A declared composite adds nothing:
    ///         <c>ReadWrite = Read | Write</c> is already the combination of the two.
    ///     </para>
    ///     <para>
    ///         <see cref="Except" /> and <see cref="DifferentFrom" /> keep comparing by <b>equality</b>, here as
    ///         everywhere else: <c>Except(Read)</c> forbids the value <c>Read</c> and still allows
    ///         <c>Read | Write</c>. Beside <see cref="OneOf" /> this constraint changes nothing, in either order —
    ///         an explicit allow-list is a terminal enumeration of exact values, and a combination written in one is
    ///         accepted on its own account.
    ///     </para>
    /// </remarks>
    /// <returns>A new generator drawing from the combination universe.</returns>
    /// <exception cref="ConflictingDummyConstraintException">
    ///     Thrown when <typeparamref name="TEnum" /> is not declared <c>[Flags]</c>, when it declares more non-zero
    ///     members than the enumerable ceiling, or when the constraint contradicts a constraint already declared.
    /// </exception>
    public DummyEnum<TEnum> AllowingCombinations() {
        ConstraintCall constraint = ConstraintCall.Of(nameof(AllowingCombinations));
        if (_combinable) { return this; }

        if (!IsFlags) {
            throw ConflictingDummyConstraintException.EnumIsNotFlags(constraint, typeof(TEnum).Name);
        }

        int generators = Declared.Count(value => ToUInt64(value) != 0UL);
        if (generators > MaxCombinableMembers) {
            throw ConflictingDummyConstraintException.TooManyCombinableMembers(constraint, typeof(TEnum).Name, V(generators), V(MaxCombinableMembers));
        }

        return new DummyEnum<TEnum>(_source, Combinations, true, _allowed, _allowedConstraint, _excluded, _exclusions);
    }

    /// <summary>Requires the value to be one of the supplied members. Declared once per generator.</summary>
    /// <param name="values">
    ///     The allowed values; duplicates are ignored. Every value must be one the type defines — a declared member,
    ///     or, on a <see cref="FlagsAttribute">[Flags]</see> enum, any combination of declared members, with or
    ///     without <see cref="AllowingCombinations" />. The generator never yields a value the type does not define,
    ///     not even an explicitly supplied one.
    /// </param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a value the type does not define.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    [SuppressMessage(SonarRule.S3267.Category, SonarRule.S3267.Id, Justification = SuppressionJustification.S3267.LoopNamesFirstOffender)]
    public DummyEnum<TEnum> OneOf(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        foreach (TEnum value in values) {
            // Writing a combination IS asking for it, so the caller does not have to say so twice: on a [Flags] enum
            // a value reachable by OR-ing declared members is accepted here whether or not AllowingCombinations()
            // was declared. The universe is not widened to match — an allow-list is the pool, so nothing else ever
            // reads it — which is why this costs no enumeration and cannot meet the combinable-members ceiling.
            if (_universe.Contains(value) || IsCombinationOfDeclaredMembers(value)) { continue; }

            throw new ArgumentException($"The value {value} {DescribeOutsideUniverse()}", nameof(values));
        }

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingDummyConstraintException.AlreadyDefined(constraint, _allowedConstraint); }

        return new DummyEnum<TEnum>(_source, _universe, _combinable, values.Distinct().ToArray(), constraint, _excluded, _exclusions);
    }

    /// <summary>
    ///     Requires the value to be none of the supplied ones, compared by <b>equality</b> — under
    ///     <see cref="AllowingCombinations" /> too, so <c>Except(Read)</c> forbids <c>Read</c> and still allows
    ///     <c>Read | Write</c>.
    /// </summary>
    /// <param name="values">The forbidden values.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyEnum<TEnum> Except(params TEnum[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the value to differ from <paramref name="value" /> — typically an existing value the test already
    ///     holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public DummyEnum<TEnum> DifferentFrom(TEnum value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public TEnum Generate() {
        // The one satisfiability question a later constraint could still have answered, asked here — where the
        // specification is whole — rather than on whatever prefix of it was declared first.
        EnsureAnyValueRemains();

        return _pool[_source.Current.Next(_pool.Count)];
    }

    /// <summary>
    ///     Why a supplied value is one the type does not define, said in the terms that type gives: a
    ///     <see cref="FlagsAttribute">[Flags]</see> enum defines its combinations too, so the sentence names both.
    /// </summary>
    private static string DescribeOutsideUniverse() {
        // No advice to give any more: a combination of declared members is accepted where it is written, so a value
        // reaching this sentence is one no combination of them can produce, and AllowingCombinations() would not
        // help. Naming what the type defines beats naming a call that cannot fix it.
        return IsFlags
                   ? $"is neither a declared member of {typeof(TEnum).Name} nor a combination of its declared members: the generator only ever yields values the type defines."
                   : $"is not a declared member of {typeof(TEnum).Name}: the generator only ever yields declared members.";
    }

    private DummyEnum<TEnum> WithExcluded(TEnum[] values, ConstraintCall applying) {
        List<TEnum>                                            excluded   = [.. _excluded, .. values];
        List<(ConstraintCall Constraint, TEnum[] Values)>       exclusions = [.. _exclusions, (applying, values.ToArray())];

        return new DummyEnum<TEnum>(_source, _universe, _combinable, _allowed, _allowedConstraint, excluded, exclusions);
    }

    /// <summary>
    ///     Refuses a generator whose pool no exclusion left anything in.
    /// </summary>
    /// <remarks>
    ///     Asked at the draw rather than as each constraint arrives, because <see cref="AllowingCombinations" />
    ///     <b>widens</b> the universe the pool is cut from: <c>Except(Left, Right)</c> on a two-member flags enum
    ///     empties the declared members, yet the combination <c>Left | Right</c> is still there to draw once
    ///     combinations are allowed. Asked mid-chain, the same two constraints were refused in one order and
    ///     honoured in the other — a verdict belonging to the constraint set, read off whichever prefix of it
    ///     happened to be declared first.
    /// </remarks>
    private void EnsureAnyValueRemains() {
        if (_pool.Count > 0) { return; }

        // The exclusion that emptied the pool is what the sentence names, whichever order the chain put it in — so
        // the single-culprit case reads exactly as it did when the check fired on that very call.
        IReadOnlyList<ConstraintCall> culprits = ExcludingConstraintsInEffect();
        ConstraintCall                blamed   = culprits.Count > 0 ? culprits[0] : _allowedConstraint ?? ConstraintCall.Of(nameof(Generate));

        throw ConflictingDummyConstraintException.NoValueRemains(blamed, DescribeExhaustion(blamed));
    }

    /// <summary>
    ///     Why the pool is empty, naming the constraint that emptied it. An exclusion is what removes values, so it
    ///     is what the sentence must name; the allow-list is the victim, and naming it produced the self-referential
    ///     "no value OneOf(...) allows remains available" this replaces. Same shape as the interval engines'
    ///     DescribeExhaustion, so the two surfaces read alike.
    /// </summary>
    private string DescribeExhaustion(ConstraintCall applying) {
        IReadOnlyList<ConstraintCall> culprits = ExcludingConstraintsInEffect();

        // No exclusion bit: the pool was empty before any of them, so there is no culprit to name and the old
        // wording is the honest one. Reachable when a universe is itself empty, not when an exclusion emptied it.
        if (culprits.Count == 0) {
            if (_allowedConstraint is not null) { return $"no value {_allowedConstraint} allows remains available"; }

            return _combinable
                       ? $"no {typeof(TEnum).Name} combination remains available"
                       : $"no declared {typeof(TEnum).Name} member remains available";
        }

        string emptied;
        if (_allowedConstraint is not null) { emptied = $"every value {_allowedConstraint} allows"; }
        else if (_combinable) { emptied = $"every {typeof(TEnum).Name} combination"; }
        else { emptied = $"every declared {typeof(TEnum).Name} member"; }

        return $"{Forbids(culprits, applying)} {emptied}";
    }

    /// <summary>
    ///     The distinct exclusion constraints that actually caused the exhaustion — those forbidding at least one
    ///     value the universe and allow-list would otherwise permit. An exclusion whose values were already outside
    ///     the pool never bit, so naming it would mislead; first-declared order is preserved.
    /// </summary>
    private IReadOnlyList<ConstraintCall> ExcludingConstraintsInEffect() {
        List<ConstraintCall> names = [];
        foreach ((ConstraintCall constraint, TEnum[] values) in _exclusions) {
            if (names.Contains(constraint)) { continue; }
            if (values.Any(WouldAllowIgnoringExclusions)) { names.Add(constraint); }
        }

        return names;
    }

    /// <summary>Whether <paramref name="value" /> would be drawable if no exclusion were applied.</summary>
    private bool WouldAllowIgnoringExclusions(TEnum value) {
        return (_allowed ?? _universe).Contains(value);
    }

    /// <summary>
    ///     The subject of the exhaustion clause. A single culprit that is the constraint being applied becomes "it",
    ///     so the message reads "Cannot apply Except(Read) because it forbids ..." rather than repeating the
    ///     constraint on both sides of "because".
    /// </summary>
    private static string Forbids(IReadOnlyList<ConstraintCall> names, ConstraintCall applying) {
        if (names.Count == 1) { return names[0] == applying ? "it forbids" : $"{names[0]} forbids"; }

        return $"{string.Join(", ", names)} forbid";
    }

}
