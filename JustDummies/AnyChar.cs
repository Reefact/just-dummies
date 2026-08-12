#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="char" /> values. Unconstrained, it draws from ASCII letters and
///     digits — the same readable default as <see cref="AnyString" />'s filler — and the constraints mirror the
///     string character families: <see cref="Alpha" />, <see cref="Numeric" />, <see cref="AlphaNumeric" />,
///     <see cref="LowerCase" />, <see cref="UpperCase" />, plus <see cref="OneOf" /> / <see cref="Except" /> /
///     <see cref="DifferentFrom" />. A combination that empties the pool fails eagerly with a
///     <see cref="ConflictingAnyConstraintException" />.
/// </summary>
public sealed class AnyChar : IAny<char>, IHasRandomSource, ICardinalityHint<char>, IPoolInspection<char> {

    #region Statics members declarations

    internal static AnyChar Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyChar(source, null, null, null, null, null, null, [], []);
    }

    private static string V(char value) {
        return $"'{value}'";
    }

    private static string Join(char[] values) {
        return string.Join(", ", values.Select(V));
    }

    #endregion

    #region Fields declarations

    private readonly IReadOnlyList<char>? _allowed;
    private readonly ConstraintCall?      _allowedConstraint;
    private readonly List<char>           _pool;
    private readonly LetterCasing?        _casing;
    private readonly ConstraintCall?      _casingConstraint;
    private readonly CharacterSet?        _charset;
    private readonly ConstraintCall?      _charsetConstraint;
    private readonly IReadOnlyList<char>  _excluded;
    // Provenance for the diagnostic path only: _excluded drives every draw decision, while this records WHICH
    // exclusion removed what, so a pool inspection can name the constraint responsible. Same split as the
    // interval engines (OrdinalIntervalSpec._exclusions).
    private readonly IReadOnlyList<(ConstraintCall Constraint, char[] Values)> _exclusions;
    private readonly RandomSource         _source;

    #endregion

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.EngineImmutableState)]
    private AnyChar(RandomSource source,
                    CharacterSet? charset, ConstraintCall? charsetConstraint,
                    LetterCasing? casing,  ConstraintCall? casingConstraint,
                    IReadOnlyList<char>? allowed, ConstraintCall? allowedConstraint,
                    IReadOnlyList<char>  excluded,
                    IReadOnlyList<(ConstraintCall Constraint, char[] Values)> exclusions) {
        _source            = source;
        _charset           = charset;
        _charsetConstraint = charsetConstraint;
        _casing            = casing;
        _casingConstraint  = casingConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        _exclusions        = exclusions;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool. The full
        // constant pool is the unconstrained start; MatchesCharset narrows it, so no per-charset pre-narrowing
        // is needed.
        IEnumerable<char> candidates = allowed ?? (IEnumerable<char>)(CharacterPools.UpperLetters + CharacterPools.LowerLetters + CharacterPools.Digits);
        _pool = candidates.Where(character => MatchesCharset(character) && MatchesCasing(character) && !excluded.Contains(character)).ToList();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // The pool is materialized once at construction, so its size is the exact number of characters drawable.
    long? ICardinalityHint<char>.DistinctCardinality => _pool.Count;

    // The pool is the exact draw set, so membership is a direct pool lookup.
    bool ICardinalityHint<char>.Contains(char value) => _pool.Contains(value);

    // Explicit, like the cardinality hint above (ADR-0067). A pool is in force only when the caller supplied one:
    // the unconstrained start is the library's own alphabet, not theirs, so there is nothing of theirs to audit.
    bool IPoolInspection<char>.IsPooled => _allowed is not null;

    IReadOnlyList<char> IPoolInspection<char>.GetSurvivors() {
        return _allowed is null ? Array.Empty<char>() : new ReadOnlyCollection<char>(_pool.ToArray());
    }

    IReadOnlyList<PoolRejection<char>> IPoolInspection<char>.GetRejections() {
        if (_allowed is null) { return Array.Empty<PoolRejection<char>>(); }

        List<PoolRejection<char>> rejections = [];
        foreach (char character in _allowed) {
            if (MatchesCharset(character) && MatchesCasing(character) && !_excluded.Contains(character)) { continue; }

            List<DeclaredConstraint> culprits = DeclaredConstraints()
                                                .Where(entry => !entry.Admits(character))
                                                .Select(entry => entry.Constraint.ToDeclaredConstraint())
                                                .ToList();

            rejections.Add(new PoolRejection<char>(character, culprits));
        }

        return new ReadOnlyCollection<PoolRejection<char>>(rejections);
    }

    /// <summary>Every declared constraint paired with the test a character must pass to satisfy it.</summary>
    private IEnumerable<(ConstraintCall Constraint, Func<char, bool> Admits)> DeclaredConstraints() {
        if (_charsetConstraint is not null) { yield return (_charsetConstraint, MatchesCharset); }
        if (_casingConstraint is not null) { yield return (_casingConstraint, MatchesCasing); }
        foreach ((ConstraintCall constraint, char[] values) in _exclusions) {
            yield return (constraint, character => !values.Contains(character));
        }
    }

    /// <summary>Restricts the character to ASCII letters only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Alpha() {
        return WithCharset(CharacterSet.Alpha, ConstraintCall.Of(nameof(Alpha)));
    }

    /// <summary>Restricts the character to ASCII digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Numeric() {
        return WithCharset(CharacterSet.Numeric, ConstraintCall.Of(nameof(Numeric)));
    }

    /// <summary>Restricts the character to ASCII letters and digits only. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar AlphaNumeric() {
        return WithCharset(CharacterSet.AlphaNumeric, ConstraintCall.Of(nameof(AlphaNumeric)));
    }

    /// <summary>Requires an alphabetic character to be lowercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar LowerCase() {
        return WithCasing(LetterCasing.Lower, ConstraintCall.Of(nameof(LowerCase)));
    }

    /// <summary>Requires an alphabetic character to be uppercase. Declared once per generator.</summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar UpperCase() {
        return WithCasing(LetterCasing.Upper, ConstraintCall.Of(nameof(UpperCase)));
    }

    /// <summary>Requires the character to be one of the supplied values. Declared once per generator.</summary>
    /// <param name="values">The allowed characters; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar OneOf(params char[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        ConstraintCall constraint = ConstraintCall.Of(nameof(OneOf), Join(values));
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_allowedConstraint == constraint) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(constraint, _allowedConstraint); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, values.Distinct().ToArray(), constraint, _excluded, _exclusions), constraint);
    }

    /// <summary>Requires the character to be none of the supplied values.</summary>
    /// <param name="values">The forbidden characters.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Except(params char[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }

        return WithExcluded(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the character to differ from <paramref name="value" /> — typically an existing value the test
    ///     already holds. Semantically equivalent to <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The character the generated character must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar DifferentFrom(char value) {
        return WithExcluded([value], ConstraintCall.Of(nameof(DifferentFrom), V(value)));
    }

    /// <inheritdoc />
    public char Generate() {
        return _pool[_source.Current.Next(_pool.Count)];
    }

    private AnyChar WithCharset(CharacterSet charset, ConstraintCall applying) {
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_charsetConstraint == applying) { return this; }
        if (_charsetConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _charsetConstraint); }

        return Validated(new AnyChar(_source, charset, applying, _casing, _casingConstraint, _allowed, _allowedConstraint, _excluded, _exclusions), applying);
    }

    private AnyChar WithCasing(LetterCasing casing, ConstraintCall applying) {
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_casingConstraint == applying) { return this; }
        if (_casingConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _casingConstraint); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, casing, applying, _allowed, _allowedConstraint, _excluded, _exclusions), applying);
    }

    private AnyChar WithExcluded(char[] values, ConstraintCall applying) {
        List<char> excluded = [.. _excluded, .. values];

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, _allowed, _allowedConstraint, excluded, [.. _exclusions, (applying, values.ToArray())]), applying);
    }

    [SuppressMessage(NetAnalyzersRule.CA1822.Category, NetAnalyzersRule.CA1822.Id, Justification = SuppressionJustification.CA1822.UniformValidatedHook)]
    [SuppressMessage(SonarRule.S2325.Category, SonarRule.S2325.Id, Justification = SuppressionJustification.S2325.UniformValidatedHook)]
    private AnyChar Validated(AnyChar candidate, ConstraintCall applying) {
        if (candidate._pool.Count > 0) { return candidate; }

        string pool = candidate._allowedConstraint is null
                          ? "no character remains in the pool the declared constraints allow"
                          : $"no character {candidate._allowedConstraint} allows satisfies the constraints already defined";

        throw ConflictingAnyConstraintException.NoValueRemains(applying, pool);
    }

    private bool MatchesCharset(char character) {
        return _charset switch {
            CharacterSet.Alpha        => CharacterPools.IsAsciiLetter(character),
            CharacterSet.Numeric      => CharacterPools.IsAsciiDigit(character),
            CharacterSet.AlphaNumeric => CharacterPools.IsAsciiLetter(character) || CharacterPools.IsAsciiDigit(character),
            _                         => true
        };
    }

    private bool MatchesCasing(char character) {
        return _casing switch {
            LetterCasing.Lower => character is not (>= 'A' and <= 'Z'),
            LetterCasing.Upper => character is not (>= 'a' and <= 'z'),
            _                  => true
        };
    }

}
