#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnySignedIntegerTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "SByte: Positive and Negative are strict, and contradict each other.")]
    public void SByteSignConstraints() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.SByte().Positive().Generate()).IsStrictlyGreaterThan((sbyte)0);
            Check.That(Dummy.SByte().Negative().Generate()).IsStrictlyLessThan((sbyte)0);
        }

        Check.ThatCode(() => Dummy.SByte().Positive().Negative())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("Negative()", "Positive()");
    }

    [Fact(DisplayName = "SByte: Between is inclusive and reaches both bounds; extremes are generable.")]
    public void SByteBounds() {
        HashSet<sbyte> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Dummy.SByte().Between(-1, 1).Generate()); }
        Check.That(seen.Contains(-1)).IsTrue();
        Check.That(seen.Contains(1)).IsTrue();

        Check.That(Dummy.SByte().LessThanOrEqualTo(sbyte.MinValue).Generate()).IsEqualTo(sbyte.MinValue);
        Check.That(Dummy.SByte().GreaterThanOrEqualTo(sbyte.MaxValue).Generate()).IsEqualTo(sbyte.MaxValue);
        Check.ThatCode(() => Dummy.SByte().GreaterThan(sbyte.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Int16: Zero pins, NonZero excludes, and the pair conflicts.")]
    public void Int16ZeroFamily() {
        Check.That(Dummy.Int16().Zero().Generate()).IsEqualTo((short)0);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Int16().Between(-1, 1).NonZero().Generate()).IsNotEqualTo((short)0);
        }
        Check.ThatCode(() => Dummy.Int16().Zero().NonZero()).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "Int16: GreaterThan and LessThan are exclusive bounds.")]
    public void Int16ExclusiveBounds() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Int16().GreaterThan(10).LessThanOrEqualTo(12).Generate()).IsGreaterOrEqualThan((short)11);
            Check.That(Dummy.Int16().LessThan(10).GreaterThanOrEqualTo(8).Generate()).IsLessOrEqualThan((short)9);
        }
    }

    [Fact(DisplayName = "Int64: full-range generation works and crossed bounds conflict naming both sides.")]
    public void Int64RangeAndConflicts() {
        HashSet<long> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Dummy.Int64().Generate()); }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        Check.ThatCode(() => Dummy.Int64().GreaterThan(100L).LessThan(10L))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("LessThan(10)", "GreaterThan(100)");
    }

    [Fact(DisplayName = "Int64: OneOf stays within the supplied values and Except never yields an excluded one.")]
    public void Int64OneOfAndExcept() {
        long[] allowed = [1L, 5L, 9L];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Dummy.Int64().OneOf(allowed).Generate())).IsTrue();
            Check.That(Dummy.Int64().Between(1L, 3L).Except(2L).Generate()).IsNotEqualTo(2L);
            Check.That(Dummy.Int64().Between(7L, 8L).DifferentFrom(7L).Generate()).IsEqualTo(8L);
        }
    }

    [Fact(DisplayName = "Int64: extremes are generable and arguments are validated.")]
    public void Int64ExtremesAndArguments() {
        Check.That(Dummy.Int64().LessThanOrEqualTo(long.MinValue).Generate()).IsEqualTo(long.MinValue);
        Check.That(Dummy.Int64().GreaterThanOrEqualTo(long.MaxValue).Generate()).IsEqualTo(long.MaxValue);
        Check.ThatCode(() => Dummy.Int64().GreaterThan(long.MaxValue)).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Int64().Between(10L, 1L)).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.Int64().OneOf()).Throws<ArgumentException>();
        Check.ThatCode(() => Dummy.Int64().Except(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "Every signed integer generator materializes its own value type through Generate().")]
    public void MaterializesEachValueType() {
        sbyte small = Dummy.SByte().Positive().Generate();
        short mid   = Dummy.Int16().Negative().Generate();
        long  wide  = Dummy.Int64().Between(1L, 10L).Generate();

        Check.That((int)small).IsStrictlyGreaterThan(0);
        Check.That((int)mid).IsStrictlyLessThan(0);
        Check.That(wide).IsGreaterOrEqualThan(1L);
    }

}
