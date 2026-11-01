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
internal static class AnyChainFacts {

    /// <summary>
    ///     The constraint calls of the chain <paramref name="invocation" /> belongs to, outermost call last, together
    ///     with the factory that rooted it. Returns <c>false</c> when the chain does not start at a
    ///     <c>JustDummies.Any</c> / <c>AnyContext</c> factory, which is the only case a rule can reason about
    ///     completely.
    /// </summary>
    public static bool TryGetChain(IInvocationOperation invocation, KnownSymbols symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory) {
        List<IInvocationOperation> collected = [];
        constraints = collected;
        factory     = null;

        // Climb to the outermost invocation of the chain, so a rule registered on any link sees the whole of it.
        IInvocationOperation outermost = invocation;
        while (OuterLinkOf(outermost, symbols) is IInvocationOperation parent) { outermost = parent; }

        // Unbounded on purpose, and it terminates: every branch below either returns or steps `current` one
        // link down the receiver chain, which a syntax tree makes finite. Written `;;` rather than with a
        // `current is not null` guard because that guard could never be false — `next` is pattern-matched
        // non-null — which left the exit path unreachable (Sonar csharpsquid:S2583). Same shape as the
        // redraw loop in AnyPattern.
        for (IInvocationOperation current = outermost;;) {
            // The factory is the first call owned by Any or AnyContext, whatever its receiver turns out to be.
            // Asked BEFORE descending on purpose: a seeded chain written in one expression — Any.WithSeed(s).Int32()
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
            if (ReceiverOf(current, symbols) is not IOperation link) { return false; }

            IOperation receiver = GeneratorFacts.Unwrap(link);

            // A generator reached through a local, a field or a helper is not followed — the chain must be one
            // expression for a rule to see every constraint it carries.
            if (receiver is not IInvocationOperation next) { return false; }

            collected.Add(current);
            current = next;
        }
    }

    /// <summary>
    ///     Whether <paramref name="invocation" /> is the receiver of a further link of its chain — so a rule analyses
    ///     the chain once, from that outer call, rather than from every link.
    /// </summary>
    public static bool HasOuterLink(IInvocationOperation invocation, KnownSymbols symbols) {
        return OuterLinkOf(invocation, symbols) is not null;
    }

    /// <summary>
    ///     The call <paramref name="invocation" /> is the receiver of: the instance call written on it, or a chain
    ///     extension taking it as its first argument.
    /// </summary>
    private static IInvocationOperation? OuterLinkOf(IInvocationOperation invocation, KnownSymbols symbols) {
        if (invocation.Parent is IInvocationOperation parent && ReferenceEquals(GeneratorFacts.Unwrap(parent.Instance ?? parent), invocation)) { return parent; }

        IOperation argument = invocation;
        while (argument.Parent is IConversionOperation conversion) { argument = conversion; }

        return argument.Parent is IArgumentOperation { Parent: IInvocationOperation extension } written
            && IsChainExtension(extension, symbols)
            && ReferenceEquals(extension.Arguments[0], written)
                   ? extension
                   : null;
    }

    /// <summary>The generator a link is written on: its instance, or what a chain extension extends.</summary>
    private static IOperation? ReceiverOf(IInvocationOperation call, KnownSymbols symbols) {
        if (call.Instance is not null) { return call.Instance; }

        return IsChainExtension(call, symbols) ? call.Arguments[0].Value : null;
    }

    /// <summary>
    ///     Whether the call is a library extension on a concrete generator that returns that same generator — a link
    ///     of the chain written as a static call only so its element type can be constrained, as <c>Ordered()</c>
    ///     requires an element type with a natural order. An extension on <c>IAny&lt;T&gt;</c> is not one: it leaves
    ///     the builder behind.
    /// </summary>
    private static bool IsChainExtension(IInvocationOperation call, KnownSymbols symbols) {
        IMethodSymbol method = call.TargetMethod;

        return method.IsExtensionMethod
            && call.Arguments.Length > 0
            && method.Parameters[0].Type.TypeKind == TypeKind.Class
            && SymbolEqualityComparer.Default.Equals(method.ReturnType, method.Parameters[0].Type)
            && symbols.IsJustDummiesMember(method);
    }

    private static bool IsFactoryOwner(INamedTypeSymbol? type, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(type, symbols.Any)
            || SymbolEqualityComparer.Default.Equals(type, symbols.AnyContext);
    }

}
