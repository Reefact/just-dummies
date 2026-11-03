using System.Collections.Generic;
using System.Linq;

using JustDummies;

using Xunit;

using static MusicScore.Domain.Tests.Notation;

namespace MusicScore.Domain.Tests;

/// <summary>The scaffolded and completed generators draw values the domain accepts, and a score the interpreter can play.</summary>
public sealed class GeneratorTests {

    private const int Draws = 200;

    [Fact]
    public void Every_generator_draws_values_its_constructor_accepts() {
        for (int draw = 0; draw < Draws; draw++) {
            Arbitrary.Duration().Generate();
            Arbitrary.Pitch().Generate();
            Arbitrary.TimeSignature().Generate();
            Arbitrary.Tempo().Generate();
            Arbitrary.VoiceEntry().Generate();
            Arbitrary.Voice().Generate();
            Arbitrary.Measure().Generate();
            Arbitrary.Staff().Generate();
            Arbitrary.Score().Generate();
            Arbitrary.PerformedEvent().Generate();
        }
    }

    [Fact]
    public void A_drawn_measure_has_voices_filling_its_meter_with_distinct_numbers() {
        for (int draw = 0; draw < Draws; draw++) {
            Measure measure = Arbitrary.Measure().Generate();

            Assert.All(measure.Voices, voice => Assert.Equal(measure.Length, voice.Length));
            Assert.Equal(measure.Voices.Count, measure.Voices.Select(voice => voice.Number).Distinct().Count());
        }
    }

    [Fact]
    public void A_drawn_score_plays_without_error_because_a_dummy_entry_is_never_tied() {
        for (int draw = 0; draw < Draws; draw++) {
            Score score = Arbitrary.Score().Generate();

            IReadOnlyList<PerformedEvent> events = score.Interpret();

            Assert.All(events, performed => Assert.True(performed.DurationSeconds > 0));
        }
    }

    [Fact]
    public void The_draws_cover_rests_notes_repeats_and_several_voices_and_staves() {
        List<Score> scores = Enumerable.Range(0, Draws).Select(_ => Arbitrary.Score().Generate()).ToList();

        Assert.Contains(scores, score => score.Staves.Count > 1);
        Assert.Contains(scores, score => score.PlayedMeasureOrder().Count > score.MeasureCount);
        Assert.Contains(scores, score => score.Staves.SelectMany(s => s.Measures).Any(m => m.Voices.Count > 1));
        Assert.Contains(scores, score => score.Staves.SelectMany(s => s.Measures).SelectMany(m => m.Voices).SelectMany(v => v.Entries).Any(e => e.IsRest));
    }

    [Fact]
    public void A_pinned_tie_always_lands_on_a_note() {
        for (int draw = 0; draw < Draws; draw++) {
            VoiceEntry tied = Arbitrary.VoiceEntry().WithTiedToNext(true).Generate();

            Assert.False(tied.IsRest);
            Assert.True(tied.TiedToNext);
        }
    }

}
