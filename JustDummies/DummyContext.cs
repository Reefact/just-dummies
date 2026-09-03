#region Usings declarations

using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

/// <summary>
///     An isolated, deterministic generation context created by <see cref="Dummy.WithSeed" />: every generator created
///     from it draws from a dedicated source seeded with <see cref="Seed" />, independent of the ambient context the
///     static <see cref="Dummy" /> entry points use. Two contexts created with the same seed yield the same sequence
///     of values.
/// </summary>
/// <remarks>
///     <para>
///         Inside a test, prefer wrapping the body in <c>Dummy.Reproducibly(...)</c>: it keeps values arbitrary by
///         default and reports a replayable seed only when the test fails. A context is the explicit-object
///         alternative for when that scope does not fit — generating a deterministic dataset outside a test body,
///         for example.
///     </para>
///     <para>
///         A context owns a single pseudo-random generator, and it is safe to draw from concurrently. What
///         parallelism costs is the replay, not the values: the draws of two threads interleave, so neither the
///         sequence nor the multiset of values a context produces is stable across runs once it is shared. A context
///         used from one thread at a time replays exactly; to keep a parallel run reproducible, give each unit of
///         work its own scope with <see cref="Dummy.UseSeed(int)" /> rather than sharing one context across threads.
///     </para>
/// </remarks>
public sealed class DummyContext {

    #region Fields declarations

    private readonly FixedRandomSource _source;

    #endregion

    internal DummyContext(int seed) {
        Seed    = seed;
        _source = new FixedRandomSource(seed);
    }

    /// <summary>The seed pinning this context's value sequence.</summary>
    public int Seed { get; }

    /// <summary>
    ///     Starts an arbitrary <see cref="string" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Dummy.String" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>A string generator to constrain fluently.</returns>
    public DummyString String() {
        return new DummyString(_source, StringSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> drawing from this context —
    ///     same fluent surface as <see cref="Dummy.StringMatching(string)" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public DummyPattern StringMatching(string pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        return DummyPattern.FromPattern(_source, pattern, ignoreCase: false);
    }

    /// <summary>
    ///     Starts a generator of arbitrary strings matching <paramref name="pattern" /> drawing from this context —
    ///     same fluent surface as <see cref="Dummy.StringMatching(Regex)" />, deterministic under this context's seed.
    ///     <see cref="RegexOptions.IgnoreCase" /> is honoured; <see cref="RegexOptions.IgnorePatternWhitespace" /> is
    ///     rejected; the remaining options are ignored.
    /// </summary>
    /// <param name="pattern">The regular expression the generated strings must match.</param>
    /// <returns>A generator of strings matching the pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pattern" /> is not a well-formed pattern, or carries <see cref="RegexOptions.IgnorePatternWhitespace" />.</exception>
    /// <exception cref="UnsupportedRegexException">Thrown when <paramref name="pattern" /> uses a construct outside the supported regular subset.</exception>
    public DummyPattern StringMatching(Regex pattern) {
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }
        if ((pattern.Options & RegexOptions.IgnorePatternWhitespace) != 0) { throw new ArgumentException("RegexOptions.IgnorePatternWhitespace changes how the pattern text is read; pass the pattern without it (or with its whitespace and comments removed).", nameof(pattern)); }

        return DummyPattern.FromPattern(_source, pattern.ToString(), (pattern.Options & RegexOptions.IgnoreCase) != 0);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Uri" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Dummy.Uri" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>A URI generator to narrow fluently.</returns>
    public DummyUri Uri() {
        return new DummyUri(_source, UriSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="int" /> generator drawing from this context — same fluent surface as
    ///     <see cref="Dummy.Int32" />, deterministic under this context's seed.
    /// </summary>
    /// <returns>An integer generator to constrain fluently.</returns>
    public DummyInt32 Int32() {
        return DummyInt32.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="sbyte" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummySByte SByte() {
        return DummySByte.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="byte" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyByte Byte() {
        return DummyByte.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="short" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyInt16 Int16() {
        return DummyInt16.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ushort" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyUInt16 UInt16() {
        return DummyUInt16.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="uint" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyUInt32 UInt32() {
        return DummyUInt32.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="long" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyInt64 Int64() {
        return DummyInt64.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ulong" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyUInt64 UInt64() {
        return DummyUInt64.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeSpan" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained, negative durations included. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c> and plus <c>WithGranularity(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyTimeSpan TimeSpan() {
        return DummyTimeSpan.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTime" /> generator drawing from this context — deterministic under this context's seed:
    ///     any representable instant unless constrained; generated values carry Utc kind. Same constraint algebra as
    ///     <see cref="DummyInt32" /> with the bounds renamed <c>After(...)</c>/<c>Before(...)</c>: no sign or zero
    ///     constraint, no <c>MultipleOf(...)</c>, plus <c>WithGranularity(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyDateTime DateTime() {
        return DummyDateTime.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTimeOffset" /> generator drawing from this context — deterministic under this context's seed:
    ///     any representable instant unless constrained; generated values carry a zero (UTC) offset. Same constraint
    ///     algebra as <see cref="DummyInt32" /> with the bounds renamed <c>After(...)</c>/<c>Before(...)</c>: no sign or
    ///     zero constraint, no <c>MultipleOf(...)</c>, plus <c>WithGranularity(...)</c> and <c>WithOffset(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyDateTimeOffset DateTimeOffset() {
        return DummyDateTimeOffset.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="double" /> generator drawing from this context — deterministic under this context's seed:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyDouble Double() {
        return DummyDouble.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="float" /> generator drawing from this context — deterministic under this context's seed:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummySingle Single() {
        return DummySingle.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="decimal" /> generator drawing from this context — deterministic under this context's seed:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less
    ///     <c>MultipleOf(...)</c> and plus <c>WithScale(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyDecimal Decimal() {
        return DummyDecimal.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="bool" /> generator drawing from this context (deterministic under this context's seed) — an even coin
    ///     flip unless pinned with <c>True()</c> or <c>False()</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyBoolean Boolean() {
        return DummyBoolean.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Guid" /> generator drawing from this context (deterministic under this context's seed) — unlike
    ///     <see cref="System.Guid.NewGuid" />, reproducible inside an <c>Dummy.Reproducibly(...)</c> run, and for every
    ///     practical purpose never empty.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyGuid Guid() {
        return DummyGuid.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <typeparamref name="TEnum" /> generator drawing from this context (deterministic under this context's seed) —
    ///     uniformly across the enum's declared members, never an undeclared numeric value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
    /// <returns>A generator to constrain fluently.</returns>
    /// <exception cref="DummyGenerationException">Thrown when <typeparamref name="TEnum" /> declares no members.</exception>
    public DummyEnum<TEnum> Enum<TEnum>()
        where TEnum : struct, Enum {
        return DummyEnum<TEnum>.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="char" /> generator drawing from this context (deterministic under this context's seed) — the whole of
    ///     ASCII, control characters included, unless constrained (ADR-0075), mirroring <see cref="DummyString" />'s character families.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyChar Char() {
        return DummyChar.Create(_source);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateOnly" /> generator drawing from this context (deterministic under this context's seed) — any
    ///     representable date unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyDateOnly DateOnly() {
        return DummyDateOnly.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeOnly" /> generator drawing from this context (deterministic under this context's seed) — any
    ///     time of day unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyTimeOnly TimeOnly() {
        return DummyTimeOnly.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Int128" /> generator drawing from this context (deterministic under this context's seed) — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyInt128 Int128() {
        return DummyInt128.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.UInt128" /> generator drawing from this context (deterministic under this context's seed) — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyUInt128 UInt128() {
        return DummyUInt128.Create(_source);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Half" /> generator drawing from this context (deterministic under this context's seed) — finite
    ///     values only. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public DummyHalf Half() {
        return DummyHalf.Create(_source);
    }
#endif

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool of caller-supplied <paramref name="values" /> drawing from
    ///     this context — same surface as <see cref="Dummy.OneOf{T}(T[])" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public DummyOneOf<T> OneOf<T>(params T[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return DummyOneOf<T>.FromPool(_source, values, ConstraintCall.OfElided(nameof(OneOf)));
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a list drawing from this context — same surface as
    ///     <see cref="Dummy.ElementOf{T}(IReadOnlyList{T})" />, deterministic under this context's seed.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public DummyOneOf<T> ElementOf<T>(IReadOnlyList<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return DummyOneOf<T>.FromPool(_source, values, ConstraintCall.OfElided(nameof(ElementOf)));
    }

    /// <summary>
    ///     Draws an arbitrary value from an explicit pool held as a sequence drawing from this context — same surface
    ///     as <see cref="Dummy.ElementOf{T}(IEnumerable{T})" />, deterministic under this context's seed. The sequence is
    ///     materialized once.
    /// </summary>
    /// <param name="values">The pool the generated value is drawn from; duplicates are ignored.</param>
    /// <typeparam name="T">The type of the pooled values.</typeparam>
    /// <returns>A generator drawing uniformly from <paramref name="values" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    public DummyOneOf<T> ElementOf<T>(IEnumerable<T> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return DummyOneOf<T>.FromPool(_source, values as IReadOnlyList<T> ?? values.ToArray(), ConstraintCall.OfElided(nameof(ElementOf)));
    }

}
