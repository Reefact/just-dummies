using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Facts about the generator surface: recognising a value that is an <c>IDummy&lt;T&gt;</c> recipe rather than the
///     value that recipe would draw. Every rule in the <c>JustDummies.Usage</c> category rests on this distinction.
/// </summary>
internal static class GeneratorFacts {

    private const string GenerateMethodName = "Generate";

    /// <summary>
    ///     Whether <paramref name="type" /> is a JustDummies generator — the <c>IDummy&lt;T&gt;</c> interface itself, or
    ///     any type implementing it. Matching the interface rather than a list of concrete builders keeps the rules
    ///     correct for <c>As(...)</c> and <c>Combine(...)</c> derivations, and for a consumer's own generator.
    /// </summary>
    public static bool IsGenerator(ITypeSymbol? type, INamedTypeSymbol iDummyType) {
        if (type is null) { return false; }

        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, iDummyType)) { return true; }

        return type.AllInterfaces.Any(implemented => SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, iDummyType));
    }

    /// <summary>
    ///     Whether <paramref name="invocation" /> is a materializing <c>Generate()</c> call — the single member of
    ///     <c>IDummy&lt;T&gt;</c>, and the only thing that turns a recipe into a value.
    /// </summary>
    public static bool IsGenerateCall(IInvocationOperation invocation, INamedTypeSymbol iDummyType) {
        IMethodSymbol method = invocation.TargetMethod;

        return method.Name == GenerateMethodName
            && method.Parameters.IsEmpty
            && IsGenerator(method.ContainingType, iDummyType);
    }

    /// <summary>
    ///     Whether the chain the <c>Generate()</c> call sits on provably starts at a static <c>JustDummies.Dummy</c>
    ///     factory — that is, whether the value is drawn from the <b>ambient</b> random source that a seed scope pins.
    /// </summary>
    /// <remarks>
    ///     Deliberately conservative: it answers "yes" only for a chain written inline from <c>Dummy</c>. A generator
    ///     reached through a local, a field or a parameter answers "no" and is not reported, which under-reports rather
    ///     than misfiring on a draw from an isolated <c>DummyContext</c> — that context is unaffected by the ambient
    ///     scope, so reporting it would be plainly wrong.
    /// </remarks>
    public static bool RootsAtAmbientDummy(IInvocationOperation invocation, INamedTypeSymbol dummyType) {
        for (IOperation? current = invocation; current is IInvocationOperation call;) {
            if (call.Instance is null) {
                // A static call: ambient only when it is one of Dummy's own factories.
                return SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, dummyType);
            }

            current = Unwrap(call.Instance);
        }

        return false;
    }

    /// <summary>
    ///     Strips the implicit conversions Roslyn inserts around a generator when it flows into an <c>object</c> or
    ///     <c>string</c> position, so the rule sees the recipe rather than the conversion wrapping it.
    /// </summary>
    public static IOperation Unwrap(IOperation operation) {
        IOperation current = operation;
        while (current is IConversionOperation { IsImplicit: true } conversion) {
            current = conversion.Operand;
        }

        return current;
    }

}
