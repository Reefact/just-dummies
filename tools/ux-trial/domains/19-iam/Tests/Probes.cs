using System.Text.RegularExpressions;
using Identity.Domain;
using JustDummies;
using Xunit;

namespace Identity.Tests;

/// <summary>Deliberate edge cases. Each asserts the behaviour observed; see NOTES.md for the judgement.</summary>
public sealed class Probes {

    private static readonly DateTimeOffset Noon = new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

    // ---- contradictions: fast, and naming both sides --------------------------------------------

    [Fact]
    public void An_expiry_window_that_ends_before_it_starts_names_both_bounds() {
        DateTimeOffset issuedAt = Noon;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateTimeOffset().After(issuedAt).Before(issuedAt.AddDays(-1)));

        Assert.Contains("After(2025-06-01T12:00:00", conflict.Message);
        Assert.Contains("Before(2025-05-31T12:00:00", conflict.Message);
    }

    [Fact]
    public void A_positive_lifetime_capped_at_zero_names_both_sides() {
        TimeSpan noLifetime = TimeSpan.Zero;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeSpan().Positive().LessThanOrEqualTo(noLifetime));

        Assert.Contains("Positive()", conflict.Message);
        Assert.Contains("LessThanOrEqualTo(00:00:00)", conflict.Message);
    }

    [Fact]
    public void A_pool_of_the_empty_guid_cannot_be_non_empty() {
        Guid[] unassigned = [Guid.Empty];

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Guid().OneOf(unassigned).NonEmpty());

        Assert.Contains("NonEmpty()", conflict.Message);
        Assert.Contains("OneOf(00000000-0000-0000-0000-000000000000)", conflict.Message);
    }

    [Fact]
    public void A_web_uri_without_a_path_cannot_take_path_segments() {
        int segments = 2;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Uri().Web().WithoutPath().WithPathSegments(segments));

        Assert.Equal("Cannot apply WithPathSegments(2) because WithoutPath() is already defined.", conflict.Message);
    }

    // ---- bounds at the edges of the calendar ----------------------------------------------------

    [Fact]
    public void A_single_instant_window_draws_that_instant() {
        DateTimeOffset drawn = Any.DateTimeOffset().Between(Noon, Noon).Generate();

        Assert.Equal(Noon, drawn);
    }

    [Fact]
    public void A_plus_fourteen_offset_one_hour_before_the_end_of_time_is_refused_naming_both() {
        DateTimeOffset lastHour = DateTimeOffset.MaxValue.AddHours(-1);
        TimeSpan       kiribati = TimeSpan.FromHours(14);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateTimeOffset().After(lastHour).WithOffset(kiribati));

        // Both sides are named; the reason (the local clock would pass 9999-12-31) is not.
        Assert.Equal("Cannot apply WithOffset(14:00:00) because After(9999-12-31T22:59:59.9999999+00:00) already requires values greater than or equal to 9999-12-31T23:00:00.0000000+00:00.", conflict.Message);
    }

    [Fact]
    public void The_last_representable_date_can_be_drawn_and_nothing_after_it() {
        DateOnly last = DateOnly.MaxValue;

        Assert.Equal(last, Any.DateOnly().Between(last, last).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(last));
    }

    [Fact]
    public void Day_granularity_with_an_offset_snaps_the_utc_instant_not_the_local_clock() {
        DateTimeOffset drawn = Any.DateTimeOffset()
                                  .Between(Noon, Noon.AddDays(30))
                                  .WithOffset(TimeSpan.FromHours(2))
                                  .WithGranularity(TimeSpan.FromDays(1))
                                  .Generate();

        // A "whole day" in Paris summer time reads 02:00 local: midnight UTC.
        Assert.Equal(TimeSpan.Zero, drawn.UtcDateTime.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(2), drawn.TimeOfDay);
    }

    [Fact]
    public void A_granularity_with_no_grid_point_inside_the_window_is_refused() {
        TimeSpan oneSecond = TimeSpan.FromSeconds(1);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateTimeOffset().Between(Noon.AddMilliseconds(100), Noon.AddMilliseconds(900)).WithGranularity(oneSecond));

        Assert.Contains("WithGranularity(00:00:01)", conflict.Message);
    }

    // ---- GUIDs ------------------------------------------------------------------------------------

    [Fact]
    public void A_drawn_guid_is_not_an_rfc_9562_version_4_guid() {
        // Guid.NewGuid() always yields version 4; a drawn Guid is 128 random bits, so code that
        // validates `id.Version == 4` (common for externally supplied ids) rejects most draws.
        int version4 = 0;
        for (int draw = 0; draw < 200; draw++) {
            if (Any.Guid().NonEmpty().Generate().Version == 4) {
                version4++;
            }
        }

        Assert.InRange(version4, 0, 50);
    }

    [Fact]
    public void A_version_4_guid_text_can_be_drawn_from_a_pattern() {
        string anyText = Any.StringMatching("^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$").Generate();

        Assert.Equal(4, Guid.Parse(anyText).Version);
    }

    // ---- URIs ----------------------------------------------------------------------------------

    [Fact]
    public void A_web_uri_carries_no_fragment_unless_asked() {
        for (int draw = 0; draw < 300; draw++) {
            Assert.Equal(string.Empty, Any.Uri().Web().Generate().Fragment);
        }
    }

    [Fact]
    public void Host_spellings_are_checked_when_declared() {
        string bareIpv6 = "::1";
        string withPort = "example.com:8080";

        ArgumentException ipv6 = Assert.Throws<ArgumentException>(() => Any.Uri().Web().WithHost(bareIpv6));
        ArgumentException port = Assert.Throws<ArgumentException>(() => Any.Uri().Web().WithHost(withPort));

        Assert.Contains("Pass \"[::1]\" instead", ipv6.Message);
        Assert.Contains("is not a valid host name", port.Message);
    }

    [Fact]
    public void A_loopback_ipv6_host_draws_a_loopback_uri() {
        Uri anyLoopback = Any.Uri().Web().UsingHttp().WithHost("[::1]").Generate();

        Assert.True(anyLoopback.IsLoopback);
    }

    // ---- patterns ------------------------------------------------------------------------------

    [Fact]
    public void A_password_policy_lookahead_is_refused_by_name_and_position() {
        string policy = @"^(?=.*\d)(?=.*[a-z]).{12,}$";

        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(policy).Generate());

        Assert.Contains("lookahead '(?=…)' at position 1", refusal.Message);
    }

    [Fact]
    public void A_jwt_shaped_token_can_be_drawn() {
        Regex jwt = new Regex(@"^[A-Za-z0-9_-]{10,40}\.[A-Za-z0-9_-]{10,40}\.[A-Za-z0-9_-]{10,40}$");

        string anyToken = Any.StringMatching(jwt).Generate();

        Assert.Equal(3, anyToken.Split('.').Length);
    }

    [Fact]
    public void Ignore_pattern_whitespace_is_refused_rather_than_misread() {
        Regex readable = new Regex(@"^ [a-z]{3} : (read|write) $", RegexOptions.IgnorePatternWhitespace);

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.StringMatching(readable));

        Assert.Contains("IgnorePatternWhitespace", refusal.Message);
    }

    [Fact]
    public void A_pattern_whose_exclusions_empty_its_two_words_ends_in_a_bounded_redraw() {
        string[] both = ["orders:read", "orders:write"];

        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.StringMatching("^orders:(read|write)$").Except(both).Generate());

        Assert.Contains("no candidate survived 10000 draws", exhausted.Message);
    }

    // ---- duplicate-free sets -------------------------------------------------------------------

    [Fact]
    public void A_set_of_scopes_larger_than_the_scope_language_is_refused_after_the_draw_budget() {
        int four = 4;

        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(
            () => Any.SetOf(Any.StringMatching("^orders:(read|write|admin)$").As(value => new Scope(value))).WithCount(four).Generate());

        Assert.Contains("produced only 3 distinct value(s)", exhausted.Message);
    }

    [Fact]
    public void A_set_of_every_role_holds_each_role_once() {
        HashSet<Role> roles = Any.SetOf(Any.Enum<Role>()).WithCount(Enum.GetValues<Role>().Length).Generate();

        Assert.Equal(Enum.GetValues<Role>().ToHashSet(), roles);
    }

    [Fact]
    public void Distinct_uris_are_distinct_by_uri_equality() {
        HashSet<Uri> anyUris = Any.SetOf(Any.Uri().Web().UsingHttps().WithHost("a.example").WithPathSegments(1)).WithCount(20).Generate();

        Assert.Equal(20, anyUris.Count);
    }

    // ---- seeds ---------------------------------------------------------------------------------

    [Fact]
    public void Two_contexts_on_the_same_seed_draw_the_same_identity_values() {
        AnyContext first  = Any.WithSeed(20261027);
        AnyContext second = Any.WithSeed(20261027);

        Assert.Equal(first.Guid().Generate(), second.Guid().Generate());
        Assert.Equal(first.Uri().Web().Generate(), second.Uri().Web().Generate());
        Assert.Equal(first.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-5), TimeSpan.FromHours(5)).Generate(),
                     second.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-5), TimeSpan.FromHours(5)).Generate());
    }

    [Fact]
    public void A_failing_body_reports_a_seed_that_replays_its_draws() {
        string?     report = null;
        AccessToken? failed = null;
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            failed = new AnyAccessToken().Generate();
            throw new InvalidOperationException("the assertion that failed");
        }, message => report = message));

        int         seed     = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);
        AccessToken? replayed = null;
        Any.Reproducibly(seed, () => replayed = new AnyAccessToken().Generate());

        Assert.Equal(failed!.TokenId, replayed!.TokenId);
        Assert.Equal(failed.ExpiresAt, replayed.ExpiresAt);
        Assert.Equal(failed.Scopes.Select(s => s.Value).Order(), replayed.Scopes.Select(s => s.Value).Order());
    }

}
