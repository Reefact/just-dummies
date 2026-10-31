using JustDummies;
using RoboticMilking.Domain;
using Xunit;

namespace RoboticMilking.Tests.Regular;

public sealed class ValueObjectTests
{
    [Fact]
    public void An_ear_tag_tells_the_country_of_the_cow()
    {
        // Arrange
        // An AnyPattern offers no StartingWith: the lambda has to replace the base with a pattern of its own.
        AnimalId anyFrenchTag = Arbitrary.AnimalId().WithValue(_ => Any.StringMatching("FR[0-9]{12}")).Generate();

        // Act
        string country = anyFrenchTag.CountryCode;

        // Assert
        Assert.Equal("FR", country);
    }

    [Fact]
    public void Too_short_a_stimulation_is_refused_for_milk_let_down()
    {
        // Arrange
        AnyMilkingSettings anySettings = Arbitrary.MilkingSettings()
                                                  .WithCleaningTime(TimeSpan.FromSeconds(5))
                                                  .WithLetDownDelay(TimeSpan.FromSeconds(9));

        // Act
        Action create = () => anySettings.Generate();

        // Assert
        Assert.Throws<ArgumentException>("letDownDelay", create);
    }

    [Fact]
    public void A_short_cleaning_time_is_accepted_when_the_delay_makes_up_the_stimulation()
    {
        // Arrange
        AnyMilkingSettings anySettings = Arbitrary.MilkingSettings()
                                                  .WithCleaningTime(TimeSpan.FromSeconds(5))
                                                  .WithLetDownDelay(TimeSpan.FromSeconds(10));

        // Act
        MilkingSettings settings = anySettings.Generate();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), settings.CleaningTime);
    }

    [Fact]
    public void A_conductivity_above_seven_and_a_half_suggests_mastitis()
    {
        // Arrange
        MilkFlowReading anyReading = Arbitrary.MilkFlowReading().WithConductivityMsPerCm(7.6m).Generate();

        // Act
        bool suggestsMastitis = anyReading.SuggestsMastitis;

        // Assert
        Assert.True(suggestsMastitis);
    }

    [Fact]
    public void A_cow_with_four_blind_quarters_cannot_be_milked()
    {
        // Arrange
        AnyAnimal anyCow = Arbitrary.Animal().WithBlindQuarters(Enum.GetValues<UdderQuarter>());

        // Act
        Action create = () => anyCow.Generate();

        // Assert
        Assert.Throws<ArgumentException>("blindQuarters", create);
    }
}
