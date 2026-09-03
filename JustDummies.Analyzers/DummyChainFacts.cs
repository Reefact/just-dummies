using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Walks a fluent generator chain back to the factory that started it, and yields the constraint calls in the
///     order they were written. Every rule that reasons about a <b>combination</b> of constraints — rather than about
///     one argument — needs this, because the library's own conflict detection is order-sensitive in its messages and
///     order-insensitive in its verdict.
/// </summary>
/// <remarks>
///     Syntactic on purpose. The chain must be written as one expression; a generator passed through a local, a field
///     or a helper is not followed. That is a deliberate limit, not an oversight: following it would mean dataflow,
///     and a rule that claims a chain is unsatisfiable must be certain of every constraint the chain carries.
/// </remarks>
internal static class DummyChainFacts {

    /// <summary>
    ///     The constraint calls of the chain <paramref name="invocation" /> belongs to, outermost call last, together
    ///     with the factory that rooted it. Returns <c>false</c> when the chain does not start at a
    ///     <c>JustDummies.Dummy</c> / <c>DummyContext</c> factory, which is the only case a rule can reason about
    ///     completely.
    /// </summary>
    public static bool TryGetChain(IInvocationOperation invocation, KnownSymbols symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory) {
        List<IInvocationOperation> collected = [];
        constraints = collected;
        factory     = null;

        // Climb to the outermost invocation of the chain, so a rule registered on any link sees the whole of it.
        IInvocationOperation outermost = invocation;
        while (outermost.Parent is IInvocationOperation parent && ReferenceEquals(GeneratorFacts.Unwrap(parent.Instance ?? parent), outermost)) {
            outermost = parent;
        }

        // Unbounded on purpose, and it terminates: every branch below either returns or steps `current` one
        // link down the receiver chain, which a syntax tree makes finite. Written `;;` rather than with a
        // `current is not null` guard because that guard could never be false — `next` is pattern-matched
        // non-null — which left the exit path unreachable (Sonar csharpsquid:S2583). Same shape as the
        // redraw loop in DummyPattern.
        for (IInvocationOperation current = outermost;;) {
            // The factory is the first call owned by Dummy or DummyContext, whatever its receiver turns out to be.
            // Asked BEFORE descending on purpose: a seeded chain written in one expression — Dummy.WithSeed(s).Int32()
            // — has an INVOCATION for Int32()'s receiver, so a walk that descends first pushes Int32() onto the
            // constraints and names WithSeed as the factory. Every rule gated on the factory name then falls
            // through, which silently disabled four of them on the very form the library recommends for
            // reproducibility.
            if (IsFactoryOwner(current.TargetMethod.ContainingType, symbols)) {
                factory = current;
                collected.Reverse();

                return true;
            }

            // Not a factory and rooted in a static call: this is not a JustDummies chain at all.
            if (current.Instance is null) { return false; }

            IOperation receiver = GeneratorFacts.Unwrap(current.Instance);

            // A generator reached through a local, a field or a helper is not followed — the chain must be one
            // expression for a rule to see every constraint it carries.
            if (receiver is not IInvocationOperation next) { return false; }

            collected.Add(current);
            current = next;
        }
    }

    private static bool IsFactoryOwner(INamedTypeSymbol? type, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(type, symbols.Dummy)
            || SymbolEqualityComparer.Default.Equals(type, symbols.DummyContext);
    }

}
