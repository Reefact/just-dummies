using CaptiveBreeding.Domain;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Discovery;

public sealed class BreedingPlanTests {

    // Every animal in a Studbook is born between 1990 and 2009: a season in the 2030s with a
    // minimum breeding age of at most 20 (and a maximum of at least 51) admits all of them.
    private static AnyBreedingPlan AnyPlanFor(Studbook studbook) {
        return Any.BreedingPlan()
                  .WithPedigree(studbook.Pedigree)
                  .WithPolicy(p => p.WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                  .WithSeasonStart(d => d.Between(new DateOnly(2030, 1, 1), new DateOnly(2035, 12, 31)));
    }

    [Fact]
    public void An_unrelated_pair_is_accepted_with_no_kinship() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).Generate();

        // Act
        BreedingPair pair = anyPlan.Propose(sire.Id, dam.Id);

        // Assert
        Assert.Equal(KinshipCoefficient.Unrelated, pair.Kinship);
        Assert.Contains(pair, anyPlan.Pairs);
    }

    [Fact]
    public void Full_siblings_are_refused_under_a_policy_stricter_than_their_kinship() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        Individual   brother  = studbook.Offspring(sire, dam, Sex.Male);
        Individual   sister   = studbook.Offspring(sire, dam, Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).WithPolicy(p => p.WithMaximumPairKinship(new KinshipCoefficient(0.125))
                                                                       .WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                                                    .Generate();

        // Act
        PairingRefusedException error = Assert.Throws<PairingRefusedException>(() => anyPlan.Propose(brother.Id, sister.Id));

        // Assert
        Assert.Equal(brother.Id, error.SireId);
        Assert.Equal(sister.Id, error.DamId);
        Assert.Empty(anyPlan.Pairs);
    }

    [Fact]
    public void A_female_cannot_be_proposed_as_sire() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   female   = studbook.Founder(Sex.Female);
        Individual   dam      = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).Generate();

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(female.Id, dam.Id));

        // Assert
        Assert.IsType<PairingRefusedException>(error);
    }

    [Fact]
    public void A_dead_partner_cannot_be_proposed() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        dam.RecordDeath(dam.BirthDate.AddYears(8));
        BreedingPlan anyPlan  = AnyPlanFor(studbook).Generate();

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.IsType<PairingRefusedException>(error);
    }

    [Fact]
    public void A_partner_too_old_at_the_start_of_the_season_cannot_be_proposed() {
        // Arrange
        // Every studbook animal is at least 21 at a season starting in 2030 or later.
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).WithPolicy(p => p.WithMinimumBreedingAgeYears(2).WithMaximumBreedingAgeYears(15))
                                                    .Generate();

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.IsType<PairingRefusedException>(error);
    }

    [Fact]
    public void An_individual_over_its_pairing_quota_is_refused_a_further_pair() {
        // Arrange
        Studbook     studbook  = new Studbook();
        Individual   sire      = studbook.Founder(Sex.Male);
        Individual   firstDam  = studbook.Founder(Sex.Female);
        Individual   secondDam = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan   = AnyPlanFor(studbook).WithPolicy(p => p.WithMaximumPairingsPerIndividual(1)
                                                                        .WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                                                     .Generate();
        anyPlan.Propose(sire.Id, firstDam.Id);

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(sire.Id, secondDam.Id));

        // Assert
        Assert.IsType<PairingRefusedException>(error);
        Assert.Single(anyPlan.Pairs);
    }

    [Fact]
    public void The_same_pair_cannot_be_proposed_twice() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).WithPolicy(p => p.WithMaximumPairingsPerIndividual(a => a.GreaterThanOrEqualTo(2))
                                                                       .WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                                                    .Generate();
        anyPlan.Propose(sire.Id, dam.Id);

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(sire.Id, dam.Id));

        // Assert
        Assert.IsType<PairingRefusedException>(error);
    }

    [Fact]
    public void Expected_offspring_inbreeding_is_the_mean_kinship_of_the_pairs() {
        // Arrange
        Studbook     studbook  = new Studbook();
        Individual   sire      = studbook.Founder(Sex.Male);
        Individual   dam       = studbook.Founder(Sex.Female);
        Individual   daughter  = studbook.Offspring(sire, dam, Sex.Female);
        Individual   outsider  = studbook.Founder(Sex.Male);
        BreedingPlan anyPlan   = AnyPlanFor(studbook).WithPolicy(p => p.WithMaximumPairKinship(new KinshipCoefficient(0.25))
                                                                        .WithMaximumPairingsPerIndividual(a => a.GreaterThanOrEqualTo(2))
                                                                        .WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                                                     .Generate();

        // Act
        anyPlan.Propose(sire.Id, daughter.Id);
        anyPlan.Propose(outsider.Id, dam.Id);

        // Assert
        Assert.Equal(0.125, anyPlan.ExpectedOffspringInbreeding);
    }

    [Fact]
    public void A_withdrawn_pair_leaves_the_plan() {
        // Arrange
        Studbook     studbook = new Studbook();
        Individual   sire     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan  = AnyPlanFor(studbook).Generate();
        BreedingPair pair     = anyPlan.Propose(sire.Id, dam.Id);

        // Act
        anyPlan.Withdraw(pair);

        // Assert
        Assert.Empty(anyPlan.Pairs);
        Assert.Equal(0.0, anyPlan.ExpectedOffspringInbreeding);
    }

    [Fact]
    public void An_empty_plan_cannot_be_approved() {
        // Arrange
        BreedingPlan anyPlan = AnyPlanFor(new Studbook()).Generate();

        // Act
        Exception? error = Record.Exception(() => anyPlan.Approve());

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(BreedingPlanStatus.Draft, anyPlan.Status);
    }

    [Fact]
    public void An_approved_plan_accepts_no_further_pair() {
        // Arrange
        Studbook     studbook  = new Studbook();
        Individual   sire      = studbook.Founder(Sex.Male);
        Individual   dam       = studbook.Founder(Sex.Female);
        Individual   otherDam  = studbook.Founder(Sex.Female);
        BreedingPlan anyPlan   = AnyPlanFor(studbook).Generate();
        anyPlan.Propose(sire.Id, dam.Id);
        anyPlan.Approve();

        // Act
        Exception? error = Record.Exception(() => anyPlan.Propose(sire.Id, otherDam.Id));

        // Assert
        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal(BreedingPlanStatus.Approved, anyPlan.Status);
    }

    [Fact]
    public void Sires_are_ranked_by_pair_kinship_then_by_mean_kinship_and_dead_males_are_left_out() {
        // Arrange
        // The dam's mate has a living son with her, so he is more represented than the newcomer
        // although both are unrelated to her; the son is her offspring (kinship 1/4).
        Studbook     studbook = new Studbook();
        Individual   mate     = studbook.Founder(Sex.Male);
        Individual   dam      = studbook.Founder(Sex.Female);
        Individual   newcomer = studbook.Founder(Sex.Male);
        Individual   deadMale = studbook.Founder(Sex.Male);
        Individual   son      = studbook.Offspring(mate, dam, Sex.Male);
        deadMale.RecordDeath(deadMale.BirthDate.AddYears(6));
        BreedingPlan anyPlan  = AnyPlanFor(studbook).WithPolicy(p => p.WithMaximumPairKinship(new KinshipCoefficient(0.25))
                                                                       .WithMinimumBreedingAgeYears(a => a.LessThanOrEqualTo(20)))
                                                    .Generate();

        // Act
        IReadOnlyList<BreedingPair> ranking = anyPlan.RankSiresFor(dam.Id);

        // Assert
        Assert.Equal([newcomer.Id, mate.Id, son.Id], ranking.Select(p => p.SireId));
    }

}
