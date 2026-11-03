using JustDummies;

namespace RailwayInterlocking.Domain.Tests.Expert.Generators;

// Written by hand beside the scaffolded file: `any generate RouteLock --force` leaves it alone.
public sealed partial class AnyRouteLock {

    /// <summary>The valid base of the lock time, for a test that needs an instant a route lock would accept.</summary>
    internal static AnyDateTimeOffset ValidLockedAt() {
        return AnyValidLockedAt();
    }

}
