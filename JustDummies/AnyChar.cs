#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

#endregion

namespace JustDummies;

/// <summary>
///     A fluent generator of arbitrary <see cref="char" /> values. Unconstrained, it draws from the <b>whole of
///     ASCII</b> — 0x00 to 0x7F, control characters included — and every constraint narrows that set, with no
///     exception (ADR-0074). A dummy that can be a carriage return or a NUL is what makes an unconstrained draw
///     worth something: the code under test had no say in it, so what it survives, it has been shown to tolerate.
///     Declare the invariant the surrounding code actually has and the draw respects it.
/// </summary>
/// <remarks>
///     The families mirror <see cref="AnyString" />'s exactly: <see cref="Printable" />, <see cref="NonPrintable" />,
///     <see cref="Whitespaces" />, <see cref="Alpha" />, <see cref="Numeric" />, <see cref="AlphaNumeric" />,
///     <see cref="Punctuation" /> and <see cref="Hexadecimal" /> each occupy one slot, so a second one contradicts
///     the first; <see cref="WithoutAlpha" /> and <see cref="WithoutNumeric" /> subtract instead and accumulate;
///     <see cref="LowerCase" /> / <see cref="UpperCase" /> constrain the letters; and
///     <see cref="OneOf" /> / <see cref="Except" /> / <see cref="DifferentFrom" /> work on values. A combination
///     that empties the pool fails eagerly with a <see cref="ConflictingAnyConstraintException" />. Nothing named
///     reaches past ASCII — a specific alphabet beyond it is <see cref="OneOf" />, whose values are yours.
/// </remarks>
public sealed class AnyChar : IAny<char>, IHasRandomSource, ICardinalityHint<char>, IPoolInspection<char> {

    #region Statics members declarations

    internal static AnyChar Create(RandomSource source) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }

        return new AnyChar(source, null, null, null, null, null, null, [], [], []);
    }

    private static string V(char value) {
        return $"'{CharacterPools.Escape(value)}'";
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
    // A subtraction removes a whole family rather than named values, and several accumulate — WithoutAlpha()
    // then WithoutNumeric() leaves what neither admits.
    private readonly IReadOnlyList<(ConstraintCall Constraint, CharacterSet Removed)> _subtractions;
    private readonly RandomSource         _source;

    #endregion

    [SuppressMessage(SonarRule.S107.Category, SonarRule.S107.Id, Justification = SuppressionJustification.S107.EngineImmutableState)]
    private AnyChar(RandomSource source,
                    CharacterSet? charset, ConstraintCall? charsetConstraint,
                    LetterCasing? casing,  ConstraintCall? casingConstraint,
                    IReadOnlyList<char>? allowed, ConstraintCall? allowedConstraint,
                    IReadOnlyList<char>  excluded,
                    IReadOnlyList<(ConstraintCall Constraint, char[] Values)> exclusions,
                    IReadOnlyList<(ConstraintCall Constraint, CharacterSet Removed)> subtractions) {
        _source            = source;
        _charset           = charset;
        _charsetConstraint = charsetConstraint;
        _casing            = casing;
        _casingConstraint  = casingConstraint;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        _exclusions        = exclusions;
        _subtractions      = subtractions;
        // Materialized once here — "constrain once, draw many": Generate never refilters the pool. The universe
        // is the whole of ASCII and every constraint narrows it, so one filter over one pool is the whole engine
        // (ADR-0074).
        IEnumerable<char> candidates = allowed ?? (IEnumerable<char>)CharacterPools.Ascii;
        _pool = candidates.Where(Admits).ToList();
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
            if (Admits(character)) { continue; }

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
        foreach ((ConstraintCall constraint, CharacterSet removed) in _subtractions) {
            yield return (constraint, character => !CharacterPools.Belongs(character, removed));
        }
        foreach ((ConstraintCall constraint, char[] values) in _exclusions) {
            yield return (constraint, character => !values.Contains(character));
        }
    }

    /// <summary>Whether every constraint declared so far admits the character.</summary>
    private bool Admits(char character) {
        return MatchesCharset(character)
            && MatchesCasing(character)
            && !_excluded.Contains(character)
            && _subtractions.All(subtraction => !CharacterPools.Belongs(character, subtraction.Removed));
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

    /// <summary>
    ///     Restricts the character to ASCII punctuation — the 32 printable characters that are neither a letter, a
    ///     digit nor the space, POSIX <c>[:punct:]</c>. The family to declare when the surrounding code requires a
    ///     character it must <b>not</b> read as alphanumeric: a separator, a delimiter, an operator. Broader than
    ///     <see cref="char.IsPunctuation(char)" />, which classifies <c>+</c>, <c>&lt;</c> and <c>$</c> as symbols
    ///     rather than punctuation — assert on the invariant the code actually has, not on that predicate. Declared
    ///     once per generator.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Punctuation() {
        return WithCharset(CharacterSet.Punctuation, ConstraintCall.Of(nameof(Punctuation)));
    }

    /// <summary>
    ///     Restricts the character to printable ASCII — every character from the space (0x20) to <c>~</c> (0x7E).
    ///     The family to declare when the surrounding code cannot take a control character: an unconstrained draw
    ///     spans the whole of ASCII, so a carriage return or a NUL is exactly what it may hand you, and saying so
    ///     here is what a length or a format invariant does elsewhere. Declared once per generator.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Printable() {
        return WithCharset(CharacterSet.Printable, ConstraintCall.Of(nameof(Printable)));
    }

    /// <summary>
    ///     Restricts the character to the ASCII characters that are <b>not</b> printable — the 33 C0 controls and
    ///     <c>DEL</c>. The family to declare when the code under test is meant to reject or strip them, so the
    ///     counter-example is drawn rather than hand-listed. Declared once per generator.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar NonPrintable() {
        return WithCharset(CharacterSet.NonPrintable, ConstraintCall.Of(nameof(NonPrintable)));
    }

    /// <summary>
    ///     Restricts the character to ASCII whitespace — the space and the tab, the readable pair the regex
    ///     generator already draws <c>\s</c> from. Declared once per generator.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Whitespaces() {
        return WithCharset(CharacterSet.Whitespaces, ConstraintCall.Of(nameof(Whitespaces)));
    }

    /// <summary>
    ///     Restricts the character to the base-16 alphabet of RFC 4648 — <c>0-9</c>, <c>A-F</c> and <c>a-f</c>.
    ///     Chain <see cref="LowerCase" /> or <see cref="UpperCase" /> for the single-case form a hash or a colour
    ///     usually requires. Declared once per generator.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyChar Hexadecimal() {
        return WithCharset(CharacterSet.Hexadecimal, ConstraintCall.Of(nameof(Hexadecimal)));
    }

    /// <summary>
    ///     Removes the ASCII letters from whatever the generator would otherwise draw. Unlike a family, a
    ///     subtraction does not occupy the one family slot and several accumulate, so
    ///     <c>WithoutAlpha().WithoutNumeric()</c> leaves the punctuation, the whitespace and the controls.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the subtraction leaves no character drawable.</exception>
    public AnyChar WithoutAlpha() {
        return Without(CharacterSet.Alpha, ConstraintCall.Of(nameof(WithoutAlpha)));
    }

    /// <summary>
    ///     Removes the ASCII digits from whatever the generator would otherwise draw. Accumulates with
    ///     <see cref="WithoutAlpha" /> rather than replacing it.
    /// </summary>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the subtraction leaves no character drawable.</exception>
    public AnyChar WithoutNumeric() {
        return Without(CharacterSet.Numeric, ConstraintCall.Of(nameof(WithoutNumeric)));
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

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, values.Distinct().ToArray(), constraint, _excluded, _exclusions, _subtractions), constraint);
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

        return Validated(new AnyChar(_source, charset, applying, _casing, _casingConstraint, _allowed, _allowedConstraint, _excluded, _exclusions, _subtractions), applying);
    }

    private AnyChar WithCasing(LetterCasing casing, ConstraintCall applying) {
        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a
        // conflict: the second declaration asks for exactly what the first already guarantees.
        if (_casingConstraint == applying) { return this; }
        if (_casingConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _casingConstraint); }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, casing, applying, _allowed, _allowedConstraint, _excluded, _exclusions, _subtractions), applying);
    }

    private AnyChar WithExcluded(char[] values, ConstraintCall applying) {
        List<char> excluded = [.. _excluded, .. values];

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, _allowed, _allowedConstraint, excluded, [.. _exclusions, (applying, values.ToArray())], _subtractions), applying);
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
        return CharacterPools.Belongs(character, _charset);
    }

    private bool MatchesCasing(char character) {
        return CharacterPools.MatchesCasing(character, _casing);
    }

    private AnyChar Without(CharacterSet removed, ConstraintCall applying) {
        // Subtractions accumulate rather than occupying a slot: re-declaring one removes what is already gone,
        // which is inert rather than contradictory, and JD024 is what reports an inert constraint.
        if (_subtractions.Any(subtraction => subtraction.Constraint == applying)) { return this; }

        return Validated(new AnyChar(_source, _charset, _charsetConstraint, _casing, _casingConstraint, _allowed, _allowedConstraint,
                                     _excluded, _exclusions, [.. _subtractions, (applying, removed)]), applying);
    }

}
