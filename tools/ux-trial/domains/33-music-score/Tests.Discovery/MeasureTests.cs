using JustDummies;

using MusicScore.Domain;

using Xunit;

using static MusicScore.Tests.Discovery.Scores;

namespace MusicScore.Tests.Discovery;

public sealed class MeasureTests {

    [Fact]
    public void A_measure_whose_voices_all_fill_the_meter_is_built() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice melody   = FourQuarters(1, anyPitch);
        Voice bass     = new Voice(2, [Note(anyPitch, new Duration(1, 2)), Note(anyPitch, new Duration(1, 2))]);
        Dynamic anyDynamic = Any.Enum<Dynamic>().Generate();

        // Act
        Measure measure = new Measure(3, FourFour, anyDynamic, [melody, bass], repeatStart: false, repeatEndTimes: 0);

        // Assert
        Assert.Equal(3, measure.Number);
        Assert.Equal(anyDynamic, measure.Dynamic);
        Assert.Equal(new Duration(1, 1), measure.Length);
        Assert.Equal(2, measure.Voices.Count);
        Assert.False(measure.RepeatEnd);
    }

    [Fact]
    public void A_voice_that_falls_short_of_the_meter_is_refused_with_both_lengths() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice threeQuarters = new Voice(1, [Note(anyPitch, Quarter), Note(anyPitch, Quarter), Note(anyPitch, Quarter)]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Bar(1, threeQuarters));

        // Assert
        Assert.Contains("3/4", error.Message);
        Assert.Contains("1/1", error.Message);
    }

    [Fact]
    public void A_voice_that_overflows_the_meter_is_refused() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice fiveQuarters = new Voice(1, [Note(anyPitch, Quarter), Note(anyPitch, Quarter), Note(anyPitch, Quarter), Note(anyPitch, Quarter), Note(anyPitch, Quarter)]);

        // Assert
        Assert.Throws<ArgumentException>(() => Bar(1, fiveQuarters));
    }

    [Fact]
    public void Every_voice_must_fill_the_meter_not_only_the_first() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice full  = FourQuarters(1, anyPitch);
        Voice short_ = new Voice(2, [Note(anyPitch, new Duration(1, 2))]);

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Bar(1, full, short_));

        // Assert
        Assert.Contains("Voice 2", error.Message);
    }

    [Fact]
    public void The_same_voices_fit_a_meter_only_when_it_lasts_as_long() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice sixEighths = new Voice(1, [Note(anyPitch, new Duration(3, 8)), Note(anyPitch, new Duration(3, 8))]);

        // Assert: 3/4 and 6/8 last the same, 4/4 does not
        Assert.Equal(new Duration(3, 4), Bar(1, new TimeSignature(6, 8), sixEighths).Length);
        Assert.Equal(new Duration(3, 4), Bar(1, new TimeSignature(3, 4), sixEighths).Length);
        Assert.Throws<ArgumentException>(() => Bar(1, FourFour, sixEighths));
    }

    [Fact]
    public void Two_voices_cannot_share_a_number() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();

        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Bar(7, FourQuarters(2, anyPitch), FourQuarters(2, anyPitch)));

        // Assert
        Assert.Contains("Measure 7", error.Message);
    }

    [Fact]
    public void A_measure_needs_a_voice_and_no_null_voice() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Dynamic anyDynamic = Any.Enum<Dynamic>().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Measure(1, FourFour, anyDynamic, [], false, 0));
        Assert.Throws<ArgumentException>(() => new Measure(1, FourFour, anyDynamic, [FourQuarters(1, anyPitch), null!], false, 0));
        Assert.Throws<ArgumentNullException>(() => new Measure(1, FourFour, anyDynamic, null!, false, 0));
        Assert.Throws<ArgumentNullException>(() => new Measure(1, null!, anyDynamic, [FourQuarters(1, anyPitch)], false, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void A_measure_is_numbered_from_one(int number) {
        // Arrange
        Voice anyVoice = FourQuarters(1, Any.Pitch().Generate());

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Bar(number, anyVoice));
    }

    [Fact]
    public void An_undefined_dynamic_is_refused() {
        // Arrange
        Voice anyVoice = FourQuarters(1, Any.Pitch().Generate());

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Measure(1, FourFour, (Dynamic)99, [anyVoice], false, 0));

        // Assert
        Assert.Equal("dynamic", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(8)]
    public void An_end_repeat_plays_its_section_two_to_eight_times_and_zero_means_none(int repeatEndTimes) {
        // Arrange
        Voice anyVoice = FourQuarters(1, Any.Pitch().Generate());

        // Act
        Measure measure = new Measure(1, FourFour, Any.Enum<Dynamic>().Generate(), [anyVoice], repeatStart: false, repeatEndTimes);

        // Assert
        Assert.Equal(repeatEndTimes, measure.RepeatEndTimes);
        Assert.Equal(repeatEndTimes > 0, measure.RepeatEnd);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(-1)]
    [InlineData(100)]
    public void An_end_repeat_count_other_than_zero_or_two_to_eight_is_refused(int repeatEndTimes) {
        // Arrange
        Voice anyVoice = FourQuarters(1, Any.Pitch().Generate());

        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new Measure(1, FourFour, Any.Enum<Dynamic>().Generate(), [anyVoice], false, repeatEndTimes));

        // Assert
        Assert.Equal("repeatEndTimes", error.ParamName);
    }

    [Fact]
    public void A_measure_orders_its_voices_by_number_and_finds_them() {
        // Arrange
        Pitch anyPitch = Any.Pitch().Generate();
        Voice third = FourQuarters(3, anyPitch);
        Voice first = FourQuarters(1, anyPitch);

        // Act
        Measure measure = Bar(1, third, first);

        // Assert
        Assert.Equal(new[] { 1, 3 }, measure.Voices.Select(voice => voice.Number));
        Assert.Same(third, measure.FindVoice(3));
        Assert.Null(measure.FindVoice(2));
    }

}
