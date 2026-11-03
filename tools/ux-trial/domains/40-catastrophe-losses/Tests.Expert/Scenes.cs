using CatastropheLosses.Domain;
using CatastropheLosses.Tests.Expert.Generators;
using JustDummies;

namespace CatastropheLosses.Tests.Expert;

/// <summary>Small worlds with known arithmetic, shared by the tests that need a loss they can compute by hand.</summary>
internal static class Scenes {

    /// <summary>No damage up to intensity 2; from intensity 8 on, 30% of the value; linear in between.</summary>
    public static VulnerabilityCurve LinearUpToThirtyPercent() {
        return new VulnerabilityCurve(Any.String().NotBlank().Generate(), [2d, 8d], [0d, 0.3d]);
    }

    /// <summary>No damage at intensity 0; the whole value lost at intensity 100; linear in between.</summary>
    public static VulnerabilityCurve LinearUpToTotalLoss() {
        return new VulnerabilityCurve(Any.String().NotBlank().Generate(), [0d, 100d], [0d, 1d]);
    }

    public static Exposure ExposureOf(GeoPoint place, decimal insuredValue, PolicyTerms terms, VulnerabilityCurve curve) {
        InsuredProperty property = Any.InsuredProperty().WithLocation(place).WithInsuredValue(insuredValue).WithVulnerability(curve).Generate();

        return new Exposure(property, terms);
    }

    public static Exposure ExposureOf(string propertyId, GeoPoint place, decimal insuredValue, PolicyTerms terms, VulnerabilityCurve curve) {
        InsuredProperty property = Any.InsuredProperty().WithId(propertyId).WithLocation(place).WithInsuredValue(insuredValue).WithVulnerability(curve).Generate();

        return new Exposure(property, terms);
    }

    /// <summary>Exposures whose property ids differ, as a portfolio requires.</summary>
    public static List<Exposure> DistinctExposures(int count) {
        return Any.ListOf(Any.Exposure()).WithCount(count).DistinctBy(exposure => exposure.Property.Id).Generate();
    }

    public static Portfolio FrozenPortfolioOf(params Exposure[] exposures) {
        Portfolio portfolio = Any.Portfolio().Generate();
        foreach (Exposure exposure in exposures) {
            portfolio.Add(exposure);
        }

        portfolio.Freeze();

        return portfolio;
    }

    /// <summary>An event of the given annual rate whose footprint centre is the given place.</summary>
    public static CatastropheEvent EventAt(GeoPoint place, double peakIntensity, double annualRate) {
        double    anyRadiusKm = Any.Double().Between(10d, Footprint.MaxRadiusKm).Generate();
        Footprint footprint   = new Footprint(place, anyRadiusKm, peakIntensity);

        return Any.CatastropheEvent().WithFootprint(footprint).WithAnnualRate(annualRate).Generate();
    }

}
