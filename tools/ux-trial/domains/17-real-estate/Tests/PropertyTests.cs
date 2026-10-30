using JustDummies;
using RealEstate.Domain;
using Xunit;

namespace RealEstate.Tests;

public sealed class PropertyTests {

    private static readonly DateOnly IssuedIn2023 = new DateOnly(2023, 5, 1);

    [Fact]
    public void A_G_rated_home_may_no_longer_be_let_from_2025() {
        // Arrange: house or apartment, the calendar treats them alike.
        Property anyGHome = Any.Property()
                               .WithKind(k => k.Except(PropertyKind.Commercial))
                               .WithDiagnosis(d => d.WithDeclaredRating(EnergyRating.G).WithIssuedOn(IssuedIn2023))
                               .Generate();

        // Act
        bool lettable = anyGHome.CanBeLetOn(new DateOnly(2025, 1, 1));

        // Assert
        Assert.False(lettable);
    }

    [Fact]
    public void Commercial_premises_are_outside_the_letting_calendar() {
        // Arrange
        Property anyGShop = Any.Property()
                               .WithKind(PropertyKind.Commercial)
                               .WithDiagnosis(d => d.WithDeclaredRating(EnergyRating.G).WithIssuedOn(IssuedIn2023))
                               .Generate();

        // Act
        bool lettable = anyGShop.CanBeLetOn(new DateOnly(2030, 1, 1));

        // Assert
        Assert.True(lettable);
    }

    [Fact]
    public void A_document_is_attached_only_once() {
        // Arrange: the folder must have room for two more, the attachment and the repeat, so a full folder
        // cannot answer first. That is a precondition, not the point.
        Property     anyProperty = Any.Property().WithDocuments(l => l.WithMaxCount(Property.MaxDocuments - 2)).Generate();
        DocumentLink anyDocument = Arbitrary.DocumentLink().Generate();
        anyProperty.Attach(anyDocument);

        // Act
        Action attachAgain = () => anyProperty.Attach(anyDocument);

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(attachAgain);
        Assert.Equal("That document is already attached.", refusal.Message);
    }

    [Fact]
    public void A_full_folder_refuses_another_document() {
        // Arrange
        Property     anyFullProperty = Any.Property().WithDocuments(l => l.WithCount(Property.MaxDocuments)).Generate();
        DocumentLink anyDocument     = Arbitrary.DocumentLink().Generate();

        // Act
        Action attach = () => anyFullProperty.Attach(anyDocument);

        // Assert
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(attach);
        Assert.Equal("The document folder is full.", refusal.Message);
    }

    [Fact]
    public void An_older_diagnosis_cannot_replace_the_current_one() {
        // Arrange
        Property                   anyProperty     = Any.Property().WithDiagnosis(d => d.WithIssuedOn(IssuedIn2023)).Generate();
        EnergyPerformanceDiagnosis anyOlderOne     = Arbitrary.EnergyPerformanceDiagnosis().WithIssuedOn(new DateOnly(2022, 1, 1)).Generate();

        // Act
        Action rediagnose = () => anyProperty.Rediagnose(anyOlderOne);

        // Assert
        Assert.Throws<InvalidOperationException>(rediagnose);
    }

    [Fact]
    public void The_price_per_square_metre_divides_by_the_living_area() {
        // Arrange
        Property anyFiftyMetreFlat = Any.Property().WithLivingArea(new Surface(50.00)).Generate();

        // Act
        decimal pricePerSquareMeter = anyFiftyMetreFlat.PricePerSquareMeter(250_000m);

        // Assert
        Assert.Equal(5_000.00m, pricePerSquareMeter);
    }
}
