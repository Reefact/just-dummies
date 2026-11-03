using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;
using CaptiveBreeding.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

/// <summary>Behaviours a reviewer would expect from MODEL.md that the domain does not deliver.</summary>
public sealed class ProbableDefectTests {

    [Fact(Skip = "domain defect (probable, MODEL.md says 'eligible'): RankSiresFor ignores the pairing quota that Propose enforces; fails on any seed")]
    public void A_sire_who_has_used_up_its_pairings_is_not_ranked_for_another_dam() {
        // Arrange: a quota of one, the only male already paired with the first dam
        Herd         herd      = new Herd();
        Individual   sire      = herd.Male();
        Individual   firstDam  = herd.Female();
        Individual   secondDam = herd.Female();
        BreedingPlan plan      = herd.PlanWith(herd.AnyPolicy().WithMaximumPairingsPerIndividual(1).Generate());
        plan.Propose(sire.Id, firstDam.Id);

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(secondDam.Id);

        // Assert: Propose would refuse him, so he is not an eligible sire
        Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, secondDam.Id));
        Assert.Empty(ranking);
    }

    [Fact(Skip = "domain defect (probable): RankSiresFor ranks a sire already paired with that dam although Propose refuses the duplicate; fails on any seed")]
    public void A_sire_already_paired_with_the_dam_is_not_ranked_for_her_again() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam.Id);

        // Assert
        Assert.Empty(ranking);
    }

    [Fact(Skip = "domain defect: Individual.RecordDeath accepts a date before the birth of an offspring already registered, so the invariant Pedigree.Register enforces can be broken afterwards; fails on any seed")]
    public void A_dam_cannot_be_recorded_dead_before_the_birth_of_an_offspring_the_studbook_holds() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        Individual offspring = herd.Son(sire, dam);

        // Assert: Register refuses such an offspring, so the studbook must not end up holding one
        Assert.ThrowsAny<Exception>(() => dam.RecordDeath(offspring.BirthDate.AddDays(-1)));
    }

}
