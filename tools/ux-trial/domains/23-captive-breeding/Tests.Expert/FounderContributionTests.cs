using CaptiveBreeding.Domain;
using CaptiveBreeding.Tests.Expert.Scenarios;

namespace CaptiveBreeding.Tests.Expert;

public sealed class FounderContributionTests {

    [Fact]
    public void Two_founders_survived_only_by_their_offspring_each_contribute_half_the_living_genes() {
        // Arrange
        Studbook   studbook = new();
        Individual anySire  = studbook.Male();
        Individual anyDam   = studbook.Female();
        studbook.OffspringOf(anySire, anyDam);
        studbook.OffspringOf(anySire, anyDam);
        studbook.RecordDeathOf(anySire, anyDam);

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();

        // Assert
        Assert.Equal(0.5, ContributionOf(anySire, contributions));
        Assert.Equal(0.5, ContributionOf(anyDam, contributions));
    }

    [Fact]
    public void A_founder_that_died_without_descendants_is_a_lost_lineage() {
        // Arrange
        Studbook   studbook       = new();
        Individual anySire        = studbook.Male();
        Individual anyDam         = studbook.Female();
        Individual anyLostFounder = studbook.Founder();
        studbook.OffspringOf(anySire, anyDam);
        studbook.RecordDeathOf(anyLostFounder);

        // Act
        IReadOnlyList<GeneticContribution> contributions = studbook.Pedigree.FounderContributions();

        // Assert
        Assert.True(contributions.Single(c => c.FounderId == anyLostFounder.Id).IsLost);
    }

    [Fact]
    public void Two_equally_represented_founders_are_worth_two_founder_equivalents() {
        // Arrange
        Studbook   studbook = new();
        Individual anySire  = studbook.Male();
        Individual anyDam   = studbook.Female();
        studbook.OffspringOf(anySire, anyDam);
        studbook.RecordDeathOf(anySire, anyDam);

        // Act
        double founderEquivalents = studbook.Pedigree.FounderEquivalents();

        // Assert
        Assert.Equal(2.0, founderEquivalents);
    }

    private static double ContributionOf(Individual founder, IReadOnlyList<GeneticContribution> contributions) {
        return contributions.Single(c => c.FounderId == founder.Id).Proportion;
    }

}
