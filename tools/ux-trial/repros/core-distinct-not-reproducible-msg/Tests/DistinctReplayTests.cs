using JustDummies;
using Repro.Domain;
using Xunit;

namespace Repro.Tests;

public sealed class DistinctReplayTests {

    private readonly ITestOutputHelper _output;

    public DistinctReplayTests(ITestOutputHelper output) {
        _output = output;
    }

    [Fact]
    public void Scaffolded_element_generator_exhaustion_message() {
        AnyGenerationException exception = Assert.Throws<AnyGenerationException>(
            () => Any.Reproducibly(1234, () => Any.ListOf(new AnyGrade()).WithCount(6).Distinct().Generate()));
        _output.WriteLine("SCAFFOLDED: " + exception.Message);
        _output.WriteLine("SEED: " + exception.Seed);
    }

    [Fact]
    public void Built_in_element_generator_exhaustion_message() {
        AnyGenerationException exception = Assert.Throws<AnyGenerationException>(
            () => Any.Reproducibly(1234, () => Any.ListOf(Any.Int32().Between(0, 4).As(value => new Grade(value))).WithCount(6).Distinct().Generate()));
        _output.WriteLine("BUILT-IN: " + exception.Message);
    }

    [Fact]
    public void Scaffolded_element_draws_replay_from_the_seed() {
        List<int> first  = new();
        List<int> second = new();
        Any.Reproducibly(1234, () => first.AddRange(Any.ListOf(new AnyGrade()).WithCount(40).Generate().Select(grade => grade.Value)));
        Any.Reproducibly(1234, () => second.AddRange(Any.ListOf(new AnyGrade()).WithCount(40).Generate().Select(grade => grade.Value)));
        _output.WriteLine("FIRST:  " + string.Join(",", first));
        _output.WriteLine("SECOND: " + string.Join(",", second));
        Assert.Equal(first, second);
    }

    [Fact]
    public void Scaffolded_distinct_set_replays_from_the_seed() {
        List<int> first  = new();
        List<int> second = new();
        Any.Reproducibly(99, () => first.AddRange(Any.ListOf(new AnyGrade()).WithCount(5).Distinct().Generate().Select(grade => grade.Value)));
        Any.Reproducibly(99, () => second.AddRange(Any.ListOf(new AnyGrade()).WithCount(5).Distinct().Generate().Select(grade => grade.Value)));
        _output.WriteLine("DISTINCT FIRST:  " + string.Join(",", first));
        _output.WriteLine("DISTINCT SECOND: " + string.Join(",", second));
        Assert.Equal(first, second);
    }

}
