using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenDummy;

/// <summary>The static factory a type is built through where it declares no constructor to call (§5.1.2).</summary>
/// <remarks>
///     Convention, not attribute and not configuration. An attribute would mean touching the developer's
///     production code to please a test tool; a configuration file would move the decision away from the call
///     site it describes.
/// </remarks>
internal static class Composition {

    /// <summary>The factory names a one-parameter conversion is recognised by, in order of preference.</summary>
    private static readonly string[] Recognised = ["Create", "From", "Of", "Parse"];

    /// <summary>
    ///     The static factory a value of <paramref name="type" /> is built through, if exactly one qualifies.
    /// </summary>
    /// <remarks>
    ///     A method qualifies when it is <c>public static</c>, returns the type, takes exactly one parameter,
    ///     and is named <c>Create</c>, <c>From</c>, <c>Of</c> or <c>Parse</c>. <c>Create</c> wins where several
    ///     do — so where any <c>Create</c> qualifies, what comes back is the <c>Create</c> overloads alone —
    ///     and where several still remain the target is refused naming them, rather than one being guessed at.
    ///     §5.1.2 states that rule; §5.4 carried it until ADR-0089 moved composition off it.
    /// </remarks>
    internal static IReadOnlyList<IMethodSymbol> FactoriesFor(INamedTypeSymbol type) {
        IMethodSymbol[] qualifying = type.GetMembers()
                                         .OfType<IMethodSymbol>()
                                         .Where(method => method.IsStatic
                                                       && method.DeclaredAccessibility == Accessibility.Public
                                                       && method.Parameters.Length == 1
                                                       && Recognised.Contains(method.Name, StringComparer.Ordinal)
                                                       && SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                                         .ToArray();

        if (qualifying.Length <= 1) { return qualifying; }

        IMethodSymbol[] preferred = qualifying.Where(method => method.Name == Recognised[0]).ToArray();

        // `Create` wins whenever there is one at all — the preference is a rule, so a name ranked below it
        // is not part of a tie it could not settle. Where several `Create` overloads remain, those are the
        // tie, and the caller refuses the target naming them rather than picking one on the developer's
        // behalf (§5.1.2). Returning every qualifying name there was invisible while the caller only asked
        // whether the set held exactly one; it stopped being invisible when the set became what the refusal
        // prints.
        return preferred.Length > 0 ? preferred : qualifying;
    }

}
