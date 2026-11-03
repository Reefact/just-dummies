using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Generators;
using CaptiveBreeding.Tests.Expert.Support;

using JustDummies;

using Xunit;

namespace CaptiveBreeding.Tests.Expert.Tests;

public sealed class PedigreeTests {

    // ---- the programme ------------------------------------------------------------------------

    [Fact]
    public void A_programme_keeps_its_name_and_its_species() {
        // Arrange
        string anyName    = Any.String().NotBlank().Generate();
        string anySpecies = Any.String().NotBlank().Generate();

        // Act
        Pedigree pedigree = new Pedigree(anyName, anySpecies);

        // Assert
        Assert.Equal(anyName, pedigree.ProgrammeName);
        Assert.Equal(anySpecies, pedigree.Species);
        Assert.Empty(pedigree.Individuals);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_programme_needs_a_name(string blank) {
        Assert.Throws<ArgumentException>(() => new Pedigree(blank, Any.String().NotBlank().Generate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_programme_manages_a_named_species(string blank) {
        Assert.Throws<ArgumentException>(() => new Pedigree(Any.String().NotBlank().Generate(), blank));
    }

    [Fact]
    public void A_programme_without_a_name_or_a_species_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Pedigree(null!, Any.String().NotBlank().Generate()));
        Assert.Throws<ArgumentNullException>(() => new Pedigree(Any.String().NotBlank().Generate(), null!));
    }

    // ---- registering ----------------------------------------------------------------------------

    [Fact]
    public void A_registered_individual_is_found_by_its_id() {
        // Arrange
        Herd herd = new Herd();

        // Act
        Individual anyFounder = herd.Male();

        // Assert
        Assert.True(herd.Pedigree.Contains(anyFounder.Id));
        Assert.Same(anyFounder, herd.Pedigree.Get(anyFounder.Id));
        Assert.Same(anyFounder, Assert.Single(herd.Pedigree.Individuals));
    }

    [Fact]
    public void An_individual_that_was_never_registered_is_not_contained() {
        // Arrange
        Herd herd = new Herd();
        herd.Male();

        // Assert
        Assert.False(herd.Pedigree.Contains(herd.UnusedId()));
    }

    [Fact]
    public void Asking_for_an_individual_that_was_never_registered_names_it() {
        // Arrange
        Herd       herd      = new Herd();
        StudbookId unknownId = herd.UnusedId();

        // Act
        KeyNotFoundException refusal = Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.Get(unknownId));

        // Assert
        Assert.Contains(unknownId.Value, refusal.Message);
        Assert.Contains(herd.Pedigree.ProgrammeName, refusal.Message);
    }

    [Fact]
    public void The_same_id_cannot_be_registered_twice() {
        // Arrange
        Herd       herd       = new Herd();
        Individual anyFounder = herd.Male();
        Individual sameId     = Any.Individual().WithId(new StudbookId(anyFounder.Id.Value)).Generate();

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Pedigree.Register(sameId));
        Assert.Single(herd.Pedigree.Individuals);
    }

    [Fact]
    public void Registering_nobody_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Herd().Pedigree.Register(null!));
    }

    [Fact]
    public void Individuals_are_listed_in_registration_order_and_founders_apart() {
        // Arrange
        Herd       herd     = new Herd();
        Individual sire     = herd.Male();
        Individual dam      = herd.Female();
        Individual offspring = herd.Son(sire, dam);

        // Assert
        Assert.Equal([sire, dam, offspring], herd.Pedigree.Individuals);
        Assert.Equal([sire, dam], herd.Pedigree.Founders);
    }

    [Fact]
    public void An_offspring_cannot_be_registered_before_its_parents() {
        // Arrange
        Herd       herd       = new Herd();
        Individual sire       = herd.Male();
        StudbookId unknownDam = herd.UnusedId();
        Individual orphan     = Any.Individual().WithSireId(sire.Id).WithDamId(unknownDam).WithBirthDate(sire.BirthDate.AddYears(3)).Generate();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.Register(orphan));
        Assert.False(herd.Pedigree.Contains(orphan.Id));
    }

    [Fact]
    public void A_female_cannot_be_registered_as_a_sire() {
        // Arrange
        Herd       herd       = new Herd();
        Individual female     = herd.Female();
        Individual otherFemale = herd.Female();
        Individual offspring  = Any.Individual().WithSireId(female.Id).WithDamId(otherFemale.Id).WithBirthDate(female.BirthDate.AddYears(3)).Generate();

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => herd.Pedigree.Register(offspring));
        Assert.Contains(female.Id.Value, refusal.Message);
    }

    [Fact]
    public void A_male_cannot_be_registered_as_a_dam() {
        // Arrange
        Herd       herd      = new Herd();
        Individual male      = herd.Male();
        Individual otherMale = herd.Male();
        Individual offspring = Any.Individual().WithSireId(male.Id).WithDamId(otherMale.Id).WithBirthDate(male.BirthDate.AddYears(3)).Generate();

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => herd.Pedigree.Register(offspring));
        Assert.Contains(otherMale.Id.Value, refusal.Message);
    }

    [Fact]
    public void An_offspring_born_the_day_its_sire_was_born_is_refused() {
        // Arrange
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Offspring(sire, dam, Sex.Female, sire.BirthDate));
    }

    [Fact]
    public void An_offspring_born_the_day_its_dam_was_born_is_refused() {
        // Arrange
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Offspring(sire, dam, Sex.Female, dam.BirthDate));
    }

    [Fact]
    public void An_offspring_born_before_a_parent_is_refused() {
        // Arrange
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Offspring(sire, dam, Sex.Male, dam.BirthDate.AddDays(-1)));
    }

    [Fact]
    public void An_offspring_born_the_day_after_both_its_parents_is_accepted() {
        // Arrange
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();
        DateOnly   dayAfterTheYoungerParent = (sire.BirthDate > dam.BirthDate ? sire.BirthDate : dam.BirthDate).AddDays(1);

        // Act
        Individual offspring = herd.Offspring(sire, dam, Sex.Female, dayAfterTheYoungerParent);

        // Assert
        Assert.True(herd.Pedigree.Contains(offspring.Id));
    }

    [Fact]
    public void An_offspring_cannot_be_registered_after_its_dam_died_before_the_birth() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        DateOnly   birthDate = dam.BirthDate.AddYears(3);
        dam.RecordDeath(birthDate.AddDays(-1));

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => herd.Offspring(sire, dam, Sex.Male, birthDate));

        // Assert
        Assert.Contains(dam.Id.Value, refusal.Message);
    }

    [Fact]
    public void An_offspring_may_be_born_the_day_its_dam_dies() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        DateOnly   birthDate = dam.BirthDate.AddYears(3);
        dam.RecordDeath(birthDate);

        // Act
        Individual offspring = herd.Offspring(sire, dam, Sex.Male, birthDate);

        // Assert
        Assert.True(herd.Pedigree.Contains(offspring.Id));
    }

    [Fact]
    public void An_offspring_may_be_born_after_its_sire_died() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        DateOnly   birthDate = sire.BirthDate.AddYears(4);
        sire.RecordDeath(sire.BirthDate.AddYears(2));

        // Act
        Individual offspring = herd.Offspring(sire, dam, Sex.Male, birthDate.AddYears(1));

        // Assert
        Assert.True(herd.Pedigree.Contains(offspring.Id));
    }

    // ---- generations and ancestors ----------------------------------------------------------------

    [Fact]
    public void A_founder_belongs_to_generation_zero() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Equal(0, herd.Pedigree.GenerationOf(herd.Male().Id));
    }

    [Fact]
    public void The_offspring_of_two_founders_belongs_to_generation_one() {
        // Arrange
        Herd herd = new Herd();
        Individual offspring = herd.Son(herd.Male(), herd.Female());

        // Assert
        Assert.Equal(1, herd.Pedigree.GenerationOf(offspring.Id));
    }

    [Fact]
    public void An_individual_is_one_generation_after_its_most_recent_parent() {
        // Arrange
        Herd       herd       = new Herd();
        Individual grandsire  = herd.Male();
        Individual granddam   = herd.Female();
        Individual sire       = herd.Son(grandsire, granddam);
        Individual outsider   = herd.Female();
        Individual offspring  = herd.Daughter(sire, outsider);

        // Assert
        Assert.Equal(2, herd.Pedigree.GenerationOf(offspring.Id));
    }

    [Fact]
    public void The_generation_of_an_unknown_individual_cannot_be_told() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.GenerationOf(herd.UnusedId()));
    }

    [Fact]
    public void A_founder_has_no_ancestors() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Empty(herd.Pedigree.AncestorsOf(herd.Female().Id));
    }

    [Fact]
    public void The_ancestors_are_every_forebear_through_both_lines_and_never_the_individual_itself() {
        // Arrange
        Herd       herd      = new Herd();
        Individual grandsire = herd.Male();
        Individual granddam  = herd.Female();
        Individual sire      = herd.Son(grandsire, granddam);
        Individual dam       = herd.Female();
        Individual offspring = herd.Daughter(sire, dam);

        // Act
        IReadOnlySet<StudbookId> ancestors = herd.Pedigree.AncestorsOf(offspring.Id);

        // Assert
        Assert.Equal(4, ancestors.Count);
        Assert.True(ancestors.SetEquals([grandsire.Id, granddam.Id, sire.Id, dam.Id]));
    }

    [Fact]
    public void The_ancestors_of_an_unknown_individual_cannot_be_told() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.AncestorsOf(herd.UnusedId()));
    }

    [Fact]
    public void Full_siblings_share_both_their_parents() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        Individual brother = herd.Son(sire, dam);
        Individual sister  = herd.Daughter(sire, dam);

        // Assert
        Assert.True(herd.Pedigree.CommonAncestorsOf(brother.Id, sister.Id).SetEquals([sire.Id, dam.Id]));
    }

    [Fact]
    public void Half_siblings_share_their_one_common_parent() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual firstDam  = herd.Female();
        Individual secondDam = herd.Female();
        Individual brother   = herd.Son(sire, firstDam);
        Individual sister    = herd.Daughter(sire, secondDam);

        // Assert
        Assert.True(herd.Pedigree.CommonAncestorsOf(brother.Id, sister.Id).SetEquals([sire.Id]));
    }

    [Fact]
    public void Unrelated_individuals_have_no_common_ancestor() {
        // Arrange
        Herd       herd  = new Herd();
        Individual first = herd.Son(herd.Male(), herd.Female());
        Individual second = herd.Daughter(herd.Male(), herd.Female());

        // Assert
        Assert.Empty(herd.Pedigree.CommonAncestorsOf(first.Id, second.Id));
    }

    // ---- kinship ----------------------------------------------------------------------------------

    [Fact]
    public void Unrelated_founders_have_no_kinship() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Equal(0.0, herd.Pedigree.KinshipBetween(herd.Male().Id, herd.Female().Id).Value);
    }

    [Fact]
    public void A_founder_shares_half_its_alleles_with_itself() {
        // Arrange
        Herd       herd    = new Herd();
        Individual founder = herd.Male();

        // Assert
        Assert.Equal(0.5, herd.Pedigree.KinshipBetween(founder.Id, founder.Id).Value);
    }

    [Fact]
    public void A_parent_and_its_offspring_have_a_kinship_of_a_quarter() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        Individual offspring = herd.Son(sire, dam);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(sire.Id, offspring.Id).Value);
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(dam.Id, offspring.Id).Value);
    }

    [Fact]
    public void Full_siblings_have_a_kinship_of_a_quarter() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        Individual brother = herd.Son(sire, dam);
        Individual sister  = herd.Daughter(sire, dam);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.KinshipBetween(brother.Id, sister.Id).Value);
    }

    [Fact]
    public void Half_siblings_have_a_kinship_of_an_eighth() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual firstDam  = herd.Female();
        Individual secondDam = herd.Female();
        Individual brother   = herd.Son(sire, firstDam);
        Individual sister    = herd.Daughter(sire, secondDam);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.KinshipBetween(brother.Id, sister.Id).Value);
    }

    [Fact]
    public void A_grandparent_and_its_grandchild_have_a_kinship_of_an_eighth() {
        // Arrange
        Herd       herd       = new Herd();
        Individual grandsire  = herd.Male();
        Individual granddam   = herd.Female();
        Individual sire       = herd.Son(grandsire, granddam);
        Individual dam        = herd.Female();
        Individual grandchild = herd.Daughter(sire, dam);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.KinshipBetween(grandsire.Id, grandchild.Id).Value);
    }

    [Fact]
    public void First_cousins_have_a_kinship_of_a_sixteenth() {
        // Arrange
        Herd       herd         = new Herd();
        Individual grandsire    = herd.Male();
        Individual granddam     = herd.Female();
        Individual uncle        = herd.Son(grandsire, granddam);
        Individual aunt         = herd.Daughter(grandsire, granddam);
        Individual unclesMate   = herd.Female();
        Individual auntsMate    = herd.Male();
        Individual firstCousin  = herd.Son(uncle, unclesMate);
        Individual secondCousin = herd.Daughter(auntsMate, aunt);

        // Assert
        Assert.Equal(0.0625, herd.Pedigree.KinshipBetween(firstCousin.Id, secondCousin.Id).Value);
    }

    [Fact]
    public void An_inbred_individual_shares_more_than_half_its_alleles_with_itself() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        Individual brother = herd.Son(sire, dam);
        Individual sister  = herd.Daughter(sire, dam);
        Individual inbred  = herd.Son(brother, sister);

        // Assert
        Assert.Equal(0.625, herd.Pedigree.KinshipBetween(inbred.Id, inbred.Id).Value);
    }

    [Fact]
    public void The_kinship_of_two_individuals_does_not_depend_on_the_order_they_are_named_in() {
        // Arrange
        Herd       herd      = new Herd();
        Individual sire      = herd.Male();
        Individual dam       = herd.Female();
        Individual brother   = herd.Son(sire, dam);
        Individual otherDam  = herd.Female();
        Individual halfSister = herd.Daughter(sire, otherDam);

        // Assert
        Assert.Equal(herd.Pedigree.KinshipBetween(brother.Id, halfSister.Id).Value, herd.Pedigree.KinshipBetween(halfSister.Id, brother.Id).Value);
    }

    [Fact]
    public void The_kinship_with_an_unregistered_individual_cannot_be_told_whichever_side_it_is_on() {
        // Arrange
        Herd       herd      = new Herd();
        Individual founder   = herd.Male();
        StudbookId unknownId = herd.UnusedId();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.KinshipBetween(founder.Id, unknownId));
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.KinshipBetween(unknownId, founder.Id));
    }

    // ---- inbreeding -------------------------------------------------------------------------------

    [Fact]
    public void A_founder_is_not_inbred() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Equal(0.0, herd.Pedigree.InbreedingOf(herd.Female().Id));
    }

    [Fact]
    public void The_offspring_of_unrelated_parents_is_not_inbred() {
        // Arrange
        Herd herd = new Herd();
        Individual offspring = herd.Son(herd.Male(), herd.Female());

        // Assert
        Assert.Equal(0.0, herd.Pedigree.InbreedingOf(offspring.Id));
    }

    [Fact]
    public void The_offspring_of_full_siblings_has_an_inbreeding_of_a_quarter() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        Individual brother = herd.Son(sire, dam);
        Individual sister  = herd.Daughter(sire, dam);
        Individual inbred  = herd.Daughter(brother, sister);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.InbreedingOf(inbred.Id));
    }

    [Fact]
    public void The_offspring_of_half_siblings_has_an_inbreeding_of_an_eighth() {
        // Arrange
        Herd       herd       = new Herd();
        Individual sire       = herd.Male();
        Individual firstDam   = herd.Female();
        Individual secondDam  = herd.Female();
        Individual halfBrother = herd.Son(sire, firstDam);
        Individual halfSister = herd.Daughter(sire, secondDam);
        Individual inbred     = herd.Son(halfBrother, halfSister);

        // Assert
        Assert.Equal(0.125, herd.Pedigree.InbreedingOf(inbred.Id));
    }

    [Fact]
    public void The_offspring_of_a_sire_and_its_own_daughter_has_an_inbreeding_of_a_quarter() {
        // Arrange
        Herd       herd     = new Herd();
        Individual sire     = herd.Male();
        Individual dam      = herd.Female();
        Individual daughter = herd.Daughter(sire, dam);
        Individual inbred   = herd.Son(sire, daughter);

        // Assert
        Assert.Equal(0.25, herd.Pedigree.InbreedingOf(inbred.Id));
    }

    [Fact]
    public void The_inbreeding_of_an_unknown_individual_cannot_be_told() {
        // Arrange
        Herd herd = new Herd();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.InbreedingOf(herd.UnusedId()));
    }

    // ---- mean kinship -----------------------------------------------------------------------------

    [Fact]
    public void The_mean_kinship_averages_the_kinship_with_every_living_individual_itself_included() {
        // Arrange
        Herd       herd     = new Herd();
        Individual sire     = herd.Male();
        Individual dam      = herd.Female();
        Individual brother  = herd.Son(sire, dam);
        Individual sister   = herd.Daughter(sire, dam);

        // Assert: (0.25 with the sire + 0.25 with the dam + 0.5 with itself + 0.25 with its sister) / 4
        Assert.Equal(0.3125, herd.Pedigree.MeanKinshipOf(brother.Id), precision: 12);
        Assert.Equal(0.3125, herd.Pedigree.MeanKinshipOf(sister.Id), precision: 12);
    }

    [Fact]
    public void The_dead_do_not_count_in_a_mean_kinship() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        Individual brother = herd.Son(sire, dam);
        Individual sister  = herd.Daughter(sire, dam);
        herd.Dies(sire);
        herd.Dies(dam);

        // Assert: (0.5 with itself + 0.25 with its sister) / 2
        Assert.Equal(0.375, herd.Pedigree.MeanKinshipOf(brother.Id), precision: 12);
    }

    [Fact]
    public void A_dead_individual_still_has_a_mean_kinship_with_the_living() {
        // Arrange
        Herd       herd    = new Herd();
        Individual sire    = herd.Male();
        Individual dam     = herd.Female();
        herd.Son(sire, dam);
        herd.Dies(sire);
        herd.Dies(dam);

        // Assert: the only living individual is the son, whose kinship with the dead sire is a quarter
        Assert.Equal(0.25, herd.Pedigree.MeanKinshipOf(sire.Id), precision: 12);
    }

    [Fact]
    public void A_mean_kinship_is_undefined_when_nobody_is_alive() {
        // Arrange
        Herd       herd    = new Herd();
        Individual founder = herd.Male();
        herd.Dies(founder);

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Pedigree.MeanKinshipOf(founder.Id));
    }

    [Fact]
    public void The_mean_kinship_of_an_unknown_individual_cannot_be_told() {
        // Arrange
        Herd herd = new Herd();
        herd.Male();

        // Assert
        Assert.Throws<KeyNotFoundException>(() => herd.Pedigree.MeanKinshipOf(herd.UnusedId()));
    }

    // ---- founder contributions ----------------------------------------------------------------------

    [Fact]
    public void Founders_alone_each_contribute_an_equal_share() {
        // Arrange
        Herd herd = new Herd();
        herd.Male();
        herd.Female();
        herd.Male();
        herd.Female();

        // Act
        IReadOnlyList<GeneticContribution> contributions = herd.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(4, contributions.Count);
        Assert.All(contributions, contribution => Assert.Equal(0.25, contribution.Proportion, precision: 12));
    }

    [Fact]
    public void The_offspring_of_two_founders_carries_half_of_each() {
        // Arrange
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();
        herd.Son(sire, dam);
        herd.Dies(sire);
        herd.Dies(dam);

        // Act
        IReadOnlyList<GeneticContribution> contributions = herd.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(2, contributions.Count);
        Assert.Equal(0.5, ProportionOf(contributions, sire), precision: 12);
        Assert.Equal(0.5, ProportionOf(contributions, dam), precision: 12);
    }

    [Fact]
    public void A_founder_whose_line_has_died_out_is_lost() {
        // Arrange
        Herd       herd         = new Herd();
        Individual sire         = herd.Male();
        Individual dam          = herd.Female();
        Individual childlessMale = herd.Male();
        herd.Son(sire, dam);
        herd.Dies(childlessMale);

        // Act
        IReadOnlyList<GeneticContribution> contributions = herd.Pedigree.FounderContributions();

        // Assert
        GeneticContribution lost = Assert.Single(contributions, contribution => contribution.IsLost);
        Assert.Equal(childlessMale.Id, lost.FounderId);
    }

    [Fact]
    public void The_shares_of_all_the_founders_add_up_to_one() {
        // Arrange
        Herd       herd        = new Herd();
        Individual sire        = herd.Male();
        Individual dam         = herd.Female();
        Individual otherSire   = herd.Male();
        Individual brother     = herd.Son(sire, dam);
        Individual sister      = herd.Daughter(sire, dam);
        Individual cousin      = herd.Son(otherSire, sister);
        herd.Daughter(brother, sister);
        herd.Dies(dam);
        herd.Dies(cousin);

        // Act
        double total = herd.Pedigree.FounderContributions().Sum(contribution => contribution.Proportion);

        // Assert
        Assert.Equal(1.0, total, precision: 12);
    }

    [Fact]
    public void The_founder_contributions_cannot_be_told_when_nobody_is_alive() {
        // Arrange
        Herd herd = new Herd();
        herd.Dies(herd.Male());

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Pedigree.FounderContributions());
    }

    // ---- founder equivalents --------------------------------------------------------------------------

    [Fact]
    public void Equally_contributing_founders_are_worth_as_many_founder_equivalents() {
        // Arrange
        Herd herd = new Herd();
        herd.Male();
        herd.Female();
        herd.Male();
        herd.Female();

        // Assert
        Assert.Equal(4.0, herd.Pedigree.FounderEquivalents(), precision: 12);
    }

    [Fact]
    public void Unequal_contributions_are_worth_fewer_founder_equivalents_than_there_are_founders() {
        // Arrange: the living are an offspring of two founders (a quarter each, with the third living founder's half)
        Herd       herd = new Herd();
        Individual sire = herd.Male();
        Individual dam  = herd.Female();
        herd.Male();
        herd.Son(sire, dam);
        herd.Dies(sire);
        herd.Dies(dam);

        // Assert: shares are 0.25, 0.25 and 0.5, so 1 / (0.0625 + 0.0625 + 0.25)
        Assert.Equal(1.0 / 0.375, herd.Pedigree.FounderEquivalents(), precision: 12);
    }

    [Fact]
    public void A_single_surviving_founder_is_worth_one_founder_equivalent() {
        // Arrange
        Herd       herd = new Herd();
        herd.Male();
        Individual dam = herd.Female();
        herd.Dies(dam);

        // Assert
        Assert.Equal(1.0, herd.Pedigree.FounderEquivalents(), precision: 12);
    }

    [Fact]
    public void The_founder_equivalents_cannot_be_told_when_nobody_is_alive() {
        // Arrange
        Herd herd = new Herd();
        herd.Dies(herd.Female());

        // Assert
        Assert.Throws<InvalidOperationException>(() => herd.Pedigree.FounderEquivalents());
    }

    private static double ProportionOf(IReadOnlyList<GeneticContribution> contributions, Individual founder) {
        return contributions.Single(contribution => contribution.FounderId == founder.Id).Proportion;
    }

}
