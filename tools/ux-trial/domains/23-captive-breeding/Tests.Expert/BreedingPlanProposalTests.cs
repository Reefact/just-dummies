using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class BreedingPlanProposalTests {

    [Fact]
    public void Two_unrelated_founders_of_breeding_age_are_paired_with_no_expected_inbreeding() {
        // Arrange
        Studbook     studbook = new();
        Individual   anySire  = studbook.Male();
        Individual   anyDam   = studbook.Female();
        BreedingPlan plan     = studbook.PlanUnder(Studbook.AnyPolicy().Generate());

        // Act
        BreedingPair pair = plan.Propose(anySire.Id, anyDam.Id);

        // Assert
        Assert.Equal(KinshipCoefficient.Unrelated, pair.Kinship);
        Assert.Contains(pair, plan.Pairs);
    }

    [Fact]
    public void Half_siblings_are_refused_under_a_policy_capped_at_first_cousin_kinship() {
        // Arrange
        Studbook     studbook        = new();
        Individual   anySharedSire   = studbook.Male();
        Individual   anyHalfBrother  = studbook.SonOf(anySharedSire, studbook.Female());
        Individual   anyHalfSister   = studbook.DaughterOf(anySharedSire, studbook.Female());
        KinshipPolicy cappedAtCousins = Studbook.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.0625)).Generate();
        BreedingPlan plan            = studbook.PlanUnder(cappedAtCousins);

        // Act
        Action propose = () => plan.Propose(anyHalfBrother.Id, anyHalfSister.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("exceeds the limit", refusal.Reason);
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void Full_siblings_are_accepted_by_a_policy_whose_limit_is_exactly_their_kinship() {
        // Arrange
        Studbook      studbook          = new();
        Individual    anySire           = studbook.Male();
        Individual    anyDam            = studbook.Female();
        Individual    anyBrother        = studbook.SonOf(anySire, anyDam);
        Individual    anySister         = studbook.DaughterOf(anySire, anyDam);
        KinshipPolicy cappedAtSiblings  = Studbook.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate();
        BreedingPlan  plan              = studbook.PlanUnder(cappedAtSiblings);

        // Act
        BreedingPair pair = plan.Propose(anyBrother.Id, anySister.Id);

        // Assert
        Assert.Equal(0.25, pair.Kinship.Value);
    }

    [Fact]
    public void A_pairing_with_a_dead_partner_is_refused() {
        // Arrange
        Studbook     studbook = new();
        Individual   anySire  = studbook.Male();
        Individual   anyDam   = studbook.Female();
        BreedingPlan plan     = studbook.PlanUnder(Studbook.AnyPolicy().Generate());
        studbook.RecordDeathOf(anyDam);

        // Act
        Action propose = () => plan.Propose(anySire.Id, anyDam.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Equal(anyDam.Id, refusal.DamId);
    }

    [Fact]
    public void A_male_proposed_as_dam_is_refused() {
        // Arrange
        Studbook     studbook     = new();
        Individual   anySire      = studbook.Male();
        Individual   anyOtherMale = studbook.Male();
        BreedingPlan plan         = studbook.PlanUnder(Studbook.AnyPolicy().Generate());

        // Act
        Action propose = () => plan.Propose(anySire.Id, anyOtherMale.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("is not female", refusal.Reason);
    }

    [Fact]
    public void A_dam_one_day_short_of_the_minimum_breeding_age_at_season_start_is_refused() {
        // Arrange
        Studbook      studbook          = new();
        Individual    anySire           = studbook.Male();
        Individual    anyYoungDam       = studbook.Register(Any.Individual().WithSex(Sex.Female).WithBirthDate(new DateOnly(2023, 3, 2)));
        KinshipPolicy anyPolicyFromTwo  = Any.KinshipPolicy().WithMinimumBreedingAgeYears(2).Generate();
        BreedingPlan  plan              = new(studbook.Pedigree, anyPolicyFromTwo, new DateOnly(2025, 3, 1));

        // Act
        Action propose = () => plan.Propose(anySire.Id, anyYoungDam.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("breeding age", refusal.Reason);
    }

    [Fact]
    public void The_same_pair_cannot_be_proposed_twice_in_a_season() {
        // Arrange
        Studbook     studbook = new();
        Individual   anySire  = studbook.Male();
        Individual   anyDam   = studbook.Female();
        BreedingPlan plan     = studbook.PlanUnder(Studbook.AnyPolicy().Generate());
        plan.Propose(anySire.Id, anyDam.Id);

        // Act
        Action proposeAgain = () => plan.Propose(anySire.Id, anyDam.Id);

        // Assert
        Assert.Throws<PairingRefusedException>(proposeAgain);
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void A_sire_allowed_a_single_pairing_cannot_be_given_a_second_dam() {
        // Arrange
        Studbook      studbook        = new();
        Individual    anySire         = studbook.Male();
        Individual    anyDam          = studbook.Female();
        Individual    anyOtherDam     = studbook.Female();
        KinshipPolicy onePairingEach  = Studbook.AnyPolicy().WithMaximumPairingsPerIndividual(1).Generate();
        BreedingPlan  plan            = studbook.PlanUnder(onePairingEach);
        plan.Propose(anySire.Id, anyDam.Id);

        // Act
        Action proposeSecond = () => plan.Propose(anySire.Id, anyOtherDam.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(proposeSecond);
        Assert.Contains($"{anySire.Id} already has 1 pairing", refusal.Reason);
    }

    [Fact]
    public void Withdrawing_a_pair_frees_its_partners_pairing_quota() {
        // Arrange
        Studbook      studbook       = new();
        Individual    anySire        = studbook.Male();
        Individual    anyDam         = studbook.Female();
        Individual    anyOtherDam    = studbook.Female();
        KinshipPolicy onePairingEach = Studbook.AnyPolicy().WithMaximumPairingsPerIndividual(1).Generate();
        BreedingPlan  plan           = studbook.PlanUnder(onePairingEach);
        BreedingPair  firstChoice    = plan.Propose(anySire.Id, anyDam.Id);
        plan.Withdraw(firstChoice);

        // Act
        BreedingPair secondChoice = plan.Propose(anySire.Id, anyOtherDam.Id);

        // Assert
        Assert.Equal([secondChoice], plan.Pairs);
    }

}
