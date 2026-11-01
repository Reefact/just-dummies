using FsCheck;
using FsCheck.Fluent;

using static JustDummies.Analyzers.PropertyTests.DeclarationCall;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="DeclarationChain" />s: short chains on one generator other than
///     <c>Any.String()</c>, drawn from a small vocabulary of the constraints that generator takes once, so a call often
///     meets itself, another spelling of itself, or a different declaration of the same constraint.
/// </summary>
/// <remarks>
///     <para>
///         Each vocabulary spells one constraint several ways the run time takes as the same declaration —
///         <c>OneOf(6, 12)</c> and <c>OneOf([12, 6, 6])</c>, a granularity of <c>TimeSpan.FromMinutes(60)</c> and
///         <c>new TimeSpan(1, 0, 0)</c>, <c>WithOffset(x)</c> and <c>WithOffsetBetween(x, x)</c> — and several ways it
///         refuses as a second one, plus a call that narrows nothing (<c>MultipleOf(1)</c>) and a call that rejects
///         its own argument, which stops the chain before any later declaration is read. Where the source writes a
///         value set as a collection expression, the oracle passes the same values as arguments: the run time
///         identifies a value set by its values (ADR-0100).
///     </para>
///     <para>
///         The values are chosen so the constraints never contradict one another, which is not this rule's to report:
///         every value set of a numeric generator is a multiple of every step beside it. A <c>Distinct(comparer)</c> is
///         left out: the run time compares comparers by reference, so no reading of the source can tell a repetition
///         from a conflict, and JD034 does not claim it. <c>Any.String()</c> has its own chains, with JD015 beside it
///         (<see cref="StringChains" />).
///     </para>
/// </remarks>
internal static class DeclarationChains {

    /// <summary>Short, so both outcomes stay frequent: a long chain from a small vocabulary is almost always refused.</summary>
    private const int MaximumLength = 4;

    private static readonly DeclarationVocabulary[] Vocabularies = [
        new("Any.Char()", context => context.Char(),
            On<AnyChar>("Alpha()", g => g.Alpha()),
            On<AnyChar>("AlphaNumeric()", g => g.AlphaNumeric()),
            On<AnyChar>("Hexadecimal()", g => g.Hexadecimal()),
            On<AnyChar>("InUpperCase()", g => g.InUpperCase()),
            On<AnyChar>("InLowerCase()", g => g.InLowerCase())),
        new("Any.Int32()", context => context.Int32(),
            On<AnyInt32>("OneOf(6)", g => g.OneOf(6)),
            On<AnyInt32>("OneOf(12)", g => g.OneOf(12)),
            On<AnyInt32>("OneOf(6, 12)", g => g.OneOf(6, 12)),
            On<AnyInt32>("OneOf([12, 6, 6])", g => g.OneOf(12, 6, 6)),
            On<AnyInt32>("MultipleOf(1)", g => g.MultipleOf(1)),
            On<AnyInt32>("MultipleOf(2)", g => g.MultipleOf(2)),
            On<AnyInt32>("MultipleOf(3)", g => g.MultipleOf(3)),
            On<AnyInt32>("MultipleOf(0)", g => g.MultipleOf(0)),
            On<AnyInt32>("Even()", g => g.Even()),
            On<AnyInt32>("OneOf()", g => g.OneOf()),
            On<AnyInt32>("Positive()", g => g.Positive())),
        // The three lattices share one slot; no value set here, since every one would refuse a parity.
        new("Any.Int64()", context => context.Int64(),
            On<AnyInt64>("Odd()", g => g.Odd()),
            On<AnyInt64>("Even()", g => g.Even()),
            On<AnyInt64>("MultipleOf(2)", g => g.MultipleOf(2)),
            On<AnyInt64>("MultipleOf(3)", g => g.MultipleOf(3)),
            On<AnyInt64>("MultipleOf(1)", g => g.MultipleOf(1))),
        new("Any.Byte()", context => context.Byte(),
            On<AnyByte>("MultipleOf(2)", g => g.MultipleOf(2)),
            On<AnyByte>("MultipleOf(4)", g => g.MultipleOf(4)),
            On<AnyByte>("Even()", g => g.Even()),
            On<AnyByte>("OneOf(4)", g => g.OneOf(4)),
            On<AnyByte>("OneOf(8, 4)", g => g.OneOf(8, 4))),
        new("Any.Double()", context => context.Double(),
            On<AnyDouble>("OneOf(1.5)", g => g.OneOf(1.5)),
            On<AnyDouble>("OneOf(2.5)", g => g.OneOf(2.5)),
            On<AnyDouble>("OneOf(2.5, 1.5)", g => g.OneOf(2.5, 1.5)),
            On<AnyDouble>("OneOf(1)", g => g.OneOf(1)),
            On<AnyDouble>("OneOf(1.0)", g => g.OneOf(1.0)),
            On<AnyDouble>("OneOf([1])", g => g.OneOf(1)),
            On<AnyDouble>("OneOf(0.0)", g => g.OneOf(0.0)),
            On<AnyDouble>("OneOf(-0.0)", g => g.OneOf(-0.0)),
            On<AnyDouble>("OneOf(double.NaN)", g => g.OneOf(double.NaN))),
        // Two pins, each declared once; the values keep every pin in order, so only a second pin is ever refused.
        new("Any.OrderedPair(Any.Int32().Between(0, 10), Any.Int32().Between(0, 10))", context => Any.OrderedPair(context.Int32().Between(0, 10), context.Int32().Between(0, 10)),
            On<AnyOrderedPair<int>>("WithFirst(3)", g => g.WithFirst(3)),
            On<AnyOrderedPair<int>>("WithFirst(4)", g => g.WithFirst(4)),
            On<AnyOrderedPair<int>>("WithSecond(8)", g => g.WithSecond(8)),
            On<AnyOrderedPair<int>>("WithSecond(9)", g => g.WithSecond(9))),
        new("Any.Decimal()", context => context.Decimal(),
            On<AnyDecimal>("WithScale(1)", g => g.WithScale(1)),
            On<AnyDecimal>("WithScale(2)", g => g.WithScale(2)),
            On<AnyDecimal>("MultipleOf(0.25m)", g => g.MultipleOf(0.25m)),
            On<AnyDecimal>("MultipleOf(0.5m)", g => g.MultipleOf(0.5m)),
            On<AnyDecimal>("MultipleOf(1m)", g => g.MultipleOf(1m)),
            On<AnyDecimal>("OneOf(1m)", g => g.OneOf(1m)),
            On<AnyDecimal>("OneOf(1.0m)", g => g.OneOf(1.0m)),
            On<AnyDecimal>("OneOf(2m)", g => g.OneOf(2m)),
            On<AnyDecimal>("WithScale(29)", g => g.WithScale(29))),
        new("Any.Enum<DayOfWeek>()", context => context.Enum<DayOfWeek>(),
            On<AnyEnum<DayOfWeek>>("OneOf(DayOfWeek.Monday)", g => g.OneOf(DayOfWeek.Monday)),
            On<AnyEnum<DayOfWeek>>("OneOf(DayOfWeek.Friday)", g => g.OneOf(DayOfWeek.Friday)),
            On<AnyEnum<DayOfWeek>>("OneOf(DayOfWeek.Friday, DayOfWeek.Monday)", g => g.OneOf(DayOfWeek.Friday, DayOfWeek.Monday)),
            On<AnyEnum<DayOfWeek>>("OneOf(DayOfWeek.Monday, DayOfWeek.Friday)", g => g.OneOf(DayOfWeek.Monday, DayOfWeek.Friday)),
            On<AnyEnum<DayOfWeek>>("OneOf((DayOfWeek)42)", g => g.OneOf((DayOfWeek)42))),
        new("Any.StringMatching(\"[a-c]\")", context => context.StringMatching("[a-c]"),
            On<AnyPattern>("OneOf(\"a\")", g => g.OneOf("a")),
            On<AnyPattern>("OneOf(\"b\")", g => g.OneOf("b")),
            On<AnyPattern>("OneOf(\"a\", \"b\")", g => g.OneOf("a", "b")),
            On<AnyPattern>("OneOf([\"b\", \"a\"])", g => g.OneOf("b", "a"))),
        new("Any.DateTime()", context => context.DateTime(),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromMinutes(60))", g => g.WithGranularity(TimeSpan.FromMinutes(60))),
            On<AnyDateTime>("WithGranularity(new TimeSpan(1, 0, 0))", g => g.WithGranularity(new TimeSpan(1, 0, 0))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromMinutes(1))", g => g.WithGranularity(TimeSpan.FromMinutes(1))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromTicks(1))", g => g.WithGranularity(TimeSpan.FromTicks(1))),
            On<AnyDateTime>("WithGranularity(TimeSpan.Zero)", g => g.WithGranularity(TimeSpan.Zero))),
        new("Any.TimeOnly()", context => context.TimeOnly(),
            On<AnyTimeOnly>("WithGranularity(TimeSpan.FromMinutes(1))", g => g.WithGranularity(TimeSpan.FromMinutes(1))),
            On<AnyTimeOnly>("WithGranularity(TimeSpan.FromSeconds(60))", g => g.WithGranularity(TimeSpan.FromSeconds(60))),
            On<AnyTimeOnly>("WithGranularity(TimeSpan.FromMinutes(5))", g => g.WithGranularity(TimeSpan.FromMinutes(5)))),
        new("Any.DateTimeOffset()", context => context.DateTimeOffset(),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.Zero)", g => g.WithOffset(TimeSpan.Zero)),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromHours(1))", g => g.WithOffset(TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromMinutes(60))", g => g.WithOffset(TimeSpan.FromMinutes(60))),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromHours(2))", g => g.WithOffset(TimeSpan.FromHours(2))),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromSeconds(30))", g => g.WithOffset(TimeSpan.FromSeconds(30))),
            On<AnyDateTimeOffset>("WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))", g => g.WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))", g => g.WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(2))),
            On<AnyDateTimeOffset>("WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1))", g => g.WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("WithGranularity(TimeSpan.FromMinutes(1))", g => g.WithGranularity(TimeSpan.FromMinutes(1))),
            On<AnyDateTimeOffset>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1)))),
        new("Any.ListOf(Any.Int32())", context => Any.ListOf(context.Int32()),
            On<AnyList<int>>("Empty()", g => g.Empty()),
            On<AnyList<int>>("WithCount(0)", g => g.WithCount(0)),
            On<AnyList<int>>("WithCount(2)", g => g.WithCount(2)),
            On<AnyList<int>>("WithCount(3)", g => g.WithCount(3)),
            On<AnyList<int>>("WithCount(-1)", g => g.WithCount(-1)),
            On<AnyList<int>>("WithCount(1000001)", g => g.WithCount(1000001))),
        new("Any.DictionaryOf(Any.Int32(), Any.Int32())", context => Any.DictionaryOf(context.Int32(), context.Int32()),
            On<AnyDictionary<int, int>>("Empty()", g => g.Empty()),
            On<AnyDictionary<int, int>>("WithCount(0)", g => g.WithCount(0)),
            On<AnyDictionary<int, int>>("WithCount(2)", g => g.WithCount(2)),
            On<AnyDictionary<int, int>>("WithCount(-1)", g => g.WithCount(-1))),
        new("Any.Uri().Web()", context => context.Uri().Web(),
            On<AnyWebUri>("UsingHttp()", g => g.UsingHttp()),
            On<AnyWebUri>("UsingHttps()", g => g.UsingHttps()),
            On<AnyWebUri>("WithHost(\"a.example\")", g => g.WithHost("a.example")),
            On<AnyWebUri>("WithHost(\"b.example\")", g => g.WithHost("b.example")),
            On<AnyWebUri>("WithPort()", g => g.WithPort()),
            On<AnyWebUri>("WithPort(80)", g => g.WithPort(80)),
            On<AnyWebUri>("WithPort(8080)", g => g.WithPort(8080)),
            On<AnyWebUri>("WithUserInfo()", g => g.WithUserInfo()),
            On<AnyWebUri>("WithUserInfo(\"a\")", g => g.WithUserInfo("a")),
            On<AnyWebUri>("WithUserInfo(password: \"p\", user: \"a\")", g => g.WithUserInfo(password: "p", user: "a")),
            On<AnyWebUri>("WithUserInfo(\"a\", \"p\")", g => g.WithUserInfo("a", "p")),
            On<AnyWebUri>("WithPathSegments(1)", g => g.WithPathSegments(1)),
            On<AnyWebUri>("WithPathSegments(2)", g => g.WithPathSegments(2)),
            On<AnyWebUri>("WithoutPath()", g => g.WithoutPath()),
            On<AnyWebUri>("WithQuery()", g => g.WithQuery()),
            On<AnyWebUri>("WithPort(0)", g => g.WithPort(0)),
            On<AnyWebUri>("WithHost(\"\")", g => g.WithHost("")),
            On<AnyWebUri>("WithHost(\"bad host\")", g => g.WithHost("bad host")),
            On<AnyWebUri>("WithUserInfo(\"a:b\")", g => g.WithUserInfo("a:b")),
            On<AnyWebUri>("WithPathSegments(-1)", g => g.WithPathSegments(-1))),
        new("Any.Uri().WebSocket()", context => context.Uri().WebSocket(),
            On<AnyWebSocketUri>("UsingWs()", g => g.UsingWs()),
            On<AnyWebSocketUri>("UsingWss()", g => g.UsingWss()),
            On<AnyWebSocketUri>("WithPort(443)", g => g.WithPort(443)),
            On<AnyWebSocketUri>("WithPort(8443)", g => g.WithPort(8443))),
        new("Any.Uri().Ftp()", context => context.Uri().Ftp(),
            On<AnyFtpUri>("WithPort(21)", g => g.WithPort(21)),
            On<AnyFtpUri>("WithPort(22)", g => g.WithPort(22)),
            On<AnyFtpUri>("WithUserInfo()", g => g.WithUserInfo()),
            On<AnyFtpUri>("WithUserInfo(\"a\")", g => g.WithUserInfo("a"))),
        new("Any.Uri().Mailto()", context => context.Uri().Mailto(),
            On<AnyMailtoUri>("WithDomain(\"a.example\")", g => g.WithDomain("a.example")),
            On<AnyMailtoUri>("WithDomain(\"b.example\")", g => g.WithDomain("b.example")),
            On<AnyMailtoUri>("WithLocalPart(\"a\")", g => g.WithLocalPart("a")),
            On<AnyMailtoUri>("WithLocalPart(\"b\")", g => g.WithLocalPart("b")),
            On<AnyMailtoUri>("WithLocalPart(\"\")", g => g.WithLocalPart("")),
            On<AnyMailtoUri>("WithLocalPart(\"a b\")", g => g.WithLocalPart("a b")),
            On<AnyMailtoUri>("WithDomain(\"bad domain\")", g => g.WithDomain("bad domain"))),
        new("Any.Uri().Relative()", context => context.Uri().Relative(),
            On<AnyRelativeUri>("WithPathSegments(1)", g => g.WithPathSegments(1)),
            On<AnyRelativeUri>("WithPathSegments(2)", g => g.WithPathSegments(2)))
    ];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call at a time.</summary>
    public static Arbitrary<DeclarationChain> Arbitrary() {
        Gen<DeclarationChain> chains = from vocabulary in Gen.Elements(Vocabularies)
                                       from length in Gen.Choose(1, MaximumLength)
                                       from calls in Gen.Elements(vocabulary.Calls.ToArray()).ListOf(length)
                                       select new DeclarationChain(vocabulary, calls.ToList());

        return Arb.From(chains, chain => chain.Shrink());
    }

    #endregion

}
