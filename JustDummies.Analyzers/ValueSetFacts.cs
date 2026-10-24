using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     The elements a constant <c>OneOf(...)</c> writes at the call site. Mirrored once and read by every rule that
///     reasons about a value set, because two rules disagreeing about what a pool holds would be worse than either
///     being silent about it — the same reason <see cref="CharacterFamilies" /> has one home.
/// </summary>
internal static class ValueSetFacts {

    /// <summary>
    ///     The elements of <paramref name="valueSet" /> that are written inline, whether as a parameter array, an
    ///     array creation or a collection expression. A pool held in a variable yields nothing: it is the case
    ///     <c>IPoolInspection&lt;T&gt;</c> answers at run time instead.
    /// </summary>
    internal static IEnumerable<IOperation> Elements(IInvocationOperation valueSet) {
        foreach (IOperation element in ParamArrayElements(valueSet)) { yield return element; }

        foreach (IArgumentOperation argument in valueSet.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.ParamArray || InlineElements(argument.Value) is not { } inline) { continue; }

            foreach (IOperation element in inline) { yield return element; }
        }
    }

    /// <summary>
    ///     Whether every value <paramref name="valueSet" /> supplies is written inline, so that <see cref="Elements" />
    ///     yields the whole set rather than part of it.
    /// </summary>
    internal static bool IsWrittenInline(IInvocationOperation valueSet) {
        return valueSet.Arguments.All(argument => argument.ArgumentKind == ArgumentKind.ParamArray || InlineElements(argument.Value) is not null);
    }

    /// <summary>
    ///     The elements of a collection written at the call site — an array creation with its initializer, or a
    ///     collection expression with no spread — or <c>null</c> for anything else.
    /// </summary>
    /// <remarks>
    ///     A collection expression is read from its syntax: the operation that models it arrived with Roslyn 4.9, above
    ///     the floor this assembly is compiled against (ADR-0001), so the syntax is what every compiler it loads under
    ///     can show.
    /// </remarks>
    private static IReadOnlyList<IOperation>? InlineElements(IOperation value) {
        IOperation unwrapped = GeneratorFacts.Unwrap(value);
        if (unwrapped is IArrayCreationOperation { Initializer: { } initializer }) { return initializer.ElementValues; }
        if (unwrapped.Syntax is not CollectionExpressionSyntax collection || unwrapped.SemanticModel is not { } model) { return null; }

        List<IOperation> elements = [];
        foreach (CollectionElementSyntax element in collection.Elements) {
            if (element is not ExpressionElementSyntax { Expression: { } expression } || model.GetOperation(expression) is not { } operation) { return null; }

            elements.Add(operation);
        }

        return elements;
    }

    /// <summary>The elements of a parameter array written inline at the call site.</summary>
    internal static IEnumerable<IOperation> ParamArrayElements(IInvocationOperation invocation) {
        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { continue; }

            foreach (IOperation element in initializer.ElementValues) { yield return element; }
        }
    }

}
