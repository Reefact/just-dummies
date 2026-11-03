using RailwayInterlocking.Domain.Tests.Expert.Generators;

namespace RailwayInterlocking.Domain.Tests.Expert.Support;

internal static class Instants {

    /// <summary>A lock time the interlocking has no opinion about, beyond "it is set".</summary>
    public static DateTimeOffset AnyLockTime() {
        return AnyRouteLock.ValidLockedAt().Generate();
    }

}
