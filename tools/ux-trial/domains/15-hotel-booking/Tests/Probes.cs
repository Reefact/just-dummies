using Booking.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Booking.Tests;

/// <summary>Deliberate edges: each test records what the library did, observed on 1.0.0-uxtrial.1.</summary>
public sealed class Probes {

    private static readonly DateOnly June1 = new DateOnly(2025, 6, 1);

    // ---- Contradictory date intervals -------------------------------------------------------

    [Fact]
    public void A_check_out_after_and_before_the_same_day_is_refused_naming_both_sides() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(June1).Before(June1));

        Assert.Equal("Cannot apply Before(2025-06-01) because After(2025-06-01) already requires values greater than or equal to 2025-06-02.", refusal.Message);
    }

    [Fact]
    public void An_open_interval_of_one_excluded_day_is_refused_naming_every_constraint() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateOnly().After(June1).Before(June1.AddDays(2)).Except(June1.AddDays(1)));

        Assert.Equal("Cannot apply Except(2025-06-02) because it forbids 2025-06-02, the only value After(2025-06-01) and Before(2025-06-03) leave.", refusal.Message);
    }

    [Fact]
    public void Reversed_date_bounds_are_an_argument_error_not_a_conflict() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.DateOnly().Between(June1.AddDays(5), June1));

        Assert.StartsWith("The start (2025-06-06) must be at or before the end (2025-06-01).", refusal.Message);
    }

    [Fact]
    public void A_one_day_interval_always_draws_that_day() {
        Assert.Equal(June1, Any.DateOnly().Between(June1, June1).Generate());
    }

    [Fact]
    public void The_last_representable_day_is_reachable_and_nothing_lies_beyond_it() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().After(DateOnly.MaxValue.AddDays(-1)).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().Before(DateOnly.MinValue));
    }

    [Fact]
    public void A_granularity_coarser_than_the_interval_is_refused() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeOnly().Between(new TimeOnly(14, 10), new TimeOnly(14, 50)).WithGranularity(TimeSpan.FromHours(1)));

        Assert.Contains("WithGranularity(01:00:00)", refusal.Message);
        Assert.Contains("14:10", refusal.Message);
    }

    [Fact]
    public void An_overnight_arrival_window_wraps_midnight_as_TimeOnly_IsBetween_does() {
        TimeOnly start = new TimeOnly(22, 0);
        TimeOnly end   = new TimeOnly(2, 0);

        Assert.True(new TimeOnly(23, 30).IsBetween(start, end)); // the BCL's own reading of the window

        // The library now reads the pair the same way (issue #357).
        TimeOnly drawn = Any.TimeOnly().Between(start, end).Generate();
        Assert.True(drawn >= start || drawn <= end);
    }

    [Fact]
    public void An_overnight_arrival_window_can_be_built_by_hand_from_an_offset() {
        IAny<TimeOnly> anyLateArrival = Any.TimeOnly()
                                           .Between(TimeOnly.MinValue, new TimeOnly(4, 0))
                                           .WithGranularity(TimeSpan.FromMinutes(15))
                                           .As(offset => offset.Add(TimeSpan.FromHours(22)));

        for (int draw = 0; draw < 200; draw++) {
            Assert.True(anyLateArrival.Generate().IsBetween(new TimeOnly(22, 0), new TimeOnly(2, 0, 0, 1)));
        }
    }

    // ---- Numbers ----------------------------------------------------------------------------

    [Fact]
    public void Nights_bounded_to_nothing_are_refused_naming_both_sides() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 30).GreaterThan(30));

        Assert.Equal("Cannot apply GreaterThan(30) because Between(1, 30) already requires values less than or equal to 30.", refusal.Message);
    }

    [Fact]
    public void A_rate_scale_the_interval_cannot_hold_is_refused() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(0.001m, 0.009m).WithScale(2));

        Assert.Equal("Cannot apply WithScale(2) because no Decimal value WithScale(2) allows remains between 0.001 and 0.009.", refusal.Message);
    }

    [Fact]
    public void A_money_amount_keeps_exactly_two_decimal_places_even_with_trailing_zeros() {
        for (int draw = 0; draw < 500; draw++) {
            Assert.Equal(2, Any.Decimal().Between(0m, 1_000_000m).WithScale(2).Generate().Scale);
        }
    }

    [Fact]
    public void WithScale_is_a_lattice_not_a_representation_so_a_fine_scale_loses_places() {
        // The guide says WithScale "fixes the number of decimal places", and up to 22 every draw does carry exactly
        // that Scale. IntelliSense says otherwise: "a value lattice, not a representation contract". At 28 the value
        // is on the 10^-28 grid but its Scale is 22 or 23 (an ordinary magnitude leaves no room for 28 places).
        List<decimal> anyValues = Enumerable.Range(0, 200).Select(_ => Any.Decimal().WithScale(28).Generate()).ToList();

        Assert.All(anyValues, value => Assert.Equal(value, decimal.Round(value, 28)));
        Assert.DoesNotContain(anyValues, value => value.Scale == 28);
    }

    // ---- Collections ------------------------------------------------------------------------

    [Fact]
    public void More_distinct_room_types_than_exist_are_refused() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<RoomType>()).WithCount(5).Generate());

        Assert.Equal("The generator can produce only 4 distinct value(s), not enough for the 5 elements it must still supply to honour WithCount(5).", refusal.Message);
    }

    [Fact]
    public void Distinct_room_numbers_come_back_distinct() {
        List<RoomNumber> anyRooms = Any.ListOf(new AnyRoomNumber()).WithCount(50).Distinct().Generate();

        Assert.Equal(50, anyRooms.Select(room => room.Value).Distinct().Count());
    }

    [Fact]
    public void A_count_above_one_million_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ListOf(Any.Int32()).WithCount(1_000_001));
    }

    // ---- Patterns ---------------------------------------------------------------------------

    [Fact]
    public void A_lookahead_in_a_password_like_code_is_refused_by_name_and_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"^(?=.*\d)[A-Z0-9]{6}$"));

        Assert.StartsWith("The regular expression pattern \"^(?=.*\\d)[A-Z0-9]{6}$\" uses a lookahead '(?=…)' at position 1", refusal.Message);
    }

    [Fact]
    public void A_word_boundary_and_a_unicode_category_are_refused() {
        Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\bLY[0-9]{8}\b"));
        Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"^\p{Lu}{2}$"));
    }

    [Fact]
    public void Named_groups_open_quantifiers_and_negated_classes_are_honoured() {
        Assert.Matches(@"^[1-9][0-9]?[0-9]{2}$", Any.StringMatching(@"^(?<floor>[1-9][0-9]?)(?<room>[0-9]{2})$").Generate());
        Assert.Matches(@"^[A-Z]{2,}$", Any.StringMatching(@"^[A-Z]{2,}$").Generate());
        Assert.Matches(@"^[^@\s]+@[^@\s]+\.[a-z]{2,}$", Any.StringMatching(@"^[^@\s]+@[^@\s]+\.[a-z]{2,}$").Generate());
    }

    [Fact]
    public void A_currency_pool_the_pattern_admits_nothing_of_is_refused_at_declaration() {
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.StringMatching("^[A-Z]{3}$").OneOf("eur", "usd"));

        Assert.Equal("Cannot apply OneOf(\"eur\", \"usd\") because StringMatching(\"^[A-Z]{3}$\") allows none of its values.", refusal.Message);
    }

    // ---- Seeds ------------------------------------------------------------------------------

    [Fact]
    public void A_seeded_scope_replays_a_whole_reservation() {
        string first  = DescribeOneReservation(20261027);
        string second = DescribeOneReservation(20261027);

        Assert.Equal(first, second);
        Assert.NotEqual(first, DescribeOneReservation(20261028));
    }

    [Fact]
    public void Reproducibly_reports_the_seed_and_that_seed_replays_the_failing_values() {
        string?  report      = null;
        DateOnly failedValue = default;
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            failedValue = new AnyStayPeriod().Generate().CheckIn;
            throw new InvalidOperationException("the code under test failed");
        }, message => report = message));

        Assert.NotNull(report);
        int seed = int.Parse(System.Text.RegularExpressions.Regex.Match(report, @"seeded with (-?\d+)").Groups[1].Value);

        DateOnly replayed = default;
        Any.Reproducibly(seed, () => replayed = new AnyStayPeriod().Generate().CheckIn);
        Assert.Equal(failedValue, replayed);
    }

    [Fact, Reproducible(Seed = 20261027)]
    public void A_pinned_attribute_seed_draws_the_same_guest_on_every_run() {
        GuestProfile anyGuest = Any.GuestProfile().Generate();

        // Recorded on the first run under this seed; any later run must replay it.
        Assert.Equal("lcjrizm1w@pmn9xhbh.ckcuon", anyGuest.Email);
    }

    private static string DescribeOneReservation(int seed) {
        using (IDisposable scope = Any.UseSeed(seed)) {
            Reservation anyReservation = Arbitrary.Reservation().Generate();

            return $"{anyReservation.Id}|{anyReservation.Room.Number}|{anyReservation.Room.Type}|{anyReservation.Stay.CheckIn}|{anyReservation.Stay.Nights}|{anyReservation.Guests.Count}|{anyReservation.Total}";
        }
    }

}
