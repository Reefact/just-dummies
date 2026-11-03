namespace KnittingPatterns.Tests.Expert.Tests;

// The generators are code the tests own: each one must only ever draw what the real constructor accepts.
public sealed class GeneratorSanityTests {

    [Fact] public void StitchRun_generator_draws_runs() { Assert.NotNull(Any.StitchRun().Generate()); }

    [Fact] public void StitchRepeat_generator_draws_repeats() { Assert.NotNull(Any.StitchRepeat().Generate()); }

    [Fact] public void Row_element_generator_draws_elements() { Assert.NotNull(Any.RowElement().Generate()); }

    [Fact] public void RowInstruction_generator_draws_rows() { Assert.NotNull(Any.RowInstruction().Generate()); }

    [Fact] public void Gauge_generator_draws_gauges() { Assert.NotNull(Any.Gauge().Generate()); }

    [Fact] public void PatternSection_generator_draws_sections() { Assert.NotNull(Any.PatternSection().Generate()); }

    [Fact] public void ExpandedRow_generator_draws_rows() { Assert.NotNull(Any.ExpandedRow().Generate()); }

    [Fact] public void Fabric_generator_draws_fabrics() { Assert.NotNull(Any.Fabric().Generate()); }

    [Fact]
    public void KnittingPattern_generator_draws_patterns_that_expand() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        Assert.NotEmpty(anyPattern.Expand());
    }

    [Fact]
    public void KnittingPattern_generator_draws_patterns_at_the_widest_cast_on() {
        KnittingPattern anyWidePattern = Any.KnittingPattern().WithCastOnCount(KnittingPattern.MaxCastOn).Generate();

        Assert.NotEmpty(anyWidePattern.Expand());
    }

}
