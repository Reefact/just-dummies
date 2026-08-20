using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     The elements a constant <c>OneOf(...)</c> writes at the call site. Mirrored once and read by every rule that
///     reasons about a value set, because two rules disagreeing about what a pool holds would be worse than either
///     being silent about it — the same reason <see cref="CharacterFamilies" /> has one home.
/// </summary>
internal static class ValueSetFacts {

    /// <summary>
    ///     The elements of <paramref name="valueSet" /> that are written inline, whether as a parameter array or as
    ///     an inline collection. A pool held in a variable yields nothing: it is the case
    ///     <c>IPoolInspection&lt;T&gt;</c> answers at run time instead.
    /// </summary>
    internal static IEnumerable<IOperation> Elements(IInvocationOperation valueSet) {
        foreach (IOperation element in ParamArrayElements(valueSet)) { yield return element; }

        foreach (IArgumentOperation argument in valueSet.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray
             && GeneratorFacts.Unwrap(argument.Value) is IArrayCreationOperation { Initializer: { } inline }) {
                foreach (IOperation element in inline.ElementValues) { yield return element; }
            }
        }
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
