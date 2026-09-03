#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class DummyUnsignedIntegerTests {

    private const int SampleCount = 200;

    [Fact(DisplayName = "Byte: Between is inclusive and reaches both bounds; extremes are generable.")]
    public void ByteBounds() {
        HashSet<byte> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            byte value = Dummy.Byte().Between(1, 3).Generate();
            seen.Add(value);
            Check.That((int)value).IsGreaterOrEqualThan(1);
            Check.That((int)value).IsLessOrEqualThan(3);
        }
        Check.That(seen.Contains(1)).IsTrue();
        Check.That(seen.Contains(3)).IsTrue();

        Check.That(Dummy.Byte().LessThanOrEqualTo(0).Generate()).IsEqualTo((byte)0);
        Check.That(Dummy.Byte().GreaterThanOrEqualTo(byte.MaxValue).Generate()).IsEqualTo(byte.MaxValue);
    }

    [Fact(DisplayName = "Byte: Zero pins, NonZero excludes, the pair conflicts, and GreaterThan(max) conflicts.")]
    public void ByteZeroAndConflicts() {
        Check.That(Dummy.Byte().Zero().Generate()).IsEqualTo((byte)0);
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.Byte().Between(0, 1).NonZero().Generate()).IsEqualTo((byte)1);
        }
        Check.ThatCode(() => Dummy.Byte().Zero().NonZero()).Throws<ConflictingDummyConstraintException>();
        Check.ThatCode(() => Dummy.Byte().GreaterThan(byte.MaxValue)).Throws<ConflictingDummyConstraintException>();
    }

    [Fact(DisplayName = "UInt16 and UInt32: exclusive bounds behave and crossed bounds conflict.")]
    public void MidWidthExclusiveBounds() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That((int)Dummy.UInt16().GreaterThan(10).LessThanOrEqualTo(12).Generate()).IsGreaterOrEqualThan(11);
            Check.That(Dummy.UInt32().LessThan(10u).GreaterThanOrEqualTo(8u).Generate()).IsLessOrEqualThan(9u);
        }

        Check.ThatCode(() => Dummy.UInt32().GreaterThan(100u).LessThan(10u))
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("LessThan(10)", "GreaterThan(100)");
    }

    [Fact(DisplayName = "UInt64: the full-width sampling path yields varied values and honors exclusions.")]
    public void UInt64FullWidth() {
        HashSet<ulong> seen = [];
        for (int i = 0; i < SampleCount; i++) { seen.Add(Dummy.UInt64().Generate()); }
        Check.That(seen.Count).IsStrictlyGreaterThan(1);

        for (int i = 0; i < SampleCount; i++) {
            Check.That(Dummy.UInt64().Between(0UL, 2UL).Except(1UL).Generate()).IsNotEqualTo(1UL);
        }
    }

    [Fact(DisplayName = "UInt64: extremes are generable and OneOf/Except behave.")]
    public void UInt64ExtremesAndSets() {
        Check.That(Dummy.UInt64().GreaterThanOrEqualTo(ulong.MaxValue).Generate()).IsEqualTo(ulong.MaxValue);
        Check.ThatCode(() => Dummy.UInt64().GreaterThan(ulong.MaxValue)).Throws<ConflictingDummyConstraintException>();

        ulong[] allowed = [1UL, 5UL];
        for (int i = 0; i < SampleCount; i++) {
            Check.That(allowed.Contains(Dummy.UInt64().OneOf(allowed).Generate())).IsTrue();
            Check.That(Dummy.UInt64().Between(7UL, 8UL).DifferentFrom(7UL).Generate()).IsEqualTo(8UL);
        }
        Check.ThatCode(() => Dummy.UInt64().Between(10UL, 1UL)).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "Every unsigned integer generator materializes its own value type through Generate().")]
    public void MaterializesEachValueType() {
        byte   tiny = Dummy.Byte().Between(1, 10).Generate();
        ushort mid  = Dummy.UInt16().NonZero().Generate();
        uint   wide = Dummy.UInt32().Between(1u, 10u).Generate();
        ulong  huge = Dummy.UInt64().Between(1UL, 10UL).Generate();

        Check.That((int)tiny).IsGreaterOrEqualThan(1);
        Check.That((int)mid).IsStrictlyGreaterThan(0);
        Check.That(wide).IsGreaterOrEqualThan(1u);
        Check.That(huge).IsGreaterOrEqualThan(1UL);
    }

}
