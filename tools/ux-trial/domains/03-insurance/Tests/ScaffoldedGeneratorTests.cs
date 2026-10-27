using Insurance.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Insurance.Tests;

/// <summary>Harness checks: each scaffolded (and hand-resolved) generator never trips its type's own guards.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawsWithoutThrowing<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.NotNull(generator.Generate());
        }
    }

    [Fact]
    public void Scaffolded_AnyPolicyNumber_always_builds_a_valid_PolicyNumber() => DrawsWithoutThrowing(Any.PolicyNumber());

    [Fact]
    public void Scaffolded_AnyCoveragePeriod_always_builds_a_valid_CoveragePeriod() => DrawsWithoutThrowing(Any.CoveragePeriod());

    [Fact]
    public void Scaffolded_AnyDeductible_always_builds_a_valid_Deductible() => DrawsWithoutThrowing(Any.Deductible());

    [Fact]
    public void Scaffolded_AnyBonusMalus_always_builds_a_valid_BonusMalus() => DrawsWithoutThrowing(Any.BonusMalus());

    [Fact]
    public void Scaffolded_AnyClaim_always_builds_a_valid_Claim() => DrawsWithoutThrowing(Any.Claim());

    [Fact]
    public void Scaffolded_AnyDriver_always_builds_a_valid_Driver() => DrawsWithoutThrowing(Any.Driver());

    [Fact]
    public void Scaffolded_AnyCarPolicy_always_builds_a_valid_CarPolicy() => DrawsWithoutThrowing(Any.CarPolicy());

    [Fact]
    public void Bug_Scaffolded_AnyResponsibility_draws_more_than_the_zero_member() {
        // `any generate Responsibility` emits `return new Responsibility();`, i.e. always Responsibility.None.
        HashSet<Responsibility> seen = [];
        for (int draw = 0; draw < Draws; draw++) {
            seen.Add(new AnyResponsibility().Generate());
        }

        Assert.True(seen.Count > 1, $"Only ever drew: {string.Join(", ", seen)}");
    }

}
