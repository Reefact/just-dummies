using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Support;
using Xunit;

namespace CaptiveBreeding.Tests;

public sealed class PedigreeCalculationsTests {

    private static readonly DateOnly FoundersBirth = new DateOnly(2000, 1, 1);
    private static readonly DateOnly FirstGenBirth = new DateOnly(2005, 1, 1);
    private static readonly DateOnly SecondGenBirth = new DateOnly(2010, 1, 1);

    private const double Tolerance = 1e-12;

    // ---- Generation, ancestors ------------------------------------------------------------

    [Fact]
    public void A_founder_belongs_to_generation_zero() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Male, FoundersBirth);

        // Assert
        Assert.Equal(0, herd.Pedigree.GenerationOf(founder));
    }

    [Fact]
    public void Each_generation_is_one_more_than_the_older_of_its_parents() {
        // Arrange
        Herd       herd       = new Herd();
        StudbookId grandsire  = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId granddam   = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId dam        = herd.Offspring(Sex.Female, FirstGenBirth, grandsire, granddam);
        StudbookId backcross  = herd.Offspring(Sex.Male, SecondGenBirth, grandsire, dam);

        // Assert
        Assert.Equal(1, herd.Pedigree.GenerationOf(dam));
        Assert.Equal(2, herd.Pedigree.GenerationOf(backcross));
    }

    [Fact]
    public void Generation_of_an_unknown_id_is_refused() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Action act = () => herd.Pedigree.GenerationOf(herd.NewUnregisteredId());

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void A_founder_has_no_ancestors() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Female, FoundersBirth);

        // Assert
        Assert.Empty(herd.Pedigree.AncestorsOf(founder));
    }

    [Fact]
    public void Ancestors_go_back_to_the_founders_without_including_the_individual_itself() {
        // Arrange
        Herd       herd       = new Herd();
        StudbookId grandsire  = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId granddam   = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId otherDam   = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId sire       = herd.Offspring(Sex.Male, FirstGenBirth, grandsire, granddam);
        StudbookId calf       = herd.Offspring(Sex.Female, SecondGenBirth, sire, otherDam);

        // Act
        IReadOnlySet<StudbookId> ancestors = herd.Pedigree.AncestorsOf(calf);

        // Assert
        Assert.Equal(4, ancestors.Count);
        Assert.Contains(sire, ancestors);
        Assert.Contains(grandsire, ancestors);
        Assert.Contains(granddam, ancestors);
        Assert.Contains(otherDam, ancestors);
        Assert.DoesNotContain(calf, ancestors);
    }

    [Fact]
    public void The_ancestors_of_an_unknown_id_are_refused() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Action act = () => herd.Pedigree.AncestorsOf(herd.NewUnregisteredId());

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    [Fact]
    public void Full_siblings_share_both_parents_as_common_ancestors() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam     = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);

        // Act
        IReadOnlySet<StudbookId> common = herd.Pedigree.CommonAncestorsOf(brother, sister);

        // Assert
        Assert.Equal(new HashSet<StudbookId> { sire, dam }, common);
    }

    [Fact]
    public void Unrelated_founders_have_no_common_ancestor() {
        // Arrange
        Herd       herd   = new Herd();
        StudbookId first  = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId second = herd.Founder(Sex.Female, FoundersBirth);

        // Assert
        Assert.Empty(herd.Pedigree.CommonAncestorsOf(first, second));
    }

    // ---- Kinship --------------------------------------------------------------------------

    [Fact]
    public void Unrelated_founders_have_a_kinship_of_zero() {
        // Arrange
        Herd       herd   = new Herd();
        StudbookId first  = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId second = herd.Founder(Sex.Female, FoundersBirth);

        // Assert
        Assert.Equal(0.0, herd.Pedigree.KinshipBetween(first, second).Value);
    }

    [Fact]
    public void A_non_inbred_founder_has_a_kinship_of_one_half_with_itself() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Male, FoundersBirth);

        // Assert
        Assert.Equal(0.5, herd.Pedigree.KinshipBetween(founder, founder).Value);
    }

    [Fact]
    public void A_parent_and_its_offspring_have_a_kinship_of_a_quarter() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId calf = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(sire, calf).Value);
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(dam, calf).Value);
    }

    [Fact]
    public void Full_siblings_have_a_kinship_of_a_quarter() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam     = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(brother, sister).Value);
    }

    [Fact]
    public void Half_siblings_have_a_kinship_of_an_eighth() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId firstDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId otherDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId first    = herd.Offspring(Sex.Male, FirstGenBirth, sire, firstDam);
        StudbookId second   = herd.Offspring(Sex.Female, FirstGenBirth, sire, otherDam);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.KinshipBetween(first, second).Value);
    }

    [Fact]
    public void A_grandparent_and_a_grandchild_have_a_kinship_of_an_eighth() {
        // Arrange
        Herd       herd       = new Herd();
        StudbookId grandsire  = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId granddam   = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId otherDam   = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId sire       = herd.Offspring(Sex.Male, FirstGenBirth, grandsire, granddam);
        StudbookId grandchild = herd.Offspring(Sex.Female, SecondGenBirth, sire, otherDam);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.KinshipBetween(grandsire, grandchild).Value);
    }

    [Fact]
    public void Kinship_is_symmetric_and_stable_across_repeated_queries() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam     = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);

        // Act
        double forward  = herd.Pedigree.KinshipBetween(brother, sister).Value;
        double backward = herd.Pedigree.KinshipBetween(sister, brother).Value;
        double again    = herd.Pedigree.KinshipBetween(brother, sister).Value;

        // Assert
        Assert.Equal(forward, backward);
        Assert.Equal(forward, again);
    }

    [Fact]
    public void Kinship_with_an_individual_born_of_full_siblings_counts_its_inbreeding() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam     = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);
        StudbookId inbred  = herd.Offspring(Sex.Male, SecondGenBirth, brother, sister);

        // Assert: 1/2 (1 + F) with F = 1/4
        Assert.Equal(0.625, herd.Pedigree.KinshipBetween(inbred, inbred).Value, Tolerance);
    }

    [Fact]
    public void Kinship_involving_an_unknown_id_is_refused_on_either_side() {
        // Arrange
        Herd       herd      = new Herd();
        StudbookId known     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId unknownId = herd.NewUnregisteredId();

        // Act
        Action unknownFirst  = () => herd.Pedigree.KinshipBetween(unknownId, known);
        Action unknownSecond = () => herd.Pedigree.KinshipBetween(known, unknownId);

        // Assert
        Assert.Throws<KeyNotFoundException>(unknownFirst);
        Assert.Throws<KeyNotFoundException>(unknownSecond);
    }

    // ---- Inbreeding -----------------------------------------------------------------------

    [Fact]
    public void A_founder_is_not_inbred() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Female, FoundersBirth);

        // Assert
        Assert.Equal(0.0, herd.Pedigree.InbreedingOf(founder));
    }

    [Fact]
    public void The_offspring_of_unrelated_parents_is_not_inbred() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId calf = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);

        // Assert
        Assert.Equal(0.0, herd.Pedigree.InbreedingOf(calf));
    }

    [Fact]
    public void The_offspring_of_full_siblings_has_an_inbreeding_of_a_quarter() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId sire    = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam     = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        StudbookId sister  = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);
        StudbookId inbred  = herd.Offspring(Sex.Male, SecondGenBirth, brother, sister);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.InbreedingOf(inbred));
    }

    [Fact]
    public void The_offspring_of_half_siblings_has_an_inbreeding_of_an_eighth() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId firstDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId otherDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId brother  = herd.Offspring(Sex.Male, FirstGenBirth, sire, firstDam);
        StudbookId sister   = herd.Offspring(Sex.Female, FirstGenBirth, sire, otherDam);
        StudbookId inbred   = herd.Offspring(Sex.Male, SecondGenBirth, brother, sister);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.InbreedingOf(inbred));
    }

    [Fact]
    public void The_offspring_of_a_sire_and_his_own_daughter_has_an_inbreeding_of_a_quarter() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam      = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId daughter = herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);
        StudbookId inbred   = herd.Offspring(Sex.Male, SecondGenBirth, sire, daughter);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.InbreedingOf(inbred));
    }

    [Fact]
    public void The_inbreeding_of_an_unknown_id_is_refused() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Action act = () => herd.Pedigree.InbreedingOf(herd.NewUnregisteredId());

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    // ---- Mean kinship ---------------------------------------------------------------------

    [Fact]
    public void Mean_kinship_averages_over_every_living_individual_itself_included() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId calf = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);

        // Assert: (1/2 + 0 + 1/4) / 3 for the sire, (1/4 + 1/4 + 1/2) / 3 for the calf
        Assert.Equal(0.25, herd.Pedigree.MeanKinshipOf(sire), Tolerance);
        Assert.Equal(1.0 / 3.0, herd.Pedigree.MeanKinshipOf(calf), Tolerance);
    }

    [Fact]
    public void Mean_kinship_ignores_the_dead() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId calf = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        herd.Kill(calf, new DateOnly(2008, 1, 1));

        // Assert: (1/2 + 0) / 2
        Assert.Equal(0.25, herd.Pedigree.MeanKinshipOf(sire), Tolerance);
    }

    [Fact]
    public void Mean_kinship_of_a_dead_individual_is_still_measured_against_the_living() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        herd.Kill(sire, new DateOnly(2008, 1, 1));

        // Assert: only the dam lives, and she is unrelated to the sire
        Assert.Equal(0.0, herd.Pedigree.MeanKinshipOf(sire), Tolerance);
    }

    [Fact]
    public void Mean_kinship_is_undefined_when_nobody_is_alive() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Male, FoundersBirth);
        herd.Kill(founder, new DateOnly(2008, 1, 1));

        // Act
        Action act = () => herd.Pedigree.MeanKinshipOf(founder);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Mean_kinship_of_an_unknown_id_is_refused() {
        // Arrange
        Herd herd = new Herd();
        herd.Founder(Sex.Male, FoundersBirth);

        // Act
        Action act = () => herd.Pedigree.MeanKinshipOf(herd.NewUnregisteredId());

        // Assert
        Assert.Throws<KeyNotFoundException>(act);
    }

    // ---- Founder contributions ------------------------------------------------------------

    [Fact]
    public void Two_founders_and_their_offspring_each_contribute_half() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);

        // Act
        IReadOnlyList<GeneticContribution> contributions = herd.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(2, contributions.Count);
        Assert.All(contributions, c => Assert.Equal(0.5, c.Proportion, Tolerance));
        Assert.Equal(new HashSet<StudbookId> { sire, dam }, contributions.Select(c => c.FounderId).ToHashSet());
    }

    [Fact]
    public void Founder_contributions_add_up_to_one() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId firstDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId otherDam = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId first    = herd.Offspring(Sex.Male, FirstGenBirth, sire, firstDam);
        herd.Offspring(Sex.Female, FirstGenBirth, sire, otherDam);
        herd.Offspring(Sex.Female, SecondGenBirth, first, otherDam);

        // Act
        double total = herd.Pedigree.FounderContributions().Sum(c => c.Proportion);

        // Assert
        Assert.Equal(1.0, total, 1e-9);
    }

    [Fact]
    public void A_founder_without_living_descendants_nor_life_of_its_own_is_lost() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam      = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId lostSire = herd.Founder(Sex.Male, FoundersBirth);
        herd.Offspring(Sex.Female, FirstGenBirth, sire, dam);
        herd.Kill(lostSire, new DateOnly(2004, 1, 1));

        // Act
        GeneticContribution lost = herd.Pedigree.FounderContributions().Single(c => c.FounderId == lostSire);

        // Assert
        Assert.True(lost.IsLost);
    }

    [Fact]
    public void Contributions_only_count_the_living() {
        // Arrange
        Herd       herd = new Herd();
        StudbookId sire = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam  = herd.Founder(Sex.Female, FoundersBirth);
        StudbookId calf = herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        herd.Kill(sire, new DateOnly(2008, 1, 1));
        herd.Kill(dam, new DateOnly(2008, 1, 1));

        // Act
        IReadOnlyList<GeneticContribution> contributions = herd.Pedigree.FounderContributions();

        // Assert: the calf alone carries half of each
        Assert.Equal(2, contributions.Count);
        Assert.All(contributions, c => Assert.Equal(0.5, c.Proportion, Tolerance));
        Assert.True(herd.Pedigree.Get(calf).IsAlive);
    }

    [Fact]
    public void Founder_contributions_are_undefined_when_nobody_is_alive() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Male, FoundersBirth);
        herd.Kill(founder, new DateOnly(2008, 1, 1));

        // Act
        Action act = () => herd.Pedigree.FounderContributions();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    // ---- Founder equivalents --------------------------------------------------------------

    [Fact]
    public void Equal_contributions_of_n_founders_make_n_founder_equivalents() {
        // Arrange
        Herd herd = new Herd();
        herd.Founder(Sex.Male, FoundersBirth);
        herd.Founder(Sex.Female, FoundersBirth);
        herd.Founder(Sex.Male, FoundersBirth);
        herd.Founder(Sex.Female, FoundersBirth);

        // Assert
        Assert.Equal(4.0, herd.Pedigree.FounderEquivalents(), Tolerance);
    }

    [Fact]
    public void Uneven_contributions_make_fewer_founder_equivalents_than_founders() {
        // Arrange
        Herd       herd     = new Herd();
        StudbookId sire     = herd.Founder(Sex.Male, FoundersBirth);
        StudbookId dam      = herd.Founder(Sex.Female, FoundersBirth);
        herd.Founder(Sex.Female, FoundersBirth);
        herd.Offspring(Sex.Male, FirstGenBirth, sire, dam);
        herd.Kill(dam, new DateOnly(2008, 1, 1));

        // Assert: living are the sire, the unrelated female and the calf -> p = (1/2, 1/6, 1/3)
        Assert.Equal(36.0 / 14.0, herd.Pedigree.FounderEquivalents(), 1e-9);
    }

    [Fact]
    public void Founder_equivalents_are_undefined_when_nobody_is_alive() {
        // Arrange
        Herd       herd    = new Herd();
        StudbookId founder = herd.Founder(Sex.Male, FoundersBirth);
        herd.Kill(founder, new DateOnly(2008, 1, 1));

        // Act
        Action act = () => herd.Pedigree.FounderEquivalents();

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

}
