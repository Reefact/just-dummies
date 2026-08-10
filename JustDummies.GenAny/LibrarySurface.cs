using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     The library, as seen through the developer's own compilation.
/// </summary>
/// <remarks>
///     Every symbol is looked up by metadata name against the compilation being analyzed, and no JustDummies
///     assembly is referenced from here (ADR-0063). That is what makes version skew between the tool and the
///     library structurally impossible: the tool never holds an opinion about which members exist, it asks.
///     <para>
///         Which is also ADR-0059, the rule this class exists to enforce: a member the compilation cannot
///         resolve is never emitted. A project on the <c>netstandard2.0</c> asset has no <c>Any.DateOnly()</c>,
///         and a scaffolder that emitted one anyway would hand the developer a file that does not compile.
///     </para>
/// </remarks>
internal sealed class LibrarySurface {

    private readonly Dictionary<string, ITypeSymbol?> factories = [];

    private LibrarySurface(INamedTypeSymbol any, INamedTypeSymbol anyOfT, INamedTypeSymbol extensions) {
        Any        = any;
        AnyOfT     = anyOfT;
        Extensions = extensions;
    }

    /// <summary>The static façade every emitted expression starts from.</summary>
    internal INamedTypeSymbol Any { get; }

    /// <summary>The recipe interface the emitted generator implements.</summary>
    internal INamedTypeSymbol AnyOfT { get; }

    /// <summary>Where <c>As</c> lives — the one hop that changes a generator's type.</summary>
    internal INamedTypeSymbol Extensions { get; }

    /// <summary>
    ///     The library as this compilation sees it, or null when the project does not reference it.
    /// </summary>
    /// <remarks>
    ///     All three or none: a compilation carrying some of them is not a JustDummies consumer, it is a
    ///     coincidence of names. The caller reports the absence rather than scaffolding a file that could not
    ///     name a single generator (§7).
    /// </remarks>
    internal static LibrarySurface? In(Compilation compilation) {
        INamedTypeSymbol? any        = compilation.GetTypeByMetadataName("JustDummies.Any");
        INamedTypeSymbol? anyOfT     = compilation.GetTypeByMetadataName("JustDummies.IAny`1");
        INamedTypeSymbol? extensions = compilation.GetTypeByMetadataName("JustDummies.AnyExtensions");

        if (any is null || anyOfT is null || extensions is null) { return null; }

        return new LibrarySurface(any, anyOfT, extensions);
    }

    /// <summary>
    ///     The builder type <c>Any.<paramref name="factory" />(…)</c> returns, or null when this compilation has
    ///     no such factory.
    /// </summary>
    /// <param name="factory">The façade method's name — <c>String</c>, <c>DateOnly</c>, <c>ListOf</c>.</param>
    /// <param name="typeArguments">How many type arguments it takes.</param>
    /// <param name="parameters">How many value arguments it takes.</param>
    internal ITypeSymbol? Returned(string factory, int typeArguments = 0, int parameters = 0) {
        string key = $"{factory}`{typeArguments}({parameters})";

        if (factories.TryGetValue(key, out ITypeSymbol? cached)) { return cached; }

        IMethodSymbol? method = Any.GetMembers(factory)
                                   .OfType<IMethodSymbol>()
                                   .FirstOrDefault(candidate => candidate.IsStatic
                                                             && candidate.DeclaredAccessibility == Accessibility.Public
                                                             && candidate.TypeParameters.Length == typeArguments
                                                             && candidate.Parameters.Length == parameters);

        factories[key] = method?.ReturnType;

        return method?.ReturnType;
    }

    /// <summary>
    ///     Whether <paramref name="generator" /> carries a constraint of that name and arity — inherited members
    ///     included, since the collection constraints are declared on a shared base (§14.3).
    /// </summary>
    internal static bool Carries(ITypeSymbol? generator, string constraint, int parameters = 0) {
        for (ITypeSymbol? type = generator; type is not null; type = type.BaseType) {
            bool declared = type.GetMembers(constraint)
                                .OfType<IMethodSymbol>()
                                .Any(candidate => candidate.DeclaredAccessibility == Accessibility.Public
                                               && !candidate.IsStatic
                                               && candidate.Parameters.Length == parameters);

            if (declared) { return true; }
        }

        return false;
    }

    /// <summary>Whether <c>As</c> resolves, which the two conversion rows of §5.2 depend on.</summary>
    internal bool CarriesAs() {
        return Extensions.GetMembers("As")
                         .OfType<IMethodSymbol>()
                         .Any(candidate => candidate.IsStatic
                                        && candidate.DeclaredAccessibility == Accessibility.Public
                                        && candidate.Parameters.Length == 2);
    }

}
