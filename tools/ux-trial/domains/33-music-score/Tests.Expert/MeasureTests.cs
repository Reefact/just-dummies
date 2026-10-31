using JustDummies;

using MusicScore.Domain;
using MusicScore.Tests.Expert.Arbitrary;

using Xunit;

namespace MusicScore.Tests.Expert;

public sealed class MeasureTests {

    [Fact]
    public void A_voice_one_beat_short_of_the_meter_is_refused() {
        // Arrange
        TimeSignature anyMeter = Any.TimeSignature().WithBeats(b => b.Between(2, 32)).Generate();
        Duration      oneBeatShort = new(anyMeter.Beats - 1, anyMeter.BeatValue);
        Voice         anyShortVoice = Any.Voice().Filling(oneBeatShort).Generate();

        // Act
        Exception? refused = Record.Exception(() => Any.Measure().WithTimeSignature(anyMeter).WithVoices([anyShortVoice]).Generate());

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Equal("voices", invalid.ParamName);
    }

    [Fact]
    public void Two_voices_with_the_same_number_are_refused() {
        // Arrange
        TimeSignature anyMeter       = Any.TimeSignature().Generate();
        int           anyVoiceNumber = Any.Int32().Between(1, Voice.MaxVoices).Generate();
        List<Voice>   anyTwinVoices  = Any.ListOf(Any.Voice().WithNumber(anyVoiceNumber).Filling(anyMeter.MeasureLength)).WithCount(2).Generate();

        // Act
        Exception? refused = Record.Exception(() => Any.Measure().WithTimeSignature(anyMeter).WithVoices(anyTwinVoices).Generate());

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Equal("voices", invalid.ParamName);
    }

    [Fact]
    public void An_end_repeat_played_only_once_is_refused() {
        // Arrange
        AnyMeasure anyMeasure = Any.Measure();

        // Act
        Exception? refused = Record.Exception(() => anyMeasure.ClosingRepeat(1).Generate());

        // Assert
        ArgumentOutOfRangeException outOfRange = Assert.IsType<ArgumentOutOfRangeException>(refused);
        Assert.Equal("repeatEndTimes", outOfRange.ParamName);
    }

    [Fact]
    public void A_measure_lists_its_voices_by_number_whatever_the_written_order() {
        // Arrange
        TimeSignature anyMeter = Any.TimeSignature().Generate();
        Voice         anyLowerVoice = Any.Voice().WithNumber(3).Filling(anyMeter.MeasureLength).Generate();
        Voice         anyUpperVoice = Any.Voice().WithNumber(1).Filling(anyMeter.MeasureLength).Generate();

        // Act
        Measure measure = Any.Measure().WithTimeSignature(anyMeter).WithVoices([anyLowerVoice, anyUpperVoice]).Generate();

        // Assert
        Assert.Equal([1, 3], measure.Voices.Select(voice => voice.Number));
    }

    [Fact]
    public void A_staff_whose_measures_are_not_numbered_in_order_is_refused() {
        // Arrange
        string  anyName          = AnyStaff.Names().Generate();
        Measure anySecondMeasure = Any.Measure().WithNumber(2).Generate();
        Measure anyFirstMeasure  = Any.Measure().WithNumber(1).Generate();

        // Act
        Exception? refused = Record.Exception(() => new Staff(anyName, [anySecondMeasure, anyFirstMeasure]));

        // Assert
        ArgumentException invalid = Assert.IsType<ArgumentException>(refused);
        Assert.Equal("measures", invalid.ParamName);
    }

}
