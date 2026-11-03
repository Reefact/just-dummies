using JustDummies;

using Xunit;

namespace InternationalCatalogue.Domain.Tests;

/// <summary>
///     The generators are code the team owns, and every other test trusts them: a generator that drew a value the
///     domain refuses would turn up as a failure far from its cause. These draw many values to say so early.
/// </summary>
public sealed class GeneratorSanityTests {

    [Fact]
    public void Every_dummy_the_catalogue_tests_rely_on_can_be_drawn_over_and_over() {
        // Arrange
        // Nothing to assert on the values: the constructors of the domain are the assertions, and any refusal fails the draw.

        // Act
        Exception? refusal = Record.Exception(() => {
            for (int draw = 0; draw < 300; draw++) {
                Any.BibliographicRecord().Generate();
                Any.Contributor().Generate();
                Any.Isbn10().Generate();
                Any.CallNumber().Generate();
                Any.ListingCollation().Generate();
                Any.Catalogue().Generate();
            }
        });

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_record_follows_the_script_of_the_title_it_is_given() {
        // Arrange
        Title cyrillicTitle = new Title("Война и мир");

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithTitle(cyrillicTitle).Generate();

        // Assert
        Assert.Equal(WritingScript.Cyrillic, record.Script);
    }

    [Fact]
    public void A_record_follows_the_script_of_the_language_tag_it_is_given() {
        // Arrange
        LanguageTag serbianInCyrillic = new LanguageTag("sr-Cyrl");

        // Act
        BibliographicRecord record = Any.BibliographicRecord().WithLanguage(serbianInCyrillic).Generate();

        // Assert
        Assert.Equal(WritingScript.Cyrillic, record.Script);
    }

}
