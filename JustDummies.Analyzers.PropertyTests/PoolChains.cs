using FsCheck;
using FsCheck.Fluent;

using static JustDummies.Analyzers.PropertyTests.DeclarationCall;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="PoolChain" />s: a value set on one of the nine numeric families JD029 reads,
///     between up to three constraints from that family's vocabulary.
/// </summary>
/// <remarks>
///     <para>
///         The pooled values are written the ways a test writes them: a literal the compiler converts to the element
///         type, and a constant of the element type itself — <c>byte.MaxValue</c>, <c>long.MinValue</c>. The second is
///         a different operation for the rule to fold, and it sits at the edge of the type, where a fold through the
///         wrong width would misjudge it.
///     </para>
///     <para>
///         Every constraint argument is a plain literal, so the constraint a diagnostic names reads back as the call's
///         own source and the blame can be checked against the run time. A chain the run time refuses before any value
///         is judged — a constraint declared twice, an argument rejected on its own — is set aside by the property, not
///         spelled out of the vocabulary: which bound pairs the run time takes as one declaration is its call to make.
///     </para>
/// </remarks>
internal static class PoolChains {

    private const int MaximumConstraints = 3;
    private const int MaximumPool        = 4;

    private static readonly PoolFamily[] Families = [
        PoolFamily.Of<AnyByte, byte>("Any.Byte()", context => context.Byte(), (g, values) => g.OneOf(values),
                                     [("0", 0), ("4", 4), ("9", 9), ("byte.MinValue", byte.MinValue), ("byte.MaxValue", byte.MaxValue)],
                                     On<AnyByte>("NonZero()", g => g.NonZero()),
                                     On<AnyByte>("GreaterThan(3)", g => g.GreaterThan(3)),
                                     On<AnyByte>("LessThanOrEqualTo(9)", g => g.LessThanOrEqualTo(9)),
                                     On<AnyByte>("MultipleOf(2)", g => g.MultipleOf(2)),
                                     On<AnyByte>("Except(0, 9)", g => g.Except(0, 9))),
        PoolFamily.Of<AnySByte, sbyte>("Any.SByte()", context => context.SByte(), (g, values) => g.OneOf(values),
                                       [("-5", -5), ("0", 0), ("3", 3), ("sbyte.MinValue", sbyte.MinValue), ("sbyte.MaxValue", sbyte.MaxValue)],
                                       On<AnySByte>("Positive()", g => g.Positive()),
                                       On<AnySByte>("Negative()", g => g.Negative()),
                                       On<AnySByte>("Zero()", g => g.Zero()),
                                       On<AnySByte>("GreaterThanOrEqualTo(-5)", g => g.GreaterThanOrEqualTo(-5)),
                                       On<AnySByte>("LessThan(3)", g => g.LessThan(3)),
                                       On<AnySByte>("DifferentFrom(0)", g => g.DifferentFrom(0))),
        PoolFamily.Of<AnyInt16, short>("Any.Int16()", context => context.Int16(), (g, values) => g.OneOf(values),
                                       [("-5", -5), ("0", 0), ("6", 6), ("short.MinValue", short.MinValue), ("short.MaxValue", short.MaxValue)],
                                       On<AnyInt16>("Negative()", g => g.Negative()),
                                       On<AnyInt16>("NonZero()", g => g.NonZero()),
                                       On<AnyInt16>("Between(-5, 6)", g => g.Between(-5, 6)),
                                       On<AnyInt16>("MultipleOf(3)", g => g.MultipleOf(3)),
                                       On<AnyInt16>("Except(-5, 6)", g => g.Except(-5, 6))),
        PoolFamily.Of<AnyUInt16, ushort>("Any.UInt16()", context => context.UInt16(), (g, values) => g.OneOf(values),
                                         [("0", 0), ("2", 2), ("7", 7), ("ushort.MaxValue", ushort.MaxValue)],
                                         On<AnyUInt16>("Zero()", g => g.Zero()),
                                         On<AnyUInt16>("NonZero()", g => g.NonZero()),
                                         On<AnyUInt16>("GreaterThanOrEqualTo(2)", g => g.GreaterThanOrEqualTo(2)),
                                         On<AnyUInt16>("LessThan(7)", g => g.LessThan(7)),
                                         On<AnyUInt16>("MultipleOf(2)", g => g.MultipleOf(2))),
        PoolFamily.Of<AnyInt32, int>("Any.Int32()", context => context.Int32(), (g, values) => g.OneOf(values),
                                     [("-4", -4), ("0", 0), ("6", 6), ("int.MinValue", int.MinValue), ("int.MaxValue", int.MaxValue)],
                                     On<AnyInt32>("Positive()", g => g.Positive()),
                                     On<AnyInt32>("Zero()", g => g.Zero()),
                                     On<AnyInt32>("GreaterThan(-4)", g => g.GreaterThan(-4)),
                                     On<AnyInt32>("LessThanOrEqualTo(6)", g => g.LessThanOrEqualTo(6)),
                                     On<AnyInt32>("MultipleOf(2)", g => g.MultipleOf(2)),
                                     On<AnyInt32>("DifferentFrom(6)", g => g.DifferentFrom(6))),
        PoolFamily.Of<AnyUInt32, uint>("Any.UInt32()", context => context.UInt32(), (g, values) => g.OneOf(values),
                                       [("0", 0U), ("3", 3U), ("8", 8U), ("uint.MaxValue", uint.MaxValue)],
                                       On<AnyUInt32>("NonZero()", g => g.NonZero()),
                                       On<AnyUInt32>("Between(3, 8)", g => g.Between(3, 8)),
                                       On<AnyUInt32>("LessThan(8)", g => g.LessThan(8)),
                                       On<AnyUInt32>("Except(3)", g => g.Except(3))),
        PoolFamily.Of<AnyInt64, long>("Any.Int64()", context => context.Int64(), (g, values) => g.OneOf(values),
                                      [("-6", -6L), ("0", 0L), ("9", 9L), ("long.MinValue", long.MinValue), ("long.MaxValue", long.MaxValue)],
                                      On<AnyInt64>("Positive()", g => g.Positive()),
                                      On<AnyInt64>("Negative()", g => g.Negative()),
                                      On<AnyInt64>("GreaterThanOrEqualTo(-6)", g => g.GreaterThanOrEqualTo(-6)),
                                      On<AnyInt64>("LessThan(9)", g => g.LessThan(9)),
                                      On<AnyInt64>("MultipleOf(3)", g => g.MultipleOf(3))),
        PoolFamily.Of<AnyUInt64, ulong>("Any.UInt64()", context => context.UInt64(), (g, values) => g.OneOf(values),
                                        [("0", 0UL), ("5", 5UL), ("10", 10UL), ("ulong.MaxValue", ulong.MaxValue)],
                                        On<AnyUInt64>("NonZero()", g => g.NonZero()),
                                        On<AnyUInt64>("GreaterThan(5)", g => g.GreaterThan(5)),
                                        On<AnyUInt64>("LessThanOrEqualTo(10)", g => g.LessThanOrEqualTo(10)),
                                        On<AnyUInt64>("MultipleOf(5)", g => g.MultipleOf(5)),
                                        On<AnyUInt64>("DifferentFrom(10)", g => g.DifferentFrom(10))),
        PoolFamily.Of<AnyDecimal, decimal>("Any.Decimal()", context => context.Decimal(), (g, values) => g.OneOf(values),
                                           [("-3m", -3m), ("0m", 0m), ("1.5m", 1.5m), ("2.25m", 2.25m), ("decimal.MaxValue", decimal.MaxValue)],
                                           On<AnyDecimal>("Positive()", g => g.Positive()),
                                           On<AnyDecimal>("Negative()", g => g.Negative()),
                                           On<AnyDecimal>("NonZero()", g => g.NonZero()),
                                           On<AnyDecimal>("LessThan(2)", g => g.LessThan(2)),
                                           On<AnyDecimal>("WithScale(0)", g => g.WithScale(0)),
                                           On<AnyDecimal>("WithScale(1)", g => g.WithScale(1)),
                                           On<AnyDecimal>("Except(-3, 0)", g => g.Except(-3, 0)))
    ];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call or one pooled value at a time.</summary>
    public static Arbitrary<PoolChain> Arbitrary() {
        Gen<PoolChain> chains = from family in Gen.Elements(Families)
                                from poolSize in Gen.Choose(1, MaximumPool)
                                from pool in Gen.Shuffle(family.Elements.ToArray())
                                from constraintCount in Gen.Choose(0, MaximumConstraints)
                                from constraints in Gen.Shuffle(family.Constraints.ToArray())
                                from position in Gen.Choose(0, MaximumConstraints)
                                select new PoolChain(family, pool.Take(poolSize).ToList(), constraints.Take(constraintCount).ToList(), position);

        return Arb.From(chains, chain => chain.Shrink());
    }

    #endregion

}
