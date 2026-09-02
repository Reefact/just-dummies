using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace JustDummies.GenAny;

/// <summary>
///     What <see cref="Composition.CandidatesFor" /> found under the name a generator for a type would carry.
/// </summary>
/// <remarks>
///     Three shapes, each answered differently by <see cref="GeneratorFor" />: exactly one usable candidate is
///     composed through; more than one is named without choosing between them (§5.4); and a name that exists
///     but names nothing usable is not the same as a name that names nothing at all — the first must never be
///     proposed as though it were the second.
/// </remarks>
internal sealed class GeneratorCandidates {

    internal GeneratorCandidates(INamedTypeSymbol? unique, IReadOnlyList<INamedTypeSymbol> tied, bool anyNamed) {
        Unique   = unique;
        Tied     = tied;
        AnyNamed = anyNamed;
    }

    /// <summary>The one usable candidate, when exactly one qualifies.</summary>
    internal INamedTypeSymbol? Unique { get; }

    /// <summary>Two or more usable candidates, when the name does not settle which one is meant.</summary>
    internal IReadOnlyList<INamedTypeSymbol> Tied { get; }

    /// <summary>
    ///     Whether a type carrying this exact name exists in the compilation at all, qualifying or not.
    /// </summary>
    /// <remarks>
    ///     True with <see cref="Unique" /> null and <see cref="Tied" /> empty is the case this type exists for:
    ///     something already answers to the name the engine would otherwise propose, and it is not a generator —
    ///     static, abstract, missing the interface, or missing the constructor. Composing through it would not
    ///     compile, and naming it as though nothing were there yet — §5.4's own mechanism for a type with no
    ///     generator — would name exactly the wrong culprit.
    /// </remarks>
    internal bool AnyNamed { get; }

}
