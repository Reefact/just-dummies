using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     What an <c>Any.String()</c> chain demands of a supplied value, read from the constants at the call site: each
///     declared constraint paired with the test a value a <c>OneOf(...)</c> writes inline must pass to satisfy it.
/// </summary>
/// <remarks>
///     <para>
///         Shared because two rules ask the same question of the same pool and must not answer it differently:
///         <c>JD029</c> names the values a constraint refuses while others survive, and <c>JD015</c> reports the pool
///         none of them survives — and stands as the reason <c>JD029</c> says nothing then. Held in two places, the
///         two drifted: one tested lengths, anchors and exclusions, the other only characters, so a pool emptied by a
///         length was each rule's to leave to the other (issue #225).
///     </para>
///     <para>
///         The predicates restate what <c>StringSpec</c> applies to an allow-list, because an analyzer references no
///         JustDummies assembly and cannot call it. A constraint not named here, or whose argument does not fold, is
///         absent rather than approximated: adding constraints only ever removes values, so a missing test can only
///         make a pool look fuller than it is, never emptier.
///     </para>
/// </remarks>
internal static class StringValueFacts {

    /// <summary>
    ///     The declared constraints whose argument is a compile-time constant, each paired with the test a supplied
    ///     value must pass to satisfy it, in declaration order.
    /// </summary>
    internal static List<(string Rendered, Func<string, bool> Admits)> Tests(IReadOnlyList<IInvocationOperation> constraints) {
        List<(string Rendered, Func<string, bool> Admits)> tests = [];

        foreach (IInvocationOperation constraint in constraints) {
            switch (constraint.TargetMethod.Name) {
                // Every name in these two labels is one PoolFor resolves, so the lookup cannot come back empty.
                case "Alpha" or "Numeric" or "AlphaNumeric" or "Punctuation" or "Printable" or "NonPrintable" or "Whitespaces" or "Hexadecimal"
                    when CharacterFamilies.PoolFor(constraint.TargetMethod.Name) is string family:
                    tests.Add(($"{constraint.TargetMethod.Name}()", DrawnFrom(family)));

                    break;

                case "WithoutAlpha" or "WithoutNumeric"
                    when CharacterFamilies.PoolFor(constraint.TargetMethod.Name.Substring("Without".Length)) is string removed:
                    tests.Add(($"{constraint.TargetMethod.Name}()", DrawnNoneOf(removed)));

                    break;

                case "WithoutChars" when TryGetSingleString(constraint, out string chars):
                    tests.Add(($"WithoutChars({Quote(chars)})", DrawnNoneOf(chars)));

                    break;

                case "NonEmpty":    tests.Add(("NonEmpty()", value => value.Length >= 1));               break;
                // Judges the value rather than each of its characters — interior whitespace is legal, and only an
                // entirely blank value is refused.
                case "NotBlank":    tests.Add(("NotBlank()", value => !string.IsNullOrWhiteSpace(value))); break;
                case "InUpperCase": tests.Add(("InUpperCase()", value => !value.Any(char.IsLower)));      break;
                case "InLowerCase": tests.Add(("InLowerCase()", value => !value.Any(char.IsUpper)));      break;

                case "WithChars" when TryGetSingleString(constraint, out string pool):
                    tests.Add(($"WithChars({Quote(pool)})", DrawnFrom(pool)));

                    break;

                case "WithLength" when TryGetSingleInt32(constraint, out int exact):
                    tests.Add(($"WithLength({exact})", value => value.Length == exact));

                    break;

                case "WithMinLength" when TryGetSingleInt32(constraint, out int minimum):
                    tests.Add(($"WithMinLength({minimum})", value => value.Length >= minimum));

                    break;

                case "WithMaxLength" when TryGetSingleInt32(constraint, out int maximum):
                    tests.Add(($"WithMaxLength({maximum})", value => value.Length <= maximum));

                    break;

                // One call, two bounds, one name — judged under that one name, exactly as the run time renders it,
                // so a hint never points at a half of a call the caller cannot loosen on its own.
                case "WithLengthBetween" when constraint.Arguments.Length == 2
                                           && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int low)
                                           && ConstantFacts.TryGetInt32(constraint.Arguments[1].Value, out int high):
                    tests.Add(($"WithLengthBetween({low}, {high})", value => value.Length >= low && value.Length <= high));

                    break;

                case "StartingWith" when TryGetSingleString(constraint, out string prefix):
                    tests.Add(($"StartingWith({Quote(prefix)})", value => value.StartsWith(prefix, StringComparison.Ordinal)));

                    break;

                case "EndingWith" when TryGetSingleString(constraint, out string suffix):
                    tests.Add(($"EndingWith({Quote(suffix)})", value => value.EndsWith(suffix, StringComparison.Ordinal)));

                    break;

                case "Containing" when TryGetSingleString(constraint, out string fragment):
                    tests.Add(($"Containing({Quote(fragment)})", value => value.IndexOf(fragment, StringComparison.Ordinal) >= 0));

                    break;

                case "DifferentFrom" when TryGetSingleString(constraint, out string excluded):
                    tests.Add(($"DifferentFrom({Quote(excluded)})", value => !string.Equals(value, excluded, StringComparison.Ordinal)));

                    break;

                case "Except": {
                    List<string> excluded = ConstantArguments(constraint);
                    if (excluded.Count == 0) { break; }

                    tests.Add(($"Except({string.Join(", ", excluded.Select(Quote))})", value => !excluded.Contains(value, StringComparer.Ordinal)));

                    break;
                }
            }
        }

        return tests;
    }

    private static Func<string, bool> DrawnFrom(string pool) {
        return value => value.All(character => pool.IndexOf(character) >= 0);
    }

    /// <summary>The counterpart of <see cref="DrawnFrom" /> for a subtraction: no character of the removed family.</summary>
    private static Func<string, bool> DrawnNoneOf(string removed) {
        return value => value.All(character => removed.IndexOf(character) < 0);
    }

    /// <summary>
    ///     The constants of an <c>Except</c>-shaped call. Empty when any of them does not fold: a partial exclusion
    ///     list would render a constraint the caller never wrote, and judge against fewer values than are declared.
    /// </summary>
    private static List<string> ConstantArguments(IInvocationOperation constraint) {
        List<string> values = [];

        foreach (IOperation element in ValueSetFacts.ParamArrayElements(constraint)) {
            if (!ConstantFacts.TryGetString(element, out string value)) { return []; }

            values.Add(value);
        }

        return values;
    }

    private static bool TryGetSingleInt32(IInvocationOperation constraint, out int value) {
        value = 0;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out value);
    }

    private static bool TryGetSingleString(IInvocationOperation constraint, out string value) {
        value = string.Empty;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out value);
    }

    private static string Quote(string value) {
        return "\"" + value + "\"";
    }

}
