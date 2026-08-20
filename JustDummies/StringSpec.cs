#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

#endregion

namespace JustDummies;

/// <summary>
///     The immutable specification behind <see cref="AnyString" />: length bounds, anchored fragments (prefix,
///     suffix, contained values), a character set, a letter casing, an optional allow-list (<c>OneOf</c>) and
///     excluded values — each remembering the constraint that set it, so a conflict message can name both sides.
///     Every mutation returns a new specification and cross-validates the whole eagerly: an <see cref="AnyString" />
///     that exists can always generate — save for an exclusion tight enough to leave a <i>shaped</i> string
///     unsatisfiable, the one failure deferred to generation (see remarks).
/// </summary>
/// <remarks>
///     <para>
///         Without an allow-list the specification is <b>constructive</b>: a generated string is laid out as
///         <c>prefix + filler + contained values + filler + suffix</c>, without overlap analysis, so the length budget
///         the fragments require is the plain sum of their lengths. A combination that only a cleverer overlapping
///         layout could satisfy is reported as a conflict — a deliberate V1 simplification, kept explicit in the
///         conflict messages. The character constraints — a family, a custom pool, the subtractions and the casing —
///         narrow the <b>filler</b> alphabet and nothing else, so an anchored fragment is kept verbatim whatever it
///         holds: it is a literal the caller wrote, not a character the generator drew (ADR-0077).
///     </para>
///     <para>
///         With an allow-list the specification is a <b>filter</b> instead: the caller supplied the values, so nothing
///         is laid out and every other constraint is answered by testing each pooled value. The layout budget
///         therefore does not apply — <c>Containing("ab").Containing("ba")</c> accepts the pooled <c>"aba"</c>, which
///         the constructive path could not have built — and satisfiability is the plain question "does any pooled
///         value survive every declared constraint?", answered eagerly at declaration. Exclusions are eager too on
///         that path, since the domain is finite and enumerable.
///     </para>
///     <para>
///         Exclusions (<c>DifferentFrom</c>/<c>Except</c>) on a <i>shaped</i> string are the one constraint not met by
///         construction: strings are not ordinal-mapped, so an excluded value is avoided by a <b>bounded</b> redraw of
///         the constructive layout — expected collisions are ≈ 0 for any non-trivial shape, the same bounded escape a
///         distinct collection uses to skip a duplicate. An exclusion tight enough to leave the shape unsatisfiable
///         (for example excluding every character a single-character length allows) is therefore the one case that
///         surfaces at generation, as a seed-bearing <see cref="AnyGenerationException" />, rather than eagerly at
///         declaration.
///     </para>
///     <para>
///         A draw is uniform over [minimum, maximum], where an undeclared maximum is the minimum plus the default
///         spread (ADR-0076): a declared bound therefore governs the value it is declared on, and the two spellings
///         of a range — <c>WithLengthBetween(a, b)</c> and <c>WithMinLength(a).WithMaxLength(b)</c> — draw alike.
///     </para>
/// </remarks>
internal sealed class StringSpec {

    /// <summary>
    ///     How far above its floor an unconstrained length reaches. Deliberately uncomfortable: a dummy short
    ///     enough to be convenient is one no length invariant is ever exercised against (ADR-0076).
    /// </summary>
    private const int DefaultLengthSpread = 1024;

    // Bounded escape for exclusions: even the tightest realistic satisfiable shape — a single free character in a
    // ~60-value pool with all but one value excluded — is found with overwhelming probability well within this many
    // draws, while a genuinely unsatisfiable exclusion fails fast. Mirrors the fixed floor of the collection dedup draw.
    private const int ExclusionRedrawBudget = 10_000;

    #region Statics members declarations

    internal static readonly StringSpec Unconstrained = new(null, null, 0, null, null, null,
                                                            null, null, null, null, [],
                                                            null, null, null, null, null, [],
                                                            [], null, null);

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Whether the declared family, casing and subtractions all admit the character.</summary>
    private static bool Admits(char character, CharacterSet? charset, LetterCasing? casing,
                               IReadOnlyList<(ConstraintCall Constraint, CharacterSet Removed)> subtractions) {
        return CharacterPools.Belongs(character, charset)
            && CharacterPools.MatchesCasing(character, casing)
            && subtractions.All(subtraction => !CharacterPools.Belongs(character, subtraction.Removed));
    }

    private static string Characters(int count) {
        return count == 1 ? "1 character" : $"{V(count)} characters";
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<string>? _allowed;
    private readonly ConstraintCall?        _allowedConstraint;
    private readonly LetterCasing?          _casing;
    private readonly ConstraintCall?        _casingConstraint;
    private readonly CharacterSet?          _charset;
    private readonly ConstraintCall?        _charsetConstraint;
    private readonly string?                _customPool;
    private readonly List<string>?          _effectiveAllowed;
    private readonly int?                   _exactLength;
    private readonly ConstraintCall?        _exactConstraint;
    private readonly IReadOnlyList<string>  _excluded;
    private readonly IReadOnlyList<(ConstraintCall Constraint, string[] Values)> _exclusions;
    private readonly string                 _fillerPool;
    private readonly IReadOnlyList<(string Fragment, ConstraintCall Constraint)> _fragments;
    // A subtraction removes a whole family rather than named values, and several accumulate.
    private readonly IReadOnlyList<(ConstraintCall Constraint, CharacterSet Removed)> _subtractions;
    private readonly int?                   _maxLength;
    private readonly ConstraintCall?        _maxConstraint;
    private readonly int                    _minLength;
    private readonly ConstraintCall?        _minConstraint;
    private readonly string?                _prefix;
    private readonly ConstraintCall?        _prefixConstraint;
    private readonly string?                _suffix;
    private readonly ConstraintCall?        _suffixConstraint;

    #endregion

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.EngineImmutableState)]
    private StringSpec(int?    exactLength, ConstraintCall? exactConstraint,
                       int     minLength,   ConstraintCall? minConstraint,
                       int?    maxLength,   ConstraintCall? maxConstraint,
                       string? prefix,      ConstraintCall? prefixConstraint,
                       string? suffix,      ConstraintCall? suffixConstraint,
                       IReadOnlyList<(string Fragment, ConstraintCall Constraint)> fragments,
                       CharacterSet? charset, ConstraintCall? charsetConstraint, string? customPool,
                       LetterCasing? casing,  ConstraintCall? casingConstraint,
                       IReadOnlyList<(ConstraintCall Constraint, string[] Values)> exclusions,
                       IReadOnlyList<(ConstraintCall Constraint, CharacterSet Removed)> subtractions,
                       IReadOnlyList<string>? allowed, ConstraintCall? allowedConstraint) {
        _exactLength       = exactLength;
        _exactConstraint   = exactConstraint;
        _minLength         = minLength;
        _minConstraint     = minConstraint;
        _maxLength         = maxLength;
        _maxConstraint     = maxConstraint;
        _prefix            = prefix;
        _prefixConstraint  = prefixConstraint;
        _suffix            = suffix;
        _suffixConstraint  = suffixConstraint;
        _fragments         = fragments;
        _charset           = charset;
        _charsetConstraint = charsetConstraint;
        _customPool        = customPool;
        _casing            = casing;
        _casingConstraint  = casingConstraint;
        _exclusions        = exclusions;
        _subtractions      = subtractions;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        // "Constrain once, draw many": the filler alphabet is settled here, never per draw. The universe is the
        // whole of ASCII and the family, the casing and the subtractions narrow it (ADR-0075).
        _fillerPool = customPool ?? new string(CharacterPools.Ascii.Where(character => Admits(character, charset, casing, subtractions)).ToArray());
        // The flat, deduplicated value list drives the redraw and the exhaustion message; the provenance in
        // _exclusions is consulted only when a conflict message must name the excluding constraint. Materialized
        // once here — "constrain once, draw many" — in first-declared order.
        _excluded = exclusions.SelectMany(pair => pair.Values).Distinct(StringComparer.Ordinal).ToList();
        // Same "constrain once, draw many" rule for the allow-list: the surviving pool is the exact domain the draw
        // samples, the cardinality a distinct collection gates on, and the set a satisfiability check counts.
        if (allowed is not null) { _effectiveAllowed = allowed.Where(Admits).ToList(); }
    }

    /// <summary>Fixes the exact length; declared once per generator.</summary>
    internal StringSpec WithExactLength(int length, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_exactConstraint == applying) { return this; }
        if (_exactConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _exactConstraint); }

        StringSpec candidate = new(length, applying, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Tightens the minimum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMinLength(int length, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (length <= _minLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, length, applying, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Tightens the maximum length; a looser bound than the current one is a no-op.</summary>
    internal StringSpec WithMaxLength(int length, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_maxLength is not null && length >= _maxLength) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, length, applying,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Anchors a prefix; declared once per generator.</summary>
    internal StringSpec WithPrefix(string prefix, ConstraintCall applying) {
        if (prefix is null) { throw new ArgumentNullException(nameof(prefix)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_prefixConstraint == applying) { return this; }
        if (_prefixConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _prefixConstraint); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   prefix, applying, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Anchors a suffix; declared once per generator.</summary>
    internal StringSpec WithSuffix(string suffix, ConstraintCall applying) {
        if (suffix is null) { throw new ArgumentNullException(nameof(suffix)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_suffixConstraint == applying) { return this; }
        if (_suffixConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _suffixConstraint); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, suffix, applying, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Adds a value the generated string must contain.</summary>
    internal StringSpec WithFragment(string fragment, ConstraintCall applying) {
        if (fragment is null) { throw new ArgumentNullException(nameof(fragment)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        List<(string Fragment, ConstraintCall Constraint)> fragments = [.. _fragments, (fragment, applying)];

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Restricts the character family; declared once per generator.</summary>
    internal StringSpec WithCharset(CharacterSet charset, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_charsetConstraint == applying) { return this; }
        if (_charsetConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _charsetConstraint); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   charset, applying, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     Restricts the filler to an explicit character pool — the general form of the named character sets.
    ///     Occupies the charset slot (declared once, and mutually exclusive with the named sets) and, because the
    ///     pool is the whole character definition, cannot combine with a casing. The pool is expected to be
    ///     distinct already.
    /// </summary>
    internal StringSpec WithCharPool(string pool, ConstraintCall applying) {
        if (pool is null) { throw new ArgumentNullException(nameof(pool)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_charsetConstraint == applying) { return this; }
        if (_charsetConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _charsetConstraint); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_casingConstraint == applying) { return this; }
        if (_casingConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _casingConstraint); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, applying, pool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Imposes a letter casing; declared once per generator.</summary>
    /// <summary>
    ///     Removes a whole family from the filler alphabet. Unlike a character set this occupies no slot and several
    ///     accumulate; re-declaring one removes what is already gone, which is inert rather than contradictory.
    /// </summary>
    internal StringSpec WithSubtraction(CharacterSet removed, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        if (_subtractions.Any(subtraction => subtraction.Constraint == applying)) { return this; }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   [.. _subtractions, (applying, removed)], _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    internal StringSpec WithCasing(LetterCasing casing, ConstraintCall applying) {
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_casingConstraint == applying) { return this; }
        if (_casingConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _casingConstraint); }
        // A custom pool and the constraint naming it are written together (WithCustomPool passes `applying, pool`),
        // so a declared pool always carries its name.
        if (_customPool is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _charsetConstraint!); }

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, casing, applying, _exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>Adds values the generated string must avoid; may be declared several times, the exclusions accumulate.</summary>
    [SuppressMessage(SonarRule.S3267.Category, SonarRule.S3267.Id, Justification = SuppressionJustification.S3267.ConditionReadsMutatedCollection)]
    internal StringSpec WithExcluded(IReadOnlyList<string> values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }

        // The applied constraint tags its own values, so a conflict message can name the exclusion that actually
        // emptied an allow-list rather than a shape constraint that merely borders it.
        List<(ConstraintCall Constraint, string[] Values)> exclusions = [.. _exclusions, (applying, values.ToArray())];

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, exclusions,
                                   _subtractions, _allowed, _allowedConstraint);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     Restricts the domain to an explicit allow-list; declared once per generator. From here on the specification
    ///     is a filter over the supplied values rather than a layout to build, so every other constraint — those
    ///     already declared and those declared later — narrows the pool instead of shaping a string.
    /// </summary>
    internal StringSpec WithAllowed(IReadOnlyList<string> values, ConstraintCall applying) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (applying is null) { throw new ArgumentNullException(nameof(applying)); }
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == applying) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _allowedConstraint); }

        string[] distinct = values.Distinct(StringComparer.Ordinal).ToArray();

        StringSpec candidate = new(_exactLength, _exactConstraint, _minLength, _minConstraint, _maxLength, _maxConstraint,
                                   _prefix, _prefixConstraint, _suffix, _suffixConstraint, _fragments,
                                   _charset, _charsetConstraint, _customPool, _casing, _casingConstraint, _exclusions,
                                   _subtractions, distinct, applying);

        return candidate.Validated(applying, this);
    }

    /// <summary>
    ///     The number of distinct values the specification can produce, or <c>null</c> when no allow-list bounds it —
    ///     a shaped string draws from a domain too wide, and too dependent on the layout, to count. Feeds
    ///     <see cref="ICardinalityHint{T}" />, so a distinct collection over a pooled generator fails eagerly.
    /// </summary>
    internal long? Cardinality => _effectiveAllowed?.Count;

    /// <summary>
    ///     Whether <paramref name="value" /> is one the specification could produce — the exact pool
    ///     <see cref="Generate" /> draws from when an allow-list is in force. Without one the answer is <c>false</c>
    ///     for every value: the two <see cref="ICardinalityHint{T}" /> members travel together, and a shaped string
    ///     advertises no cardinality, so a distinct collection never consults membership on that path (it gates on the
    ///     bound alone, then falls back to the bounded dedup draw). Answering "outside" is also the side the interface
    ///     documents as safe — it can only defer, never refuse a satisfiable specification.
    /// </summary>
    internal bool Contains(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return _effectiveAllowed is not null && _effectiveAllowed.Contains(value, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Whether an allow-list is in force. The specification is then a filter over supplied values and there is a
    ///     pool to report on; a shaped string has none. Feeds <see cref="IPoolInspection{T}.IsPooled" />.
    /// </summary>
    internal bool IsPooled => _effectiveAllowed is not null;

    /// <summary>
    ///     The supplied values satisfying every declared constraint, in the order they were supplied — the exact
    ///     domain <see cref="Generate" /> picks from. A method rather than a property: the surviving pool is the live list
    ///     the draw samples, so it is copied behind a read-only view rather than handed out for a caller to cast
    ///     back and mutate.
    /// </summary>
    internal IReadOnlyList<string> GetSurvivors() {
        return _effectiveAllowed is null
                   ? Array.Empty<string>()
                   : new ReadOnlyCollection<string>(_effectiveAllowed.ToArray());
    }

    /// <summary>
    ///     The supplied values no draw can yield, in the order they were supplied, each with the declared
    ///     constraints refusing it. Derived from the same <see cref="DeclaredConstraints" /> the pool filter runs
    ///     on, so a reported reason can never drift from the filtering it explains.
    /// </summary>
    internal IReadOnlyList<PoolRejection<string>> GetRejections() {
        if (_allowed is null) { return Array.Empty<PoolRejection<string>>(); }

        List<PoolRejection<string>> rejections = [];
        foreach (string value in _allowed) {
            if (Admits(value)) { continue; }

            List<DeclaredConstraint> culprits = DeclaredConstraints()
                                                .Where(entry => !entry.Admits(value))
                                                .Select(entry => entry.Constraint.ToDeclaredConstraint())
                                                .ToList();

            rejections.Add(new PoolRejection<string>(value, culprits));
        }

        return new ReadOnlyCollection<PoolRejection<string>>(rejections);
    }

    /// <summary>
    ///     Builds one string satisfying the whole specification. With an allow-list the draw is a uniform pick from
    ///     the surviving pool — every constraint was already applied to it, so there is nothing to redraw. Without
    ///     one the string is laid out directly, never generate-then-retry; the one redraw is to skip an excluded
    ///     value, a bounded escape (expected collisions ≈ 0 for any non-trivial shape) whose exhausted budget is
    ///     reported as the spent budget it is, with the seed to replay.
    /// </summary>
    internal string Generate(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        SeededRandom random = source.Current;
        if (_effectiveAllowed is not null) { return _effectiveAllowed[random.Next(_effectiveAllowed.Count)]; }
        if (_excluded.Count == 0) { return BuildCandidate(random); }

        for (int collisions = 0;;) {
            string candidate = BuildCandidate(random);
            if (!_excluded.Contains(candidate, StringComparer.Ordinal)) { return candidate; }
            if (++collisions >= ExclusionRedrawBudget) { throw Exhausted(source); }
        }
    }

    private string BuildCandidate(SeededRandom random) {
        int required     = RequiredLength();
        int effectiveMin = Math.Max(_minLength, required);
        // A declared maximum REPLACES the default spread (ADR-0076): the bound the caller wrote governs the value
        // they get, so a written range is the range drawn. Long arithmetic: a huge required length must saturate
        // instead of overflowing past int.MaxValue. The floor is honoured whatever the ceiling says — a maximum
        // below it is already refused at declaration.
        long ceiling      = _maxLength ?? (long)effectiveMin + DefaultLengthSpread;
        int  effectiveMax = (int)Math.Min(Math.Max(ceiling, effectiveMin), int.MaxValue);
        int  length        = _exactLength ?? random.NextInt32Inclusive(effectiveMin, effectiveMax);

        string pool         = FillerPool();
        int    fillerLength = length - required;
        int    before       = random.NextInt32Inclusive(0, fillerLength);
        int    after        = fillerLength - before;

        StringBuilder builder = new(length);
        if (_prefix is not null) { builder.Append(_prefix); }
        AppendFiller(builder, random, pool, before);
        foreach ((string fragment, ConstraintCall _) in _fragments) { builder.Append(fragment); }
        AppendFiller(builder, random, pool, after);
        if (_suffix is not null) { builder.Append(_suffix); }

        return builder.ToString();
    }

    private AnyGenerationException Exhausted(RandomSource source) {
        // A string generator draws only from its own source, so the seed replays the run fully — never the partial hint.
        Replay replay = Replay.Of(source);
        // The claim is the budget, not impossibility. The redraw is bounded, so an exhausted budget is overwhelming
        // evidence that almost nothing survives the exclusions — and the usual cause really is a shape with no value
        // left — but it is not a proof: a shape with one free value in a few hundred thousand exhausts the budget
        // most of the time and is still satisfiable. Reporting "unsatisfiable" would send a caller hunting for a
        // contradiction that need not exist.
        string message =
            $"Could not generate a string that satisfies the declared shape while excluding {DescribeExcluded()}: " +
            $"no candidate survived {V(ExclusionRedrawBudget)} draws. The redraw is bounded, so this is an exhausted " +
            "budget rather than a proof that no value remains — though the usual cause is a shape the exclusions " +
            "leave nothing of (excluding every value a fixed short length allows). Loosen the exclusions or widen " +
            "the shape. " +
            replay.Guidance;

        return new AnyGenerationException(message, replay.Seed);
    }

    private string DescribeExcluded() {
        return string.Join(", ", _excluded.Select(value => $"\"{value}\""));
    }

    /// <summary>
    ///     Cross-validates the whole specification. The layout checks belong to the constructive path only: once an
    ///     allow-list is in force nothing is laid out, so the single satisfiability question is whether any pooled
    ///     value survives every declared constraint. <paramref name="previous" /> is the specification this one was
    ///     derived from — it tells a conflict message which side was already narrowed and which is the new one.
    /// </summary>
    private StringSpec Validated(ConstraintCall applying, StringSpec previous) {
        ValidateLengthBounds(applying);
        ValidateFillerAlphabet(applying);
        if (_allowed is null) {
            ValidateFragmentBudget(applying);

            return this;
        }

        ValidateAllowedSurvives(applying, previous);

        return this;
    }

    /// <summary>
    ///     Refuses a family and a set of subtractions that leave no character to draw — <c>Numeric()</c> with
    ///     <c>WithoutNumeric()</c>, say. Only a shaped string is judged here: with a value set in force nothing is
    ///     laid out, so an empty alphabet forbids nothing and the surviving values answer for themselves.
    /// </summary>
    private void ValidateFillerAlphabet(ConstraintCall applying) {
        if (_allowed is not null || _fillerPool.Length > 0) { return; }

        ConstraintCall? family = _charsetConstraint;
        // Index from the front: the netstandard2.0 floor has no System.Index.
        ConstraintCall  culprit = _subtractions.Count > 0 ? _subtractions[_subtractions.Count - 1].Constraint : applying;

        throw ConflictingAnyConstraintException.Contradicts(applying,
                                                            ConstraintClaim.OfPhrase("the declared character family",
                                                                                     family is null ? "is left with no character at all" : $"{family} admits nothing once it is applied"),
                                                            ConstraintClaim.Of(culprit, "removes every character that remained"));
    }

    private void ValidateLengthBounds(ConstraintCall applying) {
        if (_exactLength is int exact) { ValidateExactAgainstBounds(applying, exact); }

        // Each bound is written as a pair with the constraint that set it. And this branch needs _minLength > max,
        // with max >= 0 because AnyString.WithMaxLength rejects a negative length — so _minLength > 0, which only
        // WithMinLength can produce, and it names the constraint as it sets the value.
        if (_maxLength is int max && _minLength > max) {
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                                ConstraintClaim.Of(_maxConstraint!, $"already caps the length at {V(max)}"),
                                                                                ConstraintClaim.Of(_minConstraint!, $"already requires at least {Characters(_minLength)}"));
        }
    }

    /// <summary>
    ///     Validates a fixed length against a bound already applied; throws naming the bound it contradicts. Symmetric
    ///     wording, so the message reads whether the last constraint applied was the fixed length or the bound.
    /// </summary>
    private void ValidateExactAgainstBounds(ConstraintCall applying, int exact) {
        // Same reasoning: exact >= 0 is guaranteed by the entry points, so exact < _minLength needs _minLength > 0 —
        // a declared minimum, hence a named one — and a declared exact length carries its name too.
        if (exact < _minLength) {
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                                ConstraintClaim.Of(_exactConstraint!, $"already fixes the length at {V(exact)}"),
                                                                                ConstraintClaim.Of(_minConstraint!, $"already requires at least {Characters(_minLength)}"));
        }

        if (_maxLength is int cappedAt && exact > cappedAt) {
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                                ConstraintClaim.Of(_exactConstraint!, $"already fixes the length at {V(exact)}"),
                                                                                ConstraintClaim.Of(_maxConstraint!, $"already caps the length at {V(cappedAt)}"));
        }
    }

    private void ValidateFragmentBudget(ConstraintCall applying) {
        int required = RequiredLength();
        if (required == 0) { return; }

        (string description, bool several) = DescribeFragments();
        string requires = several ? "require" : "requires";

        if (_exactLength is int exact && required > exact) {
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                                ConstraintClaim.Of(_exactConstraint!, $"allows only {Characters(exact)} while {description} {requires} {V(required)}"),
                                                                                ConstraintClaim.OfPhrase(description, $"already {requires} {Characters(required)}"));
        }

        if (_maxLength is int max && required > max) {
            throw ConflictingAnyConstraintException.Contradicts(applying,
                                                                                ConstraintClaim.Of(_maxConstraint!, $"allows at most {Characters(max)} while {description} {requires} {V(required)}"),
                                                                                ConstraintClaim.OfPhrase(description, $"already {requires} {Characters(required)}"));
        }
    }

    /// <summary>
    ///     Fails when no pooled value survives every declared constraint, with a message naming exactly the two sides
    ///     in play and claiming only what the surviving pools establish.
    /// </summary>
    private void ValidateAllowedSurvives(ConstraintCall applying, StringSpec previous) {
        if (_effectiveAllowed!.Count > 0) { return; }

        throw ConflictingAnyConstraintException.NoPooledValueSurvives(applying, DescribeEmptyPool(applying, previous));
    }

    private string DescribeEmptyPool(ConstraintCall applying, StringSpec previous) {
        // The allow-list is the constraint being applied: the values are new, and the constraints already declared
        // are the other side. Name those that reject every single value — the ones the caller must loosen — and stay
        // generic when it took a combination of them, since no individual constraint is then the culprit.
        if (previous._allowed is null) {
            IReadOnlyList<ConstraintCall> culprits = previous.ConstraintsRejectingAll(_allowed!);
            if (culprits.Count == 0) { return "no value it offers satisfies the constraints already declared"; }
            if (culprits.Count == 1) { return $"{culprits[0]} allows none of its values"; }

            return $"{string.Join(", ", culprits)} allow none of its values";
        }

        // The allow-list was already in force: it is the other side, and the constraint being applied is what
        // emptied it. Qualify only when the applied constraint is not the whole story — it admits some value the
        // allow-list declared, so the emptiness genuinely took the other constraints too. When it admits none of
        // them, loosening the others cannot help, and the qualified form would send the caller at the wrong
        // constraint, so the plain claim is both true and the useful one.
        return _allowed!.Any(AdmittedBy(applying))
                   ? $"no value {previous._allowedConstraint} allows that the other constraints leave satisfies it"
                   : $"no value {previous._allowedConstraint} allows satisfies it";
    }

    /// <summary>
    ///     The declared constraints that reject <b>every</b> value of <paramref name="values" />, in declaration
    ///     order. A constraint some value satisfies is not a culprit — naming it would blame a constraint the caller
    ///     could loosen without changing the verdict.
    /// </summary>
    private IReadOnlyList<ConstraintCall> ConstraintsRejectingAll(IReadOnlyList<string> values) {
        List<ConstraintCall> culprits = [];
        foreach ((ConstraintCall constraint, Func<string, bool> admits) in DeclaredConstraints()) {
            if (!values.Any(admits)) { culprits.Add(constraint); }
        }

        return culprits;
    }

    /// <summary>
    ///     The test a value must pass to satisfy <paramref name="constraint" /> <b>alone</b>. A constraint the
    ///     specification does not carry admits everything, which keeps a message that cannot identify its own
    ///     applied constraint on the weaker, still-true claim rather than the stronger one.
    /// </summary>
    private Func<string, bool> AdmittedBy(ConstraintCall constraint) {
        Func<string, bool>[] tests = DeclaredConstraints()
                                     .Where(entry => entry.Constraint == constraint)
                                     .Select(entry => entry.Admits)
                                     .ToArray();

        return value => tests.All(test => test(value));
    }

    /// <summary>
    ///     Every declared constraint paired with the test a value must pass to satisfy it — the single definition of
    ///     what the specification demands of a value it did not build. It drives the pool filter, the culprit search
    ///     and the blame qualification, so the three can never drift apart.
    /// </summary>
    /// <remarks>
    ///     Entries are grouped by the constraint <b>as the caller wrote it</b>, and a group's tests are conjoined.
    ///     One call can set two internal bounds — <c>WithLengthBetween(2, 3)</c> sets both, under one name — and the
    ///     caller can only loosen the call: judging its halves separately would let a constraint that alone rejects
    ///     every value escape the blame, because each half on its own admits one.
    /// </remarks>
    private IEnumerable<(ConstraintCall Constraint, Func<string, bool> Admits)> DeclaredConstraints() {
        return Declarations()
               .GroupBy(entry => entry.Constraint)
               .Select(group => {
                   Func<string, bool>[] tests = group.Select(entry => entry.Admits).ToArray();

                   return (group.Key, (Func<string, bool>)(value => tests.All(test => test(value))));
               });
    }

    [SuppressMessage(NetAnalyzersRule.CA2249.Category, NetAnalyzersRule.CA2249.Id, Justification = SuppressionJustification.CA2249.NoContainsWithComparisonDownlevel)]
    private IEnumerable<(ConstraintCall Constraint, Func<string, bool> Admits)> Declarations() {
        if (_exactLength is int exact) { yield return (_exactConstraint!, value => value.Length == exact); }
        if (_minLength > 0) { yield return (_minConstraint!, value => value.Length >= _minLength); }
        if (_maxLength is int max) { yield return (_maxConstraint!, value => value.Length <= max); }
        if (_prefix is not null) { yield return (_prefixConstraint!, value => value.StartsWith(_prefix, StringComparison.Ordinal)); }
        if (_suffix is not null) { yield return (_suffixConstraint!, value => value.EndsWith(_suffix, StringComparison.Ordinal)); }
        foreach ((string fragment, ConstraintCall constraint) in _fragments) {
            yield return (constraint, value => value.IndexOf(fragment, StringComparison.Ordinal) >= 0);
        }
        if (_charsetConstraint is not null) { yield return (_charsetConstraint, value => value.All(AllowedByPool)); }
        foreach ((ConstraintCall constraint, CharacterSet removed) in _subtractions) {
            yield return (constraint, value => value.All(character => !CharacterPools.Belongs(character, removed)));
        }
        if (_casing is LetterCasing casing) { yield return (_casingConstraint!, value => FirstAgainstCasing(value, casing) is null); }
        foreach ((ConstraintCall constraint, string[] excluded) in _exclusions) {
            yield return (constraint, value => !excluded.Contains(value, StringComparer.Ordinal));
        }
    }

    /// <summary>Whether <paramref name="value" /> satisfies every declared constraint — the allow-list filter.</summary>
    private bool Admits(string value) {
        foreach ((ConstraintCall _, Func<string, bool> admits) in DeclaredConstraints()) {
            if (!admits(value)) { return false; }
        }

        return true;
    }

    private (string Description, bool Several) DescribeFragments() {
        List<string> parts = [];
        if (_prefix is not null) { parts.Add($"the prefix \"{_prefix}\""); }
        foreach ((string fragment, ConstraintCall _) in _fragments) { parts.Add($"the contained value \"{fragment}\""); }
        if (_suffix is not null) { parts.Add($"the suffix \"{_suffix}\""); }

        return (string.Join(" and ", parts), parts.Count > 1);
    }

    private int RequiredLength() {
        int required = (_prefix?.Length ?? 0) + (_suffix?.Length ?? 0);
        foreach ((string fragment, ConstraintCall _) in _fragments) { required += fragment.Length; }

        return required;
    }

    /// <summary>Whether the declared pool — a custom one or a named family — admits the character. True when neither is declared.</summary>
    private bool AllowedByPool(char character) {
        if (_customPool is not null) { return _customPool.IndexOf(character) >= 0; }

        return _charset is not CharacterSet charset || CharacterPools.Belongs(character, charset);
    }

    /// <summary>
    ///     The first character of <paramref name="value" /> the declared casing forbids — the filter a <b>pooled</b>
    ///     value must pass. The test is the Unicode one, not an ASCII range: the constructive filler is ASCII, but a
    ///     pooled value is the caller's own text, so an accented or non-Latin letter must be judged on its actual case
    ///     rather than waved through — the generator picks that value whole, and must not hand back one the casing
    ///     refuses. An anchored fragment is not judged here at all: it is a literal, never a draw, and a casing governs
    ///     only what the generator draws (ADR-0077).
    /// </summary>
    private static char? FirstAgainstCasing(string value, LetterCasing casing) {
        foreach (char character in value) {
            if (casing == LetterCasing.Lower && char.IsUpper(character)) { return character; }
            if (casing == LetterCasing.Upper && char.IsLower(character)) { return character; }
        }

        return null;
    }

    private static void AppendFiller(StringBuilder builder, SeededRandom random, string pool, int count) {
        for (int i = 0; i < count; i++) {
            builder.Append(pool[random.Next(pool.Length)]);
        }
    }

    private string FillerPool() {
        return _fillerPool;
    }

}
