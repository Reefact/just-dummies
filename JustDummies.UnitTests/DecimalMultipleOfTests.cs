#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>MultipleOf</c> on <see cref="AnyDecimal" />: a step's grid drawn by construction, its agreement with
///     <c>WithScale</c>, and where it ends (issue #359, ADR-0123).
/// </summary>
public sealed class DecimalMultipleOfTests {

    private const int SampleCount = 400;

    [Fact(DisplayName = "Every draw is a whole number of steps inside the declared range, and both ends are reached.")]
    public void EveryDrawIsAWholeNumberOfSteps() {
        HashSet<decimal> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Decimal().Between(0m, 2m).MultipleOf(0.25m).Generate()); }

        Check.That(seen).IsOnlyMadeOf(0m, 0.25m, 0.5m, 0.75m, 1m, 1.25m, 1.5m, 1.75m, 2m).And.HasSize(9);
    }

    [Fact(DisplayName = "A range off the grid draws from the steps inside it.")]
    public void ARangeOffTheGridDrawsFromTheStepsInsideIt() {
        HashSet<decimal> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Decimal().Between(-0.4m, 0.6m).MultipleOf(0.3m).Generate()); }

        Check.That(seen).IsOnlyMadeOf(-0.3m, 0m, 0.3m, 0.6m).And.HasSize(4);
    }

    [Fact(DisplayName = "A draw with an open side stays on the grid, the one in ten that spans the domain included.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.TheOpenSideIsTheSubject)]
    public void ADrawWithAnOpenSideStaysOnTheGrid() {
        AnyDecimal generator = Any.WithSeed(359).Decimal().MultipleOf(0.25m);

        bool reachedPastTheWindow = false;
        for (int i = 0; i < SampleCount; i++) {
            decimal value = generator.Generate();
            Check.That(decimal.Round(value * 4m) == value * 4m).IsTrue();
            reachedPastTheWindow |= Math.Abs(value) > 1_000_000m;
        }

        Check.That(reachedPastTheWindow).IsTrue();
    }

    [Fact(DisplayName = "A large step stays on its grid across the domain.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.TheOpenSideIsTheSubject)]
    public void ALargeStepStaysOnItsGrid() {
        for (int i = 0; i < SampleCount; i++) {
            decimal value = Any.Decimal().MultipleOf(1e27m).Generate();
            Check.That(decimal.Truncate(value / 1e27m) * 1e27m).IsEqualTo(value);
        }
    }

    [Fact(DisplayName = "A step that is a power of ten is WithScale's grid, and reaches as far.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.TheOpenSideIsTheSubject)]
    public void AStepThatIsAPowerOfTenReachesAsFarAsWithScale() {
        decimal value = Any.Decimal().GreaterThan(1e27m).MultipleOf(0.01m).Generate();

        Check.That(value > 1e27m && decimal.Round(value, 2) == value).IsTrue();
    }

    [Fact(DisplayName = "A step's grid ends where its points stop being exact: a range past it is refused, naming the step.")]
    [SuppressMessage(JustDummiesRule.JD036.Category, JustDummiesRule.JD036.Id, Justification = SuppressionJustification.JD036.TheOpenSideIsTheSubject)]
    public void AStepsGridEndsWhereItsPointsStopBeingExact() {
        Check.ThatCode(() => Any.Decimal().GreaterThan(1e27m).MultipleOf(0.25m))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("MultipleOf(0.25)");
    }

    [Fact(DisplayName = "A step on the scale's grid composes with it, in either order.")]
    public void AStepOnTheScalesGridComposesWithIt() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Decimal().Between(0m, 1m).MultipleOf(0.25m).WithScale(2).Generate()).IsOneOf(0m, 0.25m, 0.5m, 0.75m, 1m);
            Check.That(Any.Decimal().Between(0m, 1m).WithScale(4).MultipleOf(0.25m).Generate()).IsOneOf(0m, 0.25m, 0.5m, 0.75m, 1m);
        }
    }

    [Fact(DisplayName = "A step off the scale's grid conflicts, naming both, in either order.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void AStepOffTheScalesGridConflicts() {
        Check.ThatCode(() => Any.Decimal().MultipleOf(0.25m).WithScale(1))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply WithScale(1) because MultipleOf(0.25) is already defined, and the step is not a multiple of 0.1.");
        Check.ThatCode(() => Any.Decimal().WithScale(1).MultipleOf(0.25m))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply MultipleOf(0.25) because WithScale(1) is already defined, and the step is not a multiple of 0.1.");
    }

    [Fact(DisplayName = "A second, different step is refused naming both; the same step again is a repetition.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    [SuppressMessage(JustDummiesRule.JD035.Category, JustDummiesRule.JD035.Id, Justification = SuppressionJustification.JD035.RepetitionIsTheSubject)]
    public void TheStepIsDeclaredOnce() {
        Check.ThatCode(() => Any.Decimal().MultipleOf(0.25m).MultipleOf(0.5m))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("MultipleOf(0.5)", "MultipleOf(0.25)");

        decimal value = Any.Decimal().Between(0m, 1m).MultipleOf(0.25m).MultipleOf(0.250m).Generate();
        Check.That(value).IsOneOf(0m, 0.25m, 0.5m, 0.75m, 1m);
    }

    [Fact(DisplayName = "A step that is not strictly positive is an argument error.")]
    [SuppressMessage(JustDummiesRule.JD014.Category, JustDummiesRule.JD014.Id, Justification = SuppressionJustification.JD014.ThrowIsTheSubject)]
    public void AStepThatIsNotStrictlyPositiveIsAnArgumentError() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().MultipleOf(0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().MultipleOf(-0.25m));
    }

    [Fact(DisplayName = "A range holding no step is refused at the call that empties it.")]
    public void ARangeHoldingNoStepIsRefused() {
        Check.ThatCode(() => Any.Decimal().Between(0.3m, 0.4m).MultipleOf(0.25m))
             .Throws<ConflictingAnyConstraintException>()
             .WithMessage("Cannot apply MultipleOf(0.25) because no Decimal value MultipleOf(0.25) allows remains between 0.3 and 0.4.");
    }

    [Fact(DisplayName = "The grid composes with an exclusion and filters a value set.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void TheGridComposesWithAnExclusionAndAValueSet() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Decimal().Between(0m, 1m).MultipleOf(0.5m).Except(0.5m).Generate()).IsOneOf(0m, 1m);
            Check.That(Any.Decimal().OneOf(0.2m, 0.5m, 0.75m).MultipleOf(0.25m).Generate()).IsOneOf(0.5m, 0.75m);
        }
    }

    [Fact(DisplayName = "A distinct collection counts the steps: as many as the grid holds, and not one more.")]
    public void ADistinctCollectionCountsTheSteps() {
        AnyDecimal fifths = Any.Decimal().Between(0m, 1m).MultipleOf(0.25m);

        Check.That(Any.SetOf(fifths).WithCount(5).Generate()).HasSize(5);
        Check.ThatCode(() => Any.SetOf(fifths).WithCount(6).Generate()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "The draw replays: the same seed yields the same values.")]
    public void TheDrawReplays() {
        Check.That(Draws(41)).ContainsExactly(Draws(41));

        static List<decimal> Draws(int seed) {
            AnyDecimal generator = Any.WithSeed(seed).Decimal().Between(-10m, 10m).MultipleOf(0.25m);

            return Enumerable.Range(0, 50).Select(_ => generator.Generate()).ToList();
        }
    }

}
