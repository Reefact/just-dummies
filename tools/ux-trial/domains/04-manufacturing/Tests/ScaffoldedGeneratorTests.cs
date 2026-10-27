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

    [Fact]
    public void Scaffolded_AnyMachineState_always_builds_a_declared_MachineState() {
        for (int draw = 0; draw < Draws; draw++) {
            Assert.True(Enum.IsDefined(new AnyMachineState().Generate()));
        }
    }

    // AnyShiftKind.cs is kept exactly as `any generate ShiftKind` emitted it: `return new ShiftKind();`.
    // ShiftKind numbers its members from one, so default(ShiftKind) == (ShiftKind)0 is not a member.
    [Fact]
    public void Bug_Scaffolded_AnyShiftKind_draws_an_undeclared_member() {
        for (int draw = 0; draw < Draws; draw++) {
            ShiftKind anyShift = new AnyShiftKind().Generate();
            Assert.True(Enum.IsDefined(anyShift), $"AnyShiftKind drew {(int)anyShift}, which ShiftKind does not declare.");
        }
    }

    // The recipe `any generate BatchCode` emitted, reported "1 of 1 parameters inferred", no `to verify`.
    // BatchCode's constructor also matches a Regex and throws on a mismatch; the scaffolder read only
    // ThrowIfNullOrWhiteSpace. AnyBatchCode.cs was fixed by hand; this replays the emitted recipe.
    [Fact]
    public void Bug_Scaffolded_AnyBatchCode_recipe_misses_the_format_guard_silently() {
        AlwaysBuilds(Any.String().NotBlank().As(value => new BatchCode(value)));
    }

}
