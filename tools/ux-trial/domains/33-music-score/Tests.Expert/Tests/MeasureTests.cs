using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

public sealed class MeasureTests {

    [Fact]
    public void A_voice_that_fills_the_meter_is_accepted() {
        // Arrange
        Voice voice = VoiceOf(1, Note("C4", 1, 2), Note("E4", 1, 4));

        // Act
        Measure measure = Bar(1, ThreeFour, voice).Generate();

        // Assert
        Assert.Equal(D(3, 4), measure.Length);
        Assert.Same(voice, Assert.Single(measure.Voices));
    }

    [Fact]
    public void A_compound_meter_is_filled_by_its_reduced_length() {
        // Arrange
        Voice sixEighths = VoiceOf(1, Note("C4", 1, 8), Note("D4", 1, 8), Note("E4", 1, 8), Note("F4", 1, 8), Note("G4", 1, 8), Note("A4", 1, 8));

        // Act
        Measure measure = Bar(1, new TimeSignature(6, 8), sixEighths).Generate();

        // Assert
        Assert.Equal(D(3, 4), measure.Length);
    }

    [Theory]
    [InlineData(3, 4)]
    [InlineData(5, 4)]
    [InlineData(1, 8)]
    public void A_voice_that_does_not_fill_the_meter_is_refused(long numerator, long denominator) {
        // Arrange
        Voice voice = VoiceOf(2, Note("C4", numerator, denominator));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Bar(7, FourFour, voice).Generate());

        // Assert
        Assert.Equal("voices", refusal.ParamName);
        Assert.Contains("Voice 2", refusal.Message);
        Assert.Contains("measure 7", refusal.Message);
    }

    [Fact]
    public void A_single_short_voice_among_full_ones_is_refused() {
        // Arrange
        Voice full  = VoiceOf(1, Note("C4", 1, 1));
        Voice short_ = VoiceOf(2, Note("C4", 1, 2), Rest(1, 4));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Bar(1, FourFour, full, short_).Generate());
    }

    [Fact]
    public void Two_voices_with_the_same_number_are_refused() {
        // Arrange
        Voice first  = VoiceOf(3, Note("C4", 1, 1));
        Voice second = VoiceOf(3, Note("E4", 1, 1));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Bar(1, FourFour, first, second).Generate());

        // Assert
        Assert.Equal("voices", refusal.ParamName);
    }

    [Fact]
    public void Voices_are_listed_by_number_whatever_order_they_came_in() {
        // Arrange
        Voice third  = VoiceOf(3, Note("C3", 1, 1));
        Voice first  = VoiceOf(1, Note("C5", 1, 1));
        Voice second = VoiceOf(2, Note("C4", 1, 1));

        // Act
        Measure measure = Bar(1, FourFour, third, first, second).Generate();

        // Assert
        Assert.Equal([1, 2, 3], measure.Voices.Select(voice => voice.Number));
    }

    [Fact]
    public void A_voice_is_found_by_its_number() {
        // Arrange
        Voice second = VoiceOf(2, Note("C4", 1, 1));
        Voice fourth = VoiceOf(4, Note("C4", 1, 1));

        // Act
        Measure measure = Bar(1, FourFour, second, fourth).Generate();

        // Assert
        Assert.Same(fourth, measure.FindVoice(4));
        Assert.Null(measure.FindVoice(1));
    }

    [Fact]
    public void A_measure_without_voices_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Arbitrary.Measure().WithVoices(Array.Empty<Voice>()).Generate());

        // Assert
        Assert.Equal("voices", refusal.ParamName);
    }

    [Fact]
    public void A_measure_with_a_null_voice_is_refused() {
        // Arrange
        Voice anyVoice = Arbitrary.Voice().Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Arbitrary.Measure().WithVoices(new Voice?[] { anyVoice, null }!).Generate());
    }

    [Fact]
    public void A_measure_without_a_voice_list_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Measure().WithVoices((IReadOnlyList<Voice>)null!).Generate());
    }

    [Fact]
    public void A_measure_without_meter_is_refused() {
        // Arrange
        Voice anyVoice = Arbitrary.Voice().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Measure().WithVoices([anyVoice]).WithTimeSignature((TimeSignature)null!).Generate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(int.MinValue)]
    public void A_measure_is_numbered_from_one(int number) {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithNumber(number).Generate());
    }

    [Fact]
    public void The_first_measure_is_number_one() {
        // Act
        Measure measure = Arbitrary.Measure().WithNumber(1).Generate();

        // Assert
        Assert.Equal(1, measure.Number);
    }

    [Theory]
    [InlineData(Dynamic.Pianissimo)]
    [InlineData(Dynamic.Piano)]
    [InlineData(Dynamic.MezzoPiano)]
    [InlineData(Dynamic.MezzoForte)]
    [InlineData(Dynamic.Forte)]
    [InlineData(Dynamic.Fortissimo)]
    public void A_declared_dynamic_is_kept(Dynamic dynamic) {
        // Act
        Measure measure = Arbitrary.Measure().WithDynamic(dynamic).Generate();

        // Assert
        Assert.Equal(dynamic, measure.Dynamic);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(99)]
    public void An_undeclared_dynamic_is_refused(int value) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithDynamic((Dynamic)value).Generate());

        // Assert
        Assert.Equal("dynamic", refusal.ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(8, true)]
    public void An_end_repeat_plays_its_section_two_to_eight_times_or_not_at_all(int repeatEndTimes, bool isEndRepeat) {
        // Act
        Measure measure = Arbitrary.Measure().WithRepeatEndTimes(repeatEndTimes).Generate();

        // Assert
        Assert.Equal(repeatEndTimes, measure.RepeatEndTimes);
        Assert.Equal(isEndRepeat, measure.RepeatEnd);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public void An_end_repeat_that_plays_once_or_more_than_eight_times_is_refused(int repeatEndTimes) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithRepeatEndTimes(repeatEndTimes).Generate());

        // Assert
        Assert.Equal("repeatEndTimes", refusal.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_start_repeat_barline_is_kept(bool repeatStart) {
        // Act
        Measure measure = Arbitrary.Measure().WithRepeatStart(repeatStart).Generate();

        // Assert
        Assert.Equal(repeatStart, measure.RepeatStart);
    }

    [Fact]
    public void A_measure_can_open_and_close_a_repeat_at_once() {
        // Act
        Measure measure = Arbitrary.Measure().WithRepeatStart(true).WithRepeatEndTimes(2).Generate();

        // Assert
        Assert.True(measure.RepeatStart);
        Assert.True(measure.RepeatEnd);
    }

}
