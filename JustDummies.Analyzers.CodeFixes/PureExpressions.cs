using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.Analyzers.CodeFixes;

/// <summary>
///     Whether an argument can be moved without changing when anything runs: a fix that swaps two arguments or
///     carries one past the links written between two bounds reorders their evaluation, which is observable only
///     through a side effect. So a fix is offered only on arguments that provably have none — the shapes the rules
///     fold, and nothing wider.
/// </summary>
/// <remarks>
///     Admitted: a compile-time constant; a read-only static field of a <c>System</c> value type
///     (<c>DateTime.MaxValue</c>, <c>TimeSpan.Zero</c>); the limit a <c>System</c> value type names as a property
///     (<c>DateOnly.MinValue</c>, <c>DateTimeOffset.UnixEpoch</c>) — and no other property, since <c>DateTime.Now</c>
///     reads a clock; a <c>System</c> value type built or made from such arguments (<c>new DateTime(2026, 1, 2)</c>,
///     <c>TimeSpan.FromHours(2)</c>); and a cast, a unary minus or parentheses over one. A call into the developer's
///     own code, a local, a parameter or a property of their own type is not admitted, whatever it returns: the rule
///     may still report the pair, and the fix stays away (ADR-0130).
/// </remarks>
internal static class PureExpressions {

    private const string SystemNamespace = "System";

    /// <summary>The properties a <c>System</c> value type names a limit with, which read nothing but the limit.</summary>
    private static readonly string[] Limits = ["MinValue", "MaxValue", "UnixEpoch"];

    public static bool IsPure(SemanticModel model, ExpressionSyntax expression) {
        if (model.GetConstantValue(expression).HasValue) { return true; }

        return expression switch {
            ParenthesizedExpressionSyntax parenthesized                                                           => IsPure(model, parenthesized.Expression),
            CastExpressionSyntax cast                                                                             => IsPure(model, cast.Expression),
            PrefixUnaryExpressionSyntax unary                                                                     => IsPure(model, unary.Operand),
            ObjectCreationExpressionSyntax creation when creation.Initializer is null                            => IsSystemValueType(model.GetTypeInfo(creation).Type) && AreAllPure(model, creation.ArgumentList),
            InvocationExpressionSyntax invocation when model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method => method.IsStatic && IsSystemValueType(method.ContainingType) && AreAllPure(model, invocation.ArgumentList),
            MemberAccessExpressionSyntax or IdentifierNameSyntax                                                 => IsSystemValueTypeMember(model.GetSymbolInfo(expression).Symbol),
            _                                                                                                     => false
        };
    }

    private static bool AreAllPure(SemanticModel model, BaseArgumentListSyntax? arguments) {
        return arguments is null || arguments.Arguments.All(argument => IsPure(model, argument.Expression));
    }

    /// <summary>A read-only static field of a <c>System</c> value type, or the limit one names as a property.</summary>
    private static bool IsSystemValueTypeMember(ISymbol? symbol) {
        return symbol switch {
            IFieldSymbol field       => field.IsStatic && (field.IsReadOnly || field.IsConst) && IsSystemValueType(field.ContainingType),
            IPropertySymbol property => property.IsStatic && Limits.Contains(property.Name) && IsSystemValueType(property.ContainingType),
            _                        => false
        };
    }

    private static bool IsSystemValueType(ITypeSymbol? type) {
        return type is { IsValueType: true, ContainingNamespace: { Name: SystemNamespace, ContainingNamespace.IsGlobalNamespace: true } };
    }

}
