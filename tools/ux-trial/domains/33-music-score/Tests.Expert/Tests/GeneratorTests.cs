using static MusicScore.Domain.Tests.Expert.Notation;

namespace MusicScore.Domain.Tests.Expert;

/// <summary>
///     The generators are code too. A test draws one value per run, so these draw many: whatever the
///     seed of the run, none of them may be able to build an object the domain refuses.
/// </summary>
public sealed class GeneratorTests {

    private const int Draws = 300;

    [Fact]
    public void Durations_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.Duration()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Positive_durations_on_the_binary_grid_are_never_zero_and_always_add_up() {
        // Act
        List<Duration> drawn = Any.ListOf(Arbitrary.Duration().NonZero().OnBinaryGrid()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, duration => Assert.False(duration.IsZero));
        Duration total = drawn.Aggregate(Duration.Zero, (sum, duration) => sum + duration);
        Assert.False(total.IsZero);
    }

    [Fact]
    public void Pitches_are_always_valid_whatever_their_spelling() {
        // Act
        List<Pitch> drawn = Any.ListOf(Arbitrary.Pitch()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, pitch => Assert.InRange(pitch.MidiNumber, 0, 127));
    }

    [Fact]
    public void Tempos_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.Tempo()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Time_signatures_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.TimeSignature()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Voice_entries_are_always_valid_and_never_tie_a_rest() {
        // Act
        List<VoiceEntry> drawn = Any.ListOf(Arbitrary.VoiceEntry()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, entry => Assert.False(entry.IsRest && entry.TiedToNext));
    }

    [Fact]
    public void Voices_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.Voice()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Measures_are_always_valid_and_carry_no_repeat_barline() {
        // Act
        List<Measure> drawn = Any.ListOf(Arbitrary.Measure()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, measure => Assert.False(measure.RepeatStart || measure.RepeatEnd));
    }

    [Fact]
    public void Measures_in_a_pinned_meter_have_voices_that_fill_it() {
        // Arrange
        TimeSignature meter = new(7, 8);

        // Act
        List<Measure> drawn = Any.ListOf(Arbitrary.Measure().WithTimeSignature(meter)).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, measure => Assert.All(measure.Voices, voice => Assert.Equal(D(7, 8), voice.Length)));
    }

    [Fact]
    public void Staves_are_always_valid_and_numbered_from_one() {
        // Act
        List<Staff> drawn = Any.ListOf(Arbitrary.Staff()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, staff => Assert.Equal(Enumerable.Range(1, staff.Measures.Count), staff.Measures.Select(measure => measure.Number)));
    }

    [Fact]
    public void Scores_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.Score()).WithCount(Draws).Generate().Count);
    }

    [Fact]
    public void Scores_can_always_be_interpreted_and_played() {
        // Act
        List<Score> drawn = Any.ListOf(Arbitrary.Score()).WithCount(Draws).Generate();

        // Assert
        Assert.All(drawn, score => {
            IReadOnlyList<PerformedEvent> events = score.Interpret();

            Assert.Equal(score.MeasureCount, score.PlayedMeasureOrder().Count);
            Assert.True(score.PlayedSeconds() > 0);
            Assert.All(events, performed => Assert.True(performed.End <= score.PlayedLength()));
        });
    }

    [Fact]
    public void A_score_is_drawn_again_identically_from_the_same_seed() {
        // Arrange
        string DrawnUnder(int seed) {
            string description = string.Empty;
            Any.Reproducibly(seed, () => {
                Score score = Arbitrary.Score().Generate();
                description = score.Title + "|" + score.Tempo.BeatsPerMinute + "|" + string.Join(";", score.Staves.Select(staff => staff.Name + ":" + string.Join(",", staff.Measures.Select(measure => measure.TimeSignature + "/" + measure.Dynamic + "/" + string.Join("+", measure.Voices.Select(voice => voice.Number + "=" + string.Join(" ", voice.Entries.Select(entry => entry.Pitch + "@" + entry.Duration))))))));
            });

            return description;
        }

        int anySeed = Any.Int32().Generate();

        // Act
        string first  = DrawnUnder(anySeed);
        string second = DrawnUnder(anySeed);

        // Assert
        Assert.Equal(first, second);
        Assert.NotEqual(string.Empty, first);
    }

    [Fact]
    public void Performed_events_are_always_valid() {
        Assert.Equal(Draws, Any.ListOf(Arbitrary.PerformedEvent()).WithCount(Draws).Generate().Count);
    }

}
