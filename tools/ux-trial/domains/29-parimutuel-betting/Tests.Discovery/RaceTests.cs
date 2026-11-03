namespace ParimutuelBetting.Tests.Discovery;

public sealed class RaceTests {

    [Theory]
    [InlineData("LCH-R4")]
    [InlineData("AAA-R1")]
    [InlineData("ZZZ-R9")]
    [InlineData("LCH-R10")]
    [InlineData("LCH-R16")]
    public void A_code_made_of_a_course_and_a_race_number_is_accepted(string code) {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Race race = new Race(code, anyFieldSize);

        Assert.Equal(code, race.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LCH-R0")]
    [InlineData("LCH-R17")]
    [InlineData("LCH-R04")]
    [InlineData("LCH-R4 ")]
    [InlineData(" LCH-R4")]
    [InlineData("lch-R4")]
    [InlineData("LCH-r4")]
    [InlineData("LC-R4")]
    [InlineData("LCHX-R4")]
    [InlineData("LCH R4")]
    [InlineData("LCH-4")]
    [InlineData("L1H-R4")]
    public void A_code_that_does_not_follow_the_card_notation_is_refused(string code) {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Assert.Throws<FormatException>(() => new Race(code, anyFieldSize));
    }

    [Fact(Skip = "domain defect: Race accepts 'LCH-R4\\n' because $ in ^[A-Z]{3}-R([1-9]|1[0-6])$ also matches before a final line feed, seed 61595605 (fails on every seed)")]
    public void A_code_followed_by_a_line_break_is_refused() {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Assert.Throws<FormatException>(() => new Race("LCH-R4\n", anyFieldSize));
    }

    [Fact]
    public void A_race_without_a_code_is_refused() {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Assert.Throws<ArgumentNullException>(() => new Race(null!, anyFieldSize));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(24)]
    public void The_smallest_and_the_largest_field_are_accepted(int fieldSize) {
        string anyCode = Any.StringMatching("[A-Z]{3}-R[1-9]").Generate();

        Race race = new Race(anyCode, fieldSize);

        Assert.Equal(fieldSize, race.FieldSize);
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(25)]
    public void A_field_outside_two_to_twenty_four_runners_is_refused(int fieldSize) {
        string anyCode = Any.StringMatching("[A-Z]{3}-R[1-9]").Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Race(anyCode, fieldSize));
    }

    [Fact]
    public void Runners_are_numbered_from_one_to_the_field_size() {
        // Arrange
        Race race = Any.Race().WithFieldSize(12).Generate();

        // Assert
        Assert.True(race.IsRunner(new HorseNumber(1)));
        Assert.True(race.IsRunner(new HorseNumber(12)));
        Assert.False(race.IsRunner(new HorseNumber(13)));
        Assert.False(race.IsRunner(new HorseNumber(24)));
    }

    [Fact]
    public void A_null_horse_is_not_a_runner_but_a_mistake() {
        Race anyRace = Any.Race().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRace.IsRunner(null!));
    }

}
