using CatastropheLosses.Domain;

namespace CatastropheLosses.Tests.Discovery;

/// <summary>Hand-chosen values the tests are about: they stay literals.</summary>
internal static class Samples {

    // Damage is nil up to 2, then rises linearly to 50 % at 10, and stays there.
    public static VulnerabilityCurve HalfAtTen() {
        return new VulnerabilityCurve("half-at-ten", [2d, 10d], [0d, 0.5d]);
    }

    public static GeoPoint Origin() {
        return new GeoPoint(0d, 0d);
    }

    public static Footprint PeakEightAtOrigin(double radiusKm = 100d) {
        return new Footprint(Origin(), radiusKm, 8d);
    }

    public static InsuredProperty PropertyAtOrigin(string id, decimal insuredValue) {
        return new InsuredProperty(id, Origin(), insuredValue, HalfAtTen());
    }

    public static Exposure ExposureAtOrigin(string id, decimal insuredValue, decimal deductible, decimal limit) {
        return new Exposure(PropertyAtOrigin(id, insuredValue), new PolicyTerms(deductible, limit));
    }

    public static Portfolio Frozen(params Exposure[] exposures) {
        Portfolio portfolio = new Portfolio("book", "EUR");
        foreach (Exposure exposure in exposures) {
            portfolio.Add(exposure);
        }

        portfolio.Freeze();

        return portfolio;
    }

}
