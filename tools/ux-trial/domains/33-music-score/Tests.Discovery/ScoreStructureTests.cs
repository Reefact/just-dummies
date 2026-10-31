using JustDummies;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class MeasureAndStaffTests {

    [Fact]
    public void A_voice_that_does_not_fill_the_meter_is_refused() {
        // Arrange
        Voice threeQuarters = Any.Voice().WithEntries([Any.VoiceEntry().WithDuration(new Duration(3, 4)).Generate()]).Generate();

        // Act
        Action create = () => Any.Measure().WithTimeSignature(new TimeSignature(4, 4)).WithVoices([threeQuarters]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("voices", create);
    }

    [Fact]
    public void Two_voices_with_the_same_number_are_refused() {
        // Arrange
        TimeSignature   anyMeter    = Any.TimeSignature().Generate();
        VoiceEntry      anyWholeBar = Any.VoiceEntry().WithDuration(anyMeter.MeasureLength).Generate();
        Voice           anyVoice    = Any.Voice().WithEntries([anyWholeBar]).Generate();
        Voice           anyTwin     = Any.Voice().WithNumber(anyVoice.Number).WithEntries([anyWholeBar]).Generate();

        // Act
        Action create = () => Any.Measure().WithTimeSignature(anyMeter).WithVoices([anyVoice, anyTwin]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("voices", create);
    }

    [Fact]
    public void An_end_repeat_played_only_once_is_refused() {
        // Arrange
        // Act
        Action create = () => Any.Measure().WithRepeatEndTimes(1).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("repeatEndTimes", create);
    }

    [Fact]
    public void A_staff_whose_measures_are_out_of_order_is_refused() {
        // Arrange
        Measure anyFirst  = Any.Measure().WithNumber(1).Generate();
        Measure anySecond = Any.Measure().WithNumber(2).Generate();

        // Act
        Action create = () => Any.Staff().WithMeasures([anySecond, anyFirst]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("measures", create);
    }

}

public sealed class ScoreConsistencyTests {

    [Fact]
    public void Staves_with_different_measure_counts_are_refused() {
        // Arrange
        // Both staves share their meter, so only the measure count differs.
        TimeSignature anyMeter      = Any.TimeSignature().Generate();
        Staff         anyShortStaff = Any.Staff().WithName("Violin").WithMeasures(Bars.Plain(1, anyMeter)).Generate();
        Staff         anyLongStaff  = Any.Staff().WithName("Cello").WithMeasures(Bars.Plain(2, anyMeter)).Generate();

        // Act
        Action create = () => Any.Score().WithStaves([anyShortStaff, anyLongStaff]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("staves", create);
    }

    [Fact]
    public void A_repeat_opened_inside_an_open_repeat_is_refused() {
        // Arrange
        Measure[] nested = [
            Bars.Plain(1, repeatStart: true),
            Bars.Plain(2, repeatStart: true),
            Bars.Plain(3, repeatEndTimes: 2),
        ];

        // Act
        Action create = () => Any.Score().WithStaves([Any.Staff().WithMeasures(nested).Generate()]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("staves", create);
    }

    [Fact]
    public void A_start_repeat_never_closed_is_refused() {
        // Arrange
        Measure[] unclosed = [Bars.Plain(1), Bars.Plain(2, repeatStart: true)];

        // Act
        Action create = () => Any.Score().WithStaves([Any.Staff().WithMeasures(unclosed).Generate()]).Generate();

        // Assert
        Assert.Throws<ArgumentException>("staves", create);
    }

}

/// <summary>Measures whose meter, dynamic and notes the test does not care about.</summary>
internal static class Bars {

    public static Measure Plain(int number, bool repeatStart = false, int repeatEndTimes = 0) {
        return Any.Measure().WithNumber(number).WithRepeatStart(repeatStart).WithRepeatEndTimes(repeatEndTimes).Generate();
    }

    public static Measure[] Plain(int count, TimeSignature meter) {
        return Enumerable.Range(1, count).Select(number => Any.Measure().WithNumber(number).WithTimeSignature(meter).WithRepeatStart(false).WithRepeatEndTimes(0).Generate()).ToArray();
    }

    public static Score SingleStaff(params Measure[] measures) {
        return Any.Score().WithStaves([Any.Staff().WithMeasures(measures).Generate()]).Generate();
    }

}
