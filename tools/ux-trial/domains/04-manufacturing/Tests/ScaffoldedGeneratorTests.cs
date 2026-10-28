using JustDummies;
using JustDummies.Xunit;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Tests;

/// <summary>Harness: every scaffolded generator, drawn 500 times, must never trip its type's own constructor.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyBatchCode_always_builds_a_valid_BatchCode() => AlwaysBuilds(new AnyBatchCode());

    [Fact]
    public void Scaffolded_AnyCadence_always_builds_a_valid_Cadence() => AlwaysBuilds(new AnyCadence());

    [Fact]
    public void Scaffolded_AnyCycleTime_always_builds_a_valid_CycleTime() => AlwaysBuilds(new AnyCycleTime());

    [Fact]
    public void Scaffolded_AnyTolerance_always_builds_a_valid_Tolerance() => AlwaysBuilds(new AnyTolerance());

    [Fact]
    public void Scaffolded_AnyMachine_always_builds_a_valid_Machine() => AlwaysBuilds(new AnyMachine());

    [Fact]
    public void Scaffolded_AnyShiftWindow_always_builds_a_valid_ShiftWindow() => AlwaysBuilds(new AnyShiftWindow());

    [Fact]
    public void Scaffolded_AnyWorkOrder_always_builds_a_valid_WorkOrder() => AlwaysBuilds(new AnyWorkOrder());

}
