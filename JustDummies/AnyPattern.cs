#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

#endregion

namespace JustDummies;

/// <summary>
///     A generator of arbitrary strings that <b>match a regular expression</b> — the dummy for a value whose format is
///     defined by a pattern (an order reference, a SKU, a currency code). The pattern is the whole <i>shape</i> of the
///     specification: unlike <see cref="AnyString" /> this generator exposes no further shape or length constraints —
///     express those inside the pattern instead. What it does expose is the type-agnostic trio every other generator
///     carries: the value set <see cref="OneOf(string[])" /> and the exclusion pair <see cref="Except" /> and
///     <see cref="DifferentFrom" />. It also composes like any other generator: pipe it through <c>As(...)</c> into a
///     value object, make it optional with <c>OrNull()</c>, or fold it into <c>Combine(...)</c> and the collection
///     generators.
/// </summary>
/// <remarks>
///     <para>
///         The pattern is parsed once, when the generator is created; each <see cref="Generate" /> then walks the
///         parsed tree, drawing every choice and repetition count from the generator's random context — so a run is
///         reproducible under a seed, exactly like every other generator. Wherever the pattern leaves a character
///         free, values are drawn from <b>printable ASCII</b> (<c>\s</c> may also yield a tab); a character the pattern
///         names explicitly is emitted as written, control characters included. Values are built directly rather than
///         generated-and-filtered.
///     </para>
///     <para>
///         A built value is then checked against the real .NET engine and, on the rare miss, redrawn. The structural
///         build mirrors the regular subset of the engine, but a few implementation-defined corners of empty-match
///         handling — a nullable alternative under a quantifier, whose emptiness the engine accepts or refuses
///         depending on branch order and form — cannot be mirrored structurally. Rather than model those corners, the
///         invariant "a generated value matches its pattern" is kept by construction: the check is the last word, so a
///         value the engine would reject is never returned.
///     </para>
///     <para>
///         A shape constraint is refused because it would mean building a value in the intersection of two regular
///         languages, which the library has no machinery for. An <b>exclusion</b> asks for nothing of the sort: it
///         never constructs, it rejects. The value is built from the pattern exactly as before and redrawn on a hit —
///         one more predicate inside a loop that already turns. That places it under the exception the library already
///         documents for strings: with no ordinal mapping to build the exclusion into, it is met by a <b>bounded</b>
///         redraw, so an exclusion tight enough to leave nothing surfaces at <see cref="Generate" /> as a seed-bearing
///         <see cref="AnyGenerationException" /> rather than eagerly at declaration.
///     </para>
///     <para>
///         A <b>value set</b> asks for nothing of that sort either, and for the opposite reason: once the caller
///         supplies the values there is nothing left to build. The pattern stops constructing and becomes the test
///         each supplied value passes or fails — the engine that already decides whether a built value matches,
///         pointed at the caller's values instead. The domain is then finite and enumerable, so a set the pattern
///         admits nothing of, and an exclusion that empties what it left, are both refused <b>at declaration</b>
///         naming the two sides, where the same exclusion on an unpooled generator could only spend its redraw
///         budget. Declaring a set is also what makes the surviving values reportable through
///         <see cref="IPoolInspection{T}" /> and their count answerable to a distinct collection.
///     </para>
///     <para>
///         Only the <b>regular</b> subset of the pattern language is supported (see <see cref="Any.StringMatching(string)" />);
///         a non-regular construct is refused eagerly with an <see cref="UnsupportedRegexException" /> rather than
///         silently mis-generated.
///     </para>
///     <example>
///         <code>
///         string reference = Any.StringMatching(@"^ORD-\d{8}$").Generate();
///         string other     = Any.StringMatching(@"^ORD-\d{8}$").DifferentFrom(existing).Generate();
///         string seeded    = Any.StringMatching(@"^ORD-\d{8}$").OneOf(referencesAlreadyInStore).Generate();
///         IAny&lt;OrderReference&gt; any = Any.StringMatching(@"^ORD-\d{8}$").As(OrderReference.Create);
///         </code>
///     </example>
/// </remarks>
public sealed class AnyPattern : IAny<string>, IHasRandomSource, ICardinalityHint<string>, IPoolInspection<string> {

    #region Statics members declarations

    // A nested unbounded quantifier can, in principle, expand super-linearly; this ceiling turns that into a clear
    // AnyGenerationException instead of an out-of-memory. It is far above any realistic format-validation pattern.
    private const int GenerationLimit = 65536;

    // The structural build occasionally produces a value the real engine rejects (see the class remarks). Each build
    // is verified and redrawn on a miss; the cap turns a pattern the generator cannot satisfy at all into a clear
    // error instead of an unbounded loop. A supported pattern matches on the first build save for these rare corners,
    // where a valid value appears within a handful of draws, so the cap is never approached in practice.
    private const int MatchAttemptLimit = 1000;

    // Bounded escape for exclusions, kept separate from the match budget above so the two failures never borrow each
    // other's evidence: this one counts values the pattern produced and the engine accepted, which an exclusion then
    // rejected. Mirrors the string generator's budget, and a genuinely emptied language fails fast against it.
    private const int ExclusionRedrawBudget = 10_000;

    // A safety net against catastrophic backtracking while verifying a non-matching draw — a generated value matching
    // its own pattern is near-instant, so this bites only a pathological pattern, which is treated as a miss.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    internal static AnyPattern FromPattern(RandomSource source, string pattern, bool ignoreCase) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        // Parse first: it raises the specific ArgumentException / UnsupportedRegexException for an invalid or
        // unsupported pattern. The verifier Regex is NOT built here — the Lazy<Regex> field below defers it to the
        // first actual need — so a pattern whose generation can never succeed (an unbounded quantifier with a
        // minimum in the billions, say) never pays, or risks, compiling it.
        RegexNode    root    = RegexParser.Parse(pattern, ignoreCase);
        RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

        return new AnyPattern(source, root, pattern, options);
    }

    private static string V(int value) {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Join(IReadOnlyList<string> values) {
        return string.Join(", ", values.Select(value => $"\"{value}\""));
    }

    #endregion

    #region Fields declarations

    // The value set exactly as the caller supplied it, deduplicated, or null when none is declared. Kept beside the
    // survivors because a pool inspection reports on the values that did NOT make it, which the survivors no longer
    // hold; _survivors drives every draw decision, this one drives the diagnostic.
    private readonly IReadOnlyList<string>? _allowed;
    private readonly ConstraintCall?        _allowedConstraint;
    private readonly IReadOnlyList<string>  _excluded;
    // Provenance for the diagnostic path only, mirroring AnyOneOf: _excluded answers "is this value out", while this
    // records WHICH declaration named it, so a rejection can name every constraint refusing a value rather than the
    // one that happened to remove it first.
    private readonly IReadOnlyList<(ConstraintCall Constraint, string[] Values)> _exclusions;
    private readonly string                _pattern;
    private readonly RegexNode             _root;
    private readonly RandomSource          _source;
    // The supplied values the pattern admits and the exclusions leave — the exact domain a pooled draw picks from,
    // or null when no value set is declared and the pattern still builds the value.
    private readonly IReadOnlyList<string>? _survivors;

    // Compiled at most once, on first need — see the FromPattern and Generate() remarks. Two things force it, and
    // only one of them was here first: a draw, which reaches this line only after _root.Append has vouched for the
    // pattern's generability; and OneOf, which forces it at DECLARATION because judging the caller's values is what
    // makes an emptied set an eager conflict rather than a spent redraw budget. That is a deliberate trade: a
    // pattern whose generation the ceiling refuses can still be handed a value set, and its verifier is then
    // compiled where a draw would never have compiled it.
    // Lazy<T>'s default thread-safety mode guarantees the factory runs exactly once even under concurrent
    // Generate() calls on the same instance (see the "concurrent draws" test); no thread ever sees, or pays for, a
    // second compilation. Anchored with ^(?:…)$ so it decides a full match, and honours only the option the
    // generator itself honoured (IgnoreCase), never the rest of a passed Regex's. Shared, not rebuilt, when an
    // exclusion derives a new generator: the pattern it verifies is unchanged.
    private readonly Lazy<Regex> _verifier;

    #endregion

    internal AnyPattern(RandomSource source, RegexNode root, string pattern, RegexOptions options) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (root is null) { throw new ArgumentNullException(nameof(root)); }
        if (pattern is null) { throw new ArgumentNullException(nameof(pattern)); }

        _source     = source;
        _root       = root;
        _pattern    = pattern;
        _excluded   = [];
        _exclusions = [];
        _verifier   = new Lazy<Regex>(() => new Regex("^(?:" + pattern + ")$", options, MatchTimeout));
    }

    // The one derivation constructor, shared by the value set and the exclusions: both change what may be drawn, and
    // the surviving pool has to be recomputed from BOTH whichever of the two moved. Order matters here — Admits reads
    // _verifier and _excluded, so the survivors are filtered last.
    private AnyPattern(AnyPattern origin, IReadOnlyList<string>? allowed, ConstraintCall? allowedConstraint,
                       IReadOnlyList<string> excluded, IReadOnlyList<(ConstraintCall Constraint, string[] Values)> exclusions) {
        _source            = origin._source;
        _root              = origin._root;
        _pattern           = origin._pattern;
        _verifier          = origin._verifier;
        _allowed           = allowed;
        _allowedConstraint = allowedConstraint;
        _excluded          = excluded;
        _exclusions        = exclusions;
        _survivors         = allowed?.Where(Admits).ToArray();
    }

    RandomSource? IHasRandomSource.Source => _source;

    // Explicit, like every other generator's: a cardinality is answered to a distinct collection, never offered on
    // the fluent surface. Null without a value set — the library builds values from the pattern and never enumerates
    // its language, so it cannot count it, and the collection falls back to the bounded dedup draw exactly as before.
    long? ICardinalityHint<string>.DistinctCardinality => _survivors?.Count;

    // Answering "outside" without a pool is the side the interface documents as safe: it can only defer to that same
    // bounded draw, never refuse a specification the pattern would have satisfied.
    bool ICardinalityHint<string>.Contains(string value) => value is not null && _survivors is not null && _survivors.Contains(value, StringComparer.Ordinal);

    // IsPooled is "the caller supplied the values", not "the domain is countable". A pattern with no value set builds
    // its value, so there is nothing of the caller's to audit and nothing here reports on it (ADR-0067).
    bool IPoolInspection<string>.IsPooled => _survivors is not null;

    IReadOnlyList<string> IPoolInspection<string>.GetSurvivors() {
        return _survivors is null
                   ? Array.Empty<string>()
                   : new ReadOnlyCollection<string>(_survivors.ToArray());
    }

    IReadOnlyList<PoolRejection<string>> IPoolInspection<string>.GetRejections() {
        if (_allowed is null) { return Array.Empty<PoolRejection<string>>(); }

        List<PoolRejection<string>> rejections = [];
        foreach (string value in _allowed) {
            if (Admits(value)) { continue; }

            // Every constraint refusing the value, not the first one met: the pattern when it does not match, and
            // each exclusion naming it. A reader told to loosen one of two would find the value still absent.
            List<DeclaredConstraint> culprits = [];
            if (!Matches(value)) { culprits.Add(Matching.ToDeclaredConstraint()); }
            culprits.AddRange(_exclusions.Where(entry => entry.Values.Contains(value, StringComparer.Ordinal))
                                         .Select(entry => entry.Constraint.ToDeclaredConstraint()));

            rejections.Add(new PoolRejection<string>(value, culprits));
        }

        return new ReadOnlyCollection<PoolRejection<string>>(rejections);
    }

    // The pattern rendered as the constraint it becomes once a value set turns it into a test. It is declared by the
    // factory rather than on the builder, so it carries the factory's name — the public symbol a reader can look up.
    private ConstraintCall Matching => ConstraintCall.Of(nameof(Any.StringMatching), $"\"{_pattern}\"");

    /// <summary>
    ///     Draws the value from an explicit, fixed set of <paramref name="values" /> rather than building one from the
    ///     pattern; declared once per generator. The pattern keeps its meaning and becomes the test each supplied
    ///     value must pass, so <c>Any.StringMatching(@"^\d{3}$").OneOf("123", "abcd")</c> yields <c>"123"</c>. A set
    ///     the pattern admits nothing of is a <see cref="ConflictingAnyConstraintException" /> naming both sides,
    ///     whichever order the two were declared in. Duplicate values are collapsed; the generated value is one of
    ///     the surviving values, drawn uniformly and reproducibly under a seed.
    /// </summary>
    /// <remarks>
    ///     Composes with <see cref="Except" /> and <see cref="DifferentFrom" /> in either order, and turns them
    ///     <b>eager</b>: the domain is the supplied values, so an exclusion leaving nothing is refused at declaration
    ///     rather than discovered by spending the redraw budget at <see cref="Generate" />. Comparison is ordinal,
    ///     whether or not the pattern ignores case — the pattern decides membership, ordinal equality decides
    ///     identity, and the two are not the same question.
    /// </remarks>
    /// <param name="values">The values the generated value is drawn from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyPattern OneOf(params string[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element; use OrNull() to make the whole generator nullable.", nameof(values)); }

        ConstraintCall applying  = ConstraintCall.Of(nameof(OneOf), Join(values));
        string[]       requested = values.Distinct(StringComparer.Ordinal).ToArray();

        // Re-declaring the SAME constraint is not a contradiction, so it is a no-op rather than a conflict: the
        // second declaration asks for exactly what the first already guarantees. What "the same" means is the
        // SET, not the call as it was written — OneOf documents duplicates as ignored and promises nothing about
        // order, so OneOf("a", "b"), OneOf("b", "a") and OneOf("a", "b", "b") all declare one domain. The rendered
        // call is still kept for the messages, because a conflict has to quote the caller their own words.
        if (_allowed is not null && new HashSet<string>(_allowed, StringComparer.Ordinal).SetEquals(requested)) { return this; }
        if (_allowedConstraint is not null) { throw ConflictingAnyConstraintException.AlreadyDefined(applying, _allowedConstraint); }

        AnyPattern candidate = new(this, requested, applying, _excluded, _exclusions);
        if (candidate._survivors!.Count > 0) { return candidate; }

        throw ConflictingAnyConstraintException.NoPooledValueSurvives(applying, candidate.DescribeEmptyPool());
    }

    /// <summary>
    ///     Draws the value from an explicit, fixed set of <paramref name="values" /> — the
    ///     <see cref="IEnumerable{T}" /> counterpart of <see cref="OneOf(string[])" />, for a set already held as a
    ///     sequence (a list, a LINQ result, values loaded at test setup). Same contract in every other respect.
    /// </summary>
    /// <param name="values">The values the generated value is drawn from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when the constraint contradicts a constraint already declared.</exception>
    public AnyPattern OneOf(IEnumerable<string> values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }

        return OneOf(values as string[] ?? values.ToArray());
    }

    /// <summary>
    ///     Requires the generated value to be none of the supplied <paramref name="values" />. May be declared several
    ///     times; the exclusions accumulate. Without a value set the pattern still builds the value — an exclusion
    ///     only rejects and redraws — so a pattern whose language the exclusions leave nothing of surfaces at
    ///     <see cref="Generate" /> as a seed-bearing <see cref="AnyGenerationException" />, never as a
    ///     declaration-time conflict: the library does not enumerate a regular language to prove it empty. Under
    ///     <see cref="OneOf(string[])" /> the domain <i>is</i> enumerable, and an exclusion emptying it is refused at
    ///     declaration instead. Comparison is ordinal, like string equality itself, whether or not the pattern
    ///     ignores case.
    /// </summary>
    /// <param name="values">The values the generated value must differ from; duplicates are ignored.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="values" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="values" /> is empty or contains a <c>null</c> element.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a value set is in force and no value it allows survives.</exception>
    public AnyPattern Except(params string[] values) {
        if (values is null) { throw new ArgumentNullException(nameof(values)); }
        if (values.Length == 0) { throw new ArgumentException("At least one value is required.", nameof(values)); }
        if (values.Any(value => value is null)) { throw new ArgumentException("The values must not contain a null element.", nameof(values)); }

        return Excluding(values, ConstraintCall.Of(nameof(Except), Join(values)));
    }

    /// <summary>
    ///     Requires the generated value to differ from <paramref name="value" /> — typically an existing value the
    ///     test already holds, to exercise an inequality path while keeping the format the pattern defines
    ///     (<c>Any.StringMatching(@"^ORD-\d{8}$").DifferentFrom(existing)</c>). Semantically equivalent to
    ///     <see cref="Except" />; the name carries the intent at the call site.
    /// </summary>
    /// <param name="value">The value the generated value must differ from.</param>
    /// <returns>A new generator carrying the added constraint.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <c>null</c>.</exception>
    /// <exception cref="ConflictingAnyConstraintException">Thrown when a value set is in force and no value it allows survives.</exception>
    public AnyPattern DifferentFrom(string value) {
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        return Excluding([value], ConstraintCall.Of(nameof(DifferentFrom), $"\"{value}\""));
    }

    /// <inheritdoc />
    public string Generate() {
        // With a value set the draw is a uniform pick from the surviving pool: the pattern and the exclusions were
        // both applied to it at declaration, so there is nothing to build and nothing to redraw.
        if (_survivors is not null) { return _survivors[_source.Current.Next(_survivors.Count)]; }
        if (_excluded.Count == 0) { return BuildVerified(); }

        for (int collisions = 0;;) {
            string candidate = BuildVerified();
            if (!_excluded.Contains(candidate, StringComparer.Ordinal)) { return candidate; }
            if (++collisions >= ExclusionRedrawBudget) { throw Exhausted(); }
        }
    }

    [SuppressMessage(SonarRule.S3267.Category, SonarRule.S3267.Id, Justification = SuppressionJustification.S3267.ConditionReadsMutatedCollection)]
    private AnyPattern Excluding(IReadOnlyList<string> values, ConstraintCall applying) {
        List<string> excluded = [.. _excluded];
        foreach (string value in values) {
            if (!excluded.Contains(value, StringComparer.Ordinal)) { excluded.Add(value); }
        }

        AnyPattern candidate = new(this, _allowed, _allowedConstraint, excluded, [.. _exclusions, (applying, values.ToArray())]);
        // Only a value set makes the domain enumerable, and only then can emptiness be established rather than
        // searched for. Without one the exclusion joins the redraw loop exactly as before.
        if (candidate._survivors is null || candidate._survivors.Count > 0) { return candidate; }

        throw ConflictingAnyConstraintException.NoValueRemains(applying, DescribeEmptiedPool(values));
    }

    /// <summary>
    ///     Whether <paramref name="value" /> is one a pooled draw could yield: the pattern admits it and no exclusion
    ///     names it.
    /// </summary>
    private bool Admits(string value) {
        return Matches(value) && !_excluded.Contains(value, StringComparer.Ordinal);
    }

    /// <summary>
    ///     Whether the real .NET engine agrees <paramref name="value" /> matches the pattern — the same verdict, from
    ///     the same verifier, that a built value has to pass (ADR-0027).
    /// </summary>
    private bool Matches(string value) {
        try {
            return _verifier.Value.IsMatch(value);
        } catch (RegexMatchTimeoutException) {
            // Could not decide within the budget. A value the engine cannot vouch for is not one this generator may
            // yield, so it is refused — the same side the build path takes when it treats an undecided match as a miss.
            return false;
        }
    }

    /// <summary>
    ///     Names what turned every supplied value away when a value set is declared. The set is the constraint being
    ///     applied, so the other side is the pattern, the exclusions, or both — and the message claims only the
    ///     stronger of those that is true.
    /// </summary>
    private string DescribeEmptyPool() {
        bool patternRefusesAll   = !_allowed!.Any(Matches);
        bool exclusionsRefuseAny = _allowed!.Any(value => _excluded.Contains(value, StringComparer.Ordinal));

        if (patternRefusesAll && !exclusionsRefuseAny) { return $"{Matching} allows none of its values"; }
        if (patternRefusesAll) { return $"{Matching} allows none of its values, and the exclusions already declared forbid some of them"; }

        // The pattern admits some value, so the exclusions are what emptied the rest: naming the pattern alone would
        // send the caller at a constraint they could loosen without changing the verdict.
        return "the exclusions already declared forbid every value it offers that the pattern admits";
    }

    /// <summary>
    ///     Names what an exclusion emptied when a value set was already in force. Qualified only when the exclusion
    ///     leaves some allowed value standing — the emptiness then genuinely took the pattern or the earlier
    ///     exclusions too, and saying otherwise would point at the wrong constraint.
    /// </summary>
    private string DescribeEmptiedPool(IReadOnlyList<string> values) {
        return _allowed!.Any(value => !values.Contains(value, StringComparer.Ordinal))
                   ? $"it forbids every value {_allowedConstraint} allows that the pattern and the exclusions already declared leave"
                   : $"it forbids every value {_allowedConstraint} allows";
    }

    /// <summary>Builds one value the .NET engine agrees matches the pattern, redrawing past the rare structural miss.</summary>
    private string BuildVerified() {
        for (int attempt = 0; attempt < MatchAttemptLimit; attempt++) {
            RegexGenerationContext context = new(_source.Current, GenerationLimit);
            _root.Append(context);
            string value = context.Result();

            try {
                // _root.Append above already refuses, via AnyGenerationException, a pattern whose generation can
                // never fit the ceiling — so a pattern like 'a{2147483647,}' never reaches this line, and _verifier
                // is never compiled for it. That matters beyond avoiding needless work: compiling a Regex from a
                // pattern with a quantifier bound that large has been observed to exhaust memory on at least one
                // .NET regex engine implementation, and this class must never risk that for a pattern its own
                // ceiling already refuses cleanly.
                if (_verifier.Value.IsMatch(value)) { return value; }
            } catch (RegexMatchTimeoutException) {
                // Could not decide within the budget; treat as a miss and redraw rather than return it unverified.
            }
        }

        throw AnyGenerationException.PatternVerificationFailed(V(MatchAttemptLimit));
    }

    private AnyGenerationException Exhausted() {
        // A pattern generator draws only from its own source, so the seed replays the run fully — never the partial hint.
        Replay replay = Replay.Of(_source);
        // The claim is the budget, not impossibility. The library builds values from the pattern; it never enumerates
        // the language, so it cannot prove one holds no other value. Excluding both words of "^[ab]$" really does
        // empty it — but a pattern with one free value in a few hundred thousand exhausts the same budget and is
        // still satisfiable, so the message states what was established and no more.
        string message =
            $"Could not generate a value matching \"{_pattern}\" while excluding {Join(_excluded)}: no candidate " +
            $"survived {V(ExclusionRedrawBudget)} draws. The redraw is bounded, so this is an exhausted budget rather " +
            "than a proof that the pattern matches no other value — though the usual cause is a pattern the " +
            "exclusions leave nothing of (excluding every word of a language with only a few). Loosen the exclusions " +
            "or widen the pattern. " +
            replay.Guidance;

        return new AnyGenerationException(message, replay.Seed);
    }

}
