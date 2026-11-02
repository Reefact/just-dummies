using JustDummies;

using RealEstate.Domain;

using Xunit;

namespace RealEstate.Tests.Discovery;

// The generators are test code too: these check they only ever build what the domain accepts.
public sealed class GeneratorsTests {

    private const int Draws = 300;

    [Fact]
    public void Every_generator_draws_values_the_domain_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Any.CadastralReference().Generate();
            Any.Surface().Generate();
            Any.EnergyPerformanceDiagnosis().Generate();
            Any.DocumentLink().Generate();
            Any.Transfer().Generate();
            Any.Property().Generate();
            Any.RentalListing().Generate();
        }
    }

    [Fact]
    public void A_generated_diagnosis_can_have_every_rating() {
        // Arrange
        HashSet<EnergyRating> seen = new HashSet<EnergyRating>();

        // Act
        for (int draw = 0; draw < 2_000; draw++) {
            seen.Add(Any.EnergyPerformanceDiagnosis().Generate().Rating);
        }

        // Assert
        Assert.Equal(Enum.GetValues<EnergyRating>().Length, seen.Count);
    }

    [Fact]
    public void A_rating_asked_for_is_the_rating_drawn() {
        foreach (EnergyRating rating in Enum.GetValues<EnergyRating>()) {
            for (int draw = 0; draw < 50; draw++) {
                Assert.Equal(rating, Any.EnergyPerformanceDiagnosis().OfRating(rating).Generate().Rating);
            }
        }
    }

    [Fact]
    public void A_generated_listing_is_always_lettable_on_its_start_date() {
        for (int draw = 0; draw < Draws; draw++) {
            RentalListing listing = Any.RentalListing().Generate();

            Assert.True(listing.Property.CanBeLetOn(listing.AvailableFrom));
        }
    }

    [Fact]
    public void A_generated_property_never_exceeds_the_document_limit_nor_repeats_a_location() {
        for (int draw = 0; draw < Draws; draw++) {
            Property property = Any.Property().Generate();

            Assert.InRange(property.Documents.Count, 0, Property.MaxDocuments);
            Assert.Equal(property.Documents.Count, property.Documents.Select(document => document.Location).Distinct().Count());
        }
    }

}
