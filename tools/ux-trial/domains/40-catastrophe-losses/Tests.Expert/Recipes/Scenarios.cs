using CatastropheLosses.Domain;

namespace CatastropheLosses.Tests.Expert.Recipes;

/// <summary>
/// Refinements shared by the scenario tests. They are functions, not generators, so each test keeps
/// the scaffolded base and only states what its scenario needs.
/// </summary>
public static class Scenarios {

    /// <summary>Damage ratio = intensity / 100, total loss from 100 on: easy to reason about by hand.</summary>
    public static readonly Func<AnyVulnerabilityCurve, AnyVulnerabilityCurve> LinearCurve =
        curve => curve.WithIntensities([0.0, 100.0]).WithDamageRatios([0.0, 1.0]);

    /// <summary>Terms that pass the ground-up loss through untouched: no deductible, limit at full value.</summary>
    public static PolicyTerms FullValueTerms(decimal insuredValue) => new(deductible: 0m, limit: insuredValue);

}
