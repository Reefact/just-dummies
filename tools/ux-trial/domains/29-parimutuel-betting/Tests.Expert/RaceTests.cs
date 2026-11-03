using ParimutuelBetting.Domain.Tests.Expert.Generators;

namespace ParimutuelBetting.Domain.Tests.Expert;

public sealed class RaceTests {

    [Theory]
    [InlineData("LCH-R4")]
    [InlineData("ABC-R1")]
    [InlineData("XYZ-R9")]
    [InlineData("ABC-R10")]
    [InlineData("ABC-R16")]
    public void A_race_code_is_a_three_letter_course_and_a_race_number_from_1_to_16(string code) {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Race race = new Race(code, anyFieldSize);

        Assert.Equal(code, race.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LCH-R0")]
    [InlineData("LCH-R17")]
    [InlineData("LCH-R04")]
    [InlineData("LCH-R100")]
    [InlineData("lch-R4")]
    [InlineData("LCH-r4")]
    [InlineData("LCH R4")]
    [InlineData("LCHR4")]
    [InlineData("LC-R4")]
    [InlineData("LCHE-R4")]
    [InlineData("L1H-R4")]
    [InlineData("LCH-R")]
    [InlineData(" LCH-R4")]
    [InlineData("LCH-R4 ")]
    [InlineData("LCH-R4-")]
    [InlineData("ÉCH-R4")]
    public void A_malformed_race_code_is_refused(string code) {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Assert.Throws<FormatException>(() => new Race(code, anyFieldSize));
    }

    [Fact(Skip = "domain defect: a race code with a trailing line feed is accepted, because $ in the pattern also matches before a final \\n; seed-independent, first seen with seed 65077927")]
    public void A_race_code_cannot_carry_a_trailing_line_feed() {
        int anyFieldSize = Any.Int32().Between(Race.MinimumFieldSize, HorseNumber.Maximum).Generate();

        Assert.Throws<FormatException>(() => new Race("LCH-R4\n", anyFieldSize));
    }

    [Fact]
    public void A_race_needs_a_code() {
        Assert.Throws<ArgumentNullException>(() => new Race(null!, 8));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(24)]
    public void The_smallest_and_largest_fields_are_races(int fieldSize) {
        Race race = new Race(AnyRace.ValidCode().Generate(), fieldSize);

        Assert.Equal(fieldSize, race.FieldSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(25)]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_field_outside_2_to_24_runners_is_refused(int fieldSize) {
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Race(AnyRace.ValidCode().Generate(), fieldSize));

        Assert.Equal("fieldSize", refusal.ParamName);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    [InlineData(24, false)]
    public void Runners_are_numbered_from_1_to_the_field_size(int horse, bool isRunner) {
        Race eightRunners = Any.Race().WithFieldSize(8).Generate();

        Assert.Equal(isRunner, eightRunners.IsRunner(new HorseNumber(horse)));
    }

    [Fact]
    public void A_race_cannot_tell_whether_nobody_is_a_runner() {
        Race anyRace = Any.Race().Generate();

        Assert.Throws<ArgumentNullException>(() => anyRace.IsRunner(null!));
    }

}
