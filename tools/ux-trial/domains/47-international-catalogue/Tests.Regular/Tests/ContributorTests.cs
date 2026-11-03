using InternationalCatalogue.Domain;

using JustDummies;

using Xunit;

namespace InternationalCatalogue.Tests;

public sealed class ContributorTests {

    [Fact]
    public void A_contributor_has_a_name_and_a_role() {
        // Arrange
        PersonName name = new PersonName("Calvino", "Italo");

        // Act
        Contributor contributor = new Contributor(name, ContributorRole.Translator);

        // Assert
        Assert.Same(name, contributor.Name);
        Assert.Equal(ContributorRole.Translator, contributor.Role);
        Assert.Equal("Calvino, Italo (Translator)", contributor.ToString());
    }

    [Fact]
    public void A_null_name_is_refused() {
        // Arrange
        ContributorRole anyRole = Any.Enum<ContributorRole>().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Contributor(null!, anyRole));
    }

    [Fact]
    public void An_undefined_role_is_refused() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Contributor(anyName, (ContributorRole)99));

        // Assert
        Assert.Equal("role", refusal.ParamName);
    }

    [Fact]
    public void Contributors_are_equal_when_name_and_role_are_equal() {
        // Assert
        Assert.Equal(new Contributor(new PersonName("Müller", "Anne"), ContributorRole.Author),
                     new Contributor(new PersonName("MÜLLER", "ANNE"), ContributorRole.Author));
        Assert.Equal(new Contributor(new PersonName("Müller"), ContributorRole.Editor).GetHashCode(),
                     new Contributor(new PersonName("MÜLLER"), ContributorRole.Editor).GetHashCode());
    }

    [Fact]
    public void A_different_role_or_a_different_name_makes_contributors_different() {
        // Arrange
        PersonName anyName      = Any.PersonName().Generate();
        PersonName anotherName  = new PersonName("Zz", "Zz");

        // Assert
        Assert.NotEqual(new Contributor(anyName, ContributorRole.Author), new Contributor(anyName, ContributorRole.Illustrator));
        Assert.NotEqual(new Contributor(anyName, ContributorRole.Author), new Contributor(anotherName, ContributorRole.Author));
    }

}
