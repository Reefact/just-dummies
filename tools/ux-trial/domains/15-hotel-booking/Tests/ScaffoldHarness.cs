using Booking.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Booking.Tests;

/// <summary>One check per scaffolded generator: 500 draws, and the real constructor never refuses one.</summary>
public sealed class ScaffoldHarness {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact, Reproducible]
    public void Scaffolded_AnyRoomNumber_always_builds_a_valid_RoomNumber() => AlwaysBuilds(new AnyRoomNumber());

    [Fact, Reproducible]
    public void Scaffolded_AnyGuestName_always_builds_a_valid_GuestName() => AlwaysBuilds(new AnyGuestName());

    [Fact, Reproducible]
    public void Scaffolded_AnyMoney_always_builds_a_valid_Money() => AlwaysBuilds(new AnyMoney());

    [Fact, Reproducible]
    public void Scaffolded_AnyStayPeriod_always_builds_a_valid_StayPeriod() => AlwaysBuilds(new AnyStayPeriod());

    [Fact, Reproducible]
    public void Scaffolded_AnyRoom_always_builds_a_valid_Room() => AlwaysBuilds(new AnyRoom());

    [Fact, Reproducible]
    public void Scaffolded_AnyCancellationPolicy_always_builds_a_valid_CancellationPolicy() => AlwaysBuilds(new AnyCancellationPolicy());

    [Fact, Reproducible]
    public void Scaffolded_AnyReservation_always_builds_a_valid_Reservation() => AlwaysBuilds(new AnyReservation());

    [Fact, Reproducible]
    public void Scaffolded_AnyGuestProfile_always_builds_a_valid_GuestProfile() => AlwaysBuilds(new AnyGuestProfile());

}
