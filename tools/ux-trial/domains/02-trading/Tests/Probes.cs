using JustDummies;

using Trading.Domain;

using Xunit;

namespace Trading.Tests;

/// <summary>Deliberate probes of the library's edges. Each records what was observed.</summary>
public sealed class Probes {

    private readonly ITestOutputHelper _output;

    public Probes(ITestOutputHelper output) {
        _output = output;
    }

    private string Report(Exception exception) {
        _output.WriteLine($"{exception.GetType().Name}: {exception.Message}");

        return exception.Message;
    }

    // ---- contradictions -------------------------------------------------------------------

    [Fact]
    public void Crossed_decimal_bounds_are_refused_naming_both_sides() {
        Exception refusal = Assert.ThrowsAny<DummyException>(() => Any.Decimal().GreaterThan(10m).LessThan(5m).Generate());
        string    message = Report(refusal);

        Assert.Contains("GreaterThan(10)", message);
        Assert.Contains("LessThan(5)", message);
    }

    [Fact]
    public void An_integer_lattice_with_no_point_in_range_is_refused() {
        Exception refusal = Assert.ThrowsAny<DummyException>(() => Any.Int32().Between(1, 10).MultipleOf(50).Generate());
        string    message = Report(refusal);

        Assert.Contains("MultipleOf(50)", message);
    }

    [Fact]
    public void A_decimal_scale_with_no_point_in_range_is_refused() {
        // Two decimal places admit 0.00 and 0.01, neither inside [0.001, 0.009].
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate());
        Report(refusal);

        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void A_refinement_contradicting_the_scaffolded_base_names_both_sides() {
        Exception refusal = Assert.ThrowsAny<DummyException>(() => new AnyQuantity().WithShares(shares => shares.Negative()).Generate());
        string    message = Report(refusal);

        Assert.Contains("Negative()", message);
        Assert.Contains("Between(1, 1000000)", message);
    }

    [Fact]
    public void Narrowing_a_scale_cannot_be_expressed_on_a_scaffolded_base() {
        // AnyPrice's base is WithScale(4) because Price admits *at most* four places;
        // a test wanting a cent price cannot narrow it.
        Exception refusal = Assert.ThrowsAny<DummyException>(() => new AnyPrice().WithAmount(amount => amount.WithScale(2)).Generate());
        Report(refusal);
    }

    // ---- bounds ---------------------------------------------------------------------------

    [Fact]
    public void A_decimal_range_of_one_value_draws_that_value() {
        decimal drawn = Any.Decimal().Between(12.34m, 12.34m).Generate();

        Assert.Equal(12.34m, drawn);
    }

    [Fact]
    public void The_whole_decimal_range_with_a_scale_still_draws() {
        decimal drawn = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).WithScale(2).Generate();
        _output.WriteLine(drawn.ToString(System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(drawn, decimal.Round(drawn, 2));
    }

    [Fact]
    public void A_drawn_decimal_carries_the_declared_scale() {
        decimal[] drawn = Enumerable.Range(0, 200).Select(_ => Any.Decimal().Between(1m, 2m).WithScale(4).Generate()).ToArray();
        _output.WriteLine(string.Join(", ", drawn.Take(10)));

        Assert.All(drawn, value => Assert.True(value.Scale <= 4, $"{value} has scale {value.Scale}"));
    }

    [Fact]
    public void A_scale_beyond_what_a_decimal_holds_is_refused() {
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.Decimal().WithScale(29).Generate());
        Report(refusal);
    }

    [Fact]
    public void A_pooled_value_with_a_smaller_scale_survives_WithScale() {
        decimal[] drawn = Enumerable.Range(0, 100).Select(_ => Any.Decimal().WithScale(4).OneOf(0.01m, 0.05m).Generate()).ToArray();
        _output.WriteLine(string.Join(", ", drawn.Distinct()));

        Assert.All(drawn, value => Assert.Contains(value, new[] { 0.01m, 0.05m }));
    }

    [Fact]
    public void A_tick_grid_is_drawn_through_an_integer_count_of_ticks() {
        // No MultipleOf on decimal: a price on a 0.05 grid is a number of ticks times the tick.
        decimal[] drawn = Enumerable.Range(0, 200).Select(_ => Any.Int32().Between(1, 20_000).As(ticks => ticks * 0.05m).Generate()).ToArray();

        Assert.All(drawn, price => Assert.Equal(0m, price % 0.05m));
    }

    [Fact]
    public void A_date_after_the_last_instant_is_refused() {
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.DateTime().After(DateTime.MaxValue).Generate());
        Report(refusal);

        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void A_date_near_the_last_instant_draws_it() {
        DateTime drawn = Any.DateTime().After(DateTime.MaxValue.AddTicks(-1)).Generate();

        Assert.Equal(DateTime.MaxValue, drawn);
    }

    [Fact]
    public void A_dateonly_range_at_the_first_day_draws_it() {
        DateOnly drawn = Any.DateOnly().BeforeOrEqualTo(DateOnly.MinValue).Generate();

        Assert.Equal(DateOnly.MinValue, drawn);
    }

    [Fact]
    public void A_timeonly_range_that_wraps_midnight_is_refused() {
        // A night session runs 22:00 to 02:00; TimeOnly ordering cannot say so.
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0)).Generate());
        Report(refusal);
    }

    [Fact]
    public void The_kind_of_a_drawn_datetime_follows_utc_bounds() {
        DateTime start = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime end   = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTimeKind[] kinds = Enumerable.Range(0, 50).Select(_ => Any.DateTime().Between(start, end).Generate().Kind).Distinct().ToArray();
        _output.WriteLine(string.Join(", ", kinds));

        Assert.Equal([DateTimeKind.Utc], kinds);
    }

    [Fact]
    public void The_kind_of_an_unconstrained_datetime() {
        DateTimeKind[] kinds = Enumerable.Range(0, 50).Select(_ => Any.DateTime().Generate().Kind).Distinct().ToArray();
        _output.WriteLine(string.Join(", ", kinds));

        Assert.NotEmpty(kinds);
    }

    [Fact]
    public void A_count_above_a_million_is_refused() {
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.ListOf(new AnyQuantity()).WithCount(1_000_001).Generate());
        Report(refusal);
    }

    // ---- patterns -------------------------------------------------------------------------

    [Theory]
    [InlineData(@"^[A-Z]{1,5}(\.[A-Z])?$")]
    [InlineData(@"^[A-Z]{2}[A-Z0-9]{9}\d$")]                // ISIN
    [InlineData(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$")] // FIX-style UTC timestamp
    public void Trading_formats_are_generated_and_match(string pattern) {
        string[] drawn = Enumerable.Range(0, 100).Select(_ => Any.StringMatching(pattern).Generate()).ToArray();
        _output.WriteLine(string.Join(", ", drawn.Take(5)));

        Assert.All(drawn, value => Assert.Matches(pattern, value));
    }

    [Theory]
    [InlineData(@"^[A-Z]{1,5}(?=\.)")]   // lookahead
    [InlineData(@"^\p{Lu}{1,5}$")]       // Unicode category
    [InlineData(@"^\b[A-Z]{3}\b$")]      // word boundary
    [InlineData(@"^(?i:[a-z]){3}$")]     // scoped option group
    public void Constructs_outside_the_regular_subset_are_refused_clearly(string pattern) {
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.StringMatching(pattern).Generate());
        Report(refusal);

        Assert.IsType<UnsupportedRegexException>(refusal);
    }

    // ---- reproducibility ------------------------------------------------------------------

    [Fact]
    public void A_seeded_context_replays_its_draws() {
        decimal first  = Any.WithSeed(20261027).Decimal().Between(0m, 1_000m).WithScale(2).Generate();
        decimal second = Any.WithSeed(20261027).Decimal().Between(0m, 1_000m).WithScale(2).Generate();

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_seeded_scope_replays_scaffolded_generators() {
        Order first;
        Order second;
        using (Any.UseSeed(77)) {
            first = Arbitrary.Order().Generate();
        }
        using (Any.UseSeed(77)) {
            second = Arbitrary.Order().Generate();
        }

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Ticker, second.Ticker);
        Assert.Equal(first.Limit, second.Limit);
        Assert.Equal(first.PlacedAt, second.PlacedAt);
    }

    [Fact]
    public void A_failing_body_reports_its_seed_and_the_seed_replays_the_run() {
        string? reported = null;
        Guid    failedOn = Guid.Empty;

        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            failedOn = Arbitrary.OrderBatch().Generate().Orders[0].Id;
            throw new InvalidOperationException("the test failed");
        }, message => reported = message));
        _output.WriteLine(reported ?? "<nothing reported>");

        Assert.NotNull(reported);
        int seed = int.Parse(System.Text.RegularExpressions.Regex.Match(reported!, @"seeded with (-?\d+)").Groups[1].Value);

        Guid replayed = Guid.Empty;
        Any.Reproducibly(seed, () => replayed = Arbitrary.OrderBatch().Generate().Orders[0].Id);

        Assert.Equal(failedOn, replayed);
    }

    // ---- uniqueness -----------------------------------------------------------------------

    [Fact]
    public void More_distinct_sides_than_exist_are_refused_by_name() {
        Exception refusal = Assert.ThrowsAny<DummyException>(() => Any.ListOf(Any.Enum<Side>()).WithCount(3).Distinct().Generate());
        Report(refusal);
    }

    [Fact]
    public void A_set_of_tickers_is_distinct_by_value() {
        HashSet<Ticker> drawn = Any.SetOf(new AnyTicker()).WithCount(20).Generate();

        Assert.Equal(20, drawn.Select(ticker => ticker.Symbol).Distinct().Count());
    }

    [Fact]
    public void Distinct_over_orders_without_value_equality_is_inert() {
        // Order has no Equals: JD028 is expected at build time.
        List<Order> drawn = Any.ListOf(Arbitrary.Order()).WithCount(3).Distinct().Generate();

        Assert.Equal(3, drawn.Count);
    }

    [Fact]
    public void Distinct_tickers_from_a_tiny_pattern_language() {
        // "[AB]" admits two symbols; asking for three distinct is impossible.
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.ListOf(Any.StringMatching("^[AB]$").As(symbol => new Ticker(symbol))).WithCount(3).Distinct().Generate());
        Report(refusal);
    }

    // ---- analyzers ------------------------------------------------------------------------

    [Fact]
    public void A_constant_contradiction_on_an_integer_chain() {
        // JD023 is expected here.
        Assert.ThrowsAny<DummyException>(() => Any.Int32().GreaterThan(100).LessThan(10).Generate());
    }

    [Fact]
    public void A_constant_contradiction_on_a_decimal_chain() {
        // Does any rule see this one?
        Assert.ThrowsAny<DummyException>(() => Any.Decimal().Between(10m, 20m).GreaterThan(30m).Generate());
    }

    [Fact]
    public void A_drawn_value_flowing_into_an_assertion() {
        // The library's first fundamental says this is not a dummy. Does anything flag it?
        int anyShares = Any.Int32().Between(1, 1_000).Generate();

        Quantity quantity = new(anyShares);

        Assert.Equal(anyShares, quantity.Shares);
    }

}
