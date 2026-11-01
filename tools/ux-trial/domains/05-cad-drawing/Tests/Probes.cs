using System.Text.RegularExpressions;

using Drafting.Domain;

using JustDummies;

using Xunit;

namespace Drafting.Tests;

/// <summary>Deliberate edge cases. Each test pins the behaviour observed, so the file reads as a log.</summary>
public sealed class Probes {

    // ---- contradictions -------------------------------------------------------------------------

    [Fact]
    public void Crossed_double_bounds_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(10).LessThan(5));

        Assert.Equal("Cannot apply LessThan(5) because GreaterThan(10) already requires values greater than 10.", refusal.Message);
    }

    [Fact]
    public void Positive_then_Negative_on_a_double_quotes_the_strict_bound_as_declared() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().Positive().Negative());

        Assert.Equal("Cannot apply Negative() because Positive() already requires values greater than 0.", refusal.Message);
    }

    [Fact]
    public void A_pool_disjoint_from_the_declared_range_is_refused() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, BomItem.MaximumQuantity).OneOf(10_000));

        Assert.Equal("Cannot apply OneOf(10000) because Between(1, 9999) allows none of its values.", refusal.Message);
    }

    /// <summary>
    ///     A second OneOf that is a SUBSET of the first admits values ("H"), yet it is refused with the
    ///     exception that means "the constraints admit no value". It bites through a scaffolded generator:
    ///     <c>WithZone(z => z.OneOf("H", "JS"))</c> on a base of <c>OneOf("H", "h", "JS", "js")</c>.
    /// </summary>
    [Fact]
    public void A_narrower_OneOf_over_a_OneOf_base_is_refused_although_values_remain() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.String().OneOf("H", "h", "JS", "js").OneOf("H", "JS"));

        Assert.Equal("Cannot apply OneOf(\"H\", \"JS\") because OneOf(\"H\", \"h\", \"JS\", \"js\") is already defined.", refusal.Message);
        // The working spelling: narrow a pool with Except.
        Assert.Contains(Any.String().OneOf("H", "h", "JS", "js").Except("h", "js").Generate(), new[] { "H", "JS" });
    }

    [Fact]
    public void Four_distinct_members_of_a_three_member_enum_are_refused_at_Generate_not_at_declaration() {
        Exception? atDeclarationCountFirst    = Record.Exception(() => Any.ListOf(Any.Enum<FitKind>()).WithCount(4).Distinct());
        Exception? atDeclarationDistinctFirst = Record.Exception(() => Any.ListOf(Any.Enum<FitKind>()).Distinct().WithCount(4));
        Exception? atDeclarationSet           = Record.Exception(() => Any.SetOf(Any.Enum<FitKind>()).WithCount(4));
        ConflictingAnyConstraintException atGenerate = Assert.Throws<ConflictingAnyConstraintException>(() => Any.ListOf(Any.Enum<FitKind>()).WithCount(4).Distinct().Generate());
        ConflictingAnyConstraintException setAtGenerate = Assert.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<FitKind>()).WithCount(4).Generate());

        Assert.Equal("Cannot apply Distinct() because 4 elements required to be distinct to honour WithCount(4) exceed the 3 distinct value(s) the element generator can produce.", atGenerate.Message);
        Assert.Equal("The generator can produce only 3 distinct value(s), not enough for the 4 elements it must still supply to honour WithCount(4).", setAtGenerate.Message);
        Assert.Equal("null|null|null", $"{atDeclarationCountFirst?.GetType().Name ?? "null"}|{atDeclarationDistinctFirst?.GetType().Name ?? "null"}|{atDeclarationSet?.GetType().Name ?? "null"}");
    }

    // ---- bounds ---------------------------------------------------------------------------------

    [Fact]
    public void A_double_pinned_by_equal_bounds_is_that_value() {
        Assert.Equal(25.0, Any.Double().Between(25.0, 25.0).Generate());
    }

    [Fact]
    public void The_next_representable_double_is_reachable_but_not_the_open_interval_below_it() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(1.0).LessThan(Math.BitIncrement(1.0)));
        Assert.Equal(Math.BitIncrement(1.0), Any.Double().GreaterThan(1.0).LessThanOrEqualTo(Math.BitIncrement(1.0)).Generate());
    }

    [Fact]
    public void A_full_range_double_stays_within_an_ordinary_magnitude_nine_draws_in_ten() {
        // ADR-0117: a range spelled against the type's own edges is no range; the tenth draw spans the type.
        int ordinary = 0;
        for (int draw = 0; draw < 1_000; draw++) {
            double value = Any.Double().Between(-double.MaxValue, double.MaxValue).Generate();
            Assert.True(double.IsFinite(value));
            if (Math.Abs(value) <= 1e6) { ordinary++; }
        }
        Assert.InRange(ordinary, 800, 970);
    }

    [Fact]
    public void A_micrometre_scale_range_draws_inside_it() {
        for (int draw = 0; draw < 1_000; draw++) {
            Assert.InRange(Any.Double().Between(0.001, 0.002).Generate(), 0.001, 0.002);
        }
    }

    [Fact]
    public void An_unbounded_positive_double_never_comes_near_small_sizes() {
        // Uniform over the ordinary magnitude: a 0.5 mm or 3 mm nominal is never drawn unless bounded.
        double smallest = Enumerable.Range(0, 20_000).Select(_ => Any.Double().Positive().Generate()).Min();

        Assert.True(smallest > 1, $"smallest was {smallest:R}");
    }

    [Fact]
    public void A_decimal_scale_beyond_28_is_an_argument_error_and_a_scale_grid_missing_the_range_conflicts() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().WithScale(Scale()));
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(0.0001m, 0.0009m).WithScale(3));

        Assert.Equal("Cannot apply WithScale(3) because no Decimal value WithScale(3) allows remains between 0.0001 and 0.0009.", refusal.Message);
    }

    private static int Scale() {
        return 29;
    }

    [Fact]
    public void DateOnly_edges_are_honoured_and_beyond_them_refused() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().Between(DateOnly.MaxValue, DateOnly.MaxValue).Generate());
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));

        Assert.Equal("Cannot apply After(9999-12-31) because no DateOnly value satisfies it.", refusal.Message);
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().After(TimeOnly.MaxValue));
    }

    [Fact]
    public void Sizes_above_a_million_are_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ListOf(Any.Int32()).WithCount(Count()));
    }

    private static int Count() {
        return 1_000_001;
    }

    // ---- regular expressions a drawing office would write ---------------------------------------

    [Theory]
    [InlineData(@"^(?!.*[IO])[A-Z]$", "negative lookahead")]
    [InlineData(@"^[A-Z-[IOQSXZ]]$", "character-class subtraction")]
    [InlineData(@"^\p{Lu}{3}$", "Unicode category")]
    [InlineData(@"\bMEC\b", "word-boundary")]
    public void Non_regular_or_unsupported_drawing_patterns_are_refused_by_name(string pattern, string construct) {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(pattern));

        Assert.Contains(construct, refusal.Message);
    }

    [Fact]
    public void The_revision_letter_set_is_expressible_as_explicit_ranges() {
        // What a developer writes once [A-Z-[IOQSXZ]] is refused.
        Regex revision = new(@"^[A-HJ-NPRT-WY]$");

        for (int draw = 0; draw < 200; draw++) {
            Assert.Matches(revision, Any.StringMatching(revision).Generate());
        }
    }

    [Fact]
    public void A_file_name_with_escaped_dot_and_a_non_ascii_literal_matches() {
        Regex fileName = new(@"^Ø\d{2} (MEC|ELE)-\d{5}/SH\d{2}\.dwg$");

        Assert.Matches(fileName, Any.StringMatching(fileName).Generate());
    }

    [Fact]
    public void Leading_case_insensitivity_varies_the_case() {
        string anyNumber = Any.StringMatching(@"(?i)^mec-\d{5}$").Generate();

        Assert.Matches(new Regex(@"^mec-\d{5}$", RegexOptions.IgnoreCase), anyNumber);
    }

    [Fact]
    public void IgnorePatternWhitespace_is_refused_as_an_argument_error() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.StringMatching(new Regex(@"^ MEC - \d{5} $", RegexOptions.IgnorePatternWhitespace)));

        Assert.StartsWith("RegexOptions.IgnorePatternWhitespace changes how the pattern text is read", refusal.Message);
    }

    [Fact]
    public void A_net_literal_brace_quantifier_lookalike_is_generated_as_dotnet_reads_it() {
        // .NET reads A{,3} as the literal text "A{,3}"; the generator agrees.
        string anyValue = Any.StringMatching(@"^A{,3}$").Generate();

        Assert.Matches(new Regex(@"^A{,3}$"), anyValue);
    }

    [Fact]
    public void Distinct_values_from_a_one_word_pattern_end_in_a_bounded_redraw_not_an_upfront_refusal() {
        AnyGenerationException refusal = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching(@"^P00000[12]$")).WithCount(3).Distinct().Generate());

        Assert.StartsWith("Could not generate a distinct collection of 3 elements: the element generator produced only 2 distinct value(s)", refusal.Message);
    }

    // ---- reproducibility ------------------------------------------------------------------------

    [Fact]
    public void Two_contexts_on_one_seed_draw_the_same_drawing_numbers_and_sizes() {
        AnyContext first  = Any.WithSeed(42);
        AnyContext second = Any.WithSeed(42);

        Assert.Equal(first.StringMatching(@"^(MEC|ELE|HYD)-\d{5}$").Generate(), second.StringMatching(@"^(MEC|ELE|HYD)-\d{5}$").Generate());
        Assert.Equal(first.Double().Positive().Generate(), second.Double().Positive().Generate());
    }

    [Fact]
    public void A_seeded_scope_replays_a_whole_bom_tree() {
        string first;
        string second;
        using (Any.UseSeed(7)) {
            first = Describe(Any.BomItem().WithDepth(3).Generate());
        }
        using (Any.UseSeed(7)) {
            second = Describe(Any.BomItem().WithDepth(3).Generate());
        }

        Assert.Equal(first, second);
    }

    [Fact]
    public void Reproducibly_reports_the_seed_only_on_failure_and_the_seed_replays_the_failure() {
        string? reported = null;
        int     drawnInFailingRun = 0;
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
                                                                            drawnInFailingRun = Any.Int32().Generate();
                                                                            throw new InvalidOperationException("boom");
                                                                        },
                                                                        message => reported = message));
        Assert.NotNull(reported);
        int seed = int.Parse(Regex.Match(reported, @"seeded with (-?\d+)").Groups[1].Value);

        int drawnInReplay = 0;
        Any.Reproducibly(seed, () => drawnInReplay = Any.Int32().Generate());

        Assert.Equal(drawnInFailingRun, drawnInReplay);
    }

    /// <summary>
    ///     Outside any scope, a bounded-redraw failure reports the seed the ambient source lazily picked
    ///     for the whole flow. Since #308 it no longer promises that this seed replays the run: it says no
    ///     scope pinned it and asks for one. Two failures at different points of the flow report the same
    ///     number, and replaying it restarts the stream at its first draw, not at the failing one.
    /// </summary>
    [Fact]
    public void Outside_a_scope_the_reported_seed_names_the_flow_and_asks_for_a_scope() {
        int firstDraw = Any.Int32().Generate();
        AnyGenerationException early = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching("^A$")).WithCount(2).Distinct().Generate());
        int laterDraw = Any.Int32().Generate();
        AnyGenerationException late = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching("^A$")).WithCount(2).Distinct().Generate());

        Assert.Equal(early.Seed, late.Seed);
        Assert.Contains($"This flow drew from seed {late.Seed}, which no scope pinned", late.Message);
        Assert.DoesNotContain("reproduce this run", late.Message);

        int replayed = 0;
        Any.Reproducibly(late.Seed!.Value, () => replayed = Any.Int32().Generate());
        Assert.Equal(firstDraw, replayed);
        Assert.NotEqual(laterDraw, replayed);
    }

    // ---- pools ----------------------------------------------------------------------------------

    [Fact]
    public void A_pattern_pool_reports_the_catalogue_entries_it_drops() {
        string[] partsInTheErpExport = ["P004217", "P004217-A2", "P04217", "p004218"];
        IAny<string> parts = Any.StringMatching(@"^P\d{6}(-[A-Z0-9]{2})?$").OneOf(partsInTheErpExport);

        IPoolInspection<string> pool = Assert.IsAssignableFrom<IPoolInspection<string>>(parts);

        Assert.Equal(["P04217", "p004218"], pool.GetRejections().Select(rejection => rejection.Value));
    }

    // ---- scaffolded generators ------------------------------------------------------------------

    [Fact]
    public void An_untoleranced_dimension_needs_a_null_forgiving_cast_through_the_scaffolded_pin() {
        // The parameter is IsoTolerance?, the scaffolded pin takes IsoTolerance: `WithTolerance(null)` is
        // ambiguous with the lambda overload, and `(IsoTolerance?)null` warns CS8604.
        Dimension anyPlainDimension = new AnyDimension().WithTolerance((IsoTolerance)null!).Generate();

        Assert.Null(anyPlainDimension.Tolerance);
    }

    private static string Describe(BomItem item) {
        return $"{item.Part}x{item.Quantity}[{string.Join(",", item.Children.Select(Describe))}]";
    }

}
