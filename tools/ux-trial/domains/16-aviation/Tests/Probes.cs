using System.Text.RegularExpressions;
using Aviation.Domain;
using JustDummies;
using Xunit;

namespace Aviation.Tests;

/// <summary>Deliberate edge cases. Each records what the library did; the messages are written to the test output.</summary>
public sealed class Probes {

    private static void Log(string text) => TestContext.Current.TestOutputHelper?.WriteLine(text);

    private static TException Refused<TException>(Func<object?> declareOrDraw) where TException : Exception {
        TException exception = Assert.Throws<TException>(declareOrDraw);
        Log($"{typeof(TException).Name}: {exception.Message}");
        return exception;
    }

    // ---- Contradictions -------------------------------------------------------------------------

    [Fact]
    public void Probe_TimeSpan_positive_and_negative_conflict_naming_both() {
        ConflictingAnyConstraintException exception = Refused<ConflictingAnyConstraintException>(() => Any.TimeSpan().Positive().Negative().Generate());

        Assert.Contains("Positive", exception.Message);
        Assert.Contains("Negative", exception.Message);
    }

    [Fact]
    public void Probe_TimeSpan_crossed_bounds_conflict_naming_both() {
        ConflictingAnyConstraintException exception = Refused<ConflictingAnyConstraintException>(
            () => Any.TimeSpan().GreaterThan(TimeSpan.FromHours(1)).LessThan(TimeSpan.FromMinutes(5)).Generate());

        Assert.Contains("GreaterThan", exception.Message);
        Assert.Contains("LessThan", exception.Message);
    }

    [Fact]
    public void Probe_a_slot_window_with_no_five_minute_point_conflicts() {
        DateTime ten = new DateTime(2026, 5, 4, 10, 0, 0, DateTimeKind.Utc);

        ConflictingAnyConstraintException exception = Refused<ConflictingAnyConstraintException>(
            () => Any.DateTime().Between(ten.AddMinutes(1), ten.AddMinutes(4)).WithGranularity(TimeSpan.FromMinutes(5)).Generate());

        Assert.Contains("WithGranularity", exception.Message);
    }

    [Fact]
    public void Probe_a_refinement_that_contradicts_the_scaffolded_base_is_refused() {
        ConflictingAnyConstraintException exception = Refused<ConflictingAnyConstraintException>(
            () => Any.Flight().WithBlockTime(blockTime => blockTime.Negative()).Generate());

        Assert.Contains("Positive", exception.Message);
        Assert.Contains("Negative", exception.Message);
    }

    [Fact]
    public void Probe_more_distinct_statuses_than_the_enum_declares_conflicts() {
        Refused<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<FlightStatus>()).WithCount(6).Generate());
    }

    // ---- Bounds ---------------------------------------------------------------------------------

    [Fact]
    public void Probe_min_equals_max_yields_that_instant() {
        DateTime slot = new DateTime(2026, 5, 4, 10, 0, 0, DateTimeKind.Utc);

        DateTime drawn = Any.DateTime().Between(slot, slot).Generate();

        Assert.Equal(slot, drawn);
        Log($"Kind of a draw between two UTC bounds: {drawn.Kind}");
    }

    [Fact]
    public void Probe_the_kind_of_an_unconstrained_DateTime() {
        List<DateTime> drawn = Any.ListOf(Any.DateTime()).WithCount(200).Generate();

        Log($"Kinds: {string.Join(", ", drawn.Select(value => value.Kind).Distinct())}; min {drawn.Min():O}; max {drawn.Max():O}");
    }

    [Fact]
    public void Probe_dates_at_the_edges_of_the_type() {
        DateTime nearTheEnd   = Any.DateTime().After(DateTime.MaxValue.AddTicks(-1)).Generate();
        DateTime nearTheStart = Any.DateTime().Before(DateTime.MinValue.AddTicks(1)).Generate();

        Assert.Equal(DateTime.MaxValue, nearTheEnd);
        Assert.Equal(DateTime.MinValue, nearTheStart);
        Refused<ConflictingAnyConstraintException>(() => Any.DateTime().After(DateTime.MaxValue).Generate());
    }

    [Fact]
    public void Probe_TimeSpan_over_the_whole_type_and_a_zero_granularity() {
        TimeSpan anyHuge = Any.TimeSpan().Between(TimeSpan.MinValue, TimeSpan.MaxValue).Generate();
        Log($"Whole-type TimeSpan draw: {anyHuge}");

        Refused<ArgumentOutOfRangeException>(() => Any.TimeSpan().WithGranularity(TimeSpan.Zero));
    }

    [Fact]
    public void Probe_a_TimeOnly_curfew_across_midnight() {
        // A night curfew runs 23:00 to 06:00, written Between(23:00, 06:00): the window crosses midnight (issue #357).
        TimeOnly drawn = Any.TimeOnly().Between(new TimeOnly(23, 0), new TimeOnly(6, 0)).Generate();
        Log($"curfew: {drawn}");

        Assert.True(drawn >= new TimeOnly(23, 0) || drawn <= new TimeOnly(6, 0));
    }

    [Fact]
    public void Probe_DateOnly_min_equals_max_and_TimeOnly_granularity() {
        DateOnly day      = Any.DateOnly().Between(new DateOnly(2026, 7, 14), new DateOnly(2026, 7, 14)).Generate();
        TimeOnly anySlotT = Any.TimeOnly().Between(new TimeOnly(6, 0), new TimeOnly(22, 55)).WithGranularity(TimeSpan.FromMinutes(5)).Generate();

        Assert.Equal(new DateOnly(2026, 7, 14), day);
        Assert.Equal(0, anySlotT.Minute % 5);
        Assert.Equal(0, anySlotT.Second);
    }

    // ---- Patterns -------------------------------------------------------------------------------

    [Fact]
    public void Probe_flight_number_and_ICAO_patterns_always_match() {
        AnyPattern anyFlightNumber = Any.StringMatching(@"^[A-Z0-9]{2}\d{1,4}$");
        AnyPattern anyIcao         = Any.StringMatching(new Regex("^[A-Z]{4}$"));

        for (int draw = 0; draw < 500; draw++) {
            Assert.Matches(@"^[A-Z0-9]{2}[0-9]{1,4}$", anyFlightNumber.Generate());
            Assert.Matches("^[A-Z]{4}$", anyIcao.Generate());
        }
    }

    [Fact]
    public void Probe_dotnet_character_class_subtraction() {
        // .NET's [A-Z-[N]] means "A to Z except N": a hyphenated registration whose prefix is not N.
        const string pattern = @"^[A-Z-[N]]-[A-Z]{3}$";
        Exception? refusal = Record.Exception(() => Any.StringMatching(pattern).Generate());
        if (refusal is not null) {
            Log($"{refusal.GetType().Name}: {refusal.Message}");
            Assert.IsType<UnsupportedRegexException>(refusal);
            return;
        }

        AnyPattern anyMark = Any.StringMatching(pattern);
        List<string> drawn = Enumerable.Range(0, 300).Select(_ => anyMark.Generate()).ToList();
        Log($"Samples: {string.Join(" ", drawn.Take(8))}");
        Assert.All(drawn, mark => Assert.Matches(pattern, mark));
    }

    [Fact]
    public void Probe_negative_lookahead_for_the_N_prefix_is_refused_by_name() {
        UnsupportedRegexException exception = Refused<UnsupportedRegexException>(() => Any.StringMatching(@"^(?!N)[A-Z]{1,2}-[A-Z]{3,4}$"));

        Assert.Contains("(?!", exception.Message);
    }

    [Fact]
    public void Probe_unicode_category_and_word_boundary() {
        Exception? category = Record.Exception(() => Any.StringMatching(@"^\p{Lu}{3}$").Generate());
        Exception? boundary = Record.Exception(() => Any.StringMatching(@"^\b[A-Z]{3}\b$").Generate());

        Log($"\\p{{Lu}}: {category?.GetType().Name ?? "accepted"}: {category?.Message}");
        Log($"\\b: {boundary?.GetType().Name ?? "accepted"}: {boundary?.Message}");
    }

    [Fact]
    public void Probe_a_pool_value_outside_the_pattern_is_dropped_silently() {
        IPoolInspection<string> pool = (IPoolInspection<string>)Any.StringMatching(@"^N[1-9][0-9]{0,3}[A-Z]{0,2}$").OneOf("N1", "N12345", "N123AB");

        IReadOnlyList<PoolRejection<string>> rejected = pool.GetRejections();
        Log($"Rejected: {string.Join("; ", rejected.Select(rejection => $"{rejection.Value} by {string.Join(", ", rejection.RejectedBy)}"))}");

        Assert.Equal("N12345", Assert.Single(rejected).Value);
    }

    // ---- Seeds ----------------------------------------------------------------------------------

    [Fact]
    public void Probe_the_same_seed_replays_a_whole_flight() {
        string first  = DescribeAFlightUnder(20261027);
        string second = DescribeAFlightUnder(20261027);
        string other  = DescribeAFlightUnder(20261028);

        Log(first);
        Assert.Equal(first, second);
        Assert.NotEqual(first, other);
    }

    [Fact]
    public void Probe_Reproducibly_reports_the_seed_and_replaying_it_reproduces_the_failure() {
        List<string> reports   = new List<string>();
        string?      firstMark = null;

        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            firstMark = Any.AircraftRegistration().Generate().Mark;
            throw new InvalidOperationException("the test failed");
        }, reports.Add));

        string report = Assert.Single(reports);
        Log(report);
        int seed = int.Parse(Regex.Match(report, @"seeded with (-?\d+)").Groups[1].Value);

        string? replayedMark = null;
        Any.Reproducibly(seed, () => replayedMark = Any.AircraftRegistration().Generate().Mark);

        Assert.Equal(firstMark, replayedMark);
    }

    [Fact]
    public void Probe_an_isolated_context_replays_independently_of_the_ambient_seed() {
        AnyContext first  = Any.WithSeed(7);
        AnyContext second = Any.WithSeed(7);

        List<string> fromFirst  = Any.ListOf(first.String().Alpha().InUpperCase().WithLength(3)).WithCount(5).Generate();
        List<string> fromSecond = Any.ListOf(second.String().Alpha().InUpperCase().WithLength(3)).WithCount(5).Generate();

        Assert.Equal(fromFirst, fromSecond);
    }

    private static string DescribeAFlightUnder(int seed) {
        using (Any.UseSeed(seed)) {
            Flight flight = Any.Flight().Generate();
            return $"{flight.Number} {flight.Route.Origin}-{flight.Route.Destination} {flight.Route.DistanceKm}km {flight.Aircraft.Registration} {flight.Aircraft.TypeDesignator}/{flight.Aircraft.Seats} {flight.ScheduledDeparture:O} {flight.BlockTime}";
        }
    }

    // ---- Uniqueness -----------------------------------------------------------------------------

    [Fact]
    public void Probe_distinct_airports_of_a_type_without_value_equality() {
        // IcaoAirportCode does not override Equals: distinctness is by reference (JD028 expected at build).
        List<IcaoAirportCode> airports = Any.ListOf(Any.IcaoAirportCode()).WithCount(3).Distinct().Generate();

        Assert.Equal(3, airports.Count);
    }

    [Fact]
    public void Probe_distinct_IATA_codes_with_value_equality() {
        HashSet<IataAirportCode> airports = Any.SetOf(Any.IataAirportCode()).WithCount(50).Generate();

        Assert.Equal(50, airports.Select(code => code.Value).Distinct().Count());
    }

}
