using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     Facts about the generator surface: recognising a value that is an <c>IAny&lt;T&gt;</c> recipe rather than the
///     value that recipe would draw. Every rule in the <c>JustDummies.Usage</c> category rests on this distinction.
/// </summary>
internal static class GeneratorFacts {

    private const string GenerateMethodName = "Generate";

    /// <summary>
    ///     Whether <paramref name="type" /> is a JustDummies generator — the <c>IAny&lt;T&gt;</c> interface itself, or
    ///     any type implementing it. Matching the interface rather than a list of concrete builders keeps the rules
    ///     correct for <c>As(...)</c> and <c>Combine(...)</c> derivations, and for a consumer's own generator.
    /// </summary>
    public static bool IsGenerator(ITypeSymbol? type, INamedTypeSymbol iAnyType) {
        if (type is null) { return false; }

        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, iAnyType)) { return true; }

        return type.AllInterfaces.Any(implemented => SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, iAnyType));
    }

    /// <summary>
    ///     Whether <paramref name="invocation" /> is a materializing <c>Generate()</c> call — the single member of
    ///     <c>IAny&lt;T&gt;</c>, and the only thing that turns a recipe into a value.
    /// </summary>
    public static bool IsGenerateCall(IInvocationOperation invocation, INamedTypeSymbol iAnyType) {
        IMethodSymbol method = invocation.TargetMethod;

        return method.Name == GenerateMethodName
            && method.Parameters.IsEmpty
            && IsGenerator(method.ContainingType, iAnyType);
    }

    /// <summary>
    ///     Whether the chain the <c>Generate()</c> call sits on provably starts at a static <c>JustDummies.Any</c>
    ///     factory — that is, whether the value is drawn from the <b>ambient</b> random source that a seed scope pins.
    /// </summary>
    /// <remarks>
    ///     Deliberately conservative: it answers "yes" only for a chain written inline from <c>Any</c>. A generator
    ///     reached through a local, a field or a parameter answers "no" and is not reported, which under-reports rather
    ///     than misfiring on a draw from an isolated <c>AnyContext</c> — that context is unaffected by the ambient
    ///     scope, so reporting it would be plainly wrong. The same reasoning excludes a chain written inline from
    ///     <c>Any.WithSeed(...)</c>: it is a static member of <c>Any</c>, but it hands back such a context, not a
    ///     generator. A derivation the library writes as an extension — <c>As(...)</c>, <c>OrNull()</c>,
    ///     <c>AsNullable()</c> — draws from the generator it is called on, so the walk continues through its receiver
    ///     rather than stopping at it.
    /// </remarks>
    public static bool RootsAtAmbientAny(IInvocationOperation invocation, INamedTypeSymbol anyType, INamedTypeSymbol iAnyType) {
        for (IOperation? current = invocation; current is IInvocationOperation call;) {
            if (call.Instance is not null) {
                current = Unwrap(call.Instance);

                continue;
            }

            IArgumentOperation? receiver = LibraryDerivationReceiver(call, anyType, iAnyType);
            if (receiver is not null) {
                current = Unwrap(receiver.Value);

                continue;
            }

            // A static call: ambient only when it is one of Any's own factories, which all return a generator.
            return SymbolEqualityComparer.Default.Equals(call.TargetMethod.ContainingType, anyType)
                && IsGenerator(call.TargetMethod.ReturnType, iAnyType);
        }

        return false;
    }

    /// <summary>
    ///     The receiver of <paramref name="call" /> when it is one of the library's own derivations written as an
    ///     extension method — declared beside <c>Any</c> and turning a generator into a generator — or <c>null</c>.
    /// </summary>
    /// <remarks>
    ///     Only the library's own extensions are seen through: they keep the source of the generator they wrap. A
    ///     consumer's extension method could draw from anywhere, so the walk stops there and the chain is not
    ///     reported, the conservative side.
    /// </remarks>
    private static IArgumentOperation? LibraryDerivationReceiver(IInvocationOperation call, INamedTypeSymbol anyType, INamedTypeSymbol iAnyType) {
        IMethodSymbol method = call.TargetMethod;
        if (!method.IsExtensionMethod) { return null; }
        if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, anyType.ContainingAssembly)) { return null; }
        if (!IsGenerator(method.ReturnType, iAnyType)) { return null; }

        return call.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
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
