using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     The library's argument guards that need more than a range or a non-empty check to read: an offset, a URI's user
///     info, local part, host or domain, and a value set element. Each guard runs before its call reads anything the
///     chain declared.
/// </summary>
/// <remarks>
///     <para>
///         Two kinds of answer live here. The <c>TryFind…</c> readers replicate a guard exactly — the same characters,
///         the same arithmetic — so JD014 reports what they find as an argument the call certainly rejects.
///         <see cref="MayRefuse" /> answers for what cannot be replicated: a <c>null</c>, which JD014 leaves to nullable
///         analysis; a host or a domain that is not a plain DNS name, which the run time checks through
///         <c>System.Uri</c>, whose parsing differs across target frameworks; and a user-info character whose invariant
///         lower case differs across runtimes. JD034 and JD035 stop their walk at either kind, since a call that may
///         throw on its argument may apply nothing after it.
///     </para>
///     <para>
///         Only a plain DNS name — dot-separated labels of ASCII letters, digits and inner hyphens, with a letter
///         somewhere — is vouched for. A rule that stops early can only under-report, which is the right direction for
///         one claiming that a call certainly throws.
///     </para>
/// </remarks>
internal static class ArgumentGuardFacts {

    /// <summary>The characters a user-info part or a local part may hold, compared case-insensitively.</summary>
    private const string Unreserved = "abcdefghijklmnopqrstuvwxyz0123456789-._~";

    /// <summary>
    ///     The two non-ASCII characters whose invariant lower case is an ASCII letter — <c>İ</c> to <c>i</c> and the
    ///     Kelvin sign to <c>k</c> — under some runtimes and globalization modes but not all, so a user-info part holding
    ///     one is accepted or refused depending on where the test runs.
    /// </summary>
    private const string AmbiguousCase = "İK";

    private const int  MaxLabelLength   = 63;
    private const int  MaxHostLength    = 253;
    private const int  Ipv4OctetCount   = 4;
    private const int  MaxOctetDigits   = 3;
    private const int  MaxOctetValue    = 255;
    private const char HighestAsciiChar = '\u007f';
    private const long OffsetLimitTicks = 14 * TimeSpan.TicksPerHour;

    #region Statics members declarations

    /// <summary>
    ///     The first user-info part or local part holding a character outside the unreserved ones, which the call rejects
    ///     whatever the chain declared.
    /// </summary>
    internal static bool TryFindRejectedUserInfo(IInvocationOperation call, out IOperation? offending, out string? reason) {
        return TryFindRejectedText(call, UserInfoRejection, out offending, out reason);
    }

    /// <summary>
    ///     The first host or domain the call certainly rejects: an empty one, one that is not ASCII, or one of digits and
    ///     dots that is not a canonical dotted-quad. Anything else <c>System.Uri</c> judges, which is not replicated.
    /// </summary>
    internal static bool TryFindRejectedHost(IInvocationOperation call, out IOperation? offending, out string? reason) {
        return TryFindRejectedText(call, HostRejection, out offending, out reason);
    }

    /// <summary>
    ///     The first offset the call rejects — not a whole number of minutes, or beyond ±14:00 — in the order the guard
    ///     checks them, then, for a range, a minimum after its maximum.
    /// </summary>
    internal static bool TryFindRejectedOffset(IInvocationOperation call, out IOperation? offending, out string? reason) {
        IArgumentOperation[] arguments = call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal ?? int.MaxValue).ToArray();
        long?[]              offsets   = arguments.Select(argument => ConstantFacts.TryGetTimeSpanTicks(argument.Value, out long ticks) ? ticks : (long?)null).ToArray();

        for (int index = 0; index < arguments.Length; index++) {
            if (offsets[index] is not long offset || OffsetRejection(offset) is not string rejection) { continue; }

            offending = arguments[index].Value;
            reason    = rejection;

            return true;
        }

        if (offsets.Length == 2 && offsets[0] is long minimum && offsets[1] is long maximum && minimum > maximum) {
            offending = arguments[0].Value;
            reason    = "the minimum offset must be at or before the maximum — the two arguments look transposed";

            return true;
        }

        offending = null;
        reason    = null;

        return false;
    }

    /// <summary>
    ///     The first value set element a floating-point or an enumeration generator rejects: a value that is not finite,
    ///     or one that is neither a declared member nor, on a <c>[Flags]</c> enumeration, a combination of declared
    ///     members. <c>Any.OneOf(...)</c> carries no such rule, by construction.
    /// </summary>
    internal static bool TryFindRejectedElement(IInvocationOperation call, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        if (call.TargetMethod.ContainingType.Name is not ("AnyDouble" or "AnySingle" or "AnyHalf" or "AnyEnum")) { return false; }

        return TryFindFirstRejected(call, out offending, out reason);
    }

    /// <summary>
    ///     The first bound a floating-point generator rejects for not being finite: an argument of <c>Between</c>,
    ///     <c>GreaterThan(OrEqualTo)</c> or <c>LessThan(OrEqualTo)</c> on <c>Any.Double()</c>, <c>Any.Single()</c> or
    ///     <c>Any.Half()</c>, checked in parameter order as the guard checks them.
    /// </summary>
    internal static bool TryFindNonFiniteBound(IInvocationOperation call, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        if (!IsFloatingPointGenerator(call)) { return false; }

        offending = call.Arguments.Select(argument => argument.Value).FirstOrDefault(IsNonFinite);
        if (offending is null) { return false; }

        reason = "it must be finite: NaN and the infinities are never generated, and not accepted as a bound";

        return true;
    }

    /// <summary>
    ///     The first element of an enumeration value set the type does not define: neither a declared member nor, on a
    ///     <c>[Flags]</c> enumeration, a combination of declared members. A subset of <see cref="TryFindRejectedElement" />,
    ///     kept apart because JD017 reports it and JD014 does not (issue #321).
    /// </summary>
    internal static bool TryFindUndefinedEnumElement(IInvocationOperation call, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        if (call.TargetMethod.Name != "OneOf" || call.TargetMethod.ContainingType.Name != "AnyEnum") { return false; }

        return TryFindFirstRejected(call, out offending, out reason);
    }

    private static bool TryFindFirstRejected(IInvocationOperation call, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IOperation element in ValueSetFacts.Elements(call)) {
            if (ElementRejection(element) is not string rejection) { continue; }

            offending = element;
            reason    = rejection;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Whether the call's guard may refuse one of its constant arguments where no reader here can say so for certain:
    ///     a <c>null</c> the caller wrote, a <c>null</c> in a value set, a host or a domain that is not a plain DNS name, a
    ///     user-info character whose case mapping depends on the runtime.
    /// </summary>
    internal static bool MayRefuse(IInvocationOperation call) {
        if (call.Arguments.Any(IsNullConstant)) { return true; }

        return call.TargetMethod.Name switch {
            "WithUserInfo" or "WithLocalPart" => ConstantStrings(call).Any(part => part.Any(character => AmbiguousCase.IndexOf(character) >= 0)),
            "WithHost" or "WithDomain"        => ConstantStrings(call).Any(host => !IsPlainHostName(host)),
            "OneOf"                           => ValueSetFacts.IsWrittenInline(call) && ValueSetFacts.Elements(call).Any(element => element.ConstantValue is { HasValue: true, Value: null }),
            _                                 => false
        };
    }

    /// <summary>A <c>null</c> the caller wrote, as opposed to an optional parameter left at its default.</summary>
    private static bool IsNullConstant(IArgumentOperation argument) {
        return argument.ArgumentKind != ArgumentKind.DefaultValue && argument.Value.ConstantValue is { HasValue: true, Value: null };
    }

    private static bool TryFindRejectedText(IInvocationOperation call, Func<string, string?> rejectionOf, out IOperation? offending, out string? reason) {
        foreach (IOperation value in call.Arguments.Select(argument => argument.Value)) {
            if (value.ConstantValue is not { HasValue: true, Value: string text } || rejectionOf(text) is not string rejection) { continue; }

            offending = value;
            reason    = rejection;

            return true;
        }

        offending = null;
        reason    = null;

        return false;
    }

    private static IEnumerable<string> ConstantStrings(IInvocationOperation call) {
        return call.Arguments.Select(argument => argument.Value.ConstantValue).Where(constant => constant.HasValue).Select(constant => constant.Value).OfType<string>();
    }

    /// <summary>
    ///     Why the user-info guard rejects <paramref name="part" /> wherever it runs, or <c>null</c>: a character outside
    ///     the unreserved ones once lower-cased, and not one of the two that lower-case to ASCII under some runtimes only.
    /// </summary>
    private static string? UserInfoRejection(string part) {
        return part.Where(character => AmbiguousCase.IndexOf(character) < 0 && Unreserved.IndexOf(char.ToLowerInvariant(character)) < 0).Select(character => (char?)character).FirstOrDefault() is char rejected
                   ? $"it may only contain unreserved characters (letters, digits, '-', '.', '_', '~'), and '{rejected}' is not one"
                   : null;
    }

    /// <summary>Why the host guard rejects <paramref name="host" /> whatever the target framework, or <c>null</c>.</summary>
    private static string? HostRejection(string host) {
        if (host.Length == 0) { return "it must not be empty"; }
        if (host.Any(character => character > HighestAsciiChar)) { return "it must be ASCII: pass the punycode form of an internationalized host"; }
        if (host.All(character => character is >= '0' and <= '9' or '.') && !IsCanonicalIpv4(host)) {
            return "a host of digits and dots is read as an IPv4 literal, which must be a canonical dotted-quad such as \"1.2.3.4\"";
        }

        return null;
    }

    private static bool IsCanonicalIpv4(string host) {
        string[] parts = host.Split('.');

        return parts.Length == Ipv4OctetCount
            && parts.All(part => part.Length is > 0 and <= MaxOctetDigits
                              && (part.Length == 1 || part[0] != '0')
                              && int.Parse(part, NumberStyles.None, CultureInfo.InvariantCulture) <= MaxOctetValue);
    }

    private static bool IsPlainHostName(string host) {
        return host.Length is > 0 and <= MaxHostLength && host.Any(IsAsciiLetter) && host.Split('.').All(IsPlainLabel);
    }

    private static bool IsPlainLabel(string label) {
        return label.Length is > 0 and <= MaxLabelLength
            && label[0] != '-' && label[label.Length - 1] != '-'
            && label.All(character => IsAsciiLetter(character) || character is >= '0' and <= '9' || character == '-');
    }

    private static bool IsAsciiLetter(char character) {
        return character is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    /// <summary>Why the offset guard rejects <paramref name="ticks" />, in the order it checks, or <c>null</c>.</summary>
    private static string? OffsetRejection(long ticks) {
        if (ticks % TimeSpan.TicksPerMinute != 0) { return "it must be a whole number of minutes"; }
        if (ticks is < -OffsetLimitTicks or > OffsetLimitTicks) { return "it must be within ±14:00"; }

        return null;
    }

    /// <summary>Why a value set guard rejects an element, or <c>null</c> where it accepts it or no reading is certain.</summary>
    private static string? ElementRejection(IOperation element) {
        if (IsNonFinite(element)) { return "it must be finite: NaN and the infinities are never generated"; }

        return element is { Type: INamedTypeSymbol { TypeKind: TypeKind.Enum } enumeration, ConstantValue: { HasValue: true, Value: { } value } }
                   ? EnumValueRejection(enumeration, value)
                   : null;
    }

    private static bool IsFloatingPointGenerator(IInvocationOperation call) {
        return call.TargetMethod.ContainingType.Name is "AnyDouble" or "AnySingle" or "AnyHalf";
    }

    /// <summary>
    ///     Whether <paramref name="operation" /> certainly evaluates to NaN or an infinity: a <c>double</c> or a
    ///     <c>float</c> constant that is one, the <c>Half</c> member naming one, or either converted to another
    ///     floating-point type, which keeps it non-finite.
    /// </summary>
    /// <remarks>
    ///     <c>Half</c> has no C# constants, so its <c>NaN</c> and infinities are read as the static members they are. A finite
    ///     constant that overflows on its way to a <c>Half</c>, <c>(Half)70000.0</c>, is not read: the conversion is
    ///     the run time's, and this assembly, built for netstandard2.0, cannot perform it to be certain.
    /// </remarks>
    private static bool IsNonFinite(IOperation operation) {
        IOperation value = operation;
        while (value is IConversionOperation conversion) { value = conversion.Operand; }

        return value.ConstantValue.HasValue
                   ? value.ConstantValue.Value switch {
                       double number => double.IsNaN(number) || double.IsInfinity(number),
                       float number  => float.IsNaN(number) || float.IsInfinity(number),
                       _             => false
                   }
                   : value is IMemberReferenceOperation { Member: { Name: "NaN" or "PositiveInfinity" or "NegativeInfinity" } member } && IsSystemHalf(member.ContainingType);
    }

    private static bool IsSystemHalf(INamedTypeSymbol type) {
        return type is { Name: "Half", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
    }

    /// <summary>
    ///     Why the enumeration rejects the value in a value set, or <c>null</c>: it accepts a declared member, and on a
    ///     <c>[Flags]</c> enumeration the OR of the declared members whose bits the value carries.
    /// </summary>
    private static string? EnumValueRejection(INamedTypeSymbol enumeration, object value) {
        object[] declared = enumeration.GetMembers().OfType<IFieldSymbol>().Where(member => member.HasConstantValue).Select(member => member.ConstantValue!).ToArray();
        string   written  = Convert.ToString(value, CultureInfo.InvariantCulture);
        if (declared.Contains(value)) { return null; }
        if (!enumeration.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "System.FlagsAttribute")) { return $"{written} is not a declared member of {enumeration.Name}"; }

        return IsCombinationOfDeclaredMembers(declared.Select(Bits).ToArray(), Bits(value)) ? null : $"{written} is neither a declared member of {enumeration.Name} nor a combination of its declared members";
    }

    /// <summary>Whether OR-ing the declared members whose bits the value carries gives the value itself, as the run time decides it.</summary>
    private static bool IsCombinationOfDeclaredMembers(ulong[] declared, ulong bits) {
        // The empty combination is a value only where the enumeration declares it.
        if (bits == 0UL) { return declared.Contains(0UL); }

        return declared.Where(member => member != 0UL && (member & bits) == member).Aggregate(0UL, (union, member) => union | member) == bits;
    }

    /// <summary>
    ///     An enumeration value's bits as the run time reads them: a signed value widened to 64 bits with its sign, so
    ///     <c>-1</c> carries every bit.
    /// </summary>
    private static ulong Bits(object value) {
        return value is ulong unsigned ? unsigned : unchecked((ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture));
    }

    #endregion

}
