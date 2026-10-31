using CaptiveBreeding.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace CaptiveBreeding.Tests.Regular;

[Reproducible]
public sealed class PedigreeRegistrationTests {

    [Fact]
    public void An_id_already_in_the_studbook_is_refused() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual anyFounder = studbook.Founder(Any.Enum<Sex>().Generate());
        Individual sameId     = Arbitrary.Individual().WithId(anyFounder.Id).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(sameId);

        // Assert
        Assert.Throws<InvalidOperationException>(register);
    }

    [Fact]
    public void An_offspring_of_an_unregistered_parent_is_refused() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual anyDam        = studbook.Founder(Sex.Female);
        Individual anyUnregistered = Arbitrary.Individual().WithSex(Sex.Male).WithBirthDate(anyDam.BirthDate).Generate();
        Individual anyOffspring  = Arbitrary.Individual().WithParents(anyUnregistered, anyDam).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        Assert.Throws<KeyNotFoundException>(register);
    }

    [Fact]
    public void A_female_cannot_be_recorded_as_a_sire() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual anyFemale     = studbook.Founder(Sex.Female);
        Individual anyOtherFemale = studbook.Founder(Sex.Female);
        Individual anyOffspring  = Arbitrary.Individual().WithParents(anyFemale, anyOtherFemale).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(register);
        Assert.Contains("not male", refusal.Message);
    }

    [Fact]
    public void An_offspring_born_the_same_day_as_a_parent_is_refused() {
        // Arrange
        Studbook   studbook     = new Studbook();
        Individual anySire      = studbook.Founder(Sex.Male);
        Individual anyDam       = studbook.Founder(Sex.Female);
        Individual anyOffspring = Arbitrary.Individual().WithParents(anySire, anyDam).WithBirthDate(anySire.BirthDate).Generate();

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        Assert.Throws<InvalidOperationException>(register);
    }

    [Fact]
    public void An_offspring_born_after_its_dams_death_is_refused() {
        // Arrange
        Studbook   studbook     = new Studbook();
        Individual anySire      = studbook.Founder(Sex.Male);
        Individual anyDam       = studbook.Founder(Sex.Female);
        Individual anyOffspring = Arbitrary.Individual().WithParents(anySire, anyDam).Generate();
        anyDam.RecordDeath(anyOffspring.BirthDate.AddDays(-1));

        // Act
        Action register = () => studbook.Pedigree.Register(anyOffspring);

        // Assert
        Assert.Throws<InvalidOperationException>(register);
    }

    [Fact]
    public void Parentage_is_either_complete_or_absent() {
        // Arrange
        StudbookId anySireId = Arbitrary.StudbookId().Generate();

        // Act
        Func<Individual> halfKnown = () => Arbitrary.Individual().WithSireId(anySireId).Generate();

        // Assert
        Assert.Throws<ArgumentException>(halfKnown);
    }

}

[Reproducible]
public sealed class PedigreeCalculationTests {

    [Fact]
    public void An_individual_is_one_generation_below_its_most_recent_parent() {
        // Arrange
        Studbook   studbook       = new Studbook();
        Individual anyFounderSire = studbook.Founder(Sex.Male);
        Individual anyFounderDam  = studbook.Founder(Sex.Female);
        Individual anySon         = studbook.Offspring(anyFounderSire, anyFounderDam, Sex.Male);
        Individual anyOtherDam    = studbook.Founder(Sex.Female);
        Individual anyGrandchild  = studbook.Offspring(anySon, anyOtherDam, Any.Enum<Sex>().Generate());

        // Act
        int generation = studbook.Pedigree.GenerationOf(anyGrandchild.Id);

        // Assert
        Assert.Equal(2, generation);
    }

    [Fact]
    public void Full_siblings_share_exactly_their_two_parents_as_common_ancestors() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual anySire    = studbook.Founder(Sex.Male);
        Individual anyDam     = studbook.Founder(Sex.Female);
        Individual anySibling = studbook.Offspring(anySire, anyDam, Any.Enum<Sex>().Generate());
        Individual anyOther   = studbook.Offspring(anySire, anyDam, Any.Enum<Sex>().Generate());

        // Act
        IReadOnlySet<StudbookId> common = studbook.Pedigree.CommonAncestorsOf(anySibling.Id, anyOther.Id);

        // Assert
        Assert.Equal(new HashSet<StudbookId> { anySire.Id, anyDam.Id }, common);
    }

    [Fact]
    public void Two_founders_are_unrelated() {
        // Arrange
        Studbook   studbook       = new Studbook();
        Individual anyFounder     = studbook.Founder(Any.Enum<Sex>().Generate());
        Individual anyOtherFounder = studbook.Founder(Any.Enum<Sex>().Generate());

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(anyFounder.Id, anyOtherFounder.Id);

        // Assert
        Assert.Equal(KinshipCoefficient.Unrelated, kinship);
    }

    [Fact]
    public void Full_siblings_have_a_kinship_of_a_quarter() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual anySire    = studbook.Founder(Sex.Male);
        Individual anyDam     = studbook.Founder(Sex.Female);
        Individual anyBrother = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual anySister  = studbook.Offspring(anySire, anyDam, Sex.Female);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(anyBrother.Id, anySister.Id);

        // Assert
        Assert.Equal(0.25, kinship.Value);
    }

    [Fact]
    public void Half_siblings_have_a_kinship_of_an_eighth() {
        // Arrange
        Studbook   studbook      = new Studbook();
        Individual anySire       = studbook.Founder(Sex.Male);
        Individual anyDam        = studbook.Founder(Sex.Female);
        Individual anyOtherDam   = studbook.Founder(Sex.Female);
        Individual anyHalfBrother = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual anyHalfSister = studbook.Offspring(anySire, anyOtherDam, Sex.Female);

        // Act
        KinshipCoefficient kinship = studbook.Pedigree.KinshipBetween(anyHalfBrother.Id, anyHalfSister.Id);

        // Assert
        Assert.Equal(0.125, kinship.Value);
    }

    [Fact]
    public void The_offspring_of_full_siblings_is_inbred_by_a_quarter() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual anySire    = studbook.Founder(Sex.Male);
        Individual anyDam     = studbook.Founder(Sex.Female);
        Individual anyBrother = studbook.Offspring(anySire, anyDam, Sex.Male);
        Individual anySister  = studbook.Offspring(anySire, anyDam, Sex.Female);
        Individual anyInbred  = studbook.Offspring(anyBrother, anySister, Any.Enum<Sex>().Generate());

        // Act
        double inbreeding = studbook.Pedigree.InbreedingOf(anyInbred.Id);

        // Assert
        Assert.Equal(0.25, inbreeding);
    }

    [Fact]
    public void Mean_kinship_counts_the_individual_itself_among_the_living() {
        // Arrange
        Studbook   studbook        = new Studbook();
        Individual anyFounder      = studbook.Founder(Any.Enum<Sex>().Generate());
        Individual anyOtherFounder = studbook.Founder(Any.Enum<Sex>().Generate());

        // Act
        double meanKinship = studbook.Pedigree.MeanKinshipOf(anyFounder.Id);

        // Assert
        Assert.Equal(0.25, meanKinship);   // (½ with itself + 0 with the other) / 2
    }

    [Fact]
    public void A_founder_without_living_descendants_has_lost_its_contribution() {
        // Arrange
        Studbook   studbook        = new Studbook();
        Individual anySire         = studbook.Founder(Sex.Male);
        Individual anyDam          = studbook.Founder(Sex.Female);
        Individual anyLostFounder  = studbook.Founder(Any.Enum<Sex>().Generate());
        _ = studbook.Offspring(anySire, anyDam, Any.Enum<Sex>().Generate());
        anyLostFounder.RecordDeath(Studbook.LatestFounderBirth.AddYears(1));

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();

        // Assert
        Assert.True(contributions.Single(c => c.FounderId == anyLostFounder.Id).IsLost);
    }

    [Fact]
    public void A_single_offspring_carries_half_of_each_founder_once_its_parents_have_died() {
        // Arrange
        Studbook   studbook = new Studbook();
        Individual anySire  = studbook.Founder(Sex.Male);
        Individual anyDam   = studbook.Founder(Sex.Female);
        Individual anyCalf  = studbook.Offspring(anySire, anyDam, Any.Enum<Sex>().Generate());
        anySire.RecordDeath(anyCalf.BirthDate);
        anyDam.RecordDeath(anyCalf.BirthDate);

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();

        // Assert
        Assert.All(contributions, c => Assert.Equal(0.5, c.Proportion));
    }

    [Fact]
    public void Equally_represented_founders_count_as_that_many_founder_equivalents() {
        // Arrange
        Studbook studbook = new Studbook();
        for (int i = 0; i < 4; i++) {
            studbook.Founder(Any.Enum<Sex>().Generate());
        }

        // Act
        double founderEquivalents = studbook.Pedigree.FounderEquivalents();

        // Assert
        Assert.Equal(4.0, founderEquivalents, precision: 10);
    }

    [Fact]
    public void Founder_contributions_are_undefined_once_nobody_is_alive() {
        // Arrange
        Studbook   studbook   = new Studbook();
        Individual anyFounder = studbook.Founder(Any.Enum<Sex>().Generate());
        anyFounder.RecordDeath(Studbook.LatestFounderBirth.AddYears(1));

        // Act
        Func<IReadOnlyList<GeneticContribution>> contributions = studbook.Pedigree.FounderContributions;

        // Assert
        Assert.Throws<InvalidOperationException>(contributions);
    }

}
