using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenDummy;

/// <summary>
///     Finds the type a developer named on the command line (§3.2).
/// </summary>
/// <remarks>
///     Inside the engine rather than in the shell, because §11.1 puts it there and because the IDE consumer of
///     §16 needs the same lookup — and because a name that resolves to nothing, or to several things, is an
///     outcome the result model carries rather than an exception the boundary leaks (§10.3).
/// </remarks>
internal static class TypeLookup {

    /// <summary>How many near-misses are worth offering. More reads as a list to search rather than a hint.</summary>
    private const int Suggestions = 5;

    /// <summary>
    ///     Every type <paramref name="argument" /> could mean: none, one, or several.
    /// </summary>
    /// <remarks>
    ///     A <b>nested</b> type is written the way a developer types it — <c>Order.Line</c> — and translated
    ///     here, where the separator is <c>+</c> rather than <c>.</c>. Handing the dotted form straight to a
    ///     metadata lookup returns nothing, which would report a real type as missing.
    /// </remarks>
    internal static IReadOnlyList<INamedTypeSymbol> Find(Compilation compilation, string argument) {
        if (argument.IndexOf('.') >= 0) {
            INamedTypeSymbol? qualified = ByMetadataName(compilation, argument);

            if (qualified is not null) { return [qualified]; }
        }

        return [.. Matching(compilation, argument)];
    }

    /// <summary>
    ///     The names closest to one that matched nothing, so the answer is a correction rather than a denial.
    /// </summary>
    internal static IReadOnlyList<string> Closest(Compilation compilation, string argument) {
        string wanted = argument.Substring(argument.LastIndexOf('.') + 1);

        return [.. Declared(compilation)
                  .Select(type => type.Name)
                  .Distinct(StringComparer.Ordinal)
                  .Select(name => (Name: name, Distance: Distance(name, wanted)))
                  .Where(candidate => candidate.Distance <= Math.Max(2, wanted.Length / 3))
                  .OrderBy(candidate => candidate.Distance)
                  .ThenBy(candidate => candidate.Name, StringComparer.Ordinal)
                  .Take(Suggestions)
                  .Select(candidate => candidate.Name)];
    }

    /// <summary>
    ///     A fully qualified name, trying each way a dotted argument could split into namespace and nesting.
    /// </summary>
    private static INamedTypeSymbol? ByMetadataName(Compilation compilation, string argument) {
        INamedTypeSymbol? exact = compilation.GetTypeByMetadataName(argument);

        if (exact is not null) { return exact; }

        // Shop.Domain.Order.Line -> Shop.Domain.Order+Line -> Shop.Domain+Order+Line, taking the first that
        // binds. The rightmost dots are the likeliest nesting, so they are tried first.
        char[] name = argument.ToCharArray();

        for (int index = argument.Length - 1; index >= 0; index--) {
            if (name[index] != '.') { continue; }

            name[index] = '+';

            INamedTypeSymbol? nested = compilation.GetTypeByMetadataName(new string(name));

            if (nested is not null) { return nested; }
        }

        return null;
    }

    /// <summary>
    ///     Every type whose name, or whose nested spelling, is the one asked for — source first (§3.2).
    /// </summary>
    /// <remarks>
    ///     The developer's own types win outright, and the search only widens to the referenced assemblies
    ///     when none of them answers. Both halves matter: a domain type named <c>Uri</c> or <c>Task</c> is the
    ///     one they meant, and widening past it would report an ambiguity against the framework's; while a
    ///     type that lives in a referenced project — which is most of them, since the tool is run from the
    ///     test project — is not in source at all and would otherwise read as missing.
    /// </remarks>
    private static IEnumerable<INamedTypeSymbol> Matching(Compilation compilation, string argument) {
        List<INamedTypeSymbol> inSource = [.. Named(Within(compilation.Assembly.GlobalNamespace), argument)];

        return inSource.Count > 0 ? inSource : Named(Within(compilation.GlobalNamespace), argument);
    }

    private static IEnumerable<INamedTypeSymbol> Named(IEnumerable<INamedTypeSymbol> types, string argument) {
        return types.Where(type => Spelling(type) == argument || type.Name == argument);
    }

    /// <summary>The way a developer would type this type from inside its namespace — <c>Order.Line</c>.</summary>
    private static string Spelling(INamedTypeSymbol type) {
        List<string> parts = [type.Name];

        for (INamedTypeSymbol? containing = type.ContainingType; containing is not null; containing = containing.ContainingType) {
            parts.Insert(0, containing.Name);
        }

        return string.Join(".", parts);
    }

    /// <summary>
    ///     The types a suggestion may be drawn from: the developer's own, and the referenced ones only when
    ///     there are none.
    /// </summary>
    /// <remarks>
    ///     Narrower than what <see cref="Matching" /> searches, deliberately. A near-miss is useful because the
    ///     reader recognises the name; measuring the edit distance to every type in every referenced assembly
    ///     would answer a misspelt domain type with five framework types nobody has heard of.
    /// </remarks>
    private static IEnumerable<INamedTypeSymbol> Declared(Compilation compilation) {
        List<INamedTypeSymbol> declared = [.. Within(compilation.Assembly.GlobalNamespace)];

        return declared.Count > 0 ? declared : Within(compilation.GlobalNamespace);
    }

    private static IEnumerable<INamedTypeSymbol> Within(INamespaceSymbol @namespace) {
        foreach (INamedTypeSymbol type in @namespace.GetTypeMembers()) {
            foreach (INamedTypeSymbol member in WithNested(type)) { yield return member; }
        }

        foreach (INamespaceSymbol nested in @namespace.GetNamespaceMembers()) {
            foreach (INamedTypeSymbol member in Within(nested)) { yield return member; }
        }
    }

    private static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type) {
        yield return type;

        foreach (INamedTypeSymbol nested in type.GetTypeMembers()) {
            foreach (INamedTypeSymbol member in WithNested(nested)) { yield return member; }
        }
    }

    /// <summary>
    ///     Levenshtein distance, so a typo is answered with the name that was meant.
    /// </summary>
    /// <remarks>
    ///     Two rows rather than a full matrix: the alternative allocates the product of both lengths for a
    ///     number that is thrown away, and this runs once per candidate type in the project.
    /// </remarks>
    private static int Distance(string left, string right) {
        int[] previous = new int[right.Length + 1];
        int[] current  = new int[right.Length + 1];

        for (int column = 0; column <= right.Length; column++) { previous[column] = column; }

        for (int row = 1; row <= left.Length; row++) {
            current[0] = row;

            for (int column = 1; column <= right.Length; column++) {
                int substitution = previous[column - 1] + (left[row - 1] == right[column - 1] ? 0 : 1);

                current[column] = Math.Min(Math.Min(current[column - 1] + 1, previous[column] + 1), substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

}
