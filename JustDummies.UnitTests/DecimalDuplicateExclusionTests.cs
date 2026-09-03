#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Pins that an excluded decimal value weighs once however many times it is declared. The decimal engine is the
///     one that COUNTS excluded grid points to decide satisfiability and cardinality — the ordinal engines
///     deduplicate at construction, and before this was fixed the decimal one did not, so
///     <c>Except(x).DifferentFrom(x)</c> — restating an exclusion already in force, a natural gesture — inflated the
///     count and refused a satisfiable declaration as exhausted, with a message claiming every grid value was
///     forbidden while some were still drawable.
/// </summary>
public sealed class DecimalDuplicateExclusionTests {

    private const int SampleCount = 100;

    [Fact(DisplayName = "Restating an exclusion through a second constraint does not empty a satisfiable grid.")]
    public void RestatedExclusionWeighsOnce() {
        // Grid {0.00, 0.01}: excluding 0.01 leaves 0.00. Declaring the same exclusion again changes nothing.
        DummyDecimal generator = Dummy.Decimal().Between(0.00m, 0.01m).WithScale(2).Except(0.01m).DifferentFrom(0.01m);

        for (int i = 0; i < SampleCount; i++) {
            Check.That(generator.Generate()).IsEqualTo(0.00m);
        }
    }

    [Fact(DisplayName = "A duplicate inside one Except call weighs once too.")]
    public void DuplicateWithinOneCallWeighsOnce() {
        // Grid {0.00, 0.01, 0.02}: one distinct exclusion, listed three times, still leaves two drawable values.
        DummyDecimal generator = Dummy.Decimal().Between(0.00m, 0.02m).WithScale(2).Except(0.01m, 0.01m, 0.01m);

        for (int i = 0; i < SampleCount; i++) {
            decimal value = generator.Generate();
            Check.That(value == 0.00m || value == 0.02m).IsTrue();
        }
    }

    [Fact(DisplayName = "Cardinality counts a duplicated exclusion once, so a distinct collection over the grid still fits.")]
    public void CardinalityCountsDistinctExclusionsOnly() {
        // Grid {0.00 .. 0.04}, 5 points; one value excluded twice leaves 4 — exactly what a distinct set of 4 needs.
        // Before the fix the inflated count (3) made the eager cardinality check refuse this satisfiable request.
        DummyDecimal element = Dummy.Decimal().Between(0.00m, 0.04m).WithScale(2).Except(0.02m).DifferentFrom(0.02m);

        ISet<decimal> values = Dummy.SetOf(element).WithCount(4).Generate();

        Check.That(values).HasSize(4);
        Check.That(values.Contains(0.02m)).IsFalse();
    }

    [Fact(DisplayName = "A genuinely exhausted grid still conflicts, with distinct exclusions counted honestly.")]
    public void GenuineExhaustionStillConflicts() {
        // Grid {0.00, 0.01}: excluding BOTH values really does empty it — the dedup must not weaken the eager check.
        Check.ThatCode(() => Dummy.Decimal().Between(0.00m, 0.01m).WithScale(2).Except(0.00m, 0.01m))
             .Throws<ConflictingDummyConstraintException>();
    }

}
