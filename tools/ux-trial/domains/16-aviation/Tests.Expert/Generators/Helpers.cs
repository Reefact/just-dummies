using JustDummies;

namespace Aviation.Domain.Tests.Expert.Generators;

/// <summary>What the tests share beyond the scaffolded generators.</summary>
internal static class Fixtures {

    /// <summary>Any UTC midnight within the century the generators draw dates from: the day a test hangs its times on.</summary>
    internal static readonly AnyDateTime AnyUtcMidnight =
        Any.DateTime()
           .Between(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc))
           .WithGranularity(TimeSpan.FromDays(1));

    /// <summary>Brings a flight to <paramref name="status" /> by the moves the domain allows, after the <paramref name="delays" /> it was given while it could still be delayed.</summary>
    internal static Flight FlightIn(AnyFlight flights, FlightStatus status, params TimeSpan[] delays) {
        Flight flight = flights.Generate();

        foreach (TimeSpan delay in delays) {
            flight.Delay(delay);
        }

        switch (status) {
            case FlightStatus.Scheduled:
                break;
            case FlightStatus.Boarding:
                flight.StartBoarding();
                break;
            case FlightStatus.Departed:
                flight.StartBoarding();
                flight.Depart();
                break;
            case FlightStatus.Arrived:
                flight.StartBoarding();
                flight.Depart();
                flight.Arrive();
                break;
            case FlightStatus.Cancelled:
                flight.Cancel();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return flight;
    }

}
