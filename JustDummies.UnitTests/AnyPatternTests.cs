#region Usings declarations

using System.Reflection;
using System.Text.RegularExpressions;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnyPatternTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    // The oracle: a generated value is correct iff the REAL .NET regex engine fully matches it. Anchoring with
    // ^(?:...)$ turns the partial-match IsMatch into a whole-string test, so it catches both under-generation
    // (too few characters) and over-generation (trailing junk), and handles top-level alternation correctly.
    private static void AssertMatches(string value, string pattern, RegexOptions options = RegexOptions.None) {
        Assert.True(Regex.IsMatch(value, "^(?:" + pattern + ")$", options),
                    $"generated value {Display(value)} is not matched by /{pattern}/");
    }

    private static string Display(string value) {
        return "\"" + value.Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r") + "\"";
    }

    // The oracle again, as a predicate: does the real .NET engine compile this pattern at all? Used to assert the
    // rejection taxonomy — a pattern the engine accepts must never be reported as malformed (ArgumentException).
    private static bool IsCompiledByTheRealEngine(string pattern) {
        try {
            _ = new Regex(pattern);

            return true;
        } catch (ArgumentException) {
            return false;
        }
    }

    #endregion

    [Theory(DisplayName = "Every generated value is fully matched by the real .NET regex engine.")]
    [InlineData(@"\d{8}")]
    [InlineData(@"^ORD-\d{8}$")]
    [InlineData(@"[A-Z]{3}")]
    [InlineData(@"[a-z]{2,5}")]
    [InlineData(@"(EUR|USD|GBP)")]
    [InlineData(@"[A-Za-z0-9_]+")]
    [InlineData(@"\w{4}\d{2}")]
    [InlineData(@"[^0-9]{3}")]
    [InlineData(@"colou?r")]
    [InlineData(@"a{2,4}b*c+")]
    [InlineData(@"(ab|cd){2,3}")]
    [InlineData(@"\d+\.\d{2}")]
    [InlineData(@"[A-F0-9]{6}")]
    [InlineData(@"(?:foo|bar)-\d+")]
    [InlineData(@"(?<year>\d{4})-(?<month>\d{2})")]
    [InlineData(@"(?'tag'\d{2})")]
    [InlineData(@"(?<1>x)")]          // explicitly-numbered group: a valid capture NUMBER, not an invalid name
    [InlineData(@"(?'2'y)")]          // ...same, quote form
    [InlineData(@"(?<10>ab)")]        // a multi-digit group number stays valid
    [InlineData(@"(?<a1>xy)")]        // a named group whose name merely contains digits stays valid
    [InlineData(@"^a$|^b$")]
    [InlineData(@"^^abc")]            // a run of boundary anchors is a no-op, exactly as in the real engine
    [InlineData(@"abc$$")]
    [InlineData(@"^*abc")]            // a quantifier on a zero-width anchor is a no-op too
    [InlineData(@"abc$*")]
    [InlineData(@"^{2}xy")]
    [InlineData(@"^?abc$?")]
    [InlineData(@"[-[x]]")]           // a leading '-' is an ordinary hyphen, not a subtraction operator
    [InlineData(@"[-[abc]]")]
    [InlineData(@"[\d]{3}")]
    [InlineData(@"[-a-z]{2}")]
    [InlineData(@"[a-z-]{2}")]
    [InlineData(@"[a-b-z]{4}")]
    [InlineData(@"[]a]{3}")]
    [InlineData(@"[^]]{3}")]
    [InlineData(@"[\b]")]
    [InlineData(@"[\1]")]
    [InlineData(@"[\x30-\x39]{3}")]
    [InlineData(@"(a|aa|aaa)")]
    [InlineData(@"a+?b*?")]
    [InlineData(@"\x41\x2DB")]
    [InlineData(@"\a\t")]
    [InlineData(@"\e")]
    [InlineData(@"\cM")]
    [InlineData(@"\0")]
    [InlineData(@"\07")]
    [InlineData(@"a{x}")]
    [InlineData(@"{abc}")]
    [InlineData(@"a{2,")]
    [InlineData(@"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}")]
    [InlineData(@"[A-Z]{2}\d{2}[A-Z0-9]{10}")]
    [InlineData(@"([01]\d|2[0-3]):[0-5]\d")]
    [InlineData(@"\d+\.\d+\.\d+(-[a-z]+(\.\d+)?)?")]
    [InlineData(@"\s")]
    [InlineData(@".")]
    [InlineData(@"")]
    public void GeneratedValuesMatchTheRealEngine(string pattern) {
        AnyContext context   = Any.WithSeed(20260718);
        AnyPattern generator = context.StringMatching(pattern);

        for (int i = 0; i < SampleCount; i++) {
            AssertMatches(generator.Generate(), pattern);
        }
    }

    [Fact(DisplayName = "Generated values vary from draw to draw whenever the pattern leaves room.")]
    public void GeneratedValuesVary() {
        foreach (string pattern in new[] { @"\d{8}", @"[A-Z]{3}", @"(EUR|USD|GBP)", @"[A-Za-z0-9_]+", @"a{2,4}b*c+" }) {
            AnyPattern      generator = Any.WithSeed(4242).StringMatching(pattern);
            HashSet<string> seen      = [];
            for (int i = 0; i < SampleCount; i++) { seen.Add(generator.Generate()); }
            Check.That(seen.Count).IsStrictlyGreaterThan(1);
        }
    }

    [Fact(DisplayName = "A fixed-shape pattern yields exactly that shape.")]
    public void FixedShape() {
        for (int i = 0; i < SampleCount; i++) {
            string reference = Any.StringMatching(@"^ORD-\d{8}$").Generate();
            Check.That(reference.Length).IsEqualTo(12);
            Check.That(reference).StartsWith("ORD-");
            Check.That(reference.Substring(4)).Matches("^[0-9]{8}$");
        }
    }

    [Fact(DisplayName = "Alternation draws each branch and only declared branches.")]
    public void Alternation() {
        HashSet<string> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            string value = Any.StringMatching("(EUR|USD|GBP)").Generate();
            Check.That(value == "EUR" || value == "USD" || value == "GBP").IsTrue();
            seen.Add(value);
        }

        Check.That(seen).Contains("EUR", "USD", "GBP");
    }

    [Fact(DisplayName = "Character classes, ranges and negation stay within their set.")]
    public void CharacterClasses() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.StringMatching("[A-Z]{3}").Generate()).Matches("^[A-Z]{3}$");
            Check.That(Any.StringMatching("[^0-9]{4}").Generate()).Matches("^[^0-9]{4}$");
            Check.That(Any.StringMatching(@"[\d]{5}").Generate()).Matches("^[0-9]{5}$");
        }
    }

    [Fact(DisplayName = "Bounded quantifiers stay within their bounds; unbounded ones draw the minimum plus 0 to 8.")]
    public void QuantifierBounds() {
        HashSet<int> starLengths = [];
        HashSet<int> plusLengths = [];
        HashSet<int> openLengths = [];

        for (int i = 0; i < SampleCount; i++) {
            int bounded = (Any.StringMatching("a{2,4}").Generate()).Length;
            Check.That(bounded is >= 2 and <= 4).IsTrue();

            starLengths.Add((Any.StringMatching("a*").Generate()).Length);
            plusLengths.Add((Any.StringMatching("a+").Generate()).Length);
            openLengths.Add((Any.StringMatching("a{2,}").Generate()).Length);
        }

        Check.That(starLengths.Min()).IsEqualTo(0);
        Check.That(starLengths.Max()).IsEqualTo(8);   // 0 + 0..8
        Check.That(plusLengths.Min()).IsEqualTo(1);
        Check.That(plusLengths.Max()).IsEqualTo(9);   // 1 + 0..8
        Check.That(openLengths.Min()).IsEqualTo(2);
        Check.That(openLengths.Max()).IsEqualTo(10);  // 2 + 0..8
    }

    [Fact(DisplayName = "Anchors are no-ops: the whole generated string is the match.")]
    public void AnchorsAreNoOps() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.StringMatching("^abc$").Generate()).IsEqualTo("abc");
        }
    }

    [Fact(DisplayName = "Repeated and quantified boundary anchors are no-ops, consistently for '^' and '$'.")]
    public void RepeatedAndQuantifiedAnchorsAreNoOps() {
        // '^^abc' was already accepted while the symmetric 'abc$$' was refused — an avoidable asymmetry, now closed.
        // Repeating or quantifying a zero-width boundary assertion never changes which strings match, so all of
        // these are no-ops, exactly as the real engine treats them.
        Check.That(Any.StringMatching(@"^^abc$$").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^*abc$*").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^{2}abc").Generate()).IsEqualTo("abc");
        Check.That(Any.StringMatching(@"^?abc$?").Generate()).IsEqualTo("abc");
    }

    [Fact(DisplayName = "An unbounded quantifier whose minimum sits at int.MaxValue overruns the ceiling; it never yields a short value.")]
    public void UnboundedQuantifierAtTheTopOfTheIntRangeNeverYieldsAShortValue() {
        // Regression: the unbounded repetition count was computed as 'min + Next(0, 9)' in int arithmetic, so a
        // minimum within 8 of int.MaxValue wrapped negative and the repetition loop wrote nothing — Generate()
        // returned "" for a pattern demanding 2,147,483,647 characters, in 36 draws out of 40. A value the pattern
        // does not match is the one outcome generation must never produce; overrunning the ceiling is the honest
        // answer, and the same one 'a{100000,}' already gave.
        AnyPattern generator = Any.StringMatching("a{2147483647,}");

        for (int i = 0; i < 40; i++) {
            Check.ThatCode(() => generator.Generate())
                 .Throws<AnyGenerationException>()
                 .WhichMember(caught => caught.Message).Contains("generation limit");
        }
    }

    [Theory(DisplayName = "Across the whole overflow band, an unbounded minimum overruns the ceiling rather than wrapping negative.")]
    [InlineData(2147483639)] // int.MaxValue - 8: the last minimum that could not wrap even before the fix
    [InlineData(2147483640)] // int.MaxValue - 7: the first that could, and did — 3 empty strings out of 30
    [InlineData(2147483646)]
    [InlineData(2147483647)]
    public void EveryMinimumInTheOverflowBandOverrunsTheCeiling(int minimum) {
        // The band is 'min > int.MaxValue - UnboundedExtra'. Pinned at both edges so a future change to
        // UnboundedExtra cannot narrow the guard back to a subset of it without failing here.
        AnyPattern generator = Any.WithSeed(1).StringMatching($"a{{{minimum},}}");

        for (int i = 0; i < 12; i++) {
            Check.ThatCode(() => generator.Generate()).Throws<AnyGenerationException>();
        }
    }

    [Fact(DisplayName = "A pattern that overruns the generation ceiling fails with an honest message, naming no false cause.")]
    public void OverLimitPatternFailsWithoutAFalseCause() {
        // '(a{1000}){1000}' deterministically asks for 1,000,000 characters — every quantifier is bounded, there is
        // no unbounded quantifier at all. The pattern parses fine (a resource ceiling is not a satisfiability
        // conflict); generation is what overruns the limit, and the message must not blame a quantifier that is absent.
        AnyPattern generator = Any.StringMatching(@"(a{1000}){1000}");

        Check.ThatCode(() => generator.Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(caught => caught.Message)
             .Contains("generation limit")
             .And.Contains("bounded quantifiers")  // the real cause is offered
             .And.Not.Contains("is expanding without bound");  // the old, false assertion is gone
    }

    [Fact(DisplayName = "A negated class that excludes the whole printable universe is refused as unsupported, not malformed.")]
    public void NegatedClassExcludingTheUniverseIsUnsupported() {
        // Well-formed and regular — the real engine accepts both — but no printable-ASCII character survives the
        // negation, so JustDummies cannot draw a value. That is a universe limit (unsupported), not a caller mistake
        // (malformed): the pattern is not broken, JustDummies simply does not reach outside printable ASCII.
        Check.ThatCode(() => Any.StringMatching(@"[^\x20-\x7E]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[^\s\S]")).Throws<UnsupportedRegexException>();

        Check.ThatCode(() => Any.StringMatching(@"[^\x20-\x7E]"))
             .Throws<UnsupportedRegexException>()
             .WhichMember(caught => caught.Message).Contains("printable ASCII");
    }

    [Theory(DisplayName = "A pattern the real .NET engine accepts is never rejected as malformed — it generates, or is refused as unsupported.")]
    [InlineData(@"^^abc")]
    [InlineData(@"abc$$")]
    [InlineData(@"^*abc")]
    [InlineData(@"abc$*")]
    [InlineData(@"^{2}xy")]
    [InlineData(@"^?a$?")]
    [InlineData(@"[-[x]]")]
    [InlineData(@"[a-[x]]")]         // subtraction (member precedes): .NET-valid, refused as UNSUPPORTED, not malformed
    [InlineData(@"[ab-[b]]")]
    [InlineData(@"[^\x20-\x7E]")]    // negated-empty: .NET-valid, refused as UNSUPPORTED
    [InlineData(@"[^\s\S]")]
    [InlineData(@"(a{1000}){1000}")] // over-limit: .NET-valid, fails at generation time, never as malformed
    public void PatternsAcceptedByTheRealEngineAreNeverMalformed(string pattern) {
        // The advertised taxonomy (see RegexParser's summary): ArgumentException == "the real engine rejects this
        // pattern as malformed". So a pattern the real engine COMPILES must never surface here as ArgumentException —
        // JustDummies must either generate a matching value, or refuse it as UnsupportedRegexException, or fail the
        // generation itself. This guards the whole channel, not just the individual edges #210 corrected.
        Assert.True(IsCompiledByTheRealEngine(pattern), $"test precondition: /{pattern}/ must be accepted by .NET");

        try {
            Any.WithSeed(1).StringMatching(pattern).Generate();
        } catch (ArgumentException e) {
            Assert.Fail($"/{pattern}/ is accepted by the real engine but JustDummies rejected it as malformed: {e.Message}");
        } catch (UnsupportedRegexException) {
            // acceptable: refused as unsupported (a construct or universe JustDummies declines), not as malformed
        } catch (AnyGenerationException) {
            // acceptable: a resource-limit overrun, not a verdict that the pattern is malformed
        }
    }

    [Fact(DisplayName = "A Regex with IgnoreCase generates either case.")]
    public void IgnoreCaseHonoured() {
        Regex           pattern = new("^[a-z]{5}$", RegexOptions.IgnoreCase);
        bool            sawUpper = false;
        AnyContext      context  = Any.WithSeed(99);
        AnyPattern      generator = context.StringMatching(pattern);

        for (int i = 0; i < SampleCount; i++) {
            string value = generator.Generate();
            AssertMatches(value, "[a-z]{5}", RegexOptions.IgnoreCase);
            if (value.Any(char.IsUpper)) { sawUpper = true; }
        }

        Check.That(sawUpper).IsTrue();
    }

    [Fact(DisplayName = "A matching generator composes into a value object through As.")]
    public void ComposesThroughAs() {
        IAny<OrderReference> generator = Any.StringMatching(@"^ORD-\d{8}$").As(OrderReference.Create);

        for (int i = 0; i < 50; i++) {
            OrderReference reference = generator.Generate();
            Check.That(reference.Value).StartsWith("ORD-");
            Check.That(reference.Value.Length).IsEqualTo(12);
        }
    }

    [Fact(DisplayName = "Matching is reproducible under a seed.")]
    public void ReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "DifferentFrom never yields the excluded value, and keeps the pattern's format.")]
    public void DifferentFromNeverYieldsTheExcludedValue() {
        const string pattern  = @"^ORD-\d{8}$";
        const string existing = "ORD-12345678";

        IAny<string> generator = Any.WithSeed(20260728).StringMatching(pattern).DifferentFrom(existing);

        for (int i = 0; i < SampleCount; i++) {
            string value = generator.Generate();
            Check.That(value).IsNotEqualTo(existing);
            AssertMatches(value, pattern);
        }
    }

    [Fact(DisplayName = "Except removes every supplied value, and the exclusions accumulate across declarations.")]
    public void ExceptRemovesEverySuppliedValue() {
        // A four-word language with three words excluded: the surviving draw is forced, whichever declaration
        // removed each word.
        IAny<string> generator = Any.WithSeed(20260728).StringMatching("^[ab]{2}$").Except("ab", "ba").DifferentFrom("bb");

        for (int i = 0; i < SampleCount; i++) {
            Check.That(generator.Generate()).IsEqualTo("aa");
        }
    }

    [Fact(DisplayName = "An exclusion is bounded and rejective: it keeps the draw reproducible under a seed.")]
    public void AnExclusionStaysReproducibleUnderASeed() {
        string first  = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").DifferentFrom("ABC-1234").Generate()));
        string second = string.Join("|", Enumerable.Range(0, 20).Select(_ => Any.WithSeed(7).StringMatching(@"[A-Z]{3}-\d{4}").DifferentFrom("ABC-1234").Generate()));

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "An exhausted exclusion budget reports the budget, never a claim that the pattern matches nothing else.")]
    public void AnExhaustedExclusionBudgetReportsTheBudget() {
        // "^[ab]$" really is a two-word language and both words are excluded — yet the generator only ever built
        // and rejected candidates, so the message may claim the spent budget and nothing stronger.
        Check.ThatCode(() => Any.WithSeed(20260728).StringMatching("^[ab]$").Except("a", "b").Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(error => error.Message)
             .Contains("10000 draws")
             .And.Contains("exhausted budget rather than a proof")
             .And.Not.Contains("the pattern has no other value")
             .And.Contains("Loosen the exclusions or widen the pattern");
    }

    [Fact(DisplayName = "The exhaustion names the pattern, the excluded values and the seed that replays the run.")]
    public void TheExhaustionCarriesTheSeed() {
        Check.ThatCode(() => Any.WithSeed(20260728).StringMatching("^[ab]$").Except("a", "b").Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(error => error.Seed, 20260728)
             .And.WhichMember(error => error.Message).Contains("\"^[ab]$\"", "\"a\", \"b\"", "Any.WithSeed(20260728)");
    }

    [Fact(DisplayName = "A value excluded twice is listed once: the exclusions collapse rather than accumulate.")]
    public void RepeatedExclusionsCollapse() {
        Check.ThatCode(() => Any.WithSeed(20260728).StringMatching("^[ab]$").Except("a").DifferentFrom("a").Except("a", "b").Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(error => error.Message)
             .Contains("excluding \"a\", \"b\":")
             .And.Not.Contains("\"a\", \"a\"");
    }

    [Fact(DisplayName = "A shape constraint stays refused: the value set and the exclusion pair are the whole surface.")]
    public void OnlyTheValueSetAndTheRejectivePairAreOffered() {
        // A shape constraint would mean building a value in the intersection of two regular languages, which the
        // generator has no machinery for. The exclusion pair needs none — it rejects rather than builds — and neither
        // does the value set: once the caller supplies the values there is nothing left to build, and the pattern
        // becomes the test each of them passes or fails. Those three are the whole added surface.
        string[] fluent = typeof(AnyPattern)
                          .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                          .Where(method => method.ReturnType == typeof(AnyPattern) && !method.IsSpecialName)
                          .Select(method => method.Name)
                          .Distinct()
                          .OrderBy(name => name, StringComparer.Ordinal)
                          .ToArray();

        Check.That(fluent).ContainsExactly("DifferentFrom", "Except", "OneOf");
    }

    [Fact(DisplayName = "The exclusion arguments are validated as arguments, not as conflicts.")]
    public void ExclusionArgumentsAreValidated() {
        Check.ThatCode(() => Any.StringMatching("a").Except(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.StringMatching("a").Except()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching("a").Except("a", null!)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching("a").DifferentFrom(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Non-regular constructs are refused eagerly with UnsupportedRegexException.")]
    public void UnsupportedConstructsAreRefused() {
        Check.ThatCode(() => Any.StringMatching(@"foo(?=bar)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"foo(?!bar)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?<=x)y")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"\bword\b")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(\w+)\s\1")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"\p{L}+")).Throws<UnsupportedRegexException>();

        Check.ThatCode(() => Any.StringMatching(@"a(?=b)"))
             .Throws<UnsupportedRegexException>()
             .WhichMember(caught => caught.Message).Contains("lookahead");
    }

    [Fact(DisplayName = "Constructs whose language a plain walk cannot honour are refused, never mis-generated.")]
    public void NotGeneratableConstructsAreRefused() {
        // An atomic group commits to its first matching branch: (?>ab|a)b matches only "abb", so lowering it to
        // a plain alternation could emit "ab" — refused instead.
        Check.ThatCode(() => Any.StringMatching(@"(?>ab|a)b")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?>a)")).Throws<UnsupportedRegexException>();

        // A misplaced anchor makes the pattern unmatchable by any whole string.
        Check.ThatCode(() => Any.StringMatching(@"a^")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"$a")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"x(^a)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(a$)x")).Throws<UnsupportedRegexException>();

        // .NET class subtraction removes a nested class; parsing '-[' as members would generate outside the set. It
        // is subtraction only when a base member precedes the '-[' — after a range ('[a-z-[aeiou]]') or a single
        // member ('[a-[x]]', '[ab-[b]]'). A leading '-[' is an ordinary hyphen and IS accepted (see the oracle theory).
        Check.ThatCode(() => Any.StringMatching(@"[a-z-[aeiou]]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[a-[x]]")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"[ab-[b]]")).Throws<UnsupportedRegexException>();

        // IgnorePatternWhitespace changes how the pattern text itself is read: "^A B$" matches "AB", not "A B".
        Check.ThatCode(() => Any.StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
        Check.ThatCode(() => Any.WithSeed(1).StringMatching(new Regex("^A B$", RegexOptions.IgnorePatternWhitespace))).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "A balancing group is refused as unsupported — both syntaxes, target defined or not.")]
    public void BalancingGroupsAreRefused() {
        // A balancing group '(?<-name>…)' / '(?<name1-name2>…)' pops the capture stack — the backreference family,
        // which is non-regular. Its language is not that of a plain named group: '(?<a>y)?(?<-a>x)' matches only
        // "yx" (the '-a' pop forces the optional 'a' group to have fired), yet lowering '(?<-a>x)' to an ordinary
        // named group would emit "x". It is refused instead of mis-generated. .NET accepts these two target-defined
        // patterns, so the refusal is a genuine "we decline what a plain walk cannot honour", not an echo of .NET.
        Check.ThatCode(() => Any.StringMatching(@"(?<a>y)?(?<-a>x)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?<a>y)?(?'-a'x)")).Throws<UnsupportedRegexException>(); // quote form

        // The '-' is refused even when the target group is undefined — where the real engine instead reports a
        // malformed pattern. Distinguishing the two would need a table of captured groups the generator does not
        // keep; the divergence is only in the error kind (both reject, neither mis-generates) and is accepted.
        Check.ThatCode(() => Any.StringMatching(@"(?<-a>x)")).Throws<UnsupportedRegexException>();
        Check.ThatCode(() => Any.StringMatching(@"(?'-a'x)")).Throws<UnsupportedRegexException>();          // quote form
        Check.ThatCode(() => Any.StringMatching(@"(?<x-y>z)")).Throws<UnsupportedRegexException>();         // name1-name2 form

        Check.ThatCode(() => Any.StringMatching(@"(?<a>y)?(?<-a>x)"))
             .Throws<UnsupportedRegexException>()
             .WhichMember(caught => caught.Message).Contains("balancing group");
    }

    [Fact(DisplayName = "Malformed patterns raise ArgumentException; a null pattern raises ArgumentNullException.")]
    public void MalformedPatternsAreRejected() {
        Check.ThatCode(() => Any.StringMatching(@"[a-")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"(abc")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"a{3,1}")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"*abc")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"a\")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching((string)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.StringMatching((Regex)null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Patterns the real engine rejects are rejected here too, never silently re-interpreted.")]
    public void RealEngineRejectionsAreMirrored() {
        // Each of these is refused by System.Text.RegularExpressions; accepting them would make the generator
        // produce values for patterns no production code could ever carry.
        Check.ThatCode(() => Any.StringMatching(@"a*+")).Throws<ArgumentException>();      // possessive: not a .NET construct
        Check.ThatCode(() => Any.StringMatching(@"a**")).Throws<ArgumentException>();      // nested quantifier
        Check.ThatCode(() => Any.StringMatching(@"a*??")).Throws<ArgumentException>();     // nested quantifier
        Check.ThatCode(() => Any.StringMatching(@"[]")).Throws<ArgumentException>();       // unterminated class
        Check.ThatCode(() => Any.StringMatching(@"\q")).Throws<ArgumentException>();       // unrecognized escape
        Check.ThatCode(() => Any.StringMatching(@"\x4")).Throws<ArgumentException>();      // \x expects 2 hex digits
        Check.ThatCode(() => Any.StringMatching(@"\c1")).Throws<ArgumentException>();      // \c expects a letter
        Check.ThatCode(() => Any.StringMatching(@"{2}")).Throws<ArgumentException>();      // quantifier following nothing
        Check.ThatCode(() => Any.StringMatching(@"(?<>a)")).Throws<ArgumentException>();   // empty group name
    }

    [Fact(DisplayName = "An invalid group name is rejected as malformed, matching the real engine — both syntaxes.")]
    public void InvalidGroupNamesAreRejected() {
        // A name opening with a digit is an explicit capture NUMBER, which the real engine accepts only as a positive
        // integer with no leading zero. '0' (reserved for the whole match), a leading zero, and a digit-then-letter
        // name are all rejected — here as they are there.
        Check.ThatCode(() => Any.StringMatching(@"(?<1a>x)")).Throws<ArgumentException>();  // digit then letter
        Check.ThatCode(() => Any.StringMatching(@"(?<0>x)")).Throws<ArgumentException>();   // group 0 is reserved
        Check.ThatCode(() => Any.StringMatching(@"(?<01>x)")).Throws<ArgumentException>();  // leading zero
        Check.ThatCode(() => Any.StringMatching(@"(?'0'x)")).Throws<ArgumentException>();   // quote form, reserved

        // A non-numeric name must be word characters (letter, digit or underscore); a space or a dot is malformed.
        Check.ThatCode(() => Any.StringMatching(@"(?<a b>x)")).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching(@"(?'a b'x)")).Throws<ArgumentException>(); // quote form
        Check.ThatCode(() => Any.StringMatching(@"(?<a.b>x)")).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Escape sequences generate the real characters, not their letter.")]
    public void EscapesGenerateTheRealCharacters() {
        Check.That(Any.StringMatching(@"\a").Generate()).IsEqualTo("\a");
        Check.That(Any.StringMatching(@"\e").Generate()).IsEqualTo("\u001B");
        Check.That(Any.StringMatching(@"\x41").Generate()).IsEqualTo("A");
        Check.That(Any.StringMatching(@"\u0042").Generate()).IsEqualTo("B");
        Check.That(Any.StringMatching(@"\cA").Generate()).IsEqualTo("\u0001");
        Check.That(Any.StringMatching(@"\07").Generate()).IsEqualTo("\a");
        Check.That(Any.StringMatching(@"\0").Generate()).IsEqualTo("\0");
    }

    [Fact(DisplayName = "A brace that is not a well-formed quantifier is a literal, exactly as in the real engine.")]
    public void BraceLiteralsGenerate() {
        Check.That(Any.StringMatching(@"a{x}").Generate()).IsEqualTo("a{x}");
        Check.That(Any.StringMatching(@"{abc}").Generate()).IsEqualTo("{abc}");
        Check.That(Any.StringMatching(@"a{2,").Generate()).IsEqualTo("a{2,");
    }

    [Fact(DisplayName = "Nesting groups beyond the parser's depth ceiling fails cleanly instead of overflowing the stack.")]
    public void DeepNestingFailsCleanly() {
        string deep = new string('(', 300) + "a" + new string(')', 300);

        ArgumentException caught = Assert.Throws<ArgumentException>(() => Any.StringMatching(deep));
        Check.That(caught.Message).Contains("nested");
    }

    [Theory(DisplayName = "A class range ending at U+FFFF terminates promptly and yields a member, instead of hanging.")]
    [InlineData(@"[\u0020-\uFFFF]")]  // \uFFFF escape: drives the range's upper bound to the top of the char space...
    [InlineData("[ -\uFFFF]")]        // ...and a literal U+FFFF member does the same; both once wrapped the 16-bit loop.
    public async Task ClassRangeEndingAtMaxCharTerminates(string pattern) {
        // Generate off-thread and race a deadline: a loop that wraps a 16-bit char past U+FFFF loses the race and
        // fails the test instead of hanging the whole suite (mirrors the AnyGuid carry-wraparound guard).
        Task<string> run   = Task.Run(() => Any.StringMatching(pattern).Generate());
        Task         first = await Task.WhenAny(run, Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Check.That(first == run).IsTrue();

        // The generated value is a genuine member of the class — the real .NET engine is the oracle.
        AssertMatches(await run, pattern);
    }

    [Fact(DisplayName = "A nullable alternative under a quantifier never yields a value the pattern rejects (regression #335).")]
    public void ANullableAlternativeUnderAQuantifierNeverYieldsAnUnmatchedValue() {
        // #335: the structural generator picked the zero-width \S{0} branch and emitted "", but the real .NET
        // engine refuses "" for this shape — an arcane, order- and form-dependent empty-match behaviour the
        // generator cannot mirror. Every draw must match the pattern the value was generated from.
        const string pattern    = @"(?:r{1,2}|\S{0}){1,2}";
        IAny<string> generator = Any.StringMatching(pattern);

        for (int i = 0; i < 1000; i++) {
            AssertMatches(generator.Generate(), pattern);
        }
    }

    #region The value set (issue #185)

    private static List<string> Draw(IAny<string> generator) {
        List<string> drawn = [];
        for (int i = 0; i < SampleCount; i++) { drawn.Add(generator.Generate()); }

        return drawn;
    }

    [Fact(DisplayName = "A value set draws from the supplied values, and the pattern is what keeps them.")]
    public void AValueSetDrawsFromTheValuesThePatternAdmits() {
        // The whole shape of the constraint: the caller supplies the domain, and the pattern stops building to
        // become the test each supplied value passes or fails. "abcd" is not a value the generator may yield.
        IAny<string> generator = Any.StringMatching(@"^\d{3}$").OneOf("123", "abcd", "456");

        for (int i = 0; i < SampleCount; i++) {
            Check.That(generator.Generate()).IsOneOfThese("123", "456");
        }
    }

    [Fact(DisplayName = "A value set the pattern admits nothing of conflicts at declaration, naming both sides.")]
    public void AValueSetThePatternRefusesConflicts() {
        // Eager, not deferred: with the domain supplied it can be established rather than searched for, so this is
        // a ConflictingAnyConstraintException at declaration and not an AnyGenerationException after a spent budget.
        Check.ThatCode(() => Any.StringMatching(@"^\d{3}$").OneOf("abcd", "ef"))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"abcd\", \"ef\") because StringMatching(\"^\\d{3}$\") allows none of its values.");
    }

    [Fact(DisplayName = "Under a value set an exclusion emptying it conflicts instead of spending the redraw budget.")]
    public void AnExclusionEmptyingAValueSetConflicts() {
        // Without a set this same exclusion could only be discovered at Generate, by exhausting the budget — the
        // library does not enumerate a regular language to prove it empty. A supplied set IS enumerable.
        Check.ThatCode(() => Any.StringMatching(@"^\d{3}$").OneOf("123", "456").Except("123", "456"))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply Except(\"123\", \"456\") because it forbids every value OneOf(\"123\", \"456\") allows.");
    }

    [Fact(DisplayName = "The value set and the exclusion pair compose in either order.")]
    public void TheValueSetAndTheExclusionsComposeInEitherOrder() {
        Check.That(Any.StringMatching(@"^\d{3}$").OneOf("123", "456").DifferentFrom("123").Generate()).IsEqualTo("456");
        Check.That(Any.StringMatching(@"^\d{3}$").DifferentFrom("123").OneOf("123", "456").Generate()).IsEqualTo("456");
    }

    [Fact(DisplayName = "A second value set is refused; the same one declared twice is a no-op.")]
    public void ASecondValueSetIsRefused() {
        AnyPattern generator = Any.StringMatching(@"^\d{3}$").OneOf("123", "456");

        Check.That(generator.OneOf("123", "456").Generate()).IsOneOfThese("123", "456");
        Check.ThatCode(() => generator.OneOf("789"))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply OneOf(\"789\") because OneOf(\"123\", \"456\") is already defined.");
    }

    [Fact(DisplayName = "A pooled pattern reports its survivors and what refused the rest.")]
    public void APooledPatternReportsItsSurvivorsAndRejections() {
        // Which of the two to repair — the catalogue or the invariant — is the question the inspection answers, and
        // on a pattern the answer names the pattern itself: it is the constraint that turned the value away.
        IPoolInspection<string> inspection = Any.StringMatching(@"^\d{3}$").OneOf("123", "abcd").DifferentFrom("456");

        Check.That(inspection.IsPooled).IsTrue();
        Check.That(inspection.GetSurvivors()).ContainsExactly("123");
        Check.That(inspection.GetRejections().Select(rejection => rejection.Value)).ContainsExactly("abcd");
        Check.That(inspection.GetRejections().Single().RejectedBy.Select(constraint => constraint.ToString()))
             .ContainsExactly("StringMatching(\"^\\d{3}$\")");
    }

    [Fact(DisplayName = "A value a value set and an exclusion both refuse names both of them.")]
    public void ARejectionNamesEveryConstraintRefusingTheValue() {
        // Naming one of two would send a reader at a constraint they could loosen without changing the verdict.
        IPoolInspection<string> inspection = Any.StringMatching(@"^\d{3}$").OneOf("123", "456").Except("123");

        Check.That(inspection.GetRejections().Single().RejectedBy.Select(constraint => constraint.ToString()))
             .ContainsExactly("Except(\"123\")");
    }

    [Fact(DisplayName = "A pooled pattern answers a distinct collection with the size of its surviving set.")]
    public void APooledPatternAnswersItsCardinality() {
        // Two survivors, so a third distinct element cannot be drawn — and with the domain supplied that is
        // established at declaration rather than discovered by exhausting the dedup draw.
        Check.ThatCode(() => Any.SetOf(Any.StringMatching(@"^\d{3}$").OneOf("123", "456")).WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "A value set is drawn reproducibly under a seed.")]
    public void AValueSetIsDrawnReproducibly() {
        string[] pool = ["123", "456", "789"];

        List<string> first  = Draw(Any.WithSeed(4242).StringMatching(@"^\d{3}$").OneOf(pool));
        List<string> second = Draw(Any.WithSeed(4242).StringMatching(@"^\d{3}$").OneOf(pool));

        Check.That(first).ContainsExactly(second);
        Check.That(first.Distinct()).HasSize(3);
    }

    [Fact(DisplayName = "The value set arguments are validated as arguments, not as conflicts.")]
    public void ValueSetArgumentsAreValidated() {
        Check.ThatCode(() => Any.StringMatching("a").OneOf((string[])null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.StringMatching("a").OneOf((IEnumerable<string>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Any.StringMatching("a").OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Any.StringMatching("a").OneOf("a", null!)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The sequence form accepts a set already held as a list.")]
    public void TheSequenceFormAcceptsAHeldCollection() {
        List<string> references = ["123", "456"];

        Check.That(Any.StringMatching(@"^\d{3}$").OneOf(references).Generate()).IsOneOfThese("123", "456");
    }

    [Fact(DisplayName = "Membership is ordinal even where the pattern ignores case.")]
    public void MembershipIsOrdinalEvenUnderAnIgnoreCasePattern() {
        // The pattern decides membership of the language; ordinal equality decides identity. Those are not the same
        // question, and an exclusion answers the second — so excluding "ABC" leaves "abc" drawable.
        AnyPattern generator = Any.StringMatching(new Regex("^[a-c]{3}$", RegexOptions.IgnoreCase))
                                  .OneOf("abc", "ABC")
                                  .DifferentFrom("ABC");

        Check.That(generator.Generate()).IsEqualTo("abc");
    }

    #endregion

}
