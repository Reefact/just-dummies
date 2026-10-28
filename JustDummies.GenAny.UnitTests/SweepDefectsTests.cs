using JustDummies.GenAny.UnitTests.Sweep;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     What an open entry of <see cref="SweepDefects" /> is allowed to claim.
/// </summary>
/// <remarks>
///     An entry turns a finding into a known defect, and the baseline only counts verdicts: an entry that
///     claimed more than its own symptom would let a new, unrelated failure on the same shapes replace the
///     known one without moving a single number. So each entry is held to the exact symptom it records.
/// </remarks>
public sealed class SweepDefectsTests {

    private static readonly SweepShape Setter = SweepShapes.Named("placement-setter-str");

    [Fact(DisplayName = "A draw the setter shape's constructor rejects is the known setter defect.")]
    public void ADrawTheConstructorRejectsIsTheKnownSetterDefect() {
        Check.That(SweepDefects.Claiming(Setter, Drawn(new EmittedAssembly.DrawFailure("ArgumentException", "draw 1 of 30 threw ArgumentException: value", duringDraw: true))))
             .IsNotNull();
    }

    [Fact(DisplayName = "A draw failing with another exception is not the known setter defect.")]
    public void ADrawFailingWithAnotherExceptionIsNotTheKnownSetterDefect() {
        Check.That(SweepDefects.Claiming(Setter, Drawn(new EmittedAssembly.DrawFailure("NullReferenceException", "draw 1 of 30 threw NullReferenceException", duringDraw: true))))
             .IsNull();
    }

    [Fact(DisplayName = "The generator failing at construction is not the known setter defect.")]
    public void AConstructionFailureIsNotTheKnownSetterDefect() {
        Check.That(SweepDefects.Claiming(Setter, Drawn(new EmittedAssembly.DrawFailure("ArgumentException", "new AnyPlacementSetterStr() threw ArgumentException: value"))))
             .IsNull();
    }

    [Fact(DisplayName = "A setter shape's emitted file failing to compile is not the known setter defect.")]
    public void ACompileFailureIsNotTheKnownSetterDefect() {
        SweepOutcome finding = new(Setter.Name, Setter.Family, "Scaffolded", "value=None", "CS0029: cannot convert", "-", "-",
                                   SweepVerdict.Finding, "the emitted file does not compile, away from any sentinel");

        Check.That(SweepDefects.Claiming(Setter, finding)).IsNull();
    }

    [Fact(DisplayName = "A setter shape raising a library rule is not the known setter defect.")]
    public void AnAnalyzerFindingIsNotTheKnownSetterDefect() {
        SweepOutcome finding = new(Setter.Name, Setter.Family, "Scaffolded", "value=None", "ok", "JD016 [Warning] conflict", "-",
                                   SweepVerdict.Finding, "the emitted file raises JD016");

        Check.That(SweepDefects.Claiming(Setter, finding)).IsNull();
    }

    [Fact(DisplayName = "The engine refusing a setter shape is not the known setter defect.")]
    public void AnEngineRefusalIsNotTheKnownSetterDefect() {
        SweepOutcome finding = new(Setter.Name, Setter.Family, "NoEligibleConstructor", "-", "-", "-", "-",
                                   SweepVerdict.Finding, "the engine refused a domain that compiles");

        Check.That(SweepDefects.Claiming(Setter, finding)).IsNull();
    }

    /// <summary>A finding as rule 5 writes it: compiled, no rule raised, and the draw stopped by <paramref name="failure" />.</summary>
    private static SweepOutcome Drawn(EmittedAssembly.DrawFailure failure) {
        return new SweepOutcome(Setter.Name, Setter.Family, "Scaffolded", "value=None", "ok", "ok", failure.ToString(),
                                SweepVerdict.Finding, "the generator produced a value its own domain rejects", failure);
    }

}
