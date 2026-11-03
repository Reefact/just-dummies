using JustDummies;
using JustDummies.Xunit;

using MusicScore.Domain;

using Xunit;

namespace MusicScore.Tests.Discovery;

public sealed class GeneratorSmokeTests {

    [Fact, Reproducible]
    public void Every_generator_draws_a_value_its_constructor_accepts() {
        for (int draw = 0; draw < 200; draw++) {
            Any.Duration().Generate();
            Any.Pitch().Generate();
            Any.TimeSignature().Generate();
            Any.Tempo().Generate();
            Any.VoiceEntry().Generate();
            new AnyVoice().Generate();
            Any.Measure().Generate();
            Any.Staff().Generate();
            Score score = Any.Score().Generate();
            score.Interpret();
            score.PlayedSeconds();
            Any.PerformedEvent().Generate();
        }
    }

}
