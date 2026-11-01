#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>Odd()</c> and <c>Even()</c> on the integer builders: two lattices of step two, drawn on the grid like
///     <c>MultipleOf</c>, and sharing its once-per-generator slot (issue #401).
/// </summary>
public sealed class OddEvenTests {

    private const int SampleCount = 400;

    [Fact(DisplayName = "Odd draws only odd numbers inside the declared range, and reaches both of its ends.")]
    public void OddDrawsOddNumbersAcrossTheRange() {
        HashSet<int> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            int value = Any.Int32().Between(1, 9).Odd().Generate();
            Check.That(value % 2).IsEqualTo(1);
            seen.Add(value);
        }

        Check.That(seen).IsOnlyMadeOf(1, 3, 5, 7, 9).And.HasSize(5);
    }

    [Fact(DisplayName = "Odd draws negative odd numbers too.")]
    public void OddDrawsNegativeOddNumbers() {
        HashSet<long> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Int64().Between(-5, -1).Odd().Generate()); }

        Check.That(seen).IsOnlyMadeOf(-5L, -3L, -1L).And.HasSize(3);
    }

    [Fact(DisplayName = "Even draws only even numbers inside the declared range, and reaches both of its ends.")]
    public void EvenDrawsEvenNumbersAcrossTheRange() {
        HashSet<byte> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Any.Byte().Between(2, 8).Even().Generate()); }

        Check.That(seen).IsOnlyMadeOf((byte)2, (byte)4, (byte)6, (byte)8).And.HasSize(4);
    }

    [Fact(DisplayName = "Every integer width carries both lattices.")]
    public void EveryIntegerWidthCarriesBothLattices() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.SByte().Odd().Generate()   % 2).IsNotEqualTo(0);
            Check.That(Any.Int16().Odd().Generate()   % 2).IsNotEqualTo(0);
            Check.That(Any.UInt16().Odd().Generate()  % 2).IsEqualTo(1);
            Check.That(Any.UInt32().Odd().Generate()  % 2).IsEqualTo(1U);
            Check.That(Any.UInt64().Odd().Generate()  % 2).IsEqualTo(1UL);
            Check.That(Any.SByte().Even().Generate()  % 2).IsEqualTo(0);
            Check.That(Any.UInt64().Even().Generate() % 2).IsEqualTo(0UL);
#if NET8_0_OR_GREATER
            Check.That(Any.Int128().Odd().Generate()   % 2).IsNotEqualTo(Int128.Zero);
            Check.That(Any.UInt128().Odd().Generate()  % 2).IsEqualTo(UInt128.One);
            Check.That(Any.Int128().Even().Generate()  % 2).IsEqualTo(Int128.Zero);
            Check.That(Any.UInt128().Even().Generate() % 2).IsEqualTo(UInt128.Zero);
#endif
        }
    }

    [Fact(DisplayName = "Odd reaches the extremes of its type: the largest odd value, and the smallest.")]
    public void OddReachesTheExtremesOfItsType() {
        Check.That(Any.Int32().GreaterThan(int.MaxValue - 2).Odd().Generate()).IsEqualTo(int.MaxValue);
        Check.That(Any.Int32().LessThan(int.MinValue + 2).Odd().Generate()).IsEqualTo(int.MinValue + 1);
        Check.That(Any.Byte().GreaterThan(253).Odd().Generate()).IsEqualTo((byte)255);
    }

    [Fact(DisplayName = "Odd composes with Except and with a OneOf allow-list.")]
    [SuppressMessage(JustDummiesRule.JD029.Category, JustDummiesRule.JD029.Id, Justification = SuppressionJustification.JD029.RefusedValueIsTheSubject)]
    public void OddComposesWithExceptAndOneOf() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int32().Between(1, 5).Odd().Except(3).Generate()).IsOneOf(1, 5);
            Check.That(Any.Int32().OneOf(2, 3, 4, 5).Odd().Generate()).IsOneOf(3, 5);
        }
    }

    [Fact(DisplayName = "Odd and Even share MultipleOf's slot: a second, different lattice is refused naming both sides.")]
    [SuppressMessage(JustDummiesRule.JD034.Category, JustDummiesRule.JD034.Id, Justification = SuppressionJustification.JD034.ThrowIsTheSubject)]
    public void ASecondDifferentLatticeIsRefused() {
        Check.ThatCode(() => Any.Int32().Odd().MultipleOf(3))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("MultipleOf(3)", "Odd()");
        Check.ThatCode(() => Any.Int32().Even().Odd())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Odd()", "Even()");
        Check.ThatCode(() => Any.UInt64().MultipleOf(4).Even())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Even()", "MultipleOf(4)");
    }

    [Fact(DisplayName = "The same lattice declared again is a repetition the run time ignores: Even and MultipleOf(2) are one lattice.")]
    [SuppressMessage(JustDummiesRule.JD035.Category, JustDummiesRule.JD035.Id, Justification = SuppressionJustification.JD035.RepetitionIsTheSubject)]
    public void TheSameLatticeDeclaredAgainIsIgnored() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.Int32().Odd().Odd().Generate() % 2).IsNotEqualTo(0);
            Check.That(Any.Int32().Even().MultipleOf(2).Generate() % 2).IsEqualTo(0);
        }
    }

    [Fact(DisplayName = "A range holding no odd number is refused at the call that empties it.")]
    [SuppressMessage(JustDummiesRule.JD023.Category, JustDummiesRule.JD023.Id, Justification = SuppressionJustification.JD023.ThrowIsTheSubject)]
    public void ARangeHoldingNoOddNumberIsRefused() {
        Check.ThatCode(() => Any.Int32().Between(4, 4).Odd())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Odd()");
        Check.ThatCode(() => Any.Int32().Zero().Odd()).Throws<ConflictingAnyConstraintException>();
    }

}
