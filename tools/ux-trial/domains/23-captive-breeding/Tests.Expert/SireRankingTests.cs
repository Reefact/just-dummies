using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class SireRankingTests {

    [Fact]
    public void Sires_are_ranked_by_kinship_with_the_dam_then_by_how_under_represented_they_are() {
        // Arrange
        Studbook   studbook        = new();
        Individual anyDamsFather   = studbook.Male();
        Individual anyDam          = studbook.DaughterOf(anyDamsFather, studbook.Female());
        Individual anyHalfBrother  = studbook.SonOf(anyDamsFather, studbook.Female());
        Individual anyProvenSire   = studbook.Male();                  // unrelated, but already has a living daughter
        studbook.DaughterOf(anyProvenSire, studbook.Female());
        Individual anyUnusedSire   = studbook.Male();                  // unrelated, no descendants: most under-represented
        studbook.RecordDeathOf(anyDamsFather);
        KinshipPolicy tolerantOfHalfSiblings = Studbook.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate();
        BreedingPlan  plan                   = studbook.PlanUnder(tolerantOfHalfSiblings);

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(anyDam.Id);

        // Assert
        Assert.Equal([anyUnusedSire.Id, anyProvenSire.Id, anyHalfBrother.Id], ranking.Select(p => p.SireId));
    }

    [Fact]
    public void A_male_too_closely_related_to_the_dam_is_not_a_candidate() {
        // Arrange
        Studbook      studbook          = new();
        Individual    anySire           = studbook.Male();
        Individual    anyDam            = studbook.Female();
        Individual    anyBrother        = studbook.SonOf(anySire, anyDam);
        Individual    anySister         = studbook.DaughterOf(anySire, anyDam);
        Individual    anyUnrelatedMale  = studbook.Male();
        studbook.RecordDeathOf(anySire);
        KinshipPolicy cappedBelowSiblings = Studbook.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.125)).Generate();
        BreedingPlan  plan              = studbook.PlanUnder(cappedBelowSiblings);

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(anySister.Id);

        // Assert
        Assert.DoesNotContain(ranking, p => p.SireId == anyBrother.Id);
        Assert.Contains(ranking, p => p.SireId == anyUnrelatedMale.Id);
    }

}
