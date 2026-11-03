using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Regular;

internal static class Fixtures {

    /// <summary>A decimal literal from a theory's string argument (attributes cannot carry a decimal).</summary>
    public static decimal D(string text) {
        return decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>A curve damaging nothing at 0 and everything at <paramref name="fullDamageAt" />, linear between.</summary>
    public static VulnerabilityCurve LinearCurve(double fullDamageAt) {
        return new VulnerabilityCurve("linear", [0.0, fullDamageAt], [0.0, 1.0]);
    }

    /// <summary>
    ///     An exposure standing at the footprint's centre, whose ground-up loss is <c>value x severity</c>
    ///     (the curve reaches full damage at the footprint's peak).
    /// </summary>
    public static Exposure ExposureAtCentre(Footprint footprint, string id, decimal value, decimal deductible, decimal limit) {
        InsuredProperty property = new(id, footprint.Center, value, LinearCurve(footprint.PeakIntensity));

        return new Exposure(property, new PolicyTerms(deductible, limit));
    }

    /// <summary>Exposures with pairwise distinct property ids (a drawn id could, rarely, collide).</summary>
    public static IReadOnlyList<Exposure> DistinctExposures(int count) {
        HashSet<string> ids = Any.SetOf(Any.StringMatching("PRP-[0-9]{6}")).WithCount(count).Generate();

        return ids.Select(id => Any.Exposure().WithProperty(p => p.WithId(id)).Generate()).ToList();
    }

    public static Portfolio FrozenPortfolio(params Exposure[] exposures) {
        Portfolio portfolio = Any.Portfolio().Generate();
        foreach (Exposure exposure in exposures) {
            portfolio.Add(exposure);
        }

        portfolio.Freeze();

        return portfolio;
    }

}
