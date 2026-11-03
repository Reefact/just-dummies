using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Domain.Tests.Discovery;

[Reproducible]
public sealed class ContributorTests {

    [Fact]
    public void A_contributor_has_a_name_and_a_role() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        Contributor contributor = new Contributor(anyName, ContributorRole.Translator);

        // Assert
        Assert.Same(anyName, contributor.Name);
        Assert.Equal(ContributorRole.Translator, contributor.Role);
    }

    [Fact]
    public void A_contributor_needs_a_name() {
        Assert.Throws<ArgumentNullException>(() => new Contributor(null!, ContributorRole.Author));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(99)]
    public void A_contributor_refuses_a_role_the_catalogue_does_not_define(int undefined) {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Contributor(anyName, (ContributorRole)undefined));

        // Assert
        Assert.Equal("role", error.ParamName);
    }

    [Fact]
    public void Two_contributors_are_equal_when_name_and_role_are() {
        // Arrange
        PersonName name = new PersonName("Müller", "Hans");
        PersonName sameNameInCapitals = new PersonName("MÜLLER", "HANS");

        // Act
        Contributor author = new Contributor(name, ContributorRole.Author);
        Contributor sameAuthor = new Contributor(sameNameInCapitals, ContributorRole.Author);

        // Assert
        Assert.Equal(author, sameAuthor);
        Assert.Equal(author.GetHashCode(), sameAuthor.GetHashCode());
    }

    [Fact]
    public void The_same_person_in_another_role_is_another_contributor() {
        // Arrange
        PersonName anyName = Any.PersonName().Generate();

        // Act
        Contributor author = new Contributor(anyName, ContributorRole.Author);
        Contributor editor = new Contributor(anyName, ContributorRole.Editor);

        // Assert
        Assert.NotEqual(author, editor);
    }

    [Fact]
    public void A_contributor_is_described_by_its_heading_and_role() {
        // Act
        Contributor contributor = new Contributor(new PersonName("Tolstoï", "Léon"), ContributorRole.Illustrator);

        // Assert
        Assert.Equal("Tolstoï, Léon (Illustrator)", contributor.ToString());
    }

}
