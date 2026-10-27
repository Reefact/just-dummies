using System.Text.RegularExpressions;
using JustDummies;
using JustDummies.Xunit;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

/// <summary>Deliberate edge cases: what the library refuses, how it says so, and what replays.</summary>
public sealed class Probes {

    private static void Log(string text) {
        TestContext.Current.TestOutputHelper?.WriteLine(text);
    }

    // ----- Contradictions -------------------------------------------------------------------------------------

    [Fact]
    public void Probe_contradictory_level_bounds_fail_fast_naming_both_sides() {
        int maximumLevel = 20;
        int floorAsked   = 30;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Int32().Between(1, maximumLevel).GreaterThan(floorAsked));
        Log("bounds: " + conflict.Message);

        Assert.Contains("Between", conflict.Message);
        Assert.Contains("GreaterThan", conflict.Message);
    }

    [Fact]
    public void Probe_a_decimal_range_with_no_value_at_the_declared_scale_is_refused() {
        decimal low  = 0.001m;
        decimal high = 0.009m;

        Exception? refusal = Record.Exception(() => Any.Decimal().Between(low, high).WithScale(2).Generate());
        Log("scale: " + refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Probe_a_set_larger_than_the_enum_is_refused_by_cardinality() {
        int count = 4;

        Exception? refusal = Record.Exception(() => Any.SetOf(Any.Enum<PalletType>()).WithCount(count).Generate());
        Log("enum set: " + refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Probe_a_set_of_bins_larger_than_their_opaque_domain_ends_in_a_bounded_refusal() {
        // 20 levels in one rack: 21 distinct bins do not exist, but the composed generator cannot say so.
        AnyBinLocation anyBinInOneRack = new AnyBinLocation().WithAisle('A').WithRack(1);

        Exception? refusal = Record.Exception(() => Any.SetOf(anyBinInOneRack).WithCount(21).Generate());
        Log("opaque set: " + refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<AnyGenerationException>(refusal);
    }

    [Fact]
    public void Probe_the_opaque_set_failure_replays_from_its_seed_although_its_message_says_it_may_not() {
        // The refusal above says "some values come from a generator that does not draw from this source, so they
        // are not reproducible from this seed alone". AnyBinLocation draws every value from the ambient source.
        AnyBinLocation anyBinInOneRack = new AnyBinLocation().WithAisle('A').WithRack(1);
        string first  = "";
        string second = "";

        Any.Reproducibly(424242, () => first  = string.Join(",", Any.ListOf(anyBinInOneRack).WithCount(30).Generate()));
        Any.Reproducibly(424242, () => second = string.Join(",", Any.ListOf(anyBinInOneRack).WithCount(30).Generate()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Probe_twenty_distinct_bins_in_one_rack_are_drawn_when_exactly_twenty_exist() {
        AnyBinLocation anyBinInOneRack = new AnyBinLocation().WithAisle('A').WithRack(1);

        Exception? refusal = Record.Exception(() => Any.SetOf(anyBinInOneRack).WithCount(20).Generate());
        Log("set of exactly 20: " + refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.Null(refusal);
    }

    [Fact]
    public void Probe_distinct_SSCCs_in_a_list() {
        List<Sscc> anySsccs = Any.ListOf(new AnySscc()).WithCount(200).Distinct().Generate();

        Assert.Equal(200, anySsccs.Select(sscc => sscc.Value).Distinct().Count());
    }

    // ----- Bounds ---------------------------------------------------------------------------------------------

    [Fact]
    public void Probe_min_equal_max() {
        Assert.Equal(5, Any.Int32().Between(5, 5).Generate());
        Assert.Equal(1.5m, Any.Decimal().Between(1.5m, 1.5m).WithScale(1).Generate());
        Assert.Equal(Quantity.Maximum, new AnyQuantity().WithUnits(u => u.GreaterThanOrEqualTo(Quantity.Maximum)).Generate().Units);
    }

    [Fact]
    public void Probe_huge_decimal_range_with_a_scale() {
        Exception? refusal = Record.Exception(() => {
            for (int draw = 0; draw < 200; draw++) {
                decimal value = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).WithScale(2).Generate();
                Assert.Equal(value, Math.Round(value, 2));
            }
        });
        Log("huge decimal: " + refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.Null(refusal);
    }

    [Fact]
    public void Probe_huge_integer_range() {
        long value = Any.Int64().Between(long.MinValue, long.MaxValue).Generate();
        Assert.InRange(value, long.MinValue, long.MaxValue);
    }

    [Fact]
    public void Probe_dates_at_the_edges_of_the_calendar() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().After(DateOnly.MaxValue.AddDays(-1)).Generate());
        Assert.Equal(DateOnly.MinValue, Any.DateOnly().Before(DateOnly.MinValue.AddDays(1)).Generate());
        Assert.Equal(DateTime.MinValue, Any.DateTime().Before(DateTime.MinValue.AddTicks(1)).Generate());
        Assert.Equal(TimeOnly.MaxValue, Any.TimeOnly().After(TimeOnly.MaxValue.Add(TimeSpan.FromTicks(-1))).Generate());

        Exception? afterMax = Record.Exception(() => Any.DateOnly().After(DateOnly.MaxValue).Generate());
        Log("after DateOnly.MaxValue: " + afterMax?.GetType().Name + ": " + afterMax?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(afterMax);
    }

    [Fact]
    public void Probe_a_night_shift_window_crossing_midnight() {
        TimeOnly shiftStart = new(22, 0);
        TimeOnly shiftEnd   = new(6, 0);

        Exception? refusal = Record.Exception(() => Any.TimeOnly().Between(shiftStart, shiftEnd).Generate());
        Log("night shift: " + refusal?.GetType().Name + ": " + refusal?.Message);

        // A clear refusal: a window wrapping midnight is two windows, and saying so is the caller's job.
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Probe_the_kind_of_a_drawn_DateTime() {
        DateTime unconstrained = Any.DateTime().Generate();
        DateTime utcBounded    = Any.DateTime().Between(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Generate();
        DateTime mixedBounds   = Any.DateTime().Between(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local), new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Generate();
        DateTime afterUtc      = Any.DateTime().After(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Generate();
        Log($"kinds: unconstrained={unconstrained.Kind} utcBounded={utcBounded.Kind} mixed={mixedBounds.Kind} afterUtc={afterUtc.Kind}");

        // Observed, and documented nowhere: every drawn DateTime is UTC, whatever the kind of its bounds.
        Assert.Equal([DateTimeKind.Utc, DateTimeKind.Utc, DateTimeKind.Utc, DateTimeKind.Utc],
                     [unconstrained.Kind, utcBounded.Kind, mixedBounds.Kind, afterUtc.Kind]);
    }

    // ----- Patterns -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(@"^\d{18}$")]
    [InlineData(@"^[A-Z]-(0[1-9]|[1-9]\d)-(0[1-9]|1\d|20)$")]
    [InlineData(@"^(?<aisle>[A-Z])-(?<rack>\d{2})-(?<level>\d{2})$")]
    [InlineData(@"\ASUP-\d{5}\z")]
    [InlineData(@"[A-Z-[IO]]-\d{2}-\d{2}")]
    [InlineData(@"\p{Lu}-\d{2}")]
    [InlineData(@"\bLOT\d{4}\b")]
    [InlineData(@"(?=\d{18})\d+")]
    [InlineData(@"[[:digit:]]{3}")]
    [InlineData(@"\d{,3}")]
    [InlineData(@"(?x) SUP - \d{5}")]
    public void Probe_domain_patterns(string pattern) {
        try {
            for (int draw = 0; draw < 300; draw++) {
                string value = Any.StringMatching(pattern).Generate();
                if (!Regex.IsMatch(value, pattern)) {
                    Log($"pattern {pattern}: drew '{value}' which .NET says does not match");
                    Assert.Fail($"'{value}' does not match {pattern}");
                }
            }
            Log($"pattern {pattern}: OK, e.g. '{Any.StringMatching(pattern).Generate()}'");
        } catch (DummyException exception) {
            Log($"pattern {pattern}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    [Fact]
    public void Probe_a_Regex_with_IgnorePatternWhitespace() {
        Regex supplier = new(@"SUP - \d{5}  # supplier code", RegexOptions.IgnorePatternWhitespace);
        Exception? outcome = Record.Exception(() => {
            string value = Any.StringMatching(supplier).Generate();
            Log($"ignore-whitespace: drew '{value}', matches={supplier.IsMatch(value)}");
        });
        Log("ignore-whitespace: " + outcome?.GetType().Name + ": " + outcome?.Message);

        Assert.IsType<ArgumentException>(outcome);
    }

    [Fact]
    public void Probe_backslash_d_draws_ASCII_digits_only() {
        string value = Any.StringMatching(@"\d{200}").Generate();
        Assert.All(value, digit => Assert.InRange(digit, '0', '9'));
    }

    // ----- Reproducibility ------------------------------------------------------------------------------------

    [Fact]
    public void Probe_a_seed_replays_a_whole_pallet() {
        string first  = "";
        string second = "";
        Any.Reproducibly(20261027, () => first  = Describe(new AnyPalletReceipt().Generate()));
        Any.Reproducibly(20261027, () => second = Describe(new AnyPalletReceipt().Generate()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Probe_a_failing_body_reports_its_seed_and_the_seed_replays_it() {
        string? report = null;
        string  drawnInFailingRun = "";
        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
                                                                drawnInFailingRun = new AnySscc().Generate().Value;
                                                                throw new InvalidOperationException("boom");
                                                            },
                                                            message => report = message));
        Log("report: " + report);
        Assert.NotNull(report);

        int seed = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);
        string replayed = "";
        Any.Reproducibly(seed, () => replayed = new AnySscc().Generate().Value);

        Assert.Equal(drawnInFailingRun, replayed);
    }

    [Fact]
    public void Probe_an_isolated_context_is_deterministic() {
        int[] first  = Any.ListOf(Any.WithSeed(7).Int32().Between(1, 10_000)).WithCount(20).Generate().ToArray();
        int[] second = Any.ListOf(Any.WithSeed(7).Int32().Between(1, 10_000)).WithCount(20).Generate().ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void Probe_a_scaffolded_generator_ignores_an_isolated_context() {
        // AnySscc draws from the ambient context: built from Any.WithSeed(...) it cannot be, so a fixture
        // made with a context still varies wherever a scaffolded generator is involved.
        AnyContext context = Any.WithSeed(7);
        string first  = Any.Combine(context.Int32(), new AnySscc(), (n, sscc) => $"{n}/{sscc}").Generate();
        string second = Any.Combine(Any.WithSeed(7).Int32(), new AnySscc(), (n, sscc) => $"{n}/{sscc}").Generate();
        Log($"context + scaffolded: {first} vs {second}");

        Assert.Equal(first.Split('/')[0], second.Split('/')[0]);
    }

    // ----- Pools ----------------------------------------------------------------------------------------------

    [Fact]
    public void Probe_inspecting_a_catalogue_of_EANs_against_the_format() {
        string[] catalogue = ["3760123456782", "376012345678", "37601234567A2", "4006381333931"];

        IPoolInspection<string> pool = (IPoolInspection<string>)Any.String().OneOf(catalogue).Numeric().WithLength(13);
        Log("rejections: " + string.Join(" | ", pool.GetRejections()));
        Log("survivors: " + string.Join(", ", pool.GetSurvivors()));

        Assert.Equal(2, pool.GetSurvivors().Count);
    }

    [Fact]
    public void Probe_a_pattern_pool_is_inspectable() {
        string[] catalogue = ["A-12-03", "A-1-03", "Z-99-20"];
        Exception? outcome = Record.Exception(() => {
            IPoolInspection<string> pool = (IPoolInspection<string>)Any.StringMatching(@"[A-Z]-\d{2}-\d{2}").OneOf(catalogue);
            Log("pattern pool rejections: " + string.Join(" | ", pool.GetRejections()));
        });
        Log("pattern pool: " + outcome?.GetType().Name + ": " + outcome?.Message);

        Assert.Null(outcome);
    }

    private static string Describe(PalletReceipt pallet) {
        return $"{pallet.Sscc}|{pallet.SupplierCode}|{pallet.ReceivedAt:O}|{pallet.RequiredTemperature?.MinCelsius}|"
             + string.Join(";", pallet.Lines.Select(line => $"{line.Gtin}/{line.Expected}/{line.Received}/{line.Lot}/{line.BestBefore}/{line.UnitWeightKg}"));
    }

}
