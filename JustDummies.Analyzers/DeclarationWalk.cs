using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace JustDummies.Analyzers;

/// <summary>
///     The walk JD034 and JD035 share: one chain's calls in the order the run time applies them, each compared with the
///     declarations before it. A constraint the generator takes once and a constraint whose repetition changes nothing
///     are both read here, so the two rules agree on what "the same declaration" means and on where a chain stops.
/// </summary>
/// <remarks>
///     <para>
///         "The same" means the same method and the same constant arguments, and, for a value set or an exclusion
///         list, the same values in any order (ADR-0100). Two readings follow the run time rather than the call: a
///         granularity is identified by its length, whatever <c>TimeSpan</c> spelling wrote it, and an offset by the
///         range it declares, so <c>WithOffset(x)</c> and <c>WithOffsetBetween(x, x)</c> are the same declaration. An
///         argument the compiler cannot resolve leaves the question open, and neither rule guesses.
///     </para>
///     <para>
///         The walk ends at the first call the run time refuses — a second, different declaration, or a call whose own
///         argument its guard rejects — because nothing after it is ever applied.
///     </para>
///     <para>
///         A <c>Distinct</c> comparer is left out: the run time compares comparers by reference, so no reading of the
///         source can tell a repetition from a conflict (issue #278). So are the bounds and their aliases, which fold
///         rather than repeat and belong to JD032 and JD024 (ADR-0078), and a collection's contained element, which
///         a second call requires twice.
///     </para>
/// </remarks>
internal static class DeclarationWalk {

    private const string Casing            = "casing";
    private const string FamilyOrPool      = "character family or pool";
    private const string PoolName          = "WithChars";
    private const string ValueSetName      = "OneOf";
    private const string ExclusionName     = "Except";
    private const string MultipleName      = "MultipleOf";
    private const string GranularityName   = "WithGranularity";
    private const string OffsetName        = "WithOffset";
    private const string OffsetRangeName   = "WithOffsetBetween";
    private const string ExactCount        = "exact count";
    private const string StringOwner       = "AnyString";
    private const string GuidOwner         = "AnyGuid";
    private const string LibraryNamespace  = "JustDummies";
    private const string PoolTakesNoCasing = "a WithChars pool completely defines the allowed characters and cannot be combined with a casing constraint";

    #region Statics members declarations

    /// <summary>
    ///     Every call that answers a declaration made before it, in chain order: each repetition, then at most one
    ///     conflict, after which nothing is applied.
    /// </summary>
    internal static IEnumerable<DeclarationFinding> Findings(IReadOnlyList<IInvocationOperation> constraints, Compilation compilation) {
        Dictionary<string, IInvocationOperation> slots       = [];
        List<IInvocationOperation>               repeatables = [];
        foreach (IInvocationOperation call in constraints.Where(constraint => IsLibraryMethod(constraint.TargetMethod))) {
            // The call throws on its own argument before it reads anything the chain declared.
            if (IsRefused(call, compilation)) { yield break; }

            if (SlotOf(call.TargetMethod) is not string slot) {
                foreach (DeclarationFinding repetition in AmongRepeatables(call, repeatables)) { yield return repetition; }

                continue;
            }

            if (InSlot(call, slot, slots) is not DeclarationFinding finding) { continue; }

            yield return finding;
            if (!finding.IsRepetition) { yield break; }
        }
    }

    /// <summary>
    ///     The refused call's name and argument list — <c>Numeric()</c> — rather than the whole chain up to it, which on
    ///     a long chain hides which declaration is meant.
    /// </summary>
    internal static Location LocationOf(IOperation call) {
        return call.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
                   ? Location.Create(invocation.SyntaxTree, TextSpan.FromBounds(access.Name.SpanStart, invocation.Span.End))
                   : call.Syntax.GetLocation();
    }

    /// <summary>
    ///     A call as the caller wrote it — its name and its argument list — the way the run time quotes it; a value as the
    ///     caller wrote it.
    /// </summary>
    internal static string Rendered(IOperation call) {
        return call switch {
            { Syntax: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation } => $"{access.Name}{invocation.ArgumentList}",
            IInvocationOperation invocation                                                                     => $"{invocation.TargetMethod.Name}(...)",
            _                                                                                                   => call.Syntax.ToString()
        };
    }

    private static bool IsLibraryMethod(IMethodSymbol method) {
        return method.ContainingNamespace?.ToDisplayString() == LibraryNamespace;
    }

    /// <summary>A call to a constraint the generator takes once, compared with the one already in its slot.</summary>
    private static DeclarationFinding? InSlot(IInvocationOperation call, string slot, Dictionary<string, IInvocationOperation> slots) {
        if (IsIgnored(call)) { return null; }

        string key = Key(call.TargetMethod, slot);
        if (slots.TryGetValue(key, out IInvocationOperation? first)) {
            return IsSameDeclaration(first, call) switch {
                true  => DeclarationFinding.Repeating(call, first),
                false => DeclarationFinding.Conflicting(call, first, $"the generator accepts only one {slot}"),
                null  => null
            };
        }

        if (RivalOf(call, slot, slots) is IInvocationOperation rival) { return DeclarationFinding.Conflicting(call, rival, PoolTakesNoCasing); }

        slots.Add(key, call);

        return null;
    }

    /// <summary>
    ///     A call whose repetition changes nothing, compared with the same calls before it — and, for an exclusion list,
    ///     each value compared with the ones before it in the same list (issue #280).
    /// </summary>
    private static IEnumerable<DeclarationFinding> AmongRepeatables(IInvocationOperation call, List<IInvocationOperation> repeatables) {
        if (!IsRepeatable(call.TargetMethod)) { yield break; }

        foreach (DeclarationFinding value in RepeatedValues(call)) { yield return value; }

        if (repeatables.FirstOrDefault(earlier => IsSameCall(earlier, call) == true) is IInvocationOperation repeated) {
            yield return DeclarationFinding.Repeating(call, repeated);
        } else {
            repeatables.Add(call);
        }
    }

    /// <summary>
    ///     Each value an <c>Except(...)</c> lists a second time, which removes nothing the first did not. A list with a
    ///     value that is not a constant is left alone, as a value set is: what it holds is not fully known.
    /// </summary>
    private static IEnumerable<DeclarationFinding> RepeatedValues(IInvocationOperation call) {
        if (call.TargetMethod.Name != ExclusionName || !ValueSetFacts.IsWrittenInline(call) || ValueSetFacts.ElementType(call.TargetMethod) is not { } elementType) { yield break; }

        List<(IOperation Element, object? Value)> elements = ValueSetFacts.Elements(call).Select(element => (element, ValueSetFacts.RunTimeValue(element, elementType))).ToList();
        if (elements.Any(element => element.Value is null)) { yield break; }

        Dictionary<object, IOperation> first = [];
        foreach ((IOperation element, object? value) in elements) {
            if (first.TryGetValue(value!, out IOperation? earlier)) {
                yield return DeclarationFinding.Repeating(element, earlier);
            } else {
                first.Add(value!, element);
            }
        }
    }

    /// <summary>
    ///     The once-per-generator constraint a library method declares, as the message names it, or <c>null</c> for
    ///     one that accumulates or folds instead — a bound, a contained value, a subtraction, an exclusion — and for
    ///     the comparer left out (see the remarks on the class).
    /// </summary>
    private static string? SlotOf(IMethodSymbol method) {
        string owner = method.ContainingType.Name;

        return method.Name switch {
            ValueSetName                                              => "value set",
            "WithLength"                                              => "exact length",
            "StartingWith"                                            => "prefix",
            "EndingWith"                                              => "suffix",
            "InUpperCase" or "InLowerCase"                            => Casing,
            PoolName                                                  => FamilyOrPool,
            _ when CharacterFamilies.PoolFor(method.Name) is not null => owner == StringOwner ? FamilyOrPool : "character family",
            MultipleName                                              => "MultipleOf constraint",
            "WithScale"                                               => "scale",
            GranularityName                                           => "granularity",
            OffsetName or OffsetRangeName                             => "offset",
            "WithCount"                                               => ExactCount,
            "Empty" when owner is "AnyCollection" or "AnyDictionary"  => ExactCount,
            "UsingHttp" or "UsingHttps" or "UsingWs" or "UsingWss"    => "scheme",
            "WithHost"                                                => "host",
            "WithDomain"                                              => "domain",
            "WithUserInfo"                                            => "user info",
            "WithLocalPart"                                           => "local part",
            "WithPort"                                                => "port",
            "WithPathSegments" or "WithoutPath"                       => "path",
            _                                                         => null
        };
    }

    /// <summary>
    ///     Whether the library method declares something a second identical call leaves as it was, other than a
    ///     constraint the generator takes once: an exclusion, a subtraction, a string's contained fragment, a toggle.
    ///     Each was checked against the run time; a collection's <c>Containing</c> is not one of them, since a second
    ///     call requires the element twice.
    /// </summary>
    private static bool IsRepeatable(IMethodSymbol method) {
        string owner = method.ContainingType.Name;

        return method.Name switch {
            ExclusionName or "DifferentFrom" or "NonZero"                  => true,
            "WithoutAlpha" or "WithoutNumeric" or "NotBlank"               => true,
            "AllowingCombinations" or "True" or "False"                    => true,
            "WithQuery" or "WithFragment" or "WithHeaders" or "Rooted"     => true,
            "Distinct"                                                     => method.Parameters.Length == 0,
            "Containing"                                                   => owner == StringOwner,
            "Empty" or "NonEmpty"                                          => owner == GuidOwner,
            _                                                              => false
        };
    }

    /// <summary>The slot a call fills, on the generator type that declares it.</summary>
    private static string Key(IMethodSymbol method, string slot) {
        return $"{method.ContainingType.Name}.{slot}";
    }

    /// <summary>
    ///     Whether the call rejects its own argument — a size, a step, a scale or a port out of range, an empty text, an
    ///     empty value set, the guards JD014 reports; a <c>null</c>, a URI component or a value set element its guard
    ///     refuses (<see cref="ArgumentGuardFacts" />); a granularity that is not strictly positive, an offset that is
    ///     not whole minutes within ±14:00. The guard runs before the call reads what the chain declared.
    /// </summary>
    private static bool IsRefused(IInvocationOperation call, Compilation compilation) {
        if (RejectedConstantArgumentAnalyzer.TryFindViolation(call, compilation, out _, out _) || ArgumentGuardFacts.MayRefuse(call)) { return true; }

        return call.TargetMethod.Name switch {
            GranularityName               => Granularity(call) <= 0,
            OffsetName or OffsetRangeName => OffsetRange(call) is { } range && !IsValidOffset(range),
            _                             => false
        };
    }

    /// <summary>
    ///     Whether the call narrows nothing, so the run time ignores it without filling its slot: <c>MultipleOf(1)</c>
    ///     or a granularity of one tick, since every value is on that lattice.
    /// </summary>
    private static bool IsIgnored(IInvocationOperation call) {
        return call.TargetMethod.Name switch {
            MultipleName    => Step(call) == 1,
            GranularityName => Granularity(call) == 1,
            _               => false
        };
    }

    /// <summary>The step a <c>MultipleOf(...)</c> declares, or <c>null</c> if it is not a constant.</summary>
    private static decimal? Step(IInvocationOperation call) {
        return ConstantArguments(call) is { Count: 1 } arguments && arguments[0] is IConvertible step ? step.ToDecimal(CultureInfo.InvariantCulture) : null;
    }

    /// <summary>
    ///     The declaration a casing or a <c>WithChars</c> pool cannot stand beside: the pool is the whole character
    ///     definition, so the run time rejects a casing with it in either order.
    /// </summary>
    private static IInvocationOperation? RivalOf(IInvocationOperation call, string slot, IReadOnlyDictionary<string, IInvocationOperation> slots) {
        if (call.TargetMethod.ContainingType.Name != StringOwner) { return null; }

        if (slot == Casing && slots.TryGetValue($"{StringOwner}.{FamilyOrPool}", out IInvocationOperation? family) && family.TargetMethod.Name == PoolName) { return family; }
        if (call.TargetMethod.Name == PoolName && slots.TryGetValue($"{StringOwner}.{Casing}", out IInvocationOperation? casing)) { return casing; }

        return null;
    }

    /// <summary>
    ///     Whether two calls in one slot declare the same thing — <c>true</c> for a repetition the run time ignores,
    ///     <c>false</c> for a second declaration it rejects, <c>null</c> where an argument the compiler cannot resolve
    ///     leaves it open.
    /// </summary>
    private static bool? IsSameDeclaration(IInvocationOperation first, IInvocationOperation second) {
        // Two methods declare an offset, and the run time compares the range they declare, not the call.
        if (first.TargetMethod.Name is OffsetName or OffsetRangeName) { return AreEqual(OffsetRange(first), OffsetRange(second)); }

        return first.TargetMethod.Name == GranularityName && second.TargetMethod.Name == GranularityName
                   ? AreEqual(Granularity(first), Granularity(second))
                   : IsSameCall(first, second);
    }

    /// <summary>
    ///     Whether two calls are the same call — the same method, and the same constant arguments, or the same values in
    ///     any order for a value set or an exclusion list — or <c>null</c> where an argument leaves it open.
    /// </summary>
    private static bool? IsSameCall(IInvocationOperation first, IInvocationOperation second) {
        if (first.TargetMethod.Name != second.TargetMethod.Name) { return false; }
        if (first.TargetMethod.Name is ValueSetName or ExclusionName) { return ValueSet(first) is { } before && ValueSet(second) is { } after ? before.SetEquals(after) : null; }

        return ConstantArguments(first) is { } written && ConstantArguments(second) is { } rewritten ? written.SequenceEqual(rewritten) : null;
    }

    /// <summary>Whether two readings are equal, or <c>null</c> if either could not be read.</summary>
    private static bool? AreEqual<T>(T? before, T? after) where T : struct {
        return before.HasValue && after.HasValue ? before.Value.Equals(after.Value) : null;
    }

    /// <summary>The constant value of every argument, in parameter order, or <c>null</c> if any is not a constant.</summary>
    private static List<object?>? ConstantArguments(IInvocationOperation call) {
        List<Optional<object?>> constants = InParameterOrder(call).Select(argument => argument.Value.ConstantValue).ToList();

        return constants.All(constant => constant.HasValue) ? constants.Select(constant => constant.Value).ToList() : null;
    }

    /// <summary>
    ///     The call's arguments in parameter order: Roslyn lists them in evaluation order, which named arguments can
    ///     change without changing the call.
    /// </summary>
    private static IEnumerable<IArgumentOperation> InParameterOrder(IInvocationOperation call) {
        return call.Arguments.OrderBy(argument => argument.Parameter?.Ordinal ?? int.MaxValue);
    }

    /// <summary>The ticks of the granularity a <c>WithGranularity(...)</c> declares, or <c>null</c> if it cannot be read.</summary>
    private static long? Granularity(IInvocationOperation call) {
        return call.Arguments.Length == 1 ? Ticks(call.Arguments[0]) : null;
    }

    /// <summary>The ticks of a <c>TimeSpan</c> argument, or <c>null</c> if it cannot be read.</summary>
    private static long? Ticks(IArgumentOperation argument) {
        return ConstantFacts.TryGetTimeSpanTicks(argument.Value, out long ticks) ? ticks : null;
    }

    /// <summary>
    ///     The range of offsets, in ticks, a <c>WithOffset(...)</c> or a <c>WithOffsetBetween(...)</c> declares — one
    ///     offset is the range from it to itself, as the run time records it — or <c>null</c> if an argument cannot be
    ///     read.
    /// </summary>
    private static (long Minimum, long Maximum)? OffsetRange(IInvocationOperation call) {
        long?[] offsets = InParameterOrder(call).Select(Ticks).ToArray();
        if (offsets.Length is 0 or > 2 || offsets.Any(offset => offset is null)) { return null; }

        return (offsets[0]!.Value, offsets[offsets.Length - 1]!.Value);
    }

    /// <summary>Whether the run time accepts the range: whole minutes, within ±14:00, the minimum not after the maximum.</summary>
    private static bool IsValidOffset((long Minimum, long Maximum) range) {
        const long limit = 14 * TimeSpan.TicksPerHour;

        return range.Minimum % TimeSpan.TicksPerMinute == 0 && range.Maximum % TimeSpan.TicksPerMinute == 0
            && range.Minimum >= -limit && range.Maximum <= limit && range.Minimum <= range.Maximum;
    }

    /// <summary>
    ///     The values a <c>OneOf(...)</c> or an <c>Except(...)</c> lists inline — as arguments, an array or a collection
    ///     expression — as a set of values of the generator's own type: the run time identifies a value set by its
    ///     values, ignoring order and duplicates (ADR-0100), so <c>OneOf(1.0)</c> and <c>OneOf([1])</c> on a
    ///     <c>double</c> are the same set. <c>null</c> where a collection held elsewhere, a spread or a value that is not
    ///     a constant leaves it unknown.
    /// </summary>
    private static HashSet<object>? ValueSet(IInvocationOperation call) {
        if (!ValueSetFacts.IsWrittenInline(call) || ValueSetFacts.ElementType(call.TargetMethod) is not { } elementType) { return null; }

        HashSet<object> values = [];
        foreach (IOperation element in ValueSetFacts.Elements(call)) {
            if (ValueSetFacts.RunTimeValue(element, elementType) is not { } value) { return null; }

            values.Add(value);
        }

        return values.Count > 0 ? values : null;
    }

    #endregion

}
