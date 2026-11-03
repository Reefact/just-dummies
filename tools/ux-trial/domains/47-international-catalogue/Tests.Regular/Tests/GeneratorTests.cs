using InternationalCatalogue.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace InternationalCatalogue.Tests;

/// <summary>The generators are test code too: what they draw must be something the domain accepts, whatever the seed.</summary>
public sealed class GeneratorTests {

    [Fact]
    public void Every_scaffolded_and_completed_generator_draws_values_its_constructor_accepts() {
        // Act
        void DrawMany() {
            for (int i = 0; i < 300; i++) {
                Any.RecordId().Generate();
                Any.Title().Generate();
                Any.PersonName().Generate();
                Any.Contributor().Generate();
                Any.LanguageTag().Generate();
                Any.Isbn13().Generate();
                Any.Isbn10().Generate();
                Any.Issn().Generate();
                Any.CallNumber().Generate();
                Any.ArticleRule().Generate();
                Any.BibliographicRecord().Generate();
                Any.Catalogue().Generate();
            }
        }

        // Assert
        Exception? failure = Record.Exception(DrawMany);
        Assert.Null(failure);
    }

}
