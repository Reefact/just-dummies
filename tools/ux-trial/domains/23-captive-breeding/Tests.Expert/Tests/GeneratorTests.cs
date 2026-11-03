using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;
using CaptiveBreeding.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

/// <summary>
///     The scaffolded generators are test infrastructure that the other suites rely on: a base that draws a
///     value the constructor refuses would fail those suites for a reason that has nothing to do with them.
/// </summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void The_studbook_id_generator_draws_ids_of_the_format() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.Matches("^[A-Z]{2,4}-[0-9]{4,6}\\z", Any.StudbookId().Generate().Value);
        }
    }

    [Fact]
    public void The_kinship_coefficient_generator_draws_coefficients_within_zero_and_one() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.InRange(Any.KinshipCoefficient().Generate().Value, 0.0, 1.0);
        }
    }

    [Fact]
    public void The_genetic_contribution_generator_draws_proportions_within_zero_and_one() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.InRange(Any.GeneticContribution().Generate().Proportion, 0.0, 1.0);
        }
    }

    [Fact]
    public void The_breeding_pair_generator_draws_distinct_partners_with_a_plausible_kinship() {
        for (int draw = 0; draw < Draws; draw++) {
            BreedingPair pair = Any.BreedingPair().Generate();

            Assert.NotEqual(pair.SireId, pair.DamId);
            Assert.InRange(pair.Kinship.Value, 0.0, 0.5);
        }
    }

    [Fact]
    public void The_kinship_policy_generator_draws_policies_the_constructor_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            KinshipPolicy policy = Any.KinshipPolicy().Generate();

            Assert.InRange(policy.MaximumPairKinship.Value, 0.0, 0.25);
            Assert.InRange(policy.MaximumPairingsPerIndividual, 1, 10);
            Assert.InRange(policy.MinimumBreedingAgeYears, 0, 50);
            Assert.InRange(policy.MaximumBreedingAgeYears, policy.MinimumBreedingAgeYears + 1, 100);
        }
    }

    [Fact]
    public void The_individual_generator_draws_living_founders() {
        for (int draw = 0; draw < Draws; draw++) {
            Individual individual = Any.Individual().Generate();

            Assert.True(individual.IsFounder);
            Assert.True(individual.IsAlive);
        }
    }

    [Fact]
    public void The_pedigree_and_plan_generators_draw_empty_drafts() {
        for (int draw = 0; draw < Draws; draw++) {
            BreedingPlan plan = Any.BreedingPlan().Generate();

            Assert.Empty(plan.Pedigree.Individuals);
            Assert.Equal(BreedingPlanStatus.Draft, plan.Status);
        }
    }

    [Fact]
    public void The_herd_registers_every_generation_with_parents_older_than_their_offspring() {
        for (int draw = 0; draw < 50; draw++) {
            Herd       herd      = new Herd();
            Individual sire      = herd.Male();
            Individual dam       = herd.Female();
            Individual child     = herd.Son(sire, dam);
            Individual grandchild = herd.Daughter(child, herd.Female());
            Individual greatGrandchild = herd.Son(herd.Male(), grandchild);

            Assert.True(sire.BirthDate < child.BirthDate && dam.BirthDate < child.BirthDate);
            Assert.True(child.BirthDate < grandchild.BirthDate);
            Assert.True(grandchild.BirthDate < greatGrandchild.BirthDate);

            KinshipPolicy policy = herd.AnyPolicy().Generate();
            Assert.All(herd.Pedigree.Individuals, individual => Assert.True(policy.IsOfBreedingAge(individual, herd.SeasonStart)));
        }
    }

}
