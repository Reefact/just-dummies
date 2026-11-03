using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;
using CaptiveBreeding.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class BreedingPlanTests {

    // ---- a new plan -----------------------------------------------------------------------------

    [Fact]
    public void A_new_plan_is_a_draft_without_any_pair() {
        // Act
        BreedingPlan anyPlan = Any.BreedingPlan().Generate();

        // Assert
        Assert.Equal(BreedingPlanStatus.Draft, anyPlan.Status);
        Assert.Empty(anyPlan.Pairs);
    }

    [Fact]
    public void A_plan_keeps_its_pedigree_its_policy_and_its_season_start() {
        // Arrange
        Pedigree     anyPedigree    = Any.Pedigree().Generate();
        KinshipPolicy anyPolicy     = Any.KinshipPolicy().Generate();
        DateOnly     anySeasonStart = Any.DateOnly().Generate();

        // Act
        BreedingPlan plan = new BreedingPlan(anyPedigree, anyPolicy, anySeasonStart);

        // Assert
        Assert.Same(anyPedigree, plan.Pedigree);
        Assert.Same(anyPolicy, plan.Policy);
        Assert.Equal(anySeasonStart, plan.SeasonStart);
    }

    [Fact]
    public void A_plan_needs_a_pedigree_and_a_policy() {
        Assert.Throws<ArgumentNullException>(() => new BreedingPlan(null!, Any.KinshipPolicy().Generate(), Any.DateOnly().Generate()));
        Assert.Throws<ArgumentNullException>(() => new BreedingPlan(Any.Pedigree().Generate(), null!, Any.DateOnly().Generate()));
    }

    [Fact]
    public void An_empty_plan_expects_no_inbreeding() {
        Assert.Equal(0.0, Any.BreedingPlan().Generate().ExpectedOffspringInbreeding);
    }

    // ---- proposing ----------------------------------------------------------------------------------

    [Fact]
    public void A_proposed_pair_of_unrelated_adults_joins_the_plan() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        BreedingPair pair = plan.Propose(sire.Id, dam.Id);

        // Assert
        Assert.Same(pair, Assert.Single(plan.Pairs));
        Assert.Equal(sire.Id, pair.SireId);
        Assert.Equal(dam.Id, pair.DamId);
        Assert.Equal(0.0, pair.Kinship.Value);
        Assert.Equal(BreedingPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void A_proposed_pair_carries_the_kinship_of_its_partners() {
        // Arrange
        Herd         herd    = new Herd();
        Individual   sire    = herd.Male();
        Individual   dam     = herd.Female();
        Individual   brother = herd.Son(sire, dam);
        Individual   sister  = herd.Daughter(sire, dam);
        BreedingPlan plan    = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate());

        // Act
        BreedingPair pair = plan.Propose(brother.Id, sister.Id);

        // Assert
        Assert.Equal(0.25, pair.Kinship.Value);
    }

    [Fact]
    public void A_pair_whose_kinship_equals_the_limit_is_accepted() {
        // Arrange
        Herd         herd       = new Herd();
        Individual   sire       = herd.Male();
        Individual   firstDam   = herd.Female();
        Individual   secondDam  = herd.Female();
        Individual   brother    = herd.Son(sire, firstDam);
        Individual   halfSister = herd.Daughter(sire, secondDam);
        BreedingPlan plan       = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.125)).Generate());

        // Act
        BreedingPair pair = plan.Propose(brother.Id, halfSister.Id);

        // Assert
        Assert.Equal(0.125, pair.Kinship.Value);
    }

    [Fact]
    public void A_pair_whose_kinship_exceeds_the_limit_is_refused() {
        // Arrange
        Herd         herd    = new Herd();
        Individual   sire    = herd.Male();
        Individual   dam     = herd.Female();
        Individual   brother = herd.Son(sire, dam);
        Individual   sister  = herd.Daughter(sire, dam);
        BreedingPlan plan    = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.125)).Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(brother.Id, sister.Id));

        // Assert
        Assert.Equal(brother.Id, refusal.SireId);
        Assert.Equal(sister.Id, refusal.DamId);
        Assert.Contains("kinship", refusal.Reason);
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_pair_of_two_unrelated_individuals_is_accepted_even_under_a_zero_limit() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(KinshipCoefficient.Unrelated).Generate());

        // Assert
        Assert.Equal(0.0, plan.Propose(sire.Id, dam.Id).Kinship.Value);
    }

    [Fact]
    public void A_female_cannot_be_proposed_as_the_sire() {
        // Arrange
        Herd         herd        = new Herd();
        Individual   femaleSire  = herd.Female();
        Individual   dam         = herd.Female();
        BreedingPlan plan        = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(femaleSire.Id, dam.Id));

        // Assert
        Assert.Contains("not male", refusal.Reason);
        Assert.Contains(femaleSire.Id.Value, refusal.Reason);
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_male_cannot_be_proposed_as_the_dam() {
        // Arrange
        Herd         herd     = new Herd();
        Individual   sire     = herd.Male();
        Individual   maleDam  = herd.Male();
        BreedingPlan plan     = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, maleDam.Id));

        // Assert
        Assert.Contains("not female", refusal.Reason);
        Assert.Contains(maleDam.Id.Value, refusal.Reason);
    }

    [Fact]
    public void A_dead_sire_cannot_be_proposed() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        herd.Dies(sire);
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.Contains("alive", refusal.Reason);
    }

    [Fact]
    public void A_dead_dam_cannot_be_proposed() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        herd.Dies(dam);
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.Contains("alive", refusal.Reason);
    }

    [Fact]
    public void A_sire_born_less_than_the_minimum_breeding_age_before_the_season_is_refused() {
        // Arrange: a policy of 2 to 20 years, the sire two years old on the eve of its second birthday
        Herd         herd     = new Herd();
        Individual   dam      = herd.Female();
        Individual   tooYoung = herd.Founder(Sex.Male, herd.SeasonStart.AddYears(-Herd.MinimumBreedingAgeYears).AddDays(1));
        BreedingPlan plan     = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(tooYoung.Id, dam.Id));

        // Assert
        Assert.Contains("breeding age", refusal.Reason);
    }

    [Fact]
    public void A_dam_born_less_than_the_minimum_breeding_age_before_the_season_is_refused() {
        // Arrange
        Herd         herd     = new Herd();
        Individual   sire     = herd.Male();
        Individual   tooYoung = herd.Founder(Sex.Female, herd.SeasonStart.AddYears(-Herd.MinimumBreedingAgeYears).AddDays(1));
        BreedingPlan plan     = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, tooYoung.Id));

        // Assert
        Assert.Contains("breeding age", refusal.Reason);
    }

    [Fact]
    public void An_individual_turning_the_minimum_breeding_age_on_the_season_start_is_accepted() {
        // Arrange
        Herd         herd        = new Herd();
        Individual   dam         = herd.Female();
        Individual   justOfAge   = herd.Founder(Sex.Male, herd.SeasonStart.AddYears(-Herd.MinimumBreedingAgeYears));
        BreedingPlan plan        = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.NotNull(plan.Propose(justOfAge.Id, dam.Id));
    }

    [Fact]
    public void An_individual_older_than_the_maximum_breeding_age_is_refused() {
        // Arrange: it turned 21 the day before the season, so it is past the 20 years the policy allows
        Herd         herd    = new Herd();
        Individual   sire    = herd.Male();
        Individual   tooOld  = herd.Founder(Sex.Female, herd.SeasonStart.AddYears(-(Herd.MaximumBreedingAgeYears + 1)).AddDays(-1));
        BreedingPlan plan    = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, tooOld.Id));

        // Assert
        Assert.Contains("breeding age", refusal.Reason);
    }

    [Fact]
    public void An_individual_still_twenty_on_the_season_start_is_accepted() {
        // Arrange: its twenty-first birthday falls the day after the season starts
        Herd         herd     = new Herd();
        Individual   sire     = herd.Male();
        Individual   oldest   = herd.Founder(Sex.Female, herd.SeasonStart.AddYears(-(Herd.MaximumBreedingAgeYears + 1)).AddDays(1));
        BreedingPlan plan     = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.NotNull(plan.Propose(sire.Id, oldest.Id));
    }

    [Fact]
    public void A_pair_already_in_the_plan_cannot_be_proposed_again() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(new StudbookId(sire.Id.Value), new StudbookId(dam.Id.Value)));

        // Assert
        Assert.Contains("already in the plan", refusal.Reason);
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void A_sire_may_take_part_in_as_many_pairings_as_the_policy_allows_and_no_more() {
        // Arrange: two pairings per individual
        Herd         herd      = new Herd();
        Individual   sire      = herd.Male();
        Individual   firstDam  = herd.Female();
        Individual   secondDam = herd.Female();
        Individual   thirdDam  = herd.Female();
        BreedingPlan plan      = herd.PlanWith(herd.AnyPolicy().WithMaximumPairingsPerIndividual(2).Generate());
        plan.Propose(sire.Id, firstDam.Id);
        plan.Propose(sire.Id, secondDam.Id);

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, thirdDam.Id));

        // Assert
        Assert.Contains(sire.Id.Value, refusal.Reason);
        Assert.Contains("2 pairing(s)", refusal.Reason);
        Assert.Equal(2, plan.Pairs.Count);
    }

    [Fact]
    public void A_dam_may_take_part_in_as_many_pairings_as_the_policy_allows_and_no_more() {
        // Arrange
        Herd         herd       = new Herd();
        Individual   dam        = herd.Female();
        Individual   firstSire  = herd.Male();
        Individual   secondSire = herd.Male();
        Individual   thirdSire  = herd.Male();
        BreedingPlan plan       = herd.PlanWith(herd.AnyPolicy().WithMaximumPairingsPerIndividual(2).Generate());
        plan.Propose(firstSire.Id, dam.Id);
        plan.Propose(secondSire.Id, dam.Id);

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(thirdSire.Id, dam.Id));

        // Assert
        Assert.Contains(dam.Id.Value, refusal.Reason);
        Assert.Equal(2, plan.Pairs.Count);
    }

    [Fact]
    public void Under_a_quota_of_one_nobody_is_paired_twice() {
        // Arrange
        Herd         herd      = new Herd();
        Individual   sire      = herd.Male();
        Individual   firstDam  = herd.Female();
        Individual   secondDam = herd.Female();
        BreedingPlan plan      = herd.PlanWith(herd.AnyPolicy().WithMaximumPairingsPerIndividual(1).Generate());
        plan.Propose(sire.Id, firstDam.Id);

        // Assert
        Assert.Throws<PairingRefusedException>(() => plan.Propose(sire.Id, secondDam.Id));
    }

    [Fact]
    public void An_individual_who_is_not_in_the_studbook_cannot_be_proposed() {
        // Arrange
        Herd         herd = new Herd();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Throws<KeyNotFoundException>(() => plan.Propose(herd.UnusedId(), dam.Id));
    }

    [Fact]
    public void Proposing_nobody_is_refused() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Throws<ArgumentNullException>(() => plan.Propose(null!, sire.Id));
        Assert.Throws<ArgumentNullException>(() => plan.Propose(sire.Id, null!));
    }

    [Fact]
    public void The_refusal_names_the_sire_the_dam_and_the_reason_in_its_message() {
        // Arrange
        Herd         herd       = new Herd();
        Individual   femaleSire = herd.Female();
        Individual   dam        = herd.Female();
        BreedingPlan plan       = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(() => plan.Propose(femaleSire.Id, dam.Id));

        // Assert
        Assert.Equal($"Pairing {femaleSire.Id} x {dam.Id} refused: {refusal.Reason}", refusal.Message);
    }

    // ---- withdrawing ----------------------------------------------------------------------------------

    [Fact]
    public void A_withdrawn_pair_leaves_the_plan_and_frees_its_partners() {
        // Arrange: a quota of one, so freeing the sire is observable
        Herd         herd      = new Herd();
        Individual   sire      = herd.Male();
        Individual   firstDam  = herd.Female();
        Individual   secondDam = herd.Female();
        BreedingPlan plan      = herd.PlanWith(herd.AnyPolicy().WithMaximumPairingsPerIndividual(1).Generate());
        BreedingPair pair      = plan.Propose(sire.Id, firstDam.Id);

        // Act
        plan.Withdraw(pair);
        BreedingPair replacement = plan.Propose(sire.Id, secondDam.Id);

        // Assert
        Assert.Same(replacement, Assert.Single(plan.Pairs));
    }

    [Fact]
    public void A_pair_is_withdrawn_by_its_sire_and_dam_whatever_its_recorded_kinship() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);
        BreedingPair sameCouple = new BreedingPair(sire.Id, dam.Id, new KinshipCoefficient(0.5));

        // Act
        plan.Withdraw(sameCouple);

        // Assert
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_pair_that_is_not_in_the_plan_cannot_be_withdrawn() {
        // Arrange
        Herd         herd = new Herd();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        BreedingPair anyOutsider = Any.BreedingPair().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => plan.Withdraw(anyOutsider));
    }

    [Fact]
    public void Withdrawing_nothing_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.BreedingPlan().Generate().Withdraw(null!));
    }

    // ---- approving ----------------------------------------------------------------------------------

    [Fact]
    public void A_plan_with_a_pair_can_be_approved() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);

        // Act
        plan.Approve();

        // Assert
        Assert.Equal(BreedingPlanStatus.Approved, plan.Status);
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void An_empty_plan_cannot_be_approved() {
        // Arrange
        BreedingPlan anyPlan = Any.BreedingPlan().Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => anyPlan.Approve());
        Assert.Equal(BreedingPlanStatus.Draft, anyPlan.Status);
    }

    [Fact]
    public void A_plan_that_was_emptied_again_cannot_be_approved() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Withdraw(plan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.Throws<InvalidOperationException>(() => plan.Approve());
    }

    [Fact]
    public void An_approved_plan_accepts_no_further_proposal() {
        // Arrange
        Herd         herd      = new Herd();
        Individual   sire      = herd.Male();
        Individual   dam       = herd.Female();
        Individual   otherSire = herd.Male();
        Individual   otherDam  = herd.Female();
        BreedingPlan plan      = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);
        plan.Approve();

        // Assert
        Assert.Throws<InvalidOperationException>(() => plan.Propose(otherSire.Id, otherDam.Id));
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void An_approved_plan_accepts_no_withdrawal() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        BreedingPair pair = plan.Propose(sire.Id, dam.Id);
        plan.Approve();

        // Assert
        Assert.Throws<InvalidOperationException>(() => plan.Withdraw(pair));
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void An_approved_plan_cannot_be_approved_again() {
        // Arrange
        Herd         herd = new Herd();
        Individual   sire = herd.Male();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());
        plan.Propose(sire.Id, dam.Id);
        plan.Approve();

        // Assert
        Assert.Throws<InvalidOperationException>(() => plan.Approve());
    }

    // ---- expected inbreeding --------------------------------------------------------------------------

    [Fact]
    public void The_expected_inbreeding_is_the_mean_kinship_of_the_pairs() {
        // Arrange: one pair of full siblings (a quarter) and one of unrelated founders (nothing)
        Herd         herd         = new Herd();
        Individual   sire         = herd.Male();
        Individual   dam          = herd.Female();
        Individual   brother      = herd.Son(sire, dam);
        Individual   sister       = herd.Daughter(sire, dam);
        Individual   unrelatedMale   = herd.Male();
        Individual   unrelatedFemale = herd.Female();
        BreedingPlan plan         = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate());
        plan.Propose(brother.Id, sister.Id);
        plan.Propose(unrelatedMale.Id, unrelatedFemale.Id);

        // Assert
        Assert.Equal(0.125, plan.ExpectedOffspringInbreeding, precision: 12);
    }

    [Fact]
    public void The_expected_inbreeding_follows_a_withdrawal() {
        // Arrange
        Herd         herd         = new Herd();
        Individual   sire         = herd.Male();
        Individual   dam          = herd.Female();
        Individual   brother      = herd.Son(sire, dam);
        Individual   sister       = herd.Daughter(sire, dam);
        Individual   unrelatedMale   = herd.Male();
        Individual   unrelatedFemale = herd.Female();
        BreedingPlan plan         = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate());
        BreedingPair siblings     = plan.Propose(brother.Id, sister.Id);
        plan.Propose(unrelatedMale.Id, unrelatedFemale.Id);

        // Act
        plan.Withdraw(siblings);

        // Assert
        Assert.Equal(0.0, plan.ExpectedOffspringInbreeding);
    }

    // ---- ranking the sires ----------------------------------------------------------------------------

    [Fact]
    public void Sires_are_ranked_from_the_least_related_to_the_dam() {
        // Arrange: the dam's father (a quarter), a half-brother (an eighth) and an unrelated male (nothing)
        Herd         herd        = new Herd();
        Individual   father      = herd.Male();
        Individual   mother      = herd.Female();
        Individual   dam         = herd.Daughter(father, mother);
        Individual   fathersMate = herd.Female();
        Individual   halfBrother = herd.Son(father, fathersMate);
        Individual   stranger    = herd.Male();
        BreedingPlan plan        = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.25)).Generate());

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam.Id);

        // Assert
        Assert.Equal([stranger.Id, halfBrother.Id, father.Id], ranking.Select(pair => pair.SireId));
        Assert.All(ranking, pair => Assert.Equal(dam.Id, pair.DamId));
        Assert.Equal<double>([0.0, 0.125, 0.25], ranking.Select(pair => pair.Kinship.Value));
    }

    [Fact]
    public void Sires_equally_related_to_the_dam_are_ranked_by_their_mean_kinship() {
        // Arrange: both males are unrelated to the dam; the first has two living daughters, which makes him the more represented
        Herd         herd           = new Herd();
        Individual   dam            = herd.Female();
        Individual   wellRepresented = herd.Male();
        Individual   underRepresented = herd.Male();
        Individual   mate           = herd.Female();
        herd.Daughter(wellRepresented, mate);
        herd.Daughter(wellRepresented, mate);
        BreedingPlan plan           = herd.PlanWith(herd.AnyPolicy().Generate());

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam.Id);

        // Assert
        Assert.Equal([underRepresented.Id, wellRepresented.Id], ranking.Select(pair => pair.SireId));
    }

    [Fact]
    public void Sires_who_could_not_be_proposed_are_left_out_of_the_ranking() {
        // Arrange: of the six males, only the unrelated living adult is eligible
        Herd         herd        = new Herd();
        Individual   father      = herd.Male();
        Individual   mother      = herd.Female();
        Individual   dam         = herd.Daughter(father, mother);
        Individual   eligible    = herd.Male();
        Individual   dead        = herd.Male();
        herd.Dies(dead);
        herd.Founder(Sex.Male, herd.SeasonStart.AddYears(-Herd.MinimumBreedingAgeYears).AddDays(1));                         // one day short of the minimum age
        herd.Founder(Sex.Male, herd.SeasonStart.AddYears(-(Herd.MaximumBreedingAgeYears + 1)).AddDays(-1));                   // past the maximum age
        herd.Son(father, mother);                                                                                             // the dam's full brother, above the limit
        BreedingPlan plan        = herd.PlanWith(herd.AnyPolicy().WithMaximumPairKinship(new KinshipCoefficient(0.125)).Generate());

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam.Id);

        // Assert
        Assert.Equal(eligible.Id, Assert.Single(ranking).SireId);
    }

    [Fact]
    public void A_dam_who_cannot_be_proposed_has_no_sire_to_rank() {
        // Arrange
        Herd         herd = new Herd();
        Individual   dam  = herd.Female();
        herd.Male();
        herd.Dies(dam);
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Empty(plan.RankSiresFor(dam.Id));
    }

    [Fact]
    public void A_male_has_no_sire_to_rank_for_it() {
        // Arrange
        Herd         herd = new Herd();
        Individual   male = herd.Male();
        herd.Male();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Empty(plan.RankSiresFor(male.Id));
    }

    [Fact]
    public void A_studbook_without_a_living_male_leaves_nothing_to_rank() {
        // Arrange
        Herd         herd = new Herd();
        Individual   dam  = herd.Female();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Empty(plan.RankSiresFor(dam.Id));
    }

    [Fact]
    public void Sires_cannot_be_ranked_for_a_dam_who_is_not_in_the_studbook() {
        // Arrange
        Herd         herd = new Herd();
        herd.Male();
        BreedingPlan plan = herd.PlanWith(herd.AnyPolicy().Generate());

        // Assert
        Assert.Throws<KeyNotFoundException>(() => plan.RankSiresFor(herd.UnusedId()));
    }

    [Fact]
    public void Ranking_the_sires_of_nobody_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Any.BreedingPlan().Generate().RankSiresFor(null!));
    }

}
