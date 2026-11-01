using FsCheck;
using FsCheck.Fluent;

using static JustDummies.Analyzers.PropertyTests.DeclarationCall;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="RepetitionChain" />s: short chains drawn from small vocabularies, so a call
///     often meets itself, another spelling of itself, or a different declaration of the same constraint.
/// </summary>
/// <remarks>
///     <para>
///         The oracle reads a call as ignored when it leaves every draw as it was, so each vocabulary is built for that
///         reading to mean "repeated" and nothing else. Every call changes the draws the first time it is applied, and
///         no call is made inert by a different one before it: an exclusion never lists a value another call already
///         removed, a subtraction never meets a family that lacks what it removes, and an exclusion is drawn only where
///         the excluded value is drawn often. A call the run time refuses — a second, different declaration, or an
///         argument it rejects — ends the chain, and some vocabularies carry one on purpose.
///     </para>
///     <para>
///         Left out for the same reason: an exclusion on a continuous generator (the excluded value is never drawn, so
///         excluding it is invisible), a <c>Guid</c>'s <c>NonEmpty()</c> (likewise), and bounds, which JD035 does not
///         read. The unit cases cover them.
///     </para>
/// </remarks>
internal static class RepetitionChains {

    /// <summary>Short, so both outcomes stay frequent.</summary>
    private const int MaximumLength = 5;

    private static readonly DeclarationVocabulary[] Vocabularies = [
        new("Any.String().WithLength(4)", context => context.String().WithLength(4),
            On<AnyString>("Alpha()", g => g.Alpha()),
            On<AnyString>("AlphaNumeric()", g => g.AlphaNumeric()),
            On<AnyString>("WithLength(4)", g => g.WithLength(4)),
            On<AnyString>("WithLength(-1)", g => g.WithLength(-1))),
        new("Any.String().WithLength(4).Alpha()", context => context.String().WithLength(4).Alpha(),
            On<AnyString>("InUpperCase()", g => g.InUpperCase()),
            On<AnyString>("InLowerCase()", g => g.InLowerCase()),
            On<AnyString>("Containing(\"q\")", g => g.Containing("q")),
            On<AnyString>("Containing(\"z\")", g => g.Containing("z"))),
        new("Any.String().WithLength(1).WithChars(\" a\")", context => context.String().WithLength(1).WithChars(" a"),
            On<AnyString>("NotBlank()", g => g.NotBlank())),
        new("Any.String().WithLength(4).Printable()", context => context.String().WithLength(4).Printable(),
            On<AnyString>("WithoutAlpha()", g => g.WithoutAlpha()),
            On<AnyString>("WithoutNumeric()", g => g.WithoutNumeric()),
            On<AnyString>("WithoutChars(\"~\")", g => g.WithoutChars("~")),
            On<AnyString>("StartingWith(\"-\")", g => g.StartingWith("-")),
            On<AnyString>("StartingWith(\"+\")", g => g.StartingWith("+"))),
        new("Any.String().OneOf(\"a\", \"b\", \"c\", \"d\", \"e\", \"f\", \"g\")", context => context.String().OneOf("a", "b", "c", "d", "e", "f", "g"),
            On<AnyString>("Except(\"a\")", g => g.Except("a")),
            On<AnyString>("Except(\"b\")", g => g.Except("b")),
            On<AnyString>("Except(\"c\", \"d\")", g => g.Except("c", "d")),
            On<AnyString>("Except(\"d\", \"c\")", g => g.Except("d", "c")),
            On<AnyString>("DifferentFrom(\"e\")", g => g.DifferentFrom("e")),
            On<AnyString>("Except(\"f\", \"f\")", g => g.Except("f", "f")),
            On<AnyString>("Except(\"f\")", g => g.Except("f"))),
        new("Any.String().WithLength(1)", context => context.String().WithLength(1),
            On<AnyString>("OneOf(\"a\", \"b\")", g => g.OneOf("a", "b")),
            On<AnyString>("OneOf([\"b\", \"a\", \"a\"])", g => g.OneOf("b", "a", "a")),
            On<AnyString>("OneOf(\"c\")", g => g.OneOf("c"))),
        new("Any.Char()", context => context.Char(),
            On<AnyChar>("Alpha()", g => g.Alpha()),
            On<AnyChar>("Numeric()", g => g.Numeric())),
        new("Any.Char().Alpha()", context => context.Char().Alpha(),
            On<AnyChar>("InUpperCase()", g => g.InUpperCase()),
            On<AnyChar>("InLowerCase()", g => g.InLowerCase())),
        new("Any.Char().Printable()", context => context.Char().Printable(),
            On<AnyChar>("WithoutAlpha()", g => g.WithoutAlpha()),
            On<AnyChar>("WithoutNumeric()", g => g.WithoutNumeric())),
        new("Any.Char().OneOf('a', 'b', 'c', 'd')", context => context.Char().OneOf('a', 'b', 'c', 'd'),
            On<AnyChar>("Except('a')", g => g.Except('a')),
            On<AnyChar>("Except('b')", g => g.Except('b')),
            On<AnyChar>("DifferentFrom('c')", g => g.DifferentFrom('c'))),
        new("Any.Int32().Between(1, 12)", context => context.Int32().Between(1, 12),
            On<AnyInt32>("MultipleOf(2)", g => g.MultipleOf(2)),
            On<AnyInt32>("MultipleOf(3)", g => g.MultipleOf(3)),
            On<AnyInt32>("MultipleOf(0)", g => g.MultipleOf(0)),
            On<AnyInt32>("Odd()", g => g.Odd()),
            On<AnyInt32>("Even()", g => g.Even())),
        new("Any.Int32().Between(0, 6)", context => context.Int32().Between(0, 6),
            On<AnyInt32>("Except(1)", g => g.Except(1)),
            On<AnyInt32>("Except(2)", g => g.Except(2)),
            On<AnyInt32>("Except(3, 4)", g => g.Except(3, 4)),
            On<AnyInt32>("Except(4, 3)", g => g.Except(4, 3)),
            On<AnyInt32>("DifferentFrom(5)", g => g.DifferentFrom(5)),
            On<AnyInt32>("NonZero()", g => g.NonZero())),
        new("Any.Int32().Between(1, 9)", context => context.Int32().Between(1, 9),
            On<AnyInt32>("OneOf(1, 2)", g => g.OneOf(1, 2)),
            On<AnyInt32>("OneOf(2, 1)", g => g.OneOf(2, 1)),
            On<AnyInt32>("OneOf(3)", g => g.OneOf(3))),
        new("Any.Double().Between(0, 3)", context => context.Double().Between(0, 3),
            On<AnyDouble>("OneOf(1)", g => g.OneOf(1)),
            On<AnyDouble>("OneOf(1.0)", g => g.OneOf(1.0)),
            On<AnyDouble>("OneOf(2.5)", g => g.OneOf(2.5))),
        new("Any.OrderedPair(Any.Int32().Between(0, 10), Any.Int32().Between(0, 10))", context => Any.OrderedPair(context.Int32().Between(0, 10), context.Int32().Between(0, 10)),
            On<AnyOrderedPair<int>>("WithFirst(3)", g => g.WithFirst(3)),
            On<AnyOrderedPair<int>>("WithSecond(8)", g => g.WithSecond(8))),
        new("Any.Decimal().Between(0, 10)", context => context.Decimal().Between(0, 10),
            On<AnyDecimal>("WithScale(1)", g => g.WithScale(1)),
            On<AnyDecimal>("WithScale(2)", g => g.WithScale(2))),
        // A step apart from the scale: a scale its step already lies on changes no draw without repeating anything,
        // and this vocabulary must leave "ignored" meaning "repeated".
        new("Any.Decimal().Between(-10, 10)", context => context.Decimal().Between(-10, 10),
            On<AnyDecimal>("MultipleOf(0.5m)", g => g.MultipleOf(0.5m)),
            On<AnyDecimal>("MultipleOf(0.25m)", g => g.MultipleOf(0.25m))),
        new("Any.Enum<DayOfWeek>()", context => context.Enum<DayOfWeek>(),
            On<AnyEnum<DayOfWeek>>("Except(DayOfWeek.Monday)", g => g.Except(DayOfWeek.Monday)),
            On<AnyEnum<DayOfWeek>>("Except(DayOfWeek.Tuesday)", g => g.Except(DayOfWeek.Tuesday)),
            On<AnyEnum<DayOfWeek>>("Except(DayOfWeek.Friday, DayOfWeek.Saturday)", g => g.Except(DayOfWeek.Friday, DayOfWeek.Saturday)),
            On<AnyEnum<DayOfWeek>>("Except(DayOfWeek.Saturday, DayOfWeek.Friday)", g => g.Except(DayOfWeek.Saturday, DayOfWeek.Friday)),
            On<AnyEnum<DayOfWeek>>("DifferentFrom(DayOfWeek.Sunday)", g => g.DifferentFrom(DayOfWeek.Sunday))),
        new("Any.Enum<AttributeTargets>()", context => context.Enum<AttributeTargets>(),
            On<AnyEnum<AttributeTargets>>("AllowingCombinations()", g => g.AllowingCombinations())),
        new("Any.StringMatching(\"[a-f]\")", context => context.StringMatching("[a-f]"),
            On<AnyPattern>("Except(\"a\")", g => g.Except("a")),
            On<AnyPattern>("Except(\"b\", \"c\")", g => g.Except("b", "c")),
            On<AnyPattern>("Except(\"c\", \"b\")", g => g.Except("c", "b")),
            On<AnyPattern>("DifferentFrom(\"d\")", g => g.DifferentFrom("d"))),
        new("Any.OneOf(1, 2, 3, 4)", context => context.OneOf(1, 2, 3, 4),
            On<AnyOneOf<int>>("Except(1)", g => g.Except(1)),
            On<AnyOneOf<int>>("Except(2)", g => g.Except(2)),
            On<AnyOneOf<int>>("DifferentFrom(3)", g => g.DifferentFrom(3))),
        new("Any.Boolean()", context => context.Boolean(),
            On<AnyBoolean>("True()", g => g.True())),
        new("Any.Boolean()", context => context.Boolean(),
            On<AnyBoolean>("DifferentFrom(true)", g => g.DifferentFrom(true))),
        new("Any.Guid()", context => context.Guid(),
            On<AnyGuid>("Empty()", g => g.Empty())),
        new("Any.DateTime()", context => context.DateTime(),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromHours(1))", g => g.WithGranularity(TimeSpan.FromHours(1))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromMinutes(60))", g => g.WithGranularity(TimeSpan.FromMinutes(60))),
            On<AnyDateTime>("WithGranularity(TimeSpan.FromDays(1))", g => g.WithGranularity(TimeSpan.FromDays(1)))),
        new("Any.DateTimeOffset()", context => context.DateTimeOffset(),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromHours(1))", g => g.WithOffset(TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))", g => g.WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(1))),
            On<AnyDateTimeOffset>("WithOffset(TimeSpan.FromHours(2))", g => g.WithOffset(TimeSpan.FromHours(2)))),
        new("Any.ListOf(Any.Int32().Between(1, 3))", context => Any.ListOf(context.Int32().Between(1, 3)),
            On<AnyList<int>>("WithCount(2)", g => g.WithCount(2)),
            On<AnyList<int>>("WithCount(3)", g => g.WithCount(3)),
            On<AnyList<int>>("Distinct()", g => g.Distinct()),
            On<AnyList<int>>("Ordered()", g => g.Ordered()),
            On<AnyList<int>>("WithCount(-1)", g => g.WithCount(-1))),
        new("Any.ListOf(Any.Int32())", context => Any.ListOf(context.Int32()),
            On<AnyList<int>>("Empty()", g => g.Empty()),
            On<AnyList<int>>("WithCount(0)", g => g.WithCount(0))),
        new("Any.DictionaryOf(Any.Int32(), Any.Int32())", context => Any.DictionaryOf(context.Int32(), context.Int32()),
            On<AnyDictionary<int, int>>("WithCount(1)", g => g.WithCount(1)),
            On<AnyDictionary<int, int>>("WithCount(2)", g => g.WithCount(2))),
        new("Any.Uri().Web()", context => context.Uri().Web(),
            On<AnyWebUri>("UsingHttp()", g => g.UsingHttp()),
            On<AnyWebUri>("UsingHttps()", g => g.UsingHttps()),
            On<AnyWebUri>("WithHost(\"a.example\")", g => g.WithHost("a.example")),
            On<AnyWebUri>("WithPort()", g => g.WithPort()),
            On<AnyWebUri>("WithPort(8080)", g => g.WithPort(8080)),
            On<AnyWebUri>("WithUserInfo(\"a\", \"p\")", g => g.WithUserInfo("a", "p")),
            On<AnyWebUri>("WithUserInfo(password: \"p\", user: \"a\")", g => g.WithUserInfo(password: "p", user: "a")),
            On<AnyWebUri>("WithPathSegments(2)", g => g.WithPathSegments(2)),
            On<AnyWebUri>("WithoutPath()", g => g.WithoutPath()),
            On<AnyWebUri>("WithQuery()", g => g.WithQuery()),
            On<AnyWebUri>("WithFragment()", g => g.WithFragment())),
        new("Any.Uri().WebSocket()", context => context.Uri().WebSocket(),
            On<AnyWebSocketUri>("UsingWs()", g => g.UsingWs()),
            On<AnyWebSocketUri>("UsingWss()", g => g.UsingWss()),
            On<AnyWebSocketUri>("WithQuery()", g => g.WithQuery())),
        new("Any.Uri().Mailto()", context => context.Uri().Mailto(),
            On<AnyMailtoUri>("WithDomain(\"a.example\")", g => g.WithDomain("a.example")),
            On<AnyMailtoUri>("WithLocalPart(\"a\")", g => g.WithLocalPart("a")),
            On<AnyMailtoUri>("WithLocalPart(\"b\")", g => g.WithLocalPart("b")),
            On<AnyMailtoUri>("WithHeaders()", g => g.WithHeaders())),
        new("Any.Uri().Relative()", context => context.Uri().Relative(),
            On<AnyRelativeUri>("Rooted()", g => g.Rooted()),
            On<AnyRelativeUri>("WithQuery()", g => g.WithQuery()),
            On<AnyRelativeUri>("WithPathSegments(1)", g => g.WithPathSegments(1)),
            On<AnyRelativeUri>("WithPathSegments(3)", g => g.WithPathSegments(3)))
    ];

    #region Statics members declarations

    /// <summary>The chains, with the shrinker that drops one call at a time.</summary>
    public static Arbitrary<RepetitionChain> Arbitrary() {
        Gen<RepetitionChain> chains = from vocabulary in Gen.Elements(Vocabularies)
                                      from length in Gen.Choose(1, MaximumLength)
                                      from calls in Gen.Elements(vocabulary.Calls.ToArray()).ListOf(length)
                                      select new RepetitionChain(vocabulary, calls.ToList());

        return Arb.From(chains, chain => chain.Shrink());
    }

    #endregion

}
