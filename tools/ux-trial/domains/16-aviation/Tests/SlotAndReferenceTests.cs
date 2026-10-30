using Aviation.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Aviation.Tests;

[Reproducible]
public sealed class SlotAndReferenceTests {

    private static readonly DateTime TenOClock = new DateTime(2026, 5, 4, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Two_slots_at_the_same_airport_overlap_when_their_windows_intersect() {
        // Arrange
        IcaoAirportCode anyAirport = Any.IcaoAirportCode().Generate();
        Slot            first      = Arbitrary.Slot().WithAirport(anyAirport).WithStart(TenOClock).WithDuration(TimeSpan.FromMinutes(30)).Generate();
        Slot            second     = Arbitrary.Slot().WithAirport(anyAirport).WithStart(TenOClock.AddMinutes(25)).Generate();

        // Act
        bool overlap = first.Overlaps(second);

        // Assert
        Assert.True(overlap);
    }

    [Fact]
    public void Slots_at_two_different_airports_do_not_overlap_at_the_same_time() {
        // Arrange
        List<string> anyTwoAirports = Any.ListOf(Any.String().Alpha().InUpperCase().WithLength(4)).WithCount(2).Distinct().Generate();
        Slot         first          = Arbitrary.Slot().WithAirport(new IcaoAirportCode(anyTwoAirports[0])).WithStart(TenOClock).Generate();
        Slot         second         = Arbitrary.Slot().WithAirport(new IcaoAirportCode(anyTwoAirports[1])).WithStart(TenOClock).Generate();

        // Act
        bool overlap = first.Overlaps(second);

        // Assert
        Assert.False(overlap);
    }

    [Theory]
    [InlineData(-5, true)]
    [InlineData(-6, false)]
    [InlineData(20, true)]
    [InlineData(21, false)]
    public void A_departure_respects_its_slot_within_five_minutes_either_side(int minutesFromStart, bool expected) {
        // Arrange
        Slot anySlot = Arbitrary.Slot().WithStart(TenOClock).WithDuration(TimeSpan.FromMinutes(15)).Generate();

        // Act
        bool respected = anySlot.IsRespectedBy(TenOClock.AddMinutes(minutesFromStart));

        // Assert
        Assert.Equal(expected, respected);
    }

    [Fact]
    public void A_reversed_route_swaps_its_endpoints() {
        // Arrange
        IataAirportCode paris   = new IataAirportCode("CDG");
        IataAirportCode newYork = new IataAirportCode("JFK");
        Route           anyRoute = Any.Route().WithOrigin(paris).WithDestination(newYork).Generate();

        // Act
        Route reversed = anyRoute.Reversed();

        // Assert
        Assert.Equal(newYork, reversed.Origin);
        Assert.Equal(paris, reversed.Destination);
    }

    [Fact]
    public void An_ICAO_code_in_region_L_is_european() {
        // Arrange
        IcaoAirportCode anyLCode = Any.IcaoAirportCode().WithValue(code => code.StartingWith("L")).Generate();

        // Act
        bool european = anyLCode.IsEuropean;

        // Assert
        Assert.True(european);
    }

    [Fact]
    public void A_registration_in_the_N_number_form_is_american() {
        // Arrange
        AircraftRegistration anyNNumber = Any.AircraftRegistration().WithMark(mark => mark.OneOf("N1", "N12345", "N123AB", "N7")).Generate();

        // Act
        bool american = anyNNumber.IsUnitedStates;

        // Assert
        Assert.True(american);
    }

    [Fact]
    public void Only_the_777_in_the_fleet_list_is_a_wide_body() {
        // Arrange
        Aircraft anyTripleSeven = Any.Aircraft().WithTypeDesignator("B77W").Generate();
        Aircraft anyNarrowBody  = Any.Aircraft().WithTypeDesignator(t => t.DifferentFrom("B77W")).Generate();

        // Act & Assert
        Assert.True(anyTripleSeven.IsWideBody);
        Assert.False(anyNarrowBody.IsWideBody);
    }

}
