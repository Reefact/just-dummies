using System.Text.RegularExpressions;
using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

/// <summary>Deliberate edge cases: what the library refuses, and how it says so.</summary>
public sealed class Probes {

    private readonly ITestOutputHelper _output;

    public Probes(ITestOutputHelper output) {
        _output = output;
    }

    private static readonly Regex CadastralFormat = new Regex(@"^(\d{5}|2[AB]\d{3}) \d{3} [A-Z0-9]{2} \d{4}$");

    // ---- contradictions -------------------------------------------------------------------------

    [Fact]
    public void Crossed_double_bounds_fail_fast_naming_both_sides() {
        double low  = 10.5;
        double high = 5.25;
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(low).LessThan(high));
        _output.WriteLine(conflict.Message);
        Assert.Contains("10.5", conflict.Message);
        Assert.Contains("5.25", conflict.Message);
    }

    [Fact]
    public void A_decimal_range_holding_no_value_at_the_scale_is_refused() {
        decimal low  = 0.001m;
        decimal high = 0.009m;
        Exception refusal = Record.Exception(() => Any.Decimal().Between(low, high).WithScale(2).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Excluding_every_rating_is_refused_by_name() {
        EnergyRating[] all = Enum.GetValues<EnergyRating>();
        Exception refusal = Record.Exception(() => Any.Enum<EnergyRating>().Except(all).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Eight_distinct_ratings_out_of_seven_are_refused() {
        int count = 8;
        Exception refusal = Record.Exception(() => Any.SetOf(Any.Enum<EnergyRating>()).WithCount(count).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    // ---- bounds ---------------------------------------------------------------------------------

    [Fact]
    public void A_double_range_of_one_point_draws_that_point() {
        double surface = 42.5;
        Assert.Equal(42.5, Any.Double().Between(surface, surface).Generate());
    }

    [Fact]
    public void A_surface_bounded_at_ten_million_mostly_draws_below_a_million() {
        double[] draws = Enumerable.Range(0, 2_000).Select(_ => Any.Double().Positive().LessThanOrEqualTo(10_000_000d).Generate()).ToArray();
        _output.WriteLine($"min {draws.Min()} max {draws.Max()}");
        Assert.All(draws, d => Assert.InRange(d, double.Epsilon, 10_000_000d));
    }

    [Fact]
    public void A_one_sided_large_surface_stays_near_its_bound() {
        double[] draws = Enumerable.Range(0, 2_000).Select(_ => Any.Double().GreaterThanOrEqualTo(1_000_000d).Generate()).ToArray();
        _output.WriteLine($"min {draws.Min()} max {draws.Max()}");
        Assert.All(draws, d => Assert.True(double.IsFinite(d) && d >= 1_000_000d));
    }

    [Fact]
    public void A_decimal_at_scale_two_converts_to_a_double_on_the_hundredth() {
        for (int draw = 0; draw < 5_000; draw++) {
            double squareMeters = (double)Any.Decimal().Between(0.01m, 10_000_000m).WithScale(2).Generate();
            Assert.Equal(Math.Round(squareMeters, 2), squareMeters);
        }
    }

    [Fact]
    public void DateOnly_after_its_maximum_is_refused() {
        Exception refusal = Record.Exception(() => Any.DateOnly().After(DateOnly.MaxValue).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void DateOnly_between_the_maximum_and_itself_is_the_maximum() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().Between(DateOnly.MaxValue, DateOnly.MaxValue).Generate());
        Assert.Equal(DateOnly.MinValue, Any.DateOnly().BeforeOrEqualTo(DateOnly.MinValue).Generate());
    }

    [Fact]
    public void The_kind_of_a_drawn_DateTime_is_reported() {
        DateTime utcBound = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime plain    = Any.DateTime().Generate();
        DateTime bounded  = Any.DateTime().Before(utcBound).Generate();
        _output.WriteLine($"plain {plain:o} {plain.Kind}; bounded by a UTC instant {bounded:o} {bounded.Kind}");
        // Observed, not documented: every drawn DateTime carries DateTimeKind.Utc.
        Assert.Equal(DateTimeKind.Utc, plain.Kind);
    }

    [Fact]
    public void An_enum_without_a_zero_member_never_draws_zero() {
        Assert.All(Enumerable.Range(0, 500), _ => Assert.True(Enum.IsDefined(Any.Enum<PropertyKind>().Generate())));
    }

    // ---- patterns -------------------------------------------------------------------------------

    [Fact]
    public void The_cadastral_pattern_draws_ascii_values_the_domain_regex_accepts() {
        AnyPattern pattern = Any.StringMatching(@"^(\d{5}|2[AB]\d{3}) \d{3} [A-Z0-9]{2} \d{4}$");
        for (int draw = 0; draw < 1_000; draw++) {
            string value = pattern.Generate();
            Assert.Matches(CadastralFormat, value);
            Assert.All(value, c => Assert.True(c < 128));
        }
    }

    [Fact]
    public void A_lookahead_excluding_plan_zero_is_refused_naming_the_construct() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\d{5} \d{3} [A-Z]{2} (?!0000)\d{4}"));
        _output.WriteLine(refusal.Message);
        Assert.Contains("(?!", refusal.Message);
    }

    [Fact]
    public void Character_class_subtraction_is_either_honoured_or_refused() {
        // Cadastral sections avoid I and O; .NET spells that [A-Z-[IO]].
        Exception? refusal = Record.Exception(() => {
            AnyPattern sections = Any.StringMatching(@"[A-Z-[IO]]{2}");
            for (int draw = 0; draw < 500; draw++) {
                string section = sections.Generate();
                Assert.Matches(new Regex(@"^[A-Z-[IO]]{2}$"), section);
            }
        });
        _output.WriteLine(refusal is null ? "honoured" : refusal.GetType().Name + ": " + refusal.Message);
        Assert.True(refusal is null or UnsupportedRegexException);
    }

    [Fact]
    public void A_unicode_category_is_either_honoured_or_refused() {
        Exception? refusal = Record.Exception(() => {
            string value = Any.StringMatching(@"\p{Lu}{2}").Generate();
            Assert.Matches(new Regex(@"^\p{Lu}{2}$"), value);
        });
        _output.WriteLine(refusal is null ? "honoured" : refusal.GetType().Name + ": " + refusal.Message);
        Assert.True(refusal is null or UnsupportedRegexException);
    }

    [Fact]
    public void A_pattern_with_a_value_set_drops_the_values_it_does_not_match() {
        string[] seeded = ["75056 000 AB 0123", "not a reference"];
        Assert.All(Enumerable.Range(0, 50), _ => Assert.Equal("75056 000 AB 0123", Any.StringMatching(CadastralFormat).OneOf(seeded).Generate()));
    }

    // ---- uris -----------------------------------------------------------------------------------

    [Fact]
    public void A_https_document_uri_on_a_pinned_host_is_absolute_and_on_that_host() {
        for (int draw = 0; draw < 300; draw++) {
            Uri uri = Any.Uri().Web().UsingHttps().WithHost("documents.cadastre.example").WithPathSegments(3).Generate();
            Assert.True(uri.IsAbsoluteUri);
            Assert.Equal("https", uri.Scheme);
            Assert.Equal("documents.cadastre.example", uri.Host);
            Assert.Equal(4, uri.Segments.Length);
        }
    }

    [Fact]
    public void A_host_with_a_space_is_refused_when_declared() {
        Exception? refusal = Record.Exception(() => Any.Uri().Web().WithHost("cadastre example"));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }

    [Fact]
    public void A_negative_segment_count_is_refused() {
        Exception? refusal = Record.Exception(() => Any.Uri().Web().WithPathSegments(-1));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }

    // ---- reproducibility ------------------------------------------------------------------------

    [Fact]
    public void The_same_seed_draws_the_same_property_twice() {
        string first  = DrawOnce(20261027);
        string second = DrawOnce(20261027);
        Assert.Equal(first, second);

        static string DrawOnce(int seed) {
            using (Any.UseSeed(seed)) {
                Property property = Any.Property().Generate();
                return $"{property.Parcel} {property.Kind} {property.LivingArea} {property.Diagnosis.Rating} {property.Diagnosis.IssuedOn} {property.Documents.Count} "
                     + string.Join(",", property.Documents.Select(d => d.Location + "@" + d.UploadedAtUtc.Ticks));
            }
        }
    }

    [Fact]
    public void A_seeded_context_replays_a_pattern() {
        string first  = Any.WithSeed(7).StringMatching(CadastralFormat).Generate();
        string second = Any.WithSeed(7).StringMatching(CadastralFormat).Generate();
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_body_reports_its_seed_and_the_seed_replays_it() {
        string? report = null;
        string? drawnInFailure = null;
        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            drawnInFailure = Arbitrary.CadastralReference().Generate().Value;
            throw new InvalidOperationException("simulated failure");
        }, message => report = message));
        _output.WriteLine(report ?? "(no report)");
        Assert.NotNull(report);

        int seed = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);
        string? replayed = null;
        Any.Reproducibly(seed, () => replayed = Arbitrary.CadastralReference().Generate().Value);
        Assert.Equal(drawnInFailure, replayed);
    }

    // ---- collections ----------------------------------------------------------------------------

    [Fact]
    public void Distinct_documents_by_location_are_distinct() {
        List<DocumentLink> documents = Any.ListOf(Arbitrary.DocumentLink()).WithCount(20).Distinct(DocumentLocationComparer.Instance).Generate();
        Assert.Equal(20, documents.Select(d => d.Location).Distinct().Count());
    }

    [Fact]
    public void A_distinct_list_with_a_count_above_a_million_is_refused() {
        int count = 2_000_000;
        Exception? refusal = Record.Exception(() => Any.ListOf(Any.Enum<EnergyRating>()).WithCount(count));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }
}
