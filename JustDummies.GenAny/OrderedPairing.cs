using System;

namespace JustDummies.GenAny;

/// <summary>
///     Two parameters a constructor's guard ties by an order — <c>if (start &gt; end) { throw … }</c> — drawn together
///     through <c>Any.OrderedPair</c>, so the first never follows the second (§4.2, §5.3, #405).
/// </summary>
/// <remarks>
///     The one cross-parameter rule the engine writes rather than marks: a direct comparison of two parameters of one
///     ordered type is exactly what <c>OrderedPair</c> draws by construction, so honouring it takes no solver
///     (ADR-0046). Every other rule between parameters stays <c>to verify</c>.
/// </remarks>
public sealed class OrderedPairing {

    /// <summary>Ties <paramref name="first" /> to <paramref name="second" />.</summary>
    /// <param name="first">The parameter that comes first — at or below the other.</param>
    /// <param name="second">The parameter that comes second.</param>
    /// <param name="strict">Whether the two may not be equal — the guard threw on equality too.</param>
    /// <exception cref="ArgumentNullException">A name is null.</exception>
    /// <exception cref="ArgumentException">A name is blank, or both are the same.</exception>
    public OrderedPairing(string first, string second, bool strict) {
        if (first is null) { throw new ArgumentNullException(nameof(first)); }
        if (second is null) { throw new ArgumentNullException(nameof(second)); }
        if (first.Trim().Length == 0 || second.Trim().Length == 0) { throw new ArgumentException("A pairing names two parameters.", nameof(first)); }
        if (first == second) { throw new ArgumentException("A pairing ties two different parameters.", nameof(second)); }

        First  = first;
        Second = second;
        Strict = strict;
    }

    /// <summary>The name of the parameter that comes first.</summary>
    public string First { get; }

    /// <summary>The name of the parameter that comes second.</summary>
    public string Second { get; }

    /// <summary>Whether the two may not be equal.</summary>
    public bool Strict { get; }

    /// <summary>Whether <paramref name="parameter" /> is one of the two.</summary>
    public bool Ties(string parameter) {
        return parameter == First || parameter == Second;
    }

}
