using CaptiveBreeding.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace CaptiveBreeding.Tests.Regular;

[Reproducible]
public sealed class KinshipPolicyTests {

    [Fact]
    public void An_animal_reaches_breeding_age_on_its_birthday_not_the_day_before() {
        // Arrange
        KinshipPolicy anyPolicy  = Arbitrary.KinshipPolicy().WithMinimumBreedingAgeYears(3).Generate();
        Individual    anyAnimal  = Arbitrary.Individual().WithBirthDate(d => d.Between(new DateOnly(2000, 1, 1), new DateOnly(2030, 12, 31))).Generate();
        DateOnly      birthday   = anyAnimal.BirthDate.AddYears(3);

        // Act
        bool dayBefore = anyPolicy.IsOfBreedingAge(anyAnimal, birthday.AddDays(-1));
        bool onTheDay  = anyPolicy.IsOfBreedingAge(anyAnimal, birthday);

        // Assert
        Assert.False(dayBefore);
        Assert.True(onTheDay);
    }

    [Fact]
    public void A_policy_cannot_tolerate_pairs_more_related_than_full_siblings() {
        // Arrange
        KinshipCoefficient anyAboveFullSiblings = Arbitrary.KinshipCoefficient().WithValue(v => v.GreaterThan(0.25)).Generate();

        // Act
        Func<KinshipPolicy> policy = () => Arbitrary.KinshipPolicy().WithMaximumPairKinship(anyAboveFullSiblings).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(policy);
    }

}

[Reproducible]
public sealed class BreedingPlanTests {

    // Founders are born 2000-2005 and offspring a few years after their parents, so a season in
    // the 2030s finds every animal of the default policy's breeding age once its minimum is low.
    private static AnyBreedingPlan Plan(Studbook studbook, Func<AnyKinshipPolicy, AnyKinshipPolicy> policy) {
        return Arbitrary.BreedingPlan()
                        .WithPedigree(studbook.Pedigree)
                        .WithPolicy(p => policy(p.WithMinimumBreedingAgeYears(m => m.LessThanOrEqualTo(5))))
                        .WithSeasonStart(d => d.Between(new DateOnly(2030, 1, 1), new DateOnly(2034, 12, 31)));
    }

    private static AnyBreedingPlan Plan(Studbook studbook) {
        return Plan(studbook, p => p);
    }

    [Fact]
    public void Two_unrelated_founders_make_a_pair_with_no_kinship() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   anySire  = studbook.Founder(Sex.Male);
        Individual   anyDam   = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = Plan(studbook).Generate();

        // Act
        BreedingPair pair = anyPlan.Propose(anySire.Id, anyDam.Id);

        // Assert
        Assert.Equal(KinshipCoefficient.Unrelated, pair.Kinship);
        Assert.Contains(pair, anyPlan.Pairs);
    }

    [Fact]
    public void Full_siblings_are_refused_under_a_limit_below_a_quarter() {
        // Arrange
        Studbook     studbook   = new Studbook();
        Individual   anySire    = studbook.Founder(Sex.Male);
        Individual   anyDam     = studbook.Founder(Sex.Female);
        Individual   anyBrother = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual   anySister  = studbook.Offspring(anySire, anyDam, Sex.Female);
        BreedingPlan anyPlan    = Plan(studbook, p => p.WithMaximumPairKinship(new KinshipCoefficient(0.125))).Generate();

        // Act
        Func<BreedingPair> propose = () => anyPlan.Propose(anyBrother.Id, anySister.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("exceeds the limit", refusal.Reason);
    }

    [Fact]
    public void A_dead_partner_is_refused() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   anySire  = studbook.Founder(Sex.Male);
        Individual   anyDam   = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = Plan(studbook).Generate();
        anyDam.RecordDeath(anyPlan.SeasonStart.AddDays(-1));

        // Act
        Func<BreedingPair> propose = () => anyPlan.Propose(anySire.Id, anyDam.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Equal(anyDam.Id, refusal.DamId);
    }

    [Fact]
    public void A_female_proposed_as_sire_is_refused() {
        // Arrange
        Studbook     studbook       = new Studbook();
        Individual   anyFemale      = studbook.Founder(Sex.Female);
        Individual   anyOtherFemale = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan        = Plan(studbook).Generate();

        // Act
        Func<BreedingPair> propose = () => anyPlan.Propose(anyFemale.Id, anyOtherFemale.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("not male", refusal.Reason);
    }

    [Fact]
    public void A_partner_too_young_at_the_start_of_the_season_is_refused() {
        // Arrange
        Studbook     studbook  = new Studbook();
        Individual   anySire   = studbook.Founder(Sex.Male);
        Individual   anyDam    = studbook.Founder(Sex.Female);
        Individual   anyYoung  = studbook.Offspring(anySire, anyDam, Sex.Female);
        Individual   anyMale   = studbook.Founder(Sex.Male);
        BreedingPlan anyPlan   = Arbitrary.BreedingPlan()
                                          .WithPedigree(studbook.Pedigree)
                                          .WithPolicy(p => p.WithMinimumBreedingAgeYears(2))
                                          .WithSeasonStart(anyYoung.BirthDate.AddYears(1))
                                          .Generate();

        // Act
        Func<BreedingPair> propose = () => anyPlan.Propose(anyMale.Id, anyYoung.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(propose);
        Assert.Contains("breeding age", refusal.Reason);
    }

    [Fact]
    public void The_same_pair_cannot_be_proposed_twice() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   anySire  = studbook.Founder(Sex.Male);
        Individual   anyDam   = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = Plan(studbook).Generate();
        anyPlan.Propose(anySire.Id, anyDam.Id);

        // Act
        Func<BreedingPair> again = () => anyPlan.Propose(anySire.Id, anyDam.Id);

        // Assert
        Assert.Throws<PairingRefusedException>(again);
    }

    [Fact]
    public void A_sire_cannot_exceed_its_pairing_quota() {
        // Arrange
        Studbook     studbook    = new Studbook();
        Individual   anySire     = studbook.Founder(Sex.Male);
        Individual   anyDam      = studbook.Founder(Sex.Female);
        Individual   anyOtherDam = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan     = Plan(studbook, p => p.WithMaximumPairingsPerIndividual(1)).Generate();
        anyPlan.Propose(anySire.Id, anyDam.Id);

        // Act
        Func<BreedingPair> secondPairing = () => anyPlan.Propose(anySire.Id, anyOtherDam.Id);

        // Assert
        PairingRefusedException refusal = Assert.Throws<PairingRefusedException>(secondPairing);
        Assert.Contains(anySire.Id.ToString(), refusal.Reason);
    }

    [Fact]
    public void An_empty_plan_cannot_be_approved() {
        // Arrange
        BreedingPlan anyPlan = Plan(new Studbook()).Generate();

        // Act
        Action approve = anyPlan.Approve;

        // Assert
        Assert.Throws<InvalidOperationException>(approve);
        Assert.Equal(BreedingPlanStatus.Draft, anyPlan.Status);
    }

    [Fact]
    public void An_approved_plan_no_longer_accepts_withdrawals() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   anySire  = studbook.Founder(Sex.Male);
        Individual   anyDam   = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = Plan(studbook).Generate();
        BreedingPair pair     = anyPlan.Propose(anySire.Id, anyDam.Id);
        anyPlan.Approve();

        // Act
        Action withdraw = () => anyPlan.Withdraw(pair);

        // Assert
        Assert.Throws<InvalidOperationException>(withdraw);
        Assert.Single(anyPlan.Pairs);
    }

    [Fact]
    public void A_withdrawn_pair_leaves_the_plan() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   anySire  = studbook.Founder(Sex.Male);
        Individual   anyDam   = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = Plan(studbook).Generate();
        BreedingPair pair     = anyPlan.Propose(anySire.Id, anyDam.Id);

        // Act
        anyPlan.Withdraw(pair);

        // Assert
        Assert.Empty(anyPlan.Pairs);
    }

    [Fact]
    public void Expected_offspring_inbreeding_is_the_mean_pair_kinship() {
        // Arrange: an unrelated pair (0) and a pair of half siblings (0.125).
        Studbook     studbook        = new Studbook();
        Individual   anySire         = studbook.Founder(Sex.Male);
        Individual   anyDam          = studbook.Founder(Sex.Female);
        Individual   anyOtherDam     = studbook.Founder(Sex.Female);
        Individual   anyHalfBrother  = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual   anyHalfSister   = studbook.Offspring(anySire, anyOtherDam, Sex.Female);
        Individual   anyUnrelatedMale = studbook.Founder(Sex.Male);
        Individual   anyUnrelatedDam  = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan         = Plan(studbook, p => p.WithMaximumPairKinship(new KinshipCoefficient(0.125))).Generate();
        anyPlan.Propose(anyHalfBrother.Id, anyHalfSister.Id);
        anyPlan.Propose(anyUnrelatedMale.Id, anyUnrelatedDam.Id);

        // Act
        double expected = anyPlan.ExpectedOffspringInbreeding;

        // Assert
        Assert.Equal(0.0625, expected);
    }

    [Fact]
    public void An_unrelated_male_ranks_before_the_dams_half_brother() {
        // Arrange
        Studbook     studbook         = new Studbook();
        Individual   anySire          = studbook.Founder(Sex.Male);
        Individual   anyDam           = studbook.Founder(Sex.Female);
        Individual   anyOtherDam      = studbook.Founder(Sex.Female);
        Individual   anyHalfBrother   = studbook.Offspring(anySire, anyOtherDam, Sex.Male);
        Individual   anyFemale        = studbook.Offspring(anySire, anyDam, Sex.Female);
        Individual   anyUnrelatedMale = studbook.Founder(Sex.Male);
        anySire.RecordDeath(anyFemale.BirthDate.AddYears(1));
        BreedingPlan anyPlan          = Plan(studbook, p => p.WithMaximumPairKinship(new KinshipCoefficient(0.125))).Generate();

        // Act
        IReadOnlyList<BreedingPair> ranking = anyPlan.RankSiresFor(anyFemale.Id);

        // Assert
        Assert.Equal(new[] { anyUnrelatedMale.Id, anyHalfBrother.Id }, ranking.Select(p => p.SireId));
    }

    [Fact]
    public void Ranking_leaves_out_males_above_the_kinship_limit() {
        // Arrange
        Studbook     studbook   = new Studbook();
        Individual   anySire    = studbook.Founder(Sex.Male);
        Individual   anyDam     = studbook.Founder(Sex.Female);
        Individual   anyBrother = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual   anySister  = studbook.Offspring(anySire, anyDam, Sex.Female);
        anySire.RecordDeath(anySister.BirthDate.AddYears(1));
        BreedingPlan anyPlan    = Plan(studbook, p => p.WithMaximumPairKinship(new KinshipCoefficient(0.125))).Generate();

        // Act
        IReadOnlyList<BreedingPair> ranking = anyPlan.RankSiresFor(anySister.Id);

        // Assert
        Assert.Empty(ranking);
    }

}

[Reproducible]
public sealed class BreedingPairTests {

    [Fact]
    public void Two_pairs_with_the_same_partners_are_equal_whatever_their_recorded_kinship() {
        // Arrange
        BreedingPair anyPair       = Arbitrary.BreedingPair().Generate();
        BreedingPair anyReassessed = Arbitrary.BreedingPair().WithSireId(anyPair.SireId).WithDamId(anyPair.DamId).Generate();

        // Act
        bool equal = anyPair.Equals(anyReassessed);

        // Assert
        Assert.True(equal);
    }

    [Fact]
    public void An_individual_cannot_be_paired_with_itself() {
        // Arrange
        StudbookId anyId = Arbitrary.StudbookId().Generate();

        // Act
        Func<BreedingPair> pairWithItself = () => Arbitrary.BreedingPair().WithSireId(anyId).WithDamId(anyId).Generate();

        // Assert
        Assert.Throws<ArgumentException>(pairWithItself);
    }

}
