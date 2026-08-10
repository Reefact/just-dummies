using JustDummies.GenAny;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The exit codes of §7.
/// </summary>
public sealed class ExitCodeTests {

    /// <summary>
    ///     A file carrying TODOs is a success, and that is the whole mechanism (ADR-0060).
    /// </summary>
    /// <remarks>
    ///     The write succeeded; what remains is a compile error in the developer's own build, at the exact
    ///     line, in the IDE and in CI. A non-zero code here would make a scaffolding step fail a pipeline for
    ///     doing precisely what it was asked to do.
    /// </remarks>
    [Fact(DisplayName = "A file written with TODOs exits successfully.")]
    public void AFileWrittenWithTodosExitsSuccessfully() {
        ScaffoldPlan plan = Plan(ScaffoldedParameter.Unresolved("customer", "Customer"));

        ScaffoldOutcome outcome = ScaffoldOutcome.Scaffolded(plan, GeneratorEmitter.Emit(plan));

        Check.That(outcome.File!.ContainsTodo).IsTrue();
        Check.That(ExitCode.For(outcome)).IsEqualTo(ExitCode.Success);
    }

    [Theory(DisplayName = "A refusal exits with the failure code, whatever the reason.")]
    [InlineData(ScaffoldStatus.LibraryNotReferenced)]
    [InlineData(ScaffoldStatus.NoEligibleConstructor)]
    public void ARefusalExitsWithTheFailureCode(ScaffoldStatus status) {
        Check.That(ExitCode.For(ScaffoldOutcome.Refused(status))).IsEqualTo(ExitCode.Failed);
    }

    // Several types are processed independently, so one failure does not stop the others being written — but
    // it does have to reach the caller, or a script reads a partial run as a whole one.
    [Theory(DisplayName = "A run over several types exits with the worst of them.")]
    [InlineData(new[] { 0, 0, 0 }, 0)]
    [InlineData(new[] { 0, 1, 0 }, 1)]
    [InlineData(new[] { 1, 1 }, 1)]
    [InlineData(new[] { 0, 2 }, 2)]
    public void ARunOverSeveralTypesExitsWithTheWorst(int[] codes, int expected) {
        Check.That(ExitCode.Worst(codes)).IsEqualTo(expected);
    }

    [Fact(DisplayName = "A run over no types at all is not a failure.")]
    public void ARunOverNoTypesIsNotAFailure() {
        Check.That(ExitCode.Worst([])).IsEqualTo(ExitCode.Success);
    }

    private static ScaffoldPlan Plan(ScaffoldedParameter parameter) {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyOrder",
                                ["JustDummies"],
                                [parameter]);
    }

}
