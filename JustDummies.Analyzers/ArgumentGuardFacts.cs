using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     The argument guards of the once-per-generator calls that JD014's table does not hold: a constant <c>null</c>, a
///     URI's user info or local part outside the unreserved characters, a host or a domain, and a value set element the
///     generator refuses. Each guard runs before the call reads anything the chain declared, so a call it refuses ends
///     the chain there.
/// </summary>
/// <remarks>
///     A host is checked at run time by <c>System.Uri</c>, whose parsing differs across target frameworks and is not the
///     analyzer's to replicate. Only a plain DNS name — dot-separated labels of ASCII letters, digits and inner hyphens,
///     with a letter somewhere — is vouched for; anything else answers that the guard may refuse it. A rule that stops
///     early can only under-report, which is the right direction for one claiming that a call certainly throws.
/// </remarks>
internal static class ArgumentGuardFacts {

    /// <summary>The characters a user-info part or a local part may hold, compared case-insensitively.</summary>
    private const string Unreserved = "abcdefghijklmnopqrstuvwxyz0123456789-._~";

    private const int MaxLabelLength = 63;
    private const int MaxHostLength  = 253;

    #region Statics members declarations

    /// <summary>Whether the call's guard refuses, or may refuse, one of its constant arguments.</summary>
    internal static bool MayRefuse(IInvocationOperation call) {
        if (call.Arguments.Any(IsNullConstant)) { return true; }

        return call.TargetMethod.Name switch {
            "WithUserInfo" or "WithLocalPart" => ConstantStrings(call).Any(part => !part.All(IsUnreserved)),
            "WithHost" or "WithDomain"        => ConstantStrings(call).Any(host => !IsPlainHostName(host)),
            "OneOf"                           => ValueSetFacts.IsWrittenInline(call) && ValueSetFacts.Elements(call).Any(IsRefusedElement),
            _                                 => false
        };
    }

    /// <summary>A <c>null</c> the caller wrote, as opposed to an optional parameter left at its default.</summary>
    private static bool IsNullConstant(IArgumentOperation argument) {
        return argument.ArgumentKind != ArgumentKind.DefaultValue && argument.Value.ConstantValue is { HasValue: true, Value: null };
    }

    private static IEnumerable<string> ConstantStrings(IInvocationOperation call) {
        return call.Arguments.Select(argument => argument.Value.ConstantValue).Where(constant => constant.HasValue).Select(constant => constant.Value).OfType<string>();
    }

    private static bool IsUnreserved(char character) {
        return Unreserved.IndexOf(char.ToLowerInvariant(character)) >= 0;
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

    /// <summary>
    ///     A value set element the generator refuses: a <c>null</c>, a floating-point value that is not finite, or an
    ///     enumeration value that is not one of its declared members — which a <c>[Flags]</c> combination may still be,
    ///     so that case only answers that the guard may refuse it.
    /// </summary>
    private static bool IsRefusedElement(IOperation element) {
        if (element.ConstantValue is not { HasValue: true } constant) { return false; }

        return constant.Value switch {
            null         => true,
            double value => double.IsNaN(value) || double.IsInfinity(value),
            float value  => float.IsNaN(value) || float.IsInfinity(value),
            _            => element.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumeration && !IsDeclaredMember(enumeration, constant.Value)
        };
    }

    private static bool IsDeclaredMember(INamedTypeSymbol enumeration, object value) {
        return enumeration.GetMembers().OfType<IFieldSymbol>().Any(member => member.HasConstantValue && value.Equals(member.ConstantValue));
    }

    #endregion

}
