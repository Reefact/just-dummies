using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class ContributorTests {

    [Fact]
    public void A_contributor_needs_a_name() {
        // Arrange
        ContributorRole anyRole = Any.Enum<ContributorRole>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Contributor(null!, anyRole));
    }

    [Fact]
    public void A_contributor_needs_a_defined_role() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Contributor(anyName, (ContributorRole)99));
    }

    [Fact]
    public void A_contributor_is_equal_to_one_with_an_equal_name_and_the_same_role() {
        // Arrange
        Contributor lower = new Contributor(new PersonName("tolstoï", "léon"), ContributorRole.Author);
        Contributor upper = new Contributor(new PersonName("TOLSTOÏ", "LÉON"), ContributorRole.Author);

        // Assert
        Assert.Equal(lower, upper);
        Assert.Equal(lower.GetHashCode(), upper.GetHashCode());
    }

    [Fact]
    public void The_same_person_in_another_role_is_another_contributor() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Assert
        Assert.NotEqual(new Contributor(anyName, ContributorRole.Author), new Contributor(anyName, ContributorRole.Translator));
    }

    [Fact]
    public void Another_person_in_the_same_role_is_another_contributor() {
        // Arrange
        Contributor tolstoi = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Editor);
        Contributor tchekhov = new Contributor(new PersonName("Tchekhov", "Anton"), ContributorRole.Editor);

        // Assert
        Assert.NotEqual(tolstoi, tchekhov);
    }

    [Fact]
    public void A_contributor_is_written_with_its_heading_and_its_role() {
        // Act
        Contributor contributor = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Author);

        // Assert
        Assert.Equal("Tolstoï, Léon (Author)", contributor.ToString());
    }

    [Fact]
    public void A_contributor_is_not_equal_to_nothing() {
        // Arrange
        Contributor anyContributor = Any.Contributor().Generate();

        // Assert
        Assert.False(anyContributor.Equals(null));
    }

}
