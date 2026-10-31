using ParimutuelBetting.Domain;

using Xunit;

namespace ParimutuelBetting.Tests.Regular;

public sealed class RaceTests {

    [Theory]
    [InlineData("LCH-R17")]
    [InlineData("lch-R4")]
    [InlineData("LCH-4")]
    public void A_race_code_outside_the_card_format_is_refused(string code) {
        // Arrange
        int anyFieldSize = Arbitrary.Race().Generate().FieldSize;

        // Act
        Action create = () => new Race(code, anyFieldSize);

        // Assert
        Assert.Throws<FormatException>(create);
    }

    [Fact]
    public void A_horse_numbered_beyond_the_field_is_not_a_runner() {
        // Arrange
        Race anyRaceOfEightRunners = Arbitrary.Race().WithFieldSize(8).Generate();

        // Act
        bool isRunner = anyRaceOfEightRunners.IsRunner(new HorseNumber(9));

        // Assert
        Assert.False(isRunner);
    }

    [Fact]
    public void The_last_declared_runner_is_a_runner() {
        // Arrange
        Race anyRaceOfEightRunners = Arbitrary.Race().WithFieldSize(8).Generate();

        // Act
        bool isRunner = anyRaceOfEightRunners.IsRunner(new HorseNumber(8));

        // Assert
        Assert.True(isRunner);
    }

}
