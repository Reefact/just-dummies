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

    private readonly Compilation compilation;

    private bool? carriesAs;

    private bool? carriesAsNullable;

    private LibrarySurface(Compilation compilation, INamedTypeSymbol any, INamedTypeSymbol anyOfT, INamedTypeSymbol extensions) {
        this.compilation = compilation;
        Any              = any;
        AnyOfT           = anyOfT;
        Extensions       = extensions;
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

        return new LibrarySurface(compilation, any, anyOfT, extensions);
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

    /// <summary>
    ///     Whether <c>AsNullable</c> resolves, which the value-type nullable row of §5.2 prefers.
    /// </summary>
    /// <remarks>
    ///     Asked rather than assumed, and answered <c>false</c> without complaint on an asset that predates it:
    ///     the row then writes the general <c>As</c> hop it always wrote, which still compiles and still draws.
    ///     ADR-0059 in its ordinary form — the tool holds no opinion about which members exist, it asks the
    ///     compilation in front of it.
    /// </remarks>
    internal bool CarriesAsNullable() {
        return carriesAsNullable ??= compilation.GetTypeByMetadataName("JustDummies.NullableExtensions")
                                                ?.GetMembers("AsNullable")
                                                 .OfType<IMethodSymbol>()
                                                 .Any(Lifts) == true;
    }

    /// <summary>Whether the candidate is <c>AsNullable&lt;T&gt;(this IAny&lt;T&gt;) -&gt; IAny&lt;T?&gt;</c>.</summary>
    private bool Lifts(IMethodSymbol candidate) {
        return Hop(candidate, typeParameters: 1, parameters: 1)
            && candidate.TypeParameters[0].HasValueTypeConstraint
            && Draws(candidate.Parameters[0].Type, candidate.TypeParameters[0])
            && DrawsTheNullableOf(candidate.ReturnType, candidate.TypeParameters[0]);
    }

    /// <summary>Whether <c>As</c> resolves, which the two conversion rows of §5.2 depend on.</summary>
    internal bool CarriesAs() {
        return carriesAs ??= Extensions.GetMembers("As").OfType<IMethodSymbol>().Any(Converts);
    }

    /// <summary>
    ///     Whether the candidate is
    ///     <c>As&lt;TSource, TResult&gt;(this IAny&lt;TSource&gt;, Func&lt;TSource, TResult&gt;) -&gt; IAny&lt;TResult&gt;</c>.
    /// </summary>
    private bool Converts(IMethodSymbol candidate) {
        return Hop(candidate, typeParameters: 2, parameters: 2)
            && Draws(candidate.Parameters[0].Type, candidate.TypeParameters[0])
            && Produces(candidate.Parameters[1].Type, candidate.TypeParameters[0], candidate.TypeParameters[1])
            && Draws(candidate.ReturnType, candidate.TypeParameters[1]);
    }

    /// <summary>
    ///     The shape common to both conversion rows: a public static extension of that arity.
    /// </summary>
    /// <remarks>
    ///     <c>IsExtensionMethod</c> is part of it rather than incidental. The emitted expression reads
    ///     <c>expr.As(…)</c>, not <c>AnyExtensions.As(expr, …)</c>, so a static method of the right name and
    ///     arity that is not an extension would resolve here and not there.
    /// </remarks>
    private static bool Hop(IMethodSymbol candidate, int typeParameters, int parameters) {
        return candidate.IsStatic
            && candidate.IsExtensionMethod
            && candidate.DeclaredAccessibility == Accessibility.Public
            && candidate.TypeParameters.Length == typeParameters
            && candidate.Parameters.Length == parameters;
    }

    /// <summary>Whether <paramref name="type" /> is <c>IAny&lt;<paramref name="argument" />&gt;</c>.</summary>
    private bool Draws(ITypeSymbol type, ITypeSymbol argument) {
        return type is INamedTypeSymbol named
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, AnyOfT)
            && SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], argument);
    }

    /// <summary>Whether <paramref name="type" /> is <c>IAny&lt;<paramref name="argument" />?&gt;</c>.</summary>
    private bool DrawsTheNullableOf(ITypeSymbol type, ITypeSymbol argument) {
        return type is INamedTypeSymbol named
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, AnyOfT)
            && named.TypeArguments[0] is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            && SymbolEqualityComparer.Default.Equals(nullable.TypeArguments[0], argument);
    }

    /// <summary>Whether <paramref name="type" /> is <c>Func&lt;TSource, TResult&gt;</c> over those two.</summary>
    private bool Produces(ITypeSymbol type, ITypeSymbol source, ITypeSymbol result) {
        return compilation.GetTypeByMetadataName("System.Func`2") is { } func
            && type is INamedTypeSymbol named
            && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, func)
            && SymbolEqualityComparer.Default.Equals(named.TypeArguments[0], source)
            && SymbolEqualityComparer.Default.Equals(named.TypeArguments[1], result);
    }

}
