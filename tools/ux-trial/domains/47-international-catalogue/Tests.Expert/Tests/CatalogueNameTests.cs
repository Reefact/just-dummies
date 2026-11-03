using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

public sealed class CatalogueNameTests {

    [Fact]
    public void A_catalogue_starts_empty() {
        // Arrange
        Catalogue catalogue = Any.Catalogue().Generate();

        // Assert
        Assert.Equal(0, catalogue.Count);
        Assert.Empty(catalogue.Records());
    }

    [Fact]
    public void A_catalogue_name_is_kept_in_normalisation_form_C_and_trimmed() {
        // Act
        Catalogue catalogue = new Catalogue("  Cafe\u0301 des livres  ");

        // Assert
        Assert.Equal("Caf\u00E9 des livres", catalogue.Name);
    }

    [Fact]
    public void A_catalogue_name_is_required() {
        Assert.Throws<ArgumentNullException>(() => new Catalogue(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_catalogue_needs_a_name(string blank) {
        Assert.Throws<ArgumentException>(() => new Catalogue(blank));
    }

    [Theory]
    [InlineData("Union\u0000catalogue")]
    [InlineData("Union\tcatalogue")]
    [InlineData("Union\ncatalogue")]
    [InlineData("Union\rcatalogue")]
    [InlineData("Union\u2028catalogue")]
    [InlineData("Union\u2029catalogue")]
    [InlineData("Union\u0085catalogue")]
    public void A_catalogue_name_cannot_contain_a_control_or_line_break_character(string withControl) {
        Assert.Throws<ArgumentException>(() => new Catalogue(withControl));
    }

    [Fact]
    public void A_catalogue_name_cannot_contain_an_unpaired_surrogate() {
        Assert.Throws<ArgumentException>(() => new Catalogue("Union \uD800 catalogue"));
    }

    [Fact]
    public void A_catalogue_name_may_hold_120_grapheme_clusters() {
        // Act
        Catalogue catalogue = new Catalogue(new string('a', Catalogue.MaxNameLength));

        // Assert
        Assert.Equal(120, catalogue.Name.Length);
    }

    [Fact]
    public void A_catalogue_name_of_121_grapheme_clusters_is_too_long() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Catalogue(new string('a', 121)));
    }

    [Fact]
    public void A_catalogue_name_counts_supplementary_ideographs_once() {
        // Arrange
        string oneHundredTwentyIdeographs = string.Concat(Enumerable.Repeat("\U00020000", 120));

        // Act
        Catalogue catalogue = new Catalogue(oneHundredTwentyIdeographs);

        // Assert
        Assert.Equal(oneHundredTwentyIdeographs, catalogue.Name);
    }

    [Fact]
    public void A_catalogue_may_be_named_in_any_script() {
        // Act
        Catalogue catalogue = new Catalogue("Российская государственная библиотека");

        // Assert
        Assert.Equal("Российская государственная библиотека", catalogue.Name);
    }

}
