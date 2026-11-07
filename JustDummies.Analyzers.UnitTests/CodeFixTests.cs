using JustDummies.Analyzers.CodeFixes;

using NFluent;

namespace JustDummies.Analyzers.UnitTests;

/// <summary>
///     The code fixes (ADR-0130), each checked by the source it produces, whole. The first batch set the pattern — a
///     removal, an argument swap, a rewrite — and the rest follow it, two of them offering a choice of two actions.
/// </summary>
public class CodeFixTests {

    [Theory]
    [InlineData("Any.Int32().Between(1, 10).Except(20)",               "Any.Int32().Between(1, 10)")]
    [InlineData("Any.Int32().Between(1, 10).Except(20).MultipleOf(3)", "Any.Int32().Between(1, 10).MultipleOf(3)")]
    [InlineData("Any.Int32().Positive().GreaterThan(-5)",              "Any.Int32().Positive()")]
    [InlineData("Any.String().Except()",                               "Any.String()")]
    [InlineData("Any.Int32().Between(1, 10).Except(20).Generate()",    "Any.Int32().Between(1, 10).Generate()")]
    public async Task JD024_removes_the_constraint_that_narrows_nothing(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new ScalarChainAdmitsNoValueAnalyzer(), new ConstraintWithNoEffectCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.Int32().Between(10, 5)",                                                     "Any.Int32().Between(5, 10)")]
    [InlineData("Any.String().WithLengthBetween(10, 5)",                                          "Any.String().WithLengthBetween(5, 10)")]
    [InlineData("Any.Int32().Between(maximum: 5, minimum: 10)",                                   "Any.Int32().Between(maximum: 10, minimum: 5)")]
    [InlineData("Any.DateTime().Between(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1))",     "Any.DateTime().Between(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2))")]
    [InlineData("Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1))", "Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))")]
    public async Task JD014_swaps_a_transposed_pair(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.String().WithLength(-1)")]
    [InlineData("Any.Int32().MultipleOf(0)")]
    [InlineData("Any.String().StartingWith(\"\")")]
    public async Task JD014_offers_nothing_where_a_rewrite_would_guess(string chain) {
        // A negative size, a step of zero, an empty prefix: what the developer meant is not in the call.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Theory]
    [InlineData("Any.String().WithMinLength(8).WithMaxLength(20)",                     "Any.String().WithLengthBetween(8, 20)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50)",           "Any.Int32().Between(1, 50)")]
    // Whichever bound was written first keeps its place in the chain, and the arguments come minimum first, as the range takes them.
    [InlineData("Any.Int32().LessThanOrEqualTo(50).GreaterThanOrEqualTo(1)",           "Any.Int32().Between(1, 50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).Positive().LessThanOrEqualTo(50)", "Any.Int32().Between(1, 50).Positive()")]
    [InlineData("Any.ListOf(Any.Int32()).WithMinCount(2).WithMaxCount(4).Generate()",  "Any.ListOf(Any.Int32()).WithCountBetween(2, 4).Generate()")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.MinValue).BeforeOrEqualTo(DateTime.MaxValue)", "Any.DateTime().Between(DateTime.MinValue, DateTime.MaxValue)")]
    // A constant, a cast or a minus over one, a limit a System value type names, a System value type built from
    // constants: nothing here runs when it is moved, so the rewrite keeps every observable order.
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Floor).LessThanOrEqualTo(Bounds.Ceiling)",       "Any.Int32().Between(Bounds.Floor, Bounds.Ceiling)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(-(5)).LessThanOrEqualTo((int)50L)",                      "Any.Int32().Between(-(5), (int)50L)")]
    [InlineData("Any.TimeSpan().GreaterThanOrEqualTo(TimeSpan.Zero).LessThanOrEqualTo(new TimeSpan(1, 0, 0))", "Any.TimeSpan().Between(TimeSpan.Zero, new TimeSpan(1, 0, 0))")]
    [InlineData("Any.DateOnly().AfterOrEqualTo(DateOnly.MinValue).BeforeOrEqualTo(DateOnly.FromDayNumber(7))", "Any.DateOnly().Between(DateOnly.MinValue, DateOnly.FromDayNumber(7))")]
    public async Task JD031_writes_the_pair_as_the_range(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Minimum()).LessThanOrEqualTo(50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).Positive().LessThanOrEqualTo(Bounds.Maximum())")]
    [InlineData("Any.String().WithMinLength(Bounds.Minimum()).WithMaxLength(Bounds.Maximum())")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Low).LessThanOrEqualTo(50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(bound).LessThanOrEqualTo(50)")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.Now).BeforeOrEqualTo(DateTime.MaxValue)")]
    public async Task JD031_offers_nothing_where_an_argument_may_run_code(string chain) {
        // The rewrite carries the later bound's argument past the links written between the two, so an argument that
        // runs the developer's own code -- a call, a property of their own, a local or a parameter -- could run in
        // another order. The rule still reports the pair; the fix stays away.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Fact]
    public async Task A_fix_is_named_for_what_it_does() {
        IReadOnlyList<string> removal = await CodeFixTestHarness.OffersAsync(new ScalarChainAdmitsNoValueAnalyzer(), new ConstraintWithNoEffectCodeFix(), Source("Any.Int32().Between(1, 10).Except(20)"));
        IReadOnlyList<string> swap    = await CodeFixTestHarness.OffersAsync(new RejectedConstantArgumentAnalyzer(), new RejectedConstantArgumentCodeFix(), Source("Any.Int32().Between(10, 5)"));
        IReadOnlyList<string> range   = await CodeFixTestHarness.OffersAsync(new PairedBoundsHaveARangeFormAnalyzer(), new PairedBoundsHaveARangeFormCodeFix(), Source("Any.Int32().GreaterThanOrEqualTo(1).LessThanOrEqualTo(50)"));

        Check.That(removal).ContainsExactly("Remove Except(20), which narrows nothing");
        Check.That(swap).ContainsExactly("Swap the two arguments");
        Check.That(range).ContainsExactly("Write the range as Between(1, 50)");
    }

    [Theory]
    [InlineData("Any.OneOf(1, 2, 1)",                                "Any.OneOf(1, 2)")]
    [InlineData("Any.OneOf(\"EUR\", \"USD\", \"EUR\")",             "Any.OneOf(\"EUR\", \"USD\")")]
    [InlineData("Any.Int32().OneOf(3, 3)",                           "Any.Int32().OneOf(3)")]
    [InlineData("Any.String().OneOf([\"a\", \"b\", \"a\"])",         "Any.String().OneOf([\"a\", \"b\"])")]
    [InlineData("Any.String().WithChars(\"abca\").WithLength(4)",    "Any.String().WithChars(\"abc\").WithLength(4)")]
    [InlineData("Any.String().WithChars(\"aab\").WithLength(4)",     "Any.String().WithChars(\"ab\").WithLength(4)")]
    public async Task JD025_removes_the_value_listed_twice(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new DuplicatePoolValueAnalyzer(), new DuplicatePoolValueCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.String().WithChars(\"a\\tb\\t\").WithLength(4)")]
    [InlineData("Any.String().WithChars(@\"abca\").WithLength(4)")]
    public async Task JD025_offers_nothing_where_the_repeated_character_cannot_be_placed(string chain) {
        // An escape or a verbatim literal breaks the one-column-per-character reading the rule places its diagnostic
        // by, so the rule covers the whole literal and the fix, which would have to guess which character, stays away.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new DuplicatePoolValueAnalyzer(), new DuplicatePoolValueCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Theory]
    [InlineData("Any.String().WithMinLength(8).WithMinLength(10).Generate()",     "Any.String().WithMinLength(10).Generate()")]
    // The looser bound goes whichever order the two were written in: the second here, the first above.
    [InlineData("Any.String().WithMinLength(10).WithMinLength(8).Generate()",     "Any.String().WithMinLength(10).Generate()")]
    [InlineData("Any.Int32().LessThanOrEqualTo(90).LessThanOrEqualTo(50)",        "Any.Int32().LessThanOrEqualTo(50)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(1).Positive().GreaterThanOrEqualTo(5)", "Any.Int32().Positive().GreaterThanOrEqualTo(5)")]
    [InlineData("Any.ListOf(Any.Int32()).WithMaxCount(9).WithMaxCount(5).Generate()", "Any.ListOf(Any.Int32()).WithMaxCount(5).Generate()")]
    [InlineData("Any.Double().GreaterThanOrEqualTo(1.5).GreaterThanOrEqualTo(2.5)", "Any.Double().GreaterThanOrEqualTo(2.5)")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.MinValue).AfterOrEqualTo(new DateTime(2026, 1, 1))", "Any.DateTime().AfterOrEqualTo(new DateTime(2026, 1, 1))")]
    // Two equal bounds: the second is the one removed, so the chain reads as it was first written.
    [InlineData("Any.Int32().LessThanOrEqualTo(50).LessThanOrEqualTo(50)",        "Any.Int32().LessThanOrEqualTo(50)")]
    public async Task JD032_removes_the_looser_of_the_two_bounds(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new BoundDeclaredTwiceAnalyzer(), new BoundDeclaredTwiceCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(bound).GreaterThanOrEqualTo(5)")]
    [InlineData("Any.Int32().GreaterThanOrEqualTo(Bounds.Minimum()).GreaterThanOrEqualTo(5)")]
    [InlineData("Any.DateTime().AfterOrEqualTo(DateTime.Now).AfterOrEqualTo(DateTime.MinValue)")]
    public async Task JD032_offers_nothing_where_it_cannot_tell_which_bound_is_looser(string chain) {
        // The rule still reports the pair; which of the two the run time keeps is only known once both arguments fold.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new BoundDeclaredTwiceAnalyzer(), new BoundDeclaredTwiceCodeFix(), Source(chain));

        Check.That(offered).IsEmpty();
    }

    [Theory]
    [InlineData("Any.String().Alpha().Alpha()",                                 "Any.String().Alpha()")]
    [InlineData("Any.String().WithLength(4).NonEmpty().WithLength(4)",          "Any.String().WithLength(4).NonEmpty()")]
    [InlineData("Any.Int32().Even().MultipleOf(2)",                             "Any.Int32().Even()")]
    [InlineData("Any.String().OneOf(\"a\", \"b\").OneOf(\"b\", \"a\", \"a\")", "Any.String().OneOf(\"a\", \"b\")")]
    [InlineData("Any.String().Alpha().Alpha().Generate()",                      "Any.String().Alpha().Generate()")]
    public async Task JD035_removes_the_call_that_repeats_an_earlier_declaration(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new ConstraintRepeatedAnalyzer(), new ConstraintRepeatedCodeFix(), Source(chain));

        Check.That(rewritten).IsEqualTo(Source(fixed_));
    }

    [Theory]
    [InlineData("Any.OneOf(references)", "Any.ElementOf(references)")]
    [InlineData("Any.OneOf(ids.Where(id => id > 1))", "Any.ElementOf(ids.Where(id => id > 1))")]
    public async Task JD013_draws_an_element_of_the_held_collection(string chain, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new HeldCollectionPassedToOneOfAnalyzer(), new HeldCollectionPassedToOneOfCodeFix(), Statement($"_ = {chain};"));

        Check.That(rewritten).IsEqualTo(Statement($"_ = {fixed_};"));
    }

    [Theory]
    [InlineData("_ = $\"order {Any.String().NonEmpty()}\";",     "_ = $\"order {Any.String().NonEmpty().Generate()}\";")]
    [InlineData("_ = \"order \" + Any.String().NonEmpty();",     "_ = \"order \" + Any.String().NonEmpty().Generate();")]
    [InlineData("_ = Any.String().NonEmpty().ToString();",        "_ = Any.String().NonEmpty().Generate().ToString();")]
    public async Task JD005_generates_the_value_that_reaches_the_text(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new GeneratorRenderedAsTextAnalyzer(), new GenerateTheValueCodeFix(), Statement(statement));

        Check.That(rewritten).IsEqualTo(Statement(fixed_));
    }

    [Theory]
    [InlineData("Assert.NotNull(Any.String().NonEmpty());",          "Assert.NotNull(Any.String().NonEmpty().Generate());")]
    [InlineData("object boxed = Any.Int32().Positive();",            "object boxed = Any.Int32().Positive().Generate();")]
    [InlineData("_ = new object[] { Any.Int32().Positive(), 1 };",   "_ = new object[] { Any.Int32().Positive().Generate(), 1 };")]
    // An operand Generate() could not bind to as written is parenthesized first.
    [InlineData("Assert.NotNull(true ? Any.Int32() : Any.Int32().Positive());",  "Assert.NotNull((true ? Any.Int32() : Any.Int32().Positive()).Generate());")]
    [InlineData("Assert.NotNull((IAny<int>)Any.Int32());",                       "Assert.NotNull(((IAny<int>)Any.Int32()).Generate());")]
    public async Task JD011_generates_the_value_where_an_object_was_expected(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new GeneratorWhereValueExpectedAnalyzer(), new GenerateTheValueCodeFix(), Statement(statement));

        Check.That(rewritten).IsEqualTo(Statement(fixed_));
    }

    [Theory]
    [InlineData("Any.ReproduciblyAsync(async () => { await Task.Yield(); });",     "await Any.ReproduciblyAsync(async () => { await Task.Yield(); });")]
    [InlineData("_ = Any.ReproduciblyAsync(async () => { await Task.Yield(); });", "await Any.ReproduciblyAsync(async () => { await Task.Yield(); });")]
    public async Task JD002_awaits_the_discarded_task(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), new DiscardedReproduciblyAsyncResultCodeFix(), AsyncStatement(statement));

        Check.That(rewritten).IsEqualTo(AsyncStatement(fixed_));
    }

    [Theory]
    [InlineData("Any.Reproducibly(async () => { await Task.Yield(); });",     "await Any.ReproduciblyAsync(async () => { await Task.Yield(); });")]
    [InlineData("Any.Reproducibly(42, async () => { await Task.Yield(); });", "await Any.ReproduciblyAsync(42, async () => { await Task.Yield(); });")]
    public async Task JD001_hands_the_body_to_the_asynchronous_entry_point(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), new AsyncBodyPassedToReproduciblyCodeFix(), AsyncStatement(statement));

        Check.That(rewritten).IsEqualTo(AsyncStatement(fixed_));
    }

    [Fact]
    public async Task JD001_and_JD002_offer_nothing_outside_an_async_function() {
        // Awaiting needs the enclosing function to be async, and making it so changes its signature: a decision the
        // developer takes, not the fix.
        IReadOnlyList<string> discarded = await CodeFixTestHarness.OffersAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), new DiscardedReproduciblyAsyncResultCodeFix(), Statement("Any.ReproduciblyAsync(async () => { await Task.Yield(); });"));
        IReadOnlyList<string> bound     = await CodeFixTestHarness.OffersAsync(new AsyncBodyPassedToReproduciblyAnalyzer(), new AsyncBodyPassedToReproduciblyCodeFix(), Statement("Any.Reproducibly(async () => { await Task.Yield(); });"));

        Check.That(discarded).IsEmpty();
        Check.That(bound).IsEmpty();
    }

    [Theory]
    [InlineData("_ = Any.Combine(Any.Int32(), Any.String(), (number, text) => number);",                     "_ = Any.Combine(Any.Int32(), Any.String(), (number, _) => number);")]
    // The first operand reported is the first one fixed; the other stays reported for its own fix.
    [InlineData("_ = Any.Combine(Any.Int32(), Any.String(), Any.Boolean(), (number, text, flag) => number);", "_ = Any.Combine(Any.Int32(), Any.String(), Any.Boolean(), (number, _, flag) => number);")]
    public async Task JD027_names_the_parameter_the_operand_binds_to_as_a_discard(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new UnusedCombineOperandAnalyzer(), new UnusedCombineOperandCodeFix(), Statement(statement));

        Check.That(rewritten).IsEqualTo(Statement(fixed_));
    }

    [Fact]
    public async Task JD027_makes_a_discard_pair_where_the_other_parameter_is_one_already() {
        // C# 9 reads two parameters named `_` as discards, so the fix is offered beside one.
        string rewritten = await CodeFixTestHarness.ApplyAsync(new UnusedCombineOperandAnalyzer(), new UnusedCombineOperandCodeFix(), Statement("_ = Any.Combine(Any.Int32(), Any.String(), (number, _) => 1);"));

        Check.That(rewritten).IsEqualTo(Statement("_ = Any.Combine(Any.Int32(), Any.String(), (_, _) => 1);"));
    }

    [Theory]
    [InlineData("_ = Any.Combine(Any.Int32(), Any.String(), (number, text) => { int _ = 1; return number + _; });")]
    [InlineData("_ = Any.Combine(Any.Int32(), Any.String(), (number, text) => { Func<int> _ = () => number; return _(); });")]
    // A `_` declared in an enclosing scope, before or after the lambda, or as the parameter of an outer lambda: a
    // parameter cannot take a name an enclosing scope uses (CS0136).
    [InlineData("int _ = 0; object composed = Any.Combine(Any.Int32(), Any.String(), (number, text) => number);")]
    [InlineData("object composed = Any.Combine(Any.Int32(), Any.String(), (number, text) => number); int _ = 0;")]
    [InlineData("Func<int, object> composer = _ => Any.Combine(Any.Int32(), Any.String(), (number, text) => number);")]
    [InlineData("foreach (int _ in ids) { object composed = Any.Combine(Any.Int32(), Any.String(), (number, text) => number); }")]
    public async Task JD027_offers_nothing_where_an_underscore_is_declared_around_the_parameter(string statement) {
        // A local named `_` beside a parameter named `_` is a duplicate identifier: the rule reports, the fix stays away.
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new UnusedCombineOperandAnalyzer(), new UnusedCombineOperandCodeFix(), Statement(statement));

        Check.That(offered).IsEmpty();
    }

    [Fact]
    public async Task JD010_offers_to_remove_the_attribute_or_to_make_the_method_a_test() {
        string source = Helper("[Reproducible]", "using Xunit;");

        string removed = await CodeFixTestHarness.ApplyAsync(new ReproducibleOnNonTestMethodAnalyzer(), new ReproducibleOnNonTestMethodCodeFix(), source, "Remove [Reproducible]");
        string tested  = await CodeFixTestHarness.ApplyAsync(new ReproducibleOnNonTestMethodAnalyzer(), new ReproducibleOnNonTestMethodCodeFix(), source, "Make it a test with [Fact]");

        Check.That(removed).IsEqualTo(Helper("", "using Xunit;"));
        Check.That(tested).IsEqualTo(Helper("[Reproducible]\n    [Fact]", "using Xunit;"));
    }

    [Fact]
    public async Task JD010_qualifies_Fact_where_Xunit_is_not_imported() {
        string tested = await CodeFixTestHarness.ApplyAsync(new ReproducibleOnNonTestMethodAnalyzer(), new ReproducibleOnNonTestMethodCodeFix(), Helper("[Reproducible]", ""), "Make it a test with [Fact]");

        Check.That(tested).IsEqualTo(Helper("[Reproducible]\n    [Xunit.Fact]", ""));
    }

    [Fact]
    public async Task JD010_removes_only_the_attribute_where_it_shares_its_list() {
        string removed = await CodeFixTestHarness.ApplyAsync(new ReproducibleOnNonTestMethodAnalyzer(), new ReproducibleOnNonTestMethodCodeFix(), Helper("[Obsolete, Reproducible]", "using Xunit;"), "Remove [Reproducible]");

        Check.That(removed).IsEqualTo(Helper("[Obsolete]", "using Xunit;"));
    }

    [Theory]
    [InlineData("Any.UseSeed(1234);",      "Hold the handle in a using declaration",            "using IDisposable seedScope = Any.UseSeed(1234);")]
    [InlineData("Any.UseSeed(1234);",      "Draw from an isolated context with Any.WithSeed",   "AnyContext context = Any.WithSeed(1234);")]
    [InlineData("_ = Any.UseSeed(1234);",  "Hold the handle in a using declaration",            "using IDisposable seedScope = Any.UseSeed(1234);")]
    [InlineData("Any.WithSeed(1234);",     "Capture the context to draw from it",               "AnyContext context = Any.WithSeed(1234);")]
    [InlineData("Any.WithSeed(1234);",     "Pin the ambient source with Any.UseSeed",           "using IDisposable seedScope = Any.UseSeed(1234);")]
    // A name already in scope, whether declared before or after the statement: the next free spelling.
    [InlineData("int seedScope = 1;\n        Any.UseSeed(1234);",                          "Hold the handle in a using declaration", "int seedScope = 1;\n        using IDisposable seedScope1 = Any.UseSeed(1234);")]
    [InlineData("Any.WithSeed(1234);\n        AnyContext context = Any.WithSeed(1);",      "Capture the context to draw from it",    "AnyContext context1 = Any.WithSeed(1234);\n        AnyContext context = Any.WithSeed(1);")]
    [InlineData("Any.WithSeed(1234);\n        foreach (int context in ids) { }",           "Capture the context to draw from it",    "AnyContext context1 = Any.WithSeed(1234);\n        foreach (int context in ids) { }")]
    [InlineData("Any.UseSeed(1234);\n        _ = references.Select(seedScope => seedScope);", "Hold the handle in a using declaration", "using IDisposable seedScope1 = Any.UseSeed(1234);\n        _ = references.Select(seedScope => seedScope);")]
    // The trivia inside the call and on either side of the statement stay; only what the discard assignment carried
    // between `_ =` and the call goes with it.
    [InlineData("Any.UseSeed(/* the seed */ 1234); // pinned for the method",       "Hold the handle in a using declaration",          "using IDisposable seedScope = Any.UseSeed(/* the seed */ 1234); // pinned for the method")]
    [InlineData("Any.UseSeed(1234 /* the seed */);",                                "Draw from an isolated context with Any.WithSeed", "AnyContext context = Any.WithSeed(1234 /* the seed */);")]
    [InlineData("_ = /* discarded */ Any.WithSeed(1234); // isolated",              "Capture the context to draw from it",             "AnyContext context = Any.WithSeed(1234); // isolated")]
    // A catch variable and a local function take a name as surely as a local does.
    [InlineData("try { } catch (Exception seedScope) { _ = seedScope; }\n        Any.UseSeed(1234);",  "Hold the handle in a using declaration", "try { } catch (Exception seedScope) { _ = seedScope; }\n        using IDisposable seedScope1 = Any.UseSeed(1234);")]
    [InlineData("Any.WithSeed(1234);\n        void context() { }",                                     "Capture the context to draw from it",    "AnyContext context1 = Any.WithSeed(1234);\n        void context() { }")]
    public async Task JD004_offers_the_two_readings_of_a_discarded_seeding(string statement, string title, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new DiscardedSeedingResultAnalyzer(), new DiscardedSeedingResultCodeFix(), Statement(statement), title);

        Check.That(rewritten).IsEqualTo(Statement(fixed_));
    }

    [Fact]
    public async Task JD004_avoids_a_name_a_parameter_carries() {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new DiscardedSeedingResultAnalyzer(), new DiscardedSeedingResultCodeFix(), Parameters("AnyContext context", "Any.WithSeed(1234);"), "Capture the context to draw from it");

        Check.That(rewritten).IsEqualTo(Parameters("AnyContext context", "AnyContext context1 = Any.WithSeed(1234);"));
    }

    [Fact]
    public async Task JD004_keeps_a_replay_snippet_in_the_pinned_scope_only() {
        // UseSeed with a replay snippet has no WithSeed twin: the one reading left is the scope that was meant.
        string source = Statement("Any.UseSeed(1234, \"[Reproducible(Seed = 1234)]\");", importsSystem: false);

        IReadOnlyList<string> offered   = await CodeFixTestHarness.OffersAsync(new DiscardedSeedingResultAnalyzer(), new DiscardedSeedingResultCodeFix(), source);
        string                rewritten = await CodeFixTestHarness.ApplyAsync(new DiscardedSeedingResultAnalyzer(), new DiscardedSeedingResultCodeFix(), source);

        Check.That(offered).ContainsExactly("Hold the handle in a using declaration");
        Check.That(rewritten).IsEqualTo(Statement("using System.IDisposable seedScope = Any.UseSeed(1234, \"[Reproducible(Seed = 1234)]\");", importsSystem: false));
    }

    [Fact]
    public async Task JD013_renames_the_call_however_its_member_is_spelled() {
        // OneOf under a static using is a bare identifier; the rename keeps that spelling.
        const string before = """
            using System.Collections.Generic;
            using JustDummies;
            using static JustDummies.Any;

            public static class Sample {
                public static object M(List<string> references) {
                    return OneOf(references);
                }
            }
            """;

        string rewritten = await CodeFixTestHarness.ApplyAsync(new HeldCollectionPassedToOneOfAnalyzer(), new HeldCollectionPassedToOneOfCodeFix(), before);

        Check.That(rewritten).IsEqualTo(before.Replace("return OneOf(references);", "return ElementOf(references);"));
    }

    [Theory]
    // The function that has to be async is the nearest one: an async local function or lambda inside a method that is not.
    [InlineData("async Task Run() { Any.ReproduciblyAsync(async () => { await Task.Yield(); }); }",           "async Task Run() { await Any.ReproduciblyAsync(async () => { await Task.Yield(); }); }")]
    [InlineData("Func<Task> run = async () => { Any.ReproduciblyAsync(async () => { await Task.Yield(); }); };", "Func<Task> run = async () => { await Any.ReproduciblyAsync(async () => { await Task.Yield(); }); };")]
    public async Task JD002_awaits_inside_the_nearest_async_function(string statement, string fixed_) {
        string rewritten = await CodeFixTestHarness.ApplyAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), new DiscardedReproduciblyAsyncResultCodeFix(), Statement(statement));

        Check.That(rewritten).IsEqualTo(Statement(fixed_));
    }

    [Fact]
    public async Task JD002_offers_nothing_in_a_local_function_that_is_not_async_inside_one_that_is() {
        IReadOnlyList<string> offered = await CodeFixTestHarness.OffersAsync(new DiscardedReproduciblyAsyncResultAnalyzer(), new DiscardedReproduciblyAsyncResultCodeFix(),
                                                                             AsyncStatement("void Run() { Any.ReproduciblyAsync(async () => { await Task.Yield(); }); }"));

        Check.That(offered).IsEmpty();
    }

    [Fact]
    public async Task A_choice_is_offered_as_two_actions_with_their_own_titles() {
        IReadOnlyList<string> reproducible = await CodeFixTestHarness.OffersAsync(new ReproducibleOnNonTestMethodAnalyzer(), new ReproducibleOnNonTestMethodCodeFix(), Helper("[Reproducible]", "using Xunit;"));
        IReadOnlyList<string> seeding      = await CodeFixTestHarness.OffersAsync(new DiscardedSeedingResultAnalyzer(), new DiscardedSeedingResultCodeFix(), Statement("Any.UseSeed(1234);"));
        IReadOnlyList<string> looser       = await CodeFixTestHarness.OffersAsync(new BoundDeclaredTwiceAnalyzer(), new BoundDeclaredTwiceCodeFix(), Source("Any.String().WithMinLength(8).WithMinLength(10)"));
        IReadOnlyList<string> repeated     = await CodeFixTestHarness.OffersAsync(new ConstraintRepeatedAnalyzer(), new ConstraintRepeatedCodeFix(), Source("Any.String().Alpha().Alpha()"));
        IReadOnlyList<string> operand      = await CodeFixTestHarness.OffersAsync(new UnusedCombineOperandAnalyzer(), new UnusedCombineOperandCodeFix(), Statement("_ = Any.Combine(Any.Int32(), Any.String(), (number, text) => number);"));

        Check.That(reproducible).ContainsExactly("Remove [Reproducible]", "Make it a test with [Fact]");
        Check.That(seeding).ContainsExactly("Hold the handle in a using declaration", "Draw from an isolated context with Any.WithSeed");
        Check.That(looser).ContainsExactly("Remove WithMinLength(8), the looser declaration");
        Check.That(repeated).ContainsExactly("Remove Alpha(), which repeats an earlier declaration");
        Check.That(operand).ContainsExactly("Name the parameter 'text' '_' to say the draw is deliberate");
    }

    /// <summary>A statement inside an ordinary method, with the usings and the helpers the lifecycle rules' shapes need.</summary>
    private static string Statement(string statement, bool importsSystem = true) {
        return Method("void", statement, importsSystem);
    }

    /// <summary>The same statement inside an <c>async</c> method, for the fixes that await.</summary>
    private static string AsyncStatement(string statement) {
        return Method("async Task", statement, importsSystem: true);
    }

    /// <summary>The statement inside a method taking <paramref name="parameters" /> of the test's choosing.</summary>
    private static string Parameters(string parameters, string statement) {
        return Method("void", statement, importsSystem: true, parameters);
    }

    private static string Method(string signature, string statement, bool importsSystem, string parameters = "List<string> references, List<int> ids") {
        string system = importsSystem ? "using System;" : "";

        return $$"""
            {{system}}
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            using JustDummies;

            public static class Assert {
                public static void NotNull(object value) { }
            }

            public static class Sample {
                public static {{signature}} M({{parameters}}) {
                    {{statement}}
                }
            }
            """;
    }

    /// <summary>A helper method of a test class, carrying <paramref name="attributes" /> on its own line.</summary>
    private static string Helper(string attributes, string xunit) {
        string lines = attributes.Length == 0 ? "" : attributes + "\n    ";

        return $$"""
            using System;
            using JustDummies;
            using JustDummies.Xunit;
            {{xunit}}

            public class Sample {
                {{lines}}private string Arrange() => Any.String().NonEmpty().Generate();
            }
            """;
    }

    private static string Source(string expression) {
        return $$"""
            using System;
            using JustDummies;

            public static class Bounds {
                public const int Floor = 1;
                public const int Ceiling = 50;
                public static int Low => 1;
                public static int Minimum() => 1;
                public static int Maximum() => 50;
            }

            public static class Sample {
                public static object M(int bound) {
                    return {{expression}};
                }
            }
            """;
    }

}
