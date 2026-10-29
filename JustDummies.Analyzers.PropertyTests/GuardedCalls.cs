using System.Globalization;
using System.Reflection;

using FsCheck;
using FsCheck.Fluent;

using Microsoft.CodeAnalysis.CSharp;

using static JustDummies.Analyzers.PropertyTests.DeclarationCall;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     The FsCheck generator of <see cref="GuardedCall" />s: one call per case on a guard <c>JD014</c> reads beyond a
///     range or a non-empty check — a host or a domain, a user-info part or a local part, an offset or a range of
///     offsets, a granularity, a floating-point value set — or on the enumeration value set <c>JD017</c> reads, its
///     argument drawn so that both outcomes stay frequent.
/// </summary>
/// <remarks>
///     <para>
///         Text is drawn from small alphabets mixing what each guard accepts with what it refuses: a host from letters,
///         digits, dots, a hyphen, a space, a colon and a non-ASCII letter, or from digits and dots alone so a shorthand
///         IPv4 literal is frequent; a user-info part from the unreserved characters, a few reserved ones, a non-ASCII
///         letter and <c>İ</c>. Offsets fall either side of ±14:00, half a minute off now and then. Each one is spelled
///         <c>new TimeSpan(0, minutes, seconds)</c>, whose integral arguments the analyzer reads exactly (ADR-0046).
///     </para>
///     <para>
///         The enumerations come from the base library, the only assembly the snippet references besides JustDummies:
///         <c>DayOfWeek</c>, a plain one; <c>BindingFlags</c>, a <c>[Flags]</c> one with a zero member and a gap at 128;
///         <c>AttributeTargets</c>, a <c>[Flags]</c> one with no zero member.
///     </para>
/// </remarks>
internal static class GuardedCalls {

    private const string HostCharacters     = "ab09.-ü :";
    private const string AddressCharacters  = "0129.";
    private const string UserInfoCharacters = "aZ0-._~ @:é+İ";
    private const char   AmbiguousCase      = 'İ';
    private const char   HighestAsciiChar   = '\u007f';
    private const int    MaximumTextLength  = 6;

    /// <summary>Fifteen hours, in minutes: offsets fall either side of the ±14:00 the guard allows.</summary>
    private const int OffsetMinutes = 15 * 60;

    private static readonly DeclarationVocabulary Web            = new("Any.Uri().Web()", context => context.Uri().Web());
    private static readonly DeclarationVocabulary Ftp            = new("Any.Uri().Ftp()", context => context.Uri().Ftp());
    private static readonly DeclarationVocabulary Mailto         = new("Any.Uri().Mailto()", context => context.Uri().Mailto());
    private static readonly DeclarationVocabulary DateTimeOffset = new("Any.DateTimeOffset()", context => context.DateTimeOffset());
    private static readonly DeclarationVocabulary DateTime       = new("Any.DateTime()", context => context.DateTime());
    private static readonly DeclarationVocabulary TimeSpans      = new("Any.TimeSpan()", context => context.TimeSpan());
    private static readonly DeclarationVocabulary Doubles        = new("Any.Double()", context => context.Double());
    private static readonly DeclarationVocabulary Singles        = new("Any.Single()", context => context.Single());
    private static readonly DeclarationVocabulary Days           = new("Any.Enum<DayOfWeek>()", context => context.Enum<DayOfWeek>());
    private static readonly DeclarationVocabulary Bindings       = new("Any.Enum<System.Reflection.BindingFlags>()", context => context.Enum<BindingFlags>());
    private static readonly DeclarationVocabulary Targets        = new("Any.Enum<AttributeTargets>()", context => context.Enum<AttributeTargets>());

    private static readonly (string Source, double Value)[] DoubleElements = [
        ("1.0", 1.0), ("2.5", 2.5), ("double.NaN", double.NaN), ("double.PositiveInfinity", double.PositiveInfinity), ("double.NegativeInfinity", double.NegativeInfinity)
    ];

    private static readonly (string Source, float Value)[] SingleElements = [
        ("1f", 1f), ("2.5f", 2.5f), ("float.NaN", float.NaN), ("float.PositiveInfinity", float.PositiveInfinity), ("float.NegativeInfinity", float.NegativeInfinity)
    ];

    #region Statics members declarations

    /// <summary>The calls. One call is already minimal, so nothing shrinks.</summary>
    public static Arbitrary<GuardedCall> Arbitrary() {
        return Arb.From(Gen.OneOf(HostCalls(), UserInfoCalls(), OffsetCalls(), GranularityCalls(), FloatingPointCalls(), EnumerationCalls()));
    }

    private static Gen<GuardedCall> HostCalls() {
        return from host in Gen.OneOf(Text(HostCharacters), Text(AddressCharacters))
               from call in Gen.Elements(new GuardedCall(Web, On<AnyWebUri>($"WithHost({Literal(host)})", g => g.WithHost(host)), IsClaimedHost(host)),
                                         new GuardedCall(Ftp, On<AnyFtpUri>($"WithHost({Literal(host)})", g => g.WithHost(host)), IsClaimedHost(host)),
                                         new GuardedCall(Mailto, On<AnyMailtoUri>($"WithDomain({Literal(host)})", g => g.WithDomain(host)), IsClaimedHost(host)))
               select call;
    }

    private static Gen<GuardedCall> UserInfoCalls() {
        return from user in Text(UserInfoCharacters)
               from password in Text(UserInfoCharacters)
               from call in Gen.Elements(new GuardedCall(Web, On<AnyWebUri>($"WithUserInfo({Literal(user)})", g => g.WithUserInfo(user)), IsClaimedUserInfo(user)),
                                         new GuardedCall(Web, On<AnyWebUri>($"WithUserInfo({Literal(user)}, {Literal(password)})", g => g.WithUserInfo(user, password)), IsClaimedUserInfo(user) && IsClaimedUserInfo(password)),
                                         new GuardedCall(Mailto, On<AnyMailtoUri>($"WithLocalPart({Literal(user)})", g => g.WithLocalPart(user)), IsClaimedUserInfo(user)))
               select call;
    }

    private static Gen<GuardedCall> OffsetCalls() {
        return from minimum in Offsets()
               from maximum in Offsets()
               from call in Gen.Elements(new GuardedCall(DateTimeOffset, On<AnyDateTimeOffset>($"WithOffset({minimum.Source})", g => g.WithOffset(minimum.Value)), claimed: true),
                                         new GuardedCall(DateTimeOffset, On<AnyDateTimeOffset>($"WithOffsetBetween({minimum.Source}, {maximum.Source})", g => g.WithOffsetBetween(minimum.Value, maximum.Value)), claimed: true))
               select call;
    }

    private static Gen<(string Source, TimeSpan Value)> Offsets() {
        return from minutes in Gen.Choose(-OffsetMinutes, OffsetMinutes)
               from seconds in Gen.Elements(0, 0, 0, 30)
               select ($"new TimeSpan(0, {minutes.ToString(CultureInfo.InvariantCulture)}, {seconds.ToString(CultureInfo.InvariantCulture)})", new TimeSpan(0, minutes, seconds));
    }

    private static Gen<GuardedCall> GranularityCalls() {
        return from ticks in Gen.Choose(-2, 2)
               let written = $"new TimeSpan({ticks.ToString(CultureInfo.InvariantCulture)})"
               from call in Gen.Elements(new GuardedCall(DateTime, On<AnyDateTime>($"WithGranularity({written})", g => g.WithGranularity(new TimeSpan(ticks))), claimed: true),
                                         new GuardedCall(TimeSpans, On<AnyTimeSpan>($"WithGranularity(TimeSpan.FromTicks({ticks.ToString(CultureInfo.InvariantCulture)}))", g => g.WithGranularity(TimeSpan.FromTicks(ticks))), claimed: true))
               select call;
    }

    private static Gen<GuardedCall> FloatingPointCalls() {
        return from length in Gen.Choose(1, 2)
               from doubles in Gen.Elements(DoubleElements).ListOf(length)
               from singles in Gen.Elements(SingleElements).ListOf(length)
               from inline in Gen.Elements(true, false)
               from call in Gen.Elements(new GuardedCall(Doubles, On<AnyDouble>($"OneOf({ValueSet(doubles.Select(element => element.Source), inline)})", g => g.OneOf(doubles.Select(element => element.Value).ToArray())), claimed: true),
                                         new GuardedCall(Singles, On<AnySingle>($"OneOf({ValueSet(singles.Select(element => element.Source), inline)})", g => g.OneOf(singles.Select(element => element.Value).ToArray())), claimed: true))
               select call;
    }

    private static Gen<GuardedCall> EnumerationCalls() {
        return from length in Gen.Choose(1, 2)
               from days in Gen.Choose(-2, 9).ListOf(length)
               from bindings in Gen.Choose(-2, 300).ListOf(length)
               from targets in Gen.OneOf(Gen.Choose(-2, 40), Gen.Elements(32767, 32768)).ListOf(length)
               from call in Gen.Elements(new GuardedCall(Days, On<AnyEnum<DayOfWeek>>($"OneOf({EnumValues("DayOfWeek", days)})", g => g.OneOf(days.Select(value => (DayOfWeek)value).ToArray())), claimed: true, rule: "JD017"),
                                         new GuardedCall(Bindings, On<AnyEnum<BindingFlags>>($"OneOf({EnumValues("System.Reflection.BindingFlags", bindings)})", g => g.OneOf(bindings.Select(value => (BindingFlags)value).ToArray())), claimed: true, rule: "JD017"),
                                         new GuardedCall(Targets, On<AnyEnum<AttributeTargets>>($"OneOf({EnumValues("AttributeTargets", targets)})", g => g.OneOf(targets.Select(value => (AttributeTargets)value).ToArray())), claimed: true, rule: "JD017"))
               select call;
    }

    private static Gen<string> Text(string alphabet) {
        return from length in Gen.Choose(0, MaximumTextLength)
               from characters in Gen.Elements(alphabet.ToCharArray()).ListOf(length)
               select string.Concat(characters);
    }

    /// <summary>The host guard checks these before <c>System.Uri</c> reads the host: empty, not ASCII, digits and dots.</summary>
    private static bool IsClaimedHost(string host) {
        return host.Length == 0 || host.Any(character => character > HighestAsciiChar) || host.All(character => character is >= '0' and <= '9' or '.');
    }

    private static bool IsClaimedUserInfo(string part) {
        return !part.Contains(AmbiguousCase, StringComparison.Ordinal);
    }

    private static string Literal(string text) {
        return SymbolDisplay.FormatLiteral(text, quote: true);
    }

    private static string ValueSet(IEnumerable<string> elements, bool asCollectionExpression) {
        string listed = string.Join(", ", elements);

        return asCollectionExpression ? $"[{listed}]" : listed;
    }

    private static string EnumValues(string enumeration, IEnumerable<int> values) {
        return string.Join(", ", values.Select(value => $"({enumeration})({value.ToString(CultureInfo.InvariantCulture)})"));
    }

    #endregion

}
