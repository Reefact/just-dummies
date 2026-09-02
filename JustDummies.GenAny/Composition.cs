using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     Two conventions read by name rather than declared: the generator a type owns, through which a parameter
///     the base table has no row for is drawn (§5.4), and the static factory a type is built through where it
///     declares no constructor to call (§5.1.2).
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
    ///     Every type named <c>Any{T}</c> the compilation carries for <paramref name="type" />, split into what
    ///     the engine may use and what it may not.
    /// </summary>
    /// <remarks>
    ///     A scaffolded generator <b>wins</b> over a factory: it is the developer's own answer to the question,
    ///     and it is how aggregates compose in cascade — scaffold <c>Customer</c>, re-run <c>--force</c> on
    ///     <c>Order</c>, and the open parameter closes. It works whether that type was scaffolded earlier or
    ///     written by hand — but only for a type that could actually serve as one: public, instantiable through
    ///     a public parameterless constructor, and an <c>IAny&lt;T&gt;</c> for this exact <c>T</c>. A same-named
    ///     type that fails one of those tests is not a candidate (<see cref="Qualifies" />), and treating it as
    ///     one anyway would collide with a real declaration and blame the wrong file when the developer's build
    ///     fails.
    ///     <para>
    ///         Looked up in the type's own namespace first, because that is where ADR-0062 puts it, then among
    ///         the compilation's source types. Not by walking every referenced assembly: a generator a
    ///         developer wrote is in their solution, and sweeping the whole framework for a name would cost
    ///         every parameter of every run.
    ///     </para>
    /// </remarks>
    internal static GeneratorCandidates CandidatesFor(INamedTypeSymbol type, Compilation compilation, NamingOptions naming) {
        string @namespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        string name       = TypeNaming.GeneratorNameFor(type, naming);
        string qualified  = @namespace.Length == 0 ? name : @namespace + "." + name;

        INamedTypeSymbol? beside = compilation.GetTypeByMetadataName(qualified);

        List<INamedTypeSymbol> named = beside is null ? [] : [beside];

        named.AddRange(compilation.GetSymbolsWithName(candidate => candidate == name, SymbolFilter.Type)
                                  .OfType<INamedTypeSymbol>()
                                  .Where(candidate => !SymbolEqualityComparer.Default.Equals(candidate, beside)));

        INamedTypeSymbol[] qualifying = [.. named.Where(candidate => Qualifies(candidate, type))];

        return new GeneratorCandidates(qualifying.Length == 1 ? qualifying[0] : null,
                                       qualifying.Length > 1 ? qualifying : [],
                                       named.Count > 0);
    }

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

    private static bool Qualifies(INamedTypeSymbol? candidate, INamedTypeSymbol type) {
        return candidate is { IsAbstract: false, IsStatic: false, DeclaredAccessibility: Accessibility.Public }
            && candidate.InstanceConstructors.Any(constructor => constructor.DeclaredAccessibility == Accessibility.Public
                                                              && constructor.Parameters.Length == 0)
            && candidate.AllInterfaces.Any(@interface => @interface.MetadataName == "IAny`1"
                                                      && @interface.ContainingNamespace?.ToDisplayString() == "JustDummies"
                                                      && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], type));
    }

}
