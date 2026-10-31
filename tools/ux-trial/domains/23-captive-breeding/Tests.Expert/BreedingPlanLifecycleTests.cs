using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class BreedingPlanLifecycleTests {

    [Fact]
    public void An_empty_plan_cannot_be_approved() {
        // Arrange
        Studbook     studbook = new();
        BreedingPlan plan     = new(studbook.Pedigree, Any.KinshipPolicy().Generate(), Any.DateOnly().Generate());

        // Act
        Action approve = plan.Approve;

        // Assert
        Assert.Throws<InvalidOperationException>(approve);
        Assert.Equal(BreedingPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void An_approved_plan_accepts_no_further_pairing() {
        // Arrange
        Studbook     studbook    = new();
        Individual   anySire     = studbook.Male();
        Individual   anyDam      = studbook.Female();
        Individual   anyOtherDam = studbook.Female();
        BreedingPlan plan        = studbook.PlanUnder(Studbook.AnyPolicy().Generate());
        plan.Propose(anySire.Id, anyDam.Id);
        plan.Approve();

        // Act
        Action propose = () => plan.Propose(anySire.Id, anyOtherDam.Id);

        // Assert
        Assert.Throws<InvalidOperationException>(propose);
        Assert.Equal(BreedingPlanStatus.Approved, plan.Status);
    }

    [Fact]
    public void The_expected_offspring_inbreeding_is_the_mean_kinship_of_the_planned_pairs() {
        // Arrange: one half-sibling pair (1/8) and one unrelated pair (0).
        Studbook      studbook          = new();
        Individual    anySharedSire     = studbook.Male();
        Individual    anyHalfBrother    = studbook.SonOf(anySharedSire, studbook.Female());
        Individual    anyHalfSister     = studbook.DaughterOf(anySharedSire, studbook.Female());
        Individual    anyUnrelatedSire  = studbook.Male();
        Individual    anyUnrelatedDam   = studbook.Female();
        // The policy's own "at most 0.25" is restated: refining the coefficient through its WithValue
        // restarts from AnyKinshipCoefficient's base, [0, 1], and drops the cap the policy's base set.
        KinshipPolicy tolerantOfHalfSib = Studbook.AnyPolicy()
                                                  .WithMaximumPairKinship(k => k.WithValue(v => v.Between(0.125, 0.25)))
                                                  .Generate();
        BreedingPlan  plan              = studbook.PlanUnder(tolerantOfHalfSib);
        plan.Propose(anyHalfBrother.Id, anyHalfSister.Id);
        plan.Propose(anyUnrelatedSire.Id, anyUnrelatedDam.Id);

        // Act
        double expectedInbreeding = plan.ExpectedOffspringInbreeding;

        // Assert
        Assert.Equal(0.0625, expectedInbreeding);
    }

}
