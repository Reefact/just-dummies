using JustDummies;

namespace Serialisation.Domain.Tests;

internal static class Fixtures {

    /// <summary>A batch whose whole life is spelled out; every other part is arbitrary and valid.</summary>
    public static AnyBatch BatchLiving(DateOnly manufacturedOn, DateOnly expiresOn) {
        return new AnyBatch().WithManufacturedOn(manufacturedOn)
                             .WithExpiresOn(expiresOn)
                             .WithPlannedQuantity(q => q.GreaterThanOrEqualTo(50));
    }

    public static AnyList<SerialNumber> DistinctSerials(int count) {
        return Any.ListOf(new AnySerialNumber()).WithCount(count).Distinct();
    }

    public static AnyDateOnly DatesOfTheModernEra() {
        return Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2100, 12, 31));
    }

}
