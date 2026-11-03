using System;
using System.Collections.Generic;
using System.Linq;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

public sealed class MeasureTests {

    private static Voice FullBar4by4(int number) { return V(number, Note("C4", 1, 2), Note("E4", 1, 4), Rest(1, 4)); }

    [Fact]
    public void A_measure_whose_single_voice_fills_the_meter_is_accepted() {
        // Arrange
        int     anyNumber  = Any.Int32().Positive().Generate();
        Dynamic anyDynamic = Any.Enum<Dynamic>().Generate();

        // Act
        Measure measure = new Measure(anyNumber, Meter("4/4"), anyDynamic, new[] { FullBar4by4(1) }, repeatStart: false, repeatEndTimes: 0);

        // Assert
        Assert.Equal(anyNumber, measure.Number);
        Assert.Equal(anyDynamic, measure.Dynamic);
        Assert.Equal(D(1, 1), measure.Length);
        Assert.Equal(Meter("4/4"), measure.TimeSignature);
    }

    [Fact]
    public void A_measure_in_six_eight_is_filled_by_two_dotted_quarters() {
        // Act
        Measure measure = Bar(1, "6/8", new[] { V(1, Note("C4", 3, 8), Note("G4", 3, 8)) });

        // Assert
        Assert.Equal(D(3, 4), measure.Length);
    }

    [Fact]
    public void A_voice_that_falls_short_of_the_meter_is_refused_naming_the_voice_and_both_lengths() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Bar(7, "4/4", new[] { V(2, Note("C4", 3, 4)) }));

        // Assert
        Assert.Contains("Voice 2", error.Message);
        Assert.Contains("measure 7", error.Message);
        Assert.Contains("3/4", error.Message);
        Assert.Contains("1/1", error.Message);
    }

    [Fact]
    public void A_voice_that_overruns_the_meter_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Bar(1, "3/4", new[] { V(1, Note("C4", 1, 2), Note("D4", 1, 2)) }));
    }

    [Fact]
    public void Every_voice_must_fill_the_meter_not_only_the_first() {
        // Assert
        Assert.Throws<ArgumentException>(() => Bar(1, "4/4", new[] { FullBar4by4(1), V(2, Note("C3", 1, 2)) }));
    }

    [Fact]
    public void A_voice_filling_a_different_meter_of_the_same_length_is_accepted() {
        // Arrange: 3/4 and 6/8 last the same time
        Voice voice = V(1, Note("C4", 1, 4), Note("D4", 1, 4), Note("E4", 1, 4));

        // Act
        Measure measure = Bar(1, "6/8", new[] { voice });

        // Assert
        Assert.Equal(Meter("6/8"), measure.TimeSignature);
    }

    [Fact]
    public void Triplets_fill_a_measure_exactly() {
        // Act: a triplet half (3 x 1/6) plus a half note
        Measure measure = Bar(1, "4/4", new[] { V(1, Note("C4", 1, 6), Note("D4", 1, 6), Note("E4", 1, 6), Note("F4", 1, 2)) });

        // Assert
        Assert.Single(measure.Voices);
    }

    [Fact]
    public void Voices_are_sorted_by_number_whatever_the_order_given() {
        // Act
        Measure measure = Bar(1, "4/4", new[] { FullBar4by4(3), FullBar4by4(1), FullBar4by4(4), FullBar4by4(2) });

        // Assert
        Assert.Equal(new[] { 1, 2, 3, 4 }, measure.Voices.Select(voice => voice.Number));
    }

    [Fact]
    public void Two_voices_with_the_same_number_are_refused() {
        // Act
        ArgumentException error = Assert.Throws<ArgumentException>(() => Bar(4, "4/4", new[] { FullBar4by4(2), FullBar4by4(2) }));

        // Assert
        Assert.Contains("same voice number twice", error.Message);
    }

    [Fact]
    public void A_voice_is_found_by_its_number_and_a_missing_one_is_null() {
        // Arrange
        Measure measure = Bar(1, "4/4", new[] { FullBar4by4(1), FullBar4by4(3) });

        // Assert
        Assert.Equal(3, measure.FindVoice(3)!.Number);
        Assert.Null(measure.FindVoice(2));
    }

    [Fact]
    public void A_measure_without_voices_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Bar(1, "4/4", Array.Empty<Voice>()));
    }

    [Fact]
    public void A_measure_holding_a_null_voice_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Bar(1, "4/4", new[] { FullBar4by4(1), null! }));
    }

    [Fact]
    public void A_non_positive_measure_number_is_refused() {
        // Arrange
        int anyNonPositive = Any.Int32().LessThanOrEqualTo(0).Generate();

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithNumber(anyNonPositive).Generate());
    }

    [Fact]
    public void An_unknown_dynamic_is_refused() {
        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithDynamic((Dynamic)99).Generate());
        Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithDynamic((Dynamic)(-1)).Generate());
    }

    [Fact]
    public void A_null_time_signature_is_refused() {
        // Assert
        Assert.Throws<ArgumentNullException>(() => Arbitrary.Measure().WithTimeSignature((TimeSignature)null!).WithVoices(new[] { FullBar4by4(1) }).Generate());
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(8, true)]
    public void An_end_repeat_plays_two_to_eight_times_and_zero_means_none(int repeatEndTimes, bool expectedRepeatEnd) {
        // Act
        Measure measure = Arbitrary.Measure().WithRepeatEndTimes(repeatEndTimes).Generate();

        // Assert
        Assert.Equal(repeatEndTimes, measure.RepeatEndTimes);
        Assert.Equal(expectedRepeatEnd, measure.RepeatEnd);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(100)]
    public void A_number_of_repeats_other_than_zero_or_two_to_eight_is_refused(int repeatEndTimes) {
        // Act
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Arbitrary.Measure().WithRepeatEndTimes(repeatEndTimes).Generate());

        // Assert
        Assert.Equal("repeatEndTimes", error.ParamName);
    }

    [Fact]
    public void A_start_repeat_is_independent_of_an_end_repeat() {
        // Act
        Measure measure = Arbitrary.Measure().WithRepeatStart(true).WithRepeatEndTimes(0).Generate();

        // Assert
        Assert.True(measure.RepeatStart);
        Assert.False(measure.RepeatEnd);
    }

    [Fact]
    public void A_measure_does_not_follow_later_changes_to_the_voice_list_it_was_given() {
        // Arrange
        List<Voice> voices = new List<Voice> { FullBar4by4(1) };

        // Act
        Measure measure = Bar(1, "4/4", voices);
        voices.Add(FullBar4by4(2));

        // Assert
        Assert.Single(measure.Voices);
    }

}
