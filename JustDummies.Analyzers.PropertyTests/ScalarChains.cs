using FsCheck;
using FsCheck.Fluent;

using static JustDummies.Analyzers.PropertyTests.DeclarationCall;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="ScalarChain" />s: short chains on each exact-type generator JD023 and JD038
///     read — the integers, <c>Any.Decimal()</c> and the temporal generators — drawn from a vocabulary of bounds,
///     lattices, value sets and exclusions whose values sit close enough together that a chain often empties the
///     domain, often leaves one value, and often leaves more (issue #356).
/// </summary>
/// <remarks>
///     <para>
///         The vocabularies spell what the two rules claim and what they leave to others: a transposed range and a
///         refused step are JD014's, a second lattice or value set JD034's, and the oracle sorts those out. A time of
///         day spells both windows across midnight and bounds on either side of them, with granularities that divide
///         a day, since one that does not is refused by the run time on its own and the rule stands down there. No
///         <c>Zero()</c>: a value pinned by name is left to read as the constant it is.
///     </para>
///     <para>
///         The ranges stay a few dozen values wide at most, which is as far as the rule walks before it stops counting.
///     </para>
/// </remarks>
internal static class ScalarChains {

    private const int MaximumLength = 4;

    private static readonly DateTime       D1 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime       D2 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime       D3 = new(2026, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTimeOffset O1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset O2 = new(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2));
    private static readonly DateTimeOffset O3 = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    private static readonly DeclarationVocabulary[] Vocabularies = [
        new("Any.Int32()", context => context.Int32(),
            On<AnyInt32>("Between(1, 5)", g => g.Between(1, 5)),
            On<AnyInt32>("Between(3, 3)", g => g.Between(3, 3)),
            On<AnyInt32>("Between(5, 1)", g => g.Between(5, 1)),
            On<AnyInt32>("GreaterThan(2)", g => g.GreaterThan(2)),
            On<AnyInt32>("GreaterThanOrEqualTo(4)", g => g.GreaterThanOrEqualTo(4)),
            On<AnyInt32>("LessThan(4)", g => g.LessThan(4)),
            On<AnyInt32>("LessThanOrEqualTo(3)", g => g.LessThanOrEqualTo(3)),
            On<AnyInt32>("Positive()", g => g.Positive()),
            On<AnyInt32>("Negative()", g => g.Negative()),
            On<AnyInt32>("NonZero()", g => g.NonZero()),
            On<AnyInt32>("MultipleOf(2)", g => g.MultipleOf(2)),
            On<AnyInt32>("MultipleOf(3)", g => g.MultipleOf(3)),
            On<AnyInt32>("MultipleOf(0)", g => g.MultipleOf(0)),
            On<AnyInt32>("Odd()", g => g.Odd()),
            On<AnyInt32>("Even()", g => g.Even()),
            On<AnyInt32>("OneOf(3)", g => g.OneOf(3)),
            On<AnyInt32>("OneOf(2, 4)", g => g.OneOf(2, 4)),
            On<AnyInt32>("Except(3)", g => g.Except(3)),
            On<AnyInt32>("Except(2, 4)", g => g.Except(2, 4)),
            On<AnyInt32>("DifferentFrom(4)", g => g.DifferentFrom(4)),
            On<AnyInt32>("GreaterThan(int.MaxValue)", g => g.GreaterThan(int.MaxValue)),
            On<AnyInt32>("GreaterThanOrEqualTo(int.MaxValue)", g => g.GreaterThanOrEqualTo(int.MaxValue))),
        new("Any.Byte()", context => context.Byte(),
            On<AnyByte>("Between(250, 255)", g => g.Between(250, 255)),
            On<AnyByte>("GreaterThan(254)", g => g.GreaterThan(254)),
            On<AnyByte>("GreaterThanOrEqualTo(255)", g => g.GreaterThanOrEqualTo(255)),
            On<AnyByte>("LessThan(1)", g => g.LessThan(1)),
            On<AnyByte>("LessThanOrEqualTo(2)", g => g.LessThanOrEqualTo(2)),
            On<AnyByte>("MultipleOf(100)", g => g.MultipleOf(100)),
            On<AnyByte>("Even()", g => g.Even()),
            On<AnyByte>("OneOf(0, 255)", g => g.OneOf(0, 255)),
            On<AnyByte>("Except(255)", g => g.Except(255)),
            On<AnyByte>("NonZero()", g => g.NonZero())),
        new("Any.Decimal()", context => context.Decimal(),
            On<AnyDecimal>("Between(1m, 2m)", g => g.Between(1m, 2m)),
            On<AnyDecimal>("Between(1.5m, 1.5m)", g => g.Between(1.5m, 1.5m)),
            On<AnyDecimal>("Between(2m, 1m)", g => g.Between(2m, 1m)),
            On<AnyDecimal>("GreaterThan(1m)", g => g.GreaterThan(1m)),
            On<AnyDecimal>("GreaterThanOrEqualTo(1.5m)", g => g.GreaterThanOrEqualTo(1.5m)),
            On<AnyDecimal>("LessThan(2m)", g => g.LessThan(2m)),
            On<AnyDecimal>("LessThanOrEqualTo(1.5m)", g => g.LessThanOrEqualTo(1.5m)),
            On<AnyDecimal>("Positive()", g => g.Positive()),
            On<AnyDecimal>("Negative()", g => g.Negative()),
            On<AnyDecimal>("NonZero()", g => g.NonZero()),
            On<AnyDecimal>("MultipleOf(0.5m)", g => g.MultipleOf(0.5m)),
            On<AnyDecimal>("MultipleOf(0.25m)", g => g.MultipleOf(0.25m)),
            On<AnyDecimal>("MultipleOf(0m)", g => g.MultipleOf(0m)),
            On<AnyDecimal>("WithScale(0)", g => g.WithScale(0)),
            On<AnyDecimal>("WithScale(1)", g => g.WithScale(1)),
            On<AnyDecimal>("OneOf(1.5m)", g => g.OneOf(1.5m)),
            On<AnyDecimal>("OneOf(1m, 2m)", g => g.OneOf(1m, 2m)),
            On<AnyDecimal>("Except(1.5m)", g => g.Except(1.5m)),
            On<AnyDecimal>("Except(1m, 2m)", g => g.Except(1m, 2m)),
            On<AnyDecimal>("DifferentFrom(2m)", g => g.DifferentFrom(2m)),
            On<AnyDecimal>("GreaterThan(decimal.MaxValue)", g => g.GreaterThan(decimal.MaxValue))),
        new("Any.DateTime()", context => context.DateTime(),
            On<AnyDateTime>("After(new DateTime(2026, 1, 1, 12, 0, 0))", g => g.After(D2)),
            On<AnyDateTime>("AfterOrEqualTo(new DateTime(2026, 1, 1))", g => g.AfterOrEqualTo(D1)),
            On<AnyDateTime>("Before(new DateTime(2026, 1, 1))", g => g.Before(D1)),
            On<AnyDateTime>("BeforeOrEqualTo(new DateTime(2026, 1, 1, 12, 0, 0))", g => g.BeforeOrEqualTo(D2)),
            On<AnyDateTime>("Between(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2))", g => g.Between(D1, D3)),
            On<AnyDateTime>("Between(new DateTime(2026, 1, 1, 12, 0, 0), new DateTime(2026, 1, 1, 12, 0, 0))", g => g.Between(D2, D2)),
            On<AnyDateTime>("Between(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1))", g => g.Between(D3, D1)),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromDays(1))", g => g.WithGranularity(TimeSpan.FromDays(1))),
            On<AnyDateTime>("OneOf(new DateTime(2026, 1, 1, 12, 0, 0))", g => g.OneOf(D2)),
            On<AnyDateTime>("OneOf(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2))", g => g.OneOf(D1, D3)),
            On<AnyDateTime>("Except(new DateTime(2026, 1, 1, 12, 0, 0))", g => g.Except(D2)),
            On<AnyDateTime>("DifferentFrom(new DateTime(2026, 1, 1))", g => g.DifferentFrom(D1)),
            On<AnyDateTime>("After(DateTime.MaxValue)", g => g.After(DateTime.MaxValue))),
        new("Any.DateTimeOffset()", context => context.DateTimeOffset(),
            On<AnyDateTimeOffset>("After(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))", g => g.After(O2)),
            On<AnyDateTimeOffset>("AfterOrEqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))", g => g.AfterOrEqualTo(O1)),
            On<AnyDateTimeOffset>("Before(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero))", g => g.Before(O1)),
            On<AnyDateTimeOffset>("BeforeOrEqualTo(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))", g => g.BeforeOrEqualTo(O2)),
            On<AnyDateTimeOffset>("Between(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero))", g => g.Between(O1, O3)),
            On<AnyDateTimeOffset>("Between(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)), new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))", g => g.Between(O2, O2)),
            On<AnyDateTimeOffset>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("OneOf(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))", g => g.OneOf(O2)),
            On<AnyDateTimeOffset>("Except(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(2)))", g => g.Except(O2))),
        new("Any.TimeSpan()", context => context.TimeSpan(),
            On<AnyTimeSpan>("Positive()", g => g.Positive()),
            On<AnyTimeSpan>("Negative()", g => g.Negative()),
            On<AnyTimeSpan>("NonZero()", g => g.NonZero()),
            On<AnyTimeSpan>("GreaterThan(TimeSpan.FromHours(1))", g => g.GreaterThan(TimeSpan.FromHours(1))),
            On<AnyTimeSpan>("GreaterThanOrEqualTo(TimeSpan.FromHours(2))", g => g.GreaterThanOrEqualTo(TimeSpan.FromHours(2))),
            On<AnyTimeSpan>("LessThan(TimeSpan.FromHours(2))", g => g.LessThan(TimeSpan.FromHours(2))),
            On<AnyTimeSpan>("LessThanOrEqualTo(TimeSpan.FromHours(1))", g => g.LessThanOrEqualTo(TimeSpan.FromHours(1))),
            On<AnyTimeSpan>("Between(TimeSpan.FromHours(1), TimeSpan.FromHours(3))", g => g.Between(TimeSpan.FromHours(1), TimeSpan.FromHours(3))),
            On<AnyTimeSpan>("Between(TimeSpan.FromHours(2), TimeSpan.FromHours(2))", g => g.Between(TimeSpan.FromHours(2), TimeSpan.FromHours(2))),
            On<AnyTimeSpan>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyTimeSpan>("WithGranularity(TimeSpan.FromMinutes(30))", g => g.WithGranularity(TimeSpan.FromMinutes(30))),
            On<AnyTimeSpan>("OneOf(TimeSpan.FromHours(2))", g => g.OneOf(TimeSpan.FromHours(2))),
            On<AnyTimeSpan>("Except(TimeSpan.FromHours(2))", g => g.Except(TimeSpan.FromHours(2))),
            On<AnyTimeSpan>("GreaterThan(TimeSpan.MaxValue)", g => g.GreaterThan(TimeSpan.MaxValue))),
        new("Any.DateOnly()", context => context.DateOnly(),
            On<AnyDateOnly>("After(new DateOnly(2026, 1, 1))", g => g.After(new DateOnly(2026, 1, 1))),
            On<AnyDateOnly>("AfterOrEqualTo(new DateOnly(2026, 1, 2))", g => g.AfterOrEqualTo(new DateOnly(2026, 1, 2))),
            On<AnyDateOnly>("Before(new DateOnly(2026, 1, 3))", g => g.Before(new DateOnly(2026, 1, 3))),
            On<AnyDateOnly>("BeforeOrEqualTo(new DateOnly(2026, 1, 2))", g => g.BeforeOrEqualTo(new DateOnly(2026, 1, 2))),
            On<AnyDateOnly>("Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3))", g => g.Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3))),
            On<AnyDateOnly>("Between(new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 1))", g => g.Between(new DateOnly(2026, 1, 3), new DateOnly(2026, 1, 1))),
            On<AnyDateOnly>("OneOf(new DateOnly(2026, 1, 2))", g => g.OneOf(new DateOnly(2026, 1, 2))),
            On<AnyDateOnly>("OneOf(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3))", g => g.OneOf(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3))),
            On<AnyDateOnly>("Except(new DateOnly(2026, 1, 2))", g => g.Except(new DateOnly(2026, 1, 2))),
            On<AnyDateOnly>("DifferentFrom(new DateOnly(2026, 1, 1))", g => g.DifferentFrom(new DateOnly(2026, 1, 1)))),
        new("Any.TimeOnly()", context => context.TimeOnly(),
            On<AnyTimeOnly>("After(new TimeOnly(10, 0))", g => g.After(new TimeOnly(10, 0))),
            On<AnyTimeOnly>("AfterOrEqualTo(new TimeOnly(23, 0))", g => g.AfterOrEqualTo(new TimeOnly(23, 0))),
            On<AnyTimeOnly>("Before(new TimeOnly(2, 0))", g => g.Before(new TimeOnly(2, 0))),
            On<AnyTimeOnly>("BeforeOrEqualTo(new TimeOnly(12, 0))", g => g.BeforeOrEqualTo(new TimeOnly(12, 0))),
            On<AnyTimeOnly>("Between(new TimeOnly(9, 0), new TimeOnly(17, 0))", g => g.Between(new TimeOnly(9, 0), new TimeOnly(17, 0))),
            On<AnyTimeOnly>("Between(new TimeOnly(22, 0), new TimeOnly(6, 0))", g => g.Between(new TimeOnly(22, 0), new TimeOnly(6, 0))),
            On<AnyTimeOnly>("Between(new TimeOnly(23, 0), new TimeOnly(1, 0))", g => g.Between(new TimeOnly(23, 0), new TimeOnly(1, 0))),
            On<AnyTimeOnly>("Between(new TimeOnly(23, 30), new TimeOnly(23, 30))", g => g.Between(new TimeOnly(23, 30), new TimeOnly(23, 30))),
            On<AnyTimeOnly>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyTimeOnly>("WithGranularity(TimeSpan.FromMinutes(30))", g => g.WithGranularity(TimeSpan.FromMinutes(30))),
            On<AnyTimeOnly>("OneOf(new TimeOnly(23, 30))", g => g.OneOf(new TimeOnly(23, 30))),
            On<AnyTimeOnly>("OneOf(new TimeOnly(10, 0), new TimeOnly(12, 0))", g => g.OneOf(new TimeOnly(10, 0), new TimeOnly(12, 0))),
            On<AnyTimeOnly>("Except(new TimeOnly(23, 30))", g => g.Except(new TimeOnly(23, 30))),
            On<AnyTimeOnly>("DifferentFrom(new TimeOnly(12, 0))", g => g.DifferentFrom(new TimeOnly(12, 0))))
    ];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call at a time.</summary>
    public static Arbitrary<ScalarChain> Arbitrary() {
        Gen<ScalarChain> chains = from vocabulary in Gen.Elements(Vocabularies)
                                  from length in Gen.Choose(1, MaximumLength)
                                  from calls in Gen.Elements(vocabulary.Calls.ToArray()).ListOf(length)
                                  select new ScalarChain(vocabulary, calls.ToList());

        return Arb.From(chains, chain => chain.Shrink());
    }

    #endregion

}
