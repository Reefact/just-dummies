using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Generators;
using CaptiveBreeding.Tests.Support;
using JustDummies;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class BreedingPlanTests {

    private static readonly DateOnly SeasonStart = new DateOnly(2025, 1, 1);

    private static readonly DateOnly AdultBirth = new DateOnly(2015, 6, 1);

    /// <summary>A policy where age is the only thing that matters: breeding from 2 to 15 years old.</summary>
    private static KinshipPolicy AgePolicy() {
        return Any.KinshipPolicy()
                  .WithMinimumBreedingAgeYears(2)
                  .WithMaximumBreedingAgeYears(15)
                  .Generate();
    }

    /// <summary>A policy where the kinship limit is the only thing that matters.</summary>
    private static KinshipPolicy KinshipLimitPolicy(double maximumKinship) {
        return Any.KinshipPolicy()
                  .WithMaximumPairKinship(new KinshipCoefficient(maximumKinship))
                  .WithMinimumBreedingAgeYears(2)
                  .WithMaximumBreedingAgeYears(15)
                  .Generate();
    }

    /// <summary>A policy where the number of pairings per individual is the only thing that matters.</summary>
    private static KinshipPolicy QuotaPolicy(int pairings) {
        return Any.KinshipPolicy()
                  .WithMaximumPairingsPerIndividual(pairings)
                  .WithMinimumBreedingAgeYears(2)
                  .WithMaximumBreedingAgeYears(15)
                  .Generate();
    }

    private static BreedingPlan PlanFor(Herd herd, KinshipPolicy policy) {
        return new BreedingPlan(herd.Pedigree, policy, SeasonStart);
    }

    private static PairingRefusedException Refusal(Func<BreedingPair> propose) {
        return Assert.Throws<PairingRefusedException>(propose);
    }

    // ---- Construction ---------------------------------------------------------------------

    [Fact]
    public void A_new_plan_is_an_empty_draft() {
        // Arrange
        Herd         herd   = new Herd();
        KinshipPolicy policy = AgePolicy();

        // Act
        BreedingPlan plan = new BreedingPlan(herd.Pedigree, policy, SeasonStart);

        // Assert
        Assert.Same(herd.Pedigree, plan.Pedigree);
        Assert.Same(policy, plan.Policy);
        Assert.Equal(SeasonStart, plan.SeasonStart);
        Assert.Equal(BreedingPlanStatus.Draft, plan.Status);
        Assert.Empty(plan.Pairs);
        Assert.Equal(0.0, plan.ExpectedOffspringInbreeding);
    }

    [Fact]
    public void A_plan_requires_a_pedigree_and_a_policy() {
        // Arrange
        Herd          herd   = new Herd();
        KinshipPolicy policy = AgePolicy();

        // Act
        Action noPedigree = () => new BreedingPlan(null!, policy, SeasonStart);
        Action noPolicy   = () => new BreedingPlan(herd.Pedigree, null!, SeasonStart);

        // Assert
        Assert.Throws<ArgumentNullException>(noPedigree);
        Assert.Throws<ArgumentNullException>(noPolicy);
    }

    // ---- Propose: accepted ----------------------------------------------------------------

    [Fact]
    public void A_proposed_pair_of_unrelated_adults_joins_the_plan_with_its_kinship() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        BreedingPair pair = plan.Propose(sire, dam);

        // Assert
        Assert.Equal(sire, pair.SireId);
        Assert.Equal(dam, pair.DamId);
        Assert.Equal(0.0, pair.Kinship.Value);
        Assert.Same(pair, Assert.Single(plan.Pairs));
    }

    [Fact]
    public void A_pair_whose_kinship_equals_the_limit_is_accepted() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, new DateOnly(2000, 1, 1));
        StudbookId dam     = herd.Founder(Sex.Female, new DateOnly(2000, 1, 1));
        StudbookId brother = herd.Offspring(Sex.Male, AdultBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, AdultBirth, sire, dam);
        BreedingPlan plan  = PlanFor(herd, KinshipLimitPolicy(0.25));

        // Act
        BreedingPair pair = plan.Propose(brother, sister);

        // Assert
        Assert.Equal(0.25, pair.Kinship.Value);
    }

    [Theory]
    [InlineData("2023-01-01", true)]  // exactly two years old on the first day of the season
    [InlineData("2023-01-02", false)] // one day short
    public void A_partner_must_have_reached_the_minimum_breeding_age_at_the_start_of_the_season(string birth, bool accepted) {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, DateOnly.Parse(birth));
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, dam);

        // Assert
        if (accepted) {
            Assert.Equal(sire, propose().SireId);
        } else {
            Refusal(propose);
        }
    }

    [Theory]
    [InlineData("2009-12-31", true)]  // fifteen years and a day on the first day of the season
    [InlineData("2009-01-01", false)] // sixteen
    public void A_partner_must_not_have_passed_the_maximum_breeding_age_at_the_start_of_the_season(string birth, bool accepted) {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, DateOnly.Parse(birth));
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, dam);

        // Assert
        if (accepted) {
            Assert.Equal(dam, propose().DamId);
        } else {
            Refusal(propose);
        }
    }

    [Fact]
    public void Breeding_age_is_judged_at_the_season_start_not_today() {
        // Arrange
        Herd         herd       = new Herd();
        StudbookId   sire       = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam        = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan thisSeason = PlanFor(herd, AgePolicy());
        BreedingPlan farFuture  = new BreedingPlan(herd.Pedigree, AgePolicy(), new DateOnly(2045, 1, 1));

        // Act
        BreedingPair accepted = thisSeason.Propose(sire, dam);
        Func<BreedingPair> tooLate = () => farFuture.Propose(sire, dam);

        // Assert
        Assert.NotNull(accepted);
        Refusal(tooLate);
    }

    // ---- Propose: refused -----------------------------------------------------------------

    [Fact]
    public void A_female_cannot_be_proposed_as_sire() {
        // Arrange
        Herd         herd    = new Herd();
        StudbookId   notSire = herd.Founder(Sex.Female, AdultBirth);
        StudbookId   dam     = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan    = PlanFor(herd, AgePolicy());

        // Act
        PairingRefusedException refusal = Refusal(() => plan.Propose(notSire, dam));

        // Assert
        Assert.Equal(notSire, refusal.SireId);
        Assert.Equal(dam, refusal.DamId);
        Assert.NotEmpty(refusal.Reason);
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_male_cannot_be_proposed_as_dam() {
        // Arrange
        Herd         herd   = new Herd();
        StudbookId   sire   = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   notDam = herd.Founder(Sex.Male, AdultBirth);
        BreedingPlan plan   = PlanFor(herd, AgePolicy());

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, notDam);

        // Assert
        Refusal(propose);
    }

    [Fact]
    public void A_dead_sire_cannot_be_paired() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        herd.Kill(sire, new DateOnly(2024, 6, 1));
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, dam);

        // Assert
        Refusal(propose);
    }

    [Fact]
    public void A_dead_dam_cannot_be_paired() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        herd.Kill(dam, new DateOnly(2024, 6, 1));
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, dam);

        // Assert
        Refusal(propose);
    }

    [Fact]
    public void A_pair_whose_kinship_exceeds_the_limit_is_refused_and_not_recorded() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, new DateOnly(2000, 1, 1));
        StudbookId dam     = herd.Founder(Sex.Female, new DateOnly(2000, 1, 1));
        StudbookId brother = herd.Offspring(Sex.Male, AdultBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, AdultBirth, sire, dam);
        BreedingPlan plan  = PlanFor(herd, KinshipLimitPolicy(0.2));

        // Act
        Func<BreedingPair> propose = () => plan.Propose(brother, sister);

        // Assert
        Refusal(propose);
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_pair_cannot_be_proposed_twice() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, QuotaPolicy(5));
        plan.Propose(sire, dam);

        // Act
        Func<BreedingPair> propose = () => plan.Propose(sire, dam);

        // Assert
        Refusal(propose);
        Assert.Single(plan.Pairs);
    }

    [Fact]
    public void An_individual_cannot_exceed_the_pairings_the_policy_allows() {
        // Arrange
        Herd         herd      = new Herd();
        StudbookId   sire      = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   firstDam  = herd.Founder(Sex.Female, AdultBirth);
        StudbookId   secondDam = herd.Founder(Sex.Female, AdultBirth);
        StudbookId   thirdDam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan      = PlanFor(herd, QuotaPolicy(2));
        plan.Propose(sire, firstDam);
        plan.Propose(sire, secondDam);

        // Act
        PairingRefusedException refusal = Refusal(() => plan.Propose(sire, thirdDam));

        // Assert
        Assert.Equal(2, plan.Pairs.Count);
        Assert.Contains(sire.Value, refusal.Reason);
    }

    [Fact]
    public void The_dam_is_subject_to_the_pairing_quota_too() {
        // Arrange
        Herd         herd       = new Herd();
        StudbookId   firstSire  = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   secondSire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam        = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan       = PlanFor(herd, QuotaPolicy(1));
        plan.Propose(firstSire, dam);

        // Act
        PairingRefusedException refusal = Refusal(() => plan.Propose(secondSire, dam));

        // Assert
        Assert.Contains(dam.Value, refusal.Reason);
    }

    [Fact]
    public void Withdrawing_a_pair_frees_the_pairings_of_both_partners() {
        // Arrange
        Herd         herd       = new Herd();
        StudbookId   firstSire  = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   secondSire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam        = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan       = PlanFor(herd, QuotaPolicy(1));
        BreedingPair first      = plan.Propose(firstSire, dam);

        // Act
        plan.Withdraw(first);
        BreedingPair second = plan.Propose(secondSire, dam);

        // Assert
        Assert.Same(second, Assert.Single(plan.Pairs));
    }

    [Fact]
    public void An_unknown_partner_cannot_be_proposed() {
        // Arrange
        Herd         herd      = new Herd();
        StudbookId   sire      = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam       = herd.Founder(Sex.Female, AdultBirth);
        StudbookId   unknownId = herd.NewUnregisteredId();
        BreedingPlan plan      = PlanFor(herd, AgePolicy());

        // Act
        Action unknownSire = () => plan.Propose(unknownId, dam);
        Action unknownDam  = () => plan.Propose(sire, unknownId);

        // Assert
        Assert.Throws<KeyNotFoundException>(unknownSire);
        Assert.Throws<KeyNotFoundException>(unknownDam);
    }

    [Fact]
    public void Proposing_nobody_is_refused() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   id   = herd.Founder(Sex.Male, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Action noSire = () => plan.Propose(null!, id);
        Action noDam  = () => plan.Propose(id, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(noSire);
        Assert.Throws<ArgumentNullException>(noDam);
    }

    // ---- Withdraw -------------------------------------------------------------------------

    [Fact]
    public void A_withdrawn_pair_leaves_the_plan() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());
        BreedingPair pair = plan.Propose(sire, dam);

        // Act
        plan.Withdraw(pair);

        // Assert
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_pair_is_withdrawn_by_its_sire_and_dam_whatever_the_instance() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());
        plan.Propose(sire, dam);

        // Act
        plan.Withdraw(new BreedingPair(sire, dam, KinshipCoefficient.Unrelated));

        // Assert
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void A_pair_that_is_not_in_the_plan_cannot_be_withdrawn() {
        // Arrange
        Herd         herd         = new Herd();
        BreedingPlan plan         = PlanFor(herd, AgePolicy());
        BreedingPair anyOtherPair = Any.BreedingPair().Generate();

        // Act
        Action act = () => plan.Withdraw(anyOtherPair);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Withdrawing_nothing_is_refused() {
        // Arrange
        BreedingPlan plan = PlanFor(new Herd(), AgePolicy());

        // Act
        Action act = () => plan.Withdraw(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    // ---- Approve --------------------------------------------------------------------------

    [Fact]
    public void An_empty_plan_cannot_be_approved() {
        // Arrange
        BreedingPlan plan = PlanFor(new Herd(), AgePolicy());

        // Act
        Action act = () => plan.Approve();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
        Assert.Equal(BreedingPlanStatus.Draft, plan.Status);
    }

    [Fact]
    public void An_approved_plan_is_frozen() {
        // Arrange
        Herd         herd     = new Herd();
        StudbookId   sire     = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam      = herd.Founder(Sex.Female, AdultBirth);
        StudbookId   otherDam = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan     = PlanFor(herd, AgePolicy());
        BreedingPair pair     = plan.Propose(sire, dam);
        plan.Approve();

        // Act
        Action propose  = () => plan.Propose(sire, otherDam);
        Action withdraw = () => plan.Withdraw(pair);
        Action approve  = () => plan.Approve();

        // Assert
        Assert.Equal(BreedingPlanStatus.Approved, plan.Status);
        Assert.Throws<InvalidOperationException>(propose);
        Assert.Throws<InvalidOperationException>(withdraw);
        Assert.Throws<InvalidOperationException>(approve);
        Assert.Same(pair, Assert.Single(plan.Pairs));
    }

    [Fact]
    public void A_refused_proposal_on_an_approved_plan_reports_the_freeze_before_any_pairing_rule() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   sire = herd.Founder(Sex.Male, AdultBirth);
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());
        plan.Propose(sire, dam);
        plan.Approve();

        // Act
        Action act = () => plan.Propose(sire, dam);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    // ---- Expected inbreeding --------------------------------------------------------------

    [Fact]
    public void The_expected_offspring_inbreeding_is_the_mean_kinship_of_the_pairs() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId parent1 = herd.Founder(Sex.Male, new DateOnly(2000, 1, 1));
        StudbookId parent2 = herd.Founder(Sex.Female, new DateOnly(2000, 1, 1));
        StudbookId brother = herd.Offspring(Sex.Male, AdultBirth, parent1, parent2);
        StudbookId sister  = herd.Offspring(Sex.Female, AdultBirth, parent1, parent2);
        StudbookId stranger    = herd.Founder(Sex.Male, AdultBirth);
        StudbookId strangerDam = herd.Founder(Sex.Female, AdultBirth);
        BreedingPlan plan = PlanFor(herd, KinshipLimitPolicy(0.25));
        plan.Propose(brother, sister);      // 0.25
        plan.Propose(stranger, strangerDam); // 0.0

        // Assert
        Assert.Equal(0.125, plan.ExpectedOffspringInbreeding, 1e-12);
    }

    // ---- Ranking sires --------------------------------------------------------------------

    [Fact]
    public void Sires_are_ranked_by_pair_kinship_then_by_how_under_represented_they_are() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId grandsire = herd.Founder(Sex.Male, new DateOnly(2000, 1, 1));
        StudbookId granddam  = herd.Founder(Sex.Female, new DateOnly(2000, 1, 1));
        StudbookId brother   = herd.Offspring(Sex.Male, AdultBirth, grandsire, granddam);
        StudbookId dam       = herd.Offspring(Sex.Female, AdultBirth, grandsire, granddam);

        StudbookId rareSire     = herd.Founder(Sex.Male, AdultBirth);
        StudbookId commonSire   = herd.Founder(Sex.Male, AdultBirth);
        StudbookId commonSireMate = herd.Founder(Sex.Female, AdultBirth);
        herd.Offspring(Sex.Female, new DateOnly(2022, 1, 1), commonSire, commonSireMate);

        BreedingPlan plan = PlanFor(herd, KinshipLimitPolicy(0.25));

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam);

        // Assert: the two unrelated males first (the one with a living daughter is better represented, so last of them);
        //         then her brother, at kinship 1/4. Her father, born in 2000, is past the breeding age.
        Assert.Equal([rareSire, commonSire, brother], ranking.Select(p => p.SireId));
    }

    [Fact]
    public void Ineligible_males_are_left_out_of_the_ranking() {
        // Arrange
        Herd       herd       = new Herd();
        StudbookId dam        = herd.Founder(Sex.Female, AdultBirth);
        StudbookId eligible   = herd.Founder(Sex.Male, AdultBirth);
        StudbookId dead       = herd.Founder(Sex.Male, AdultBirth);
        StudbookId tooYoung   = herd.Founder(Sex.Male, new DateOnly(2024, 6, 1));
        StudbookId tooOld     = herd.Founder(Sex.Male, new DateOnly(2000, 1, 1));
        herd.Founder(Sex.Female, AdultBirth);
        herd.Kill(dead, new DateOnly(2024, 6, 1));
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam);

        // Assert
        Assert.Equal([eligible], ranking.Select(p => p.SireId));
        Assert.DoesNotContain(ranking, p => p.SireId == tooYoung || p.SireId == tooOld);
    }

    [Fact]
    public void Ranking_sires_for_an_ineligible_dam_gives_nobody() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        herd.Founder(Sex.Male, AdultBirth);
        herd.Kill(dam, new DateOnly(2024, 6, 1));
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        IReadOnlyList<BreedingPair> ranking = plan.RankSiresFor(dam);

        // Assert
        Assert.Empty(ranking);
    }

    [Fact]
    public void Ranking_sires_does_not_add_anything_to_the_plan() {
        // Arrange
        Herd         herd = new Herd();
        StudbookId   dam  = herd.Founder(Sex.Female, AdultBirth);
        herd.Founder(Sex.Male, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        plan.RankSiresFor(dam);

        // Assert
        Assert.Empty(plan.Pairs);
    }

    [Fact]
    public void Ranking_sires_for_an_unknown_dam_is_refused() {
        // Arrange
        Herd         herd = new Herd();
        herd.Founder(Sex.Male, AdultBirth);
        BreedingPlan plan = PlanFor(herd, AgePolicy());

        // Act
        Action act = () => plan.RankSiresFor(herd.NewUnregisteredId());

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void Ranking_sires_for_nobody_is_refused() {
        // Arrange
        BreedingPlan plan = PlanFor(new Herd(), AgePolicy());

        // Act
        Action act = () => plan.RankSiresFor(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

}
