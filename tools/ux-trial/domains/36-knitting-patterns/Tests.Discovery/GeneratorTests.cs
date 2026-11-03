using JustDummies;

using KnittingPatterns.Domain;

using Xunit;

namespace KnittingPatterns.Tests.Discovery;

// The generators are hand-completed code: these check that what they draw is something the domain accepts.
public sealed class GeneratorTests {

    [Fact]
    public void The_generators_draw_values_the_domain_accepts() {
        Assert.NotNull(Any.StitchRun().Generate());
        Assert.NotNull(Any.StitchRepeat().Generate());
        Assert.NotNull(Any.RowInstruction().Generate());
        Assert.NotNull(Any.Gauge().Generate());
        Assert.NotNull(Any.PatternSection().Generate());
        Assert.NotNull(Any.ExpandedRow().Generate());
        Assert.NotNull(Any.KnittingPattern().Generate());
    }

    [Fact]
    public void A_drawn_pattern_can_be_worked_from_the_first_row_to_the_last() {
        KnittingPattern anyPattern = Any.KnittingPattern().Generate();

        IReadOnlyList<ExpandedRow> rows = anyPattern.Expand();

        Assert.NotEmpty(rows);
    }

}
