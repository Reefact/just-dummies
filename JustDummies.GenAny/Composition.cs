using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     How a type the base table has no row for is still drawn: through a generator already scaffolded for it,
///     or through its own static factory (§5.4).
/// </summary>
/// <remarks>
///     Convention, not attribute and not configuration. An attribute would mean touching the developer's
///     production code to please a test tool; a configuration file would move the decision away from the call
///     site it describes.
/// </remarks>
internal static class Composition {

    /// <summary>The factory names a one-parameter conversion is recognised by, in order of preference.</summary>
    private static readonly string[] Recognised = ["Create", "From", "Of", "Parse"];

    /// <summary>
    ///     The generator already scaffolded for <paramref name="type" />, if the compilation has one.
    /// </summary>
    /// <remarks>
    ///     A scaffolded generator <b>wins</b> over a factory: it is the developer's own answer to the question,
    ///     and it is how aggregates compose in cascade — scaffold <c>Customer</c>, re-run <c>--force</c> on
    ///     <c>Order</c>, and the open parameter closes. It works whether that type was scaffolded earlier or
    ///     written by hand.
    ///     <para>
    ///         Looked up in the type's own namespace first, because that is where ADR-0062 puts it, then among
    ///         the compilation's source types. Not by walking every referenced assembly: a generator a
    ///         developer wrote is in their solution, and sweeping the whole framework for a name would cost
    ///         every parameter of every run.
    ///     </para>
    /// </remarks>
    internal static INamedTypeSymbol? ScaffoldedFor(INamedTypeSymbol type, Compilation compilation, NamingOptions naming) {
        string     name      = TypeNaming.GeneratorNameFor(type, naming);
        string     @namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string     qualified = @namespace.Length == 0 ? name : @namespace + "." + name;

        INamedTypeSymbol? beside = compilation.GetTypeByMetadataName(qualified);

        if (Qualifies(beside, type)) { return beside; }

        return compilation.GetSymbolsWithName(candidate => candidate == name, SymbolFilter.Type)
                          .OfType<INamedTypeSymbol>()
                          .FirstOrDefault(candidate => Qualifies(candidate, type));
    }

    /// <summary>
    ///     The static factory a value of <paramref name="type" /> is built through, if exactly one qualifies.
    /// </summary>
    /// <remarks>
    ///     A method qualifies when it is <c>public static</c>, returns the type, takes exactly one parameter,
    ///     and is named <c>Create</c>, <c>From</c>, <c>Of</c> or <c>Parse</c>. <c>Create</c> wins where several
    ///     do; where several still remain the parameter is left unresolved rather than guessed at.
    /// </remarks>
    internal static IMethodSymbol? FactoryFor(INamedTypeSymbol type) {
        IMethodSymbol[] qualifying = type.GetMembers()
                                         .OfType<IMethodSymbol>()
                                         .Where(method => method.IsStatic
                                                       && method.DeclaredAccessibility == Accessibility.Public
                                                       && method.Parameters.Length == 1
                                                       && Recognised.Contains(method.Name, StringComparer.Ordinal)
                                                       && SymbolEqualityComparer.Default.Equals(method.ReturnType, type))
                                         .ToArray();

        if (qualifying.Length <= 1) { return qualifying.FirstOrDefault(); }

        IMethodSymbol[] preferred = qualifying.Where(method => method.Name == Recognised[0]).ToArray();

        return preferred.Length == 1 ? preferred[0] : null;
    }

    private static bool Qualifies(INamedTypeSymbol? candidate, INamedTypeSymbol type) {
        return candidate is { IsAbstract: false, IsStatic: false, DeclaredAccessibility: Accessibility.Public }
            && candidate.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public
                                                              && constructor.Parameters.Length == 0)
            && candidate.AllInterfaces.Any(@interface => @interface.MetadataName == "IAny`1"
                                                      && @interface.ContainingNamespace?.ToDisplayString() == "JustDummies"
                                                      && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], type));
    }

}
