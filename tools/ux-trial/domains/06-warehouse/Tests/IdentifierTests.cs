using JustDummies;
using JustDummies.Xunit;
using Warehouse.Domain;
using Xunit;

namespace Warehouse.Tests;

[Reproducible]
public sealed class IdentifierTests {

    [Fact]
    public void An_SSCC_whose_check_digit_is_off_by_one_is_refused() {
        // Arrange
        string anyValidSscc  = new AnySscc().Generate().Value;
        int    checkDigit    = anyValidSscc[^1] - '0';
        string corruptedSscc = anyValidSscc[..^1] + (checkDigit + 1) % 10;

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Sscc(corruptedSscc));

        // Assert
        Assert.Contains("check digit", refusal.Message);
    }

    [Fact]
    public void The_GS1_check_digit_of_a_published_SSCC_is_recomputed() {
        // The SSCC of the GS1 General Specifications' worked example, 00614141123456789 + check digit 0:
        // a literal, because the assertion is about it.
        // Act
        int checkDigit = Gs1.ComputeCheckDigit("00614141123456789");

        // Assert
        Assert.Equal(0, checkDigit);
    }

    [Fact]
    public void An_EAN_13_reports_its_GS1_country_prefix() {
        // Arrange
        string anyFrenchBody = Any.String().StartingWith("376").Numeric().WithLength(12).Generate();
        Ean13  anyFrenchItem = new Ean13(anyFrenchBody + Gs1.ComputeCheckDigit(anyFrenchBody));

        // Act
        string prefix = anyFrenchItem.CountryPrefix;

        // Assert
        Assert.Equal("376", prefix);
    }

    [Fact]
    public void A_bin_location_prints_rack_and_level_on_two_digits() {
        // Act
        string printed = new BinLocation('A', 12, 3).ToString();

        // Assert
        Assert.Equal("A-12-03", printed);
    }

    [Fact]
    public void A_bin_location_is_parsed_from_its_printed_form() {
        // Act
        BinLocation parsed = BinLocation.Parse("B-07-15");

        // Assert
        Assert.Equal(('B', 7, 15), (parsed.Aisle, parsed.Rack, parsed.Level));
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    public void Only_levels_above_four_need_a_reach_truck(int level, bool expected) {
        // Arrange
        BinLocation anyBinAtThatLevel = new AnyBinLocation().WithLevel(level).Generate();

        // Act
        bool needsReachTruck = anyBinAtThatLevel.NeedsReachTruck;

        // Assert
        Assert.Equal(expected, needsReachTruck);
    }

    [Theory]
    [InlineData(-20.0, StorageZone.Frozen)]
    [InlineData(-18.0, StorageZone.Frozen)]
    [InlineData(8.0, StorageZone.Chilled)]
    [InlineData(8.1, StorageZone.Ambient)]
    public void The_storage_zone_follows_the_maximum_temperature(double maxCelsius, StorageZone expected) {
        // Arrange
        decimal          max                = (decimal)maxCelsius;
        TemperatureRange anyRangeWithThatMax = new AnyTemperatureRange()
                                               .WithMaxCelsius(max)
                                               .WithMinCelsius(min => min.LessThan(max))
                                               .Generate();

        // Act
        StorageZone zone = anyRangeWithThatMax.Zone;

        // Assert
        Assert.Equal(expected, zone);
    }

}
