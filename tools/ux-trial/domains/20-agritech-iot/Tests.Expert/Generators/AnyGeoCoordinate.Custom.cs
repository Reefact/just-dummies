// Written by hand, beside the scaffolded file: `any generate GeoCoordinate --force` leaves it alone.
using JustDummies;

namespace AgriTech.Domain.Tests.Expert.Generators;

public sealed partial class AnyGeoCoordinate {

    /// <summary>A latitude the constructor accepts, for a test that needs one on its own.</summary>
    internal static AnyDouble ValidLatitude() {
        return AnyValidLatitude();
    }

    /// <summary>A longitude the constructor accepts, for a test that needs one on its own.</summary>
    internal static AnyDouble ValidLongitude() {
        return AnyValidLongitude();
    }

}
