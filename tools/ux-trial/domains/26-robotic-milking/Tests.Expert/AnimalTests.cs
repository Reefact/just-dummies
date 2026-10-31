namespace RoboticMilking.Tests.Expert;

public sealed class AnimalTests {

    [Fact]
    public void An_ear_tag_names_its_country_by_its_first_two_letters() {
        // Arrange
        AnimalId id = new AnimalId("FR123456789012");

        // Act
        string countryCode = id.CountryCode;

        // Assert
        Assert.Equal("FR", countryCode);
    }

    [Theory]
    [InlineData("fr123456789012")]
    [InlineData("FR12345678901")]
    [InlineData("F1234567890123")]
    public void An_ear_tag_other_than_two_capitals_and_twelve_digits_is_refused(string malformed) {
        // Act
        Exception? refusal = Record.Exception(() => new AnimalId(malformed));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void Blind_quarters_are_not_milkable() {
        // Arrange
        AnimalId        anyId           = Any.AnimalId().Generate();
        int             anyLactation    = Any.Int32().Between(1, Animal.MaxLactationNumber).Generate();
        int             anyDaysInMilk   = Any.Int32().Between(0, Animal.MaxDaysInMilk).Generate();
        DateTimeOffset? anyLastMilkedAt = Any.DateTimeOffset().OrNull().Generate();

        // Act
        Animal cow = new Animal(anyId, anyLactation, anyDaysInMilk, [UdderQuarter.LeftRear, UdderQuarter.LeftRear], anyLastMilkedAt);

        // Assert
        Assert.Equal([UdderQuarter.LeftFront, UdderQuarter.RightFront, UdderQuarter.RightRear], cow.MilkableQuarters);
        Assert.False(cow.IsMilkable(UdderQuarter.LeftRear));
    }

    [Fact]
    public void A_cow_with_four_blind_quarters_is_refused() {
        // Arrange
        AnyAnimal anyCowBlindEverywhere = Any.Animal().WithBlindQuarters(Enum.GetValues<UdderQuarter>());

        // Act
        Exception? refusal = Record.Exception(() => anyCowBlindEverywhere.Generate());

        // Assert
        ArgumentException argumentError = Assert.IsType<ArgumentException>(refusal);
        Assert.Equal("blindQuarters", argumentError.ParamName);
    }

}
