using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD034 — reports an <c>Any.String()</c> chain that declares a constraint the generator takes once a second
///     time with a different call: an exact length, a prefix, a suffix, a character family or pool, a casing or a
///     value set — or a casing beside a <c>WithChars</c> pool, which cannot combine with one.
/// </summary>
/// <remarks>
///     <para>
///         The run time refuses each of these at the call, on every seed, with a message saying the first
///         declaration is already defined. It refuses them whatever the values would make of them —
///         <c>Alpha().AlphaNumeric()</c> is refused although every letter satisfies both — so this is a conflict of
///         declaration, not of values, and it is not <c>JD015</c>'s: that rule reports constants that admit no value
///         (issue #274).
///     </para>
///     <para>
///         The same call repeated is a no-op the run time ignores, so it is not reported: "the same" means the same
///         method and the same constant arguments, and, for a value set, the same values in any order (ADR-0100). An
///         argument the compiler cannot resolve leaves that question open, and the rule stays silent rather than
///         guess.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstraintAlreadyDefinedAnalyzer : DiagnosticAnalyzer {

    private const string Casing            = "casing";
    private const string FamilyOrPool      = "character family or pool";
    private const string PoolName          = "WithChars";
    private const string ValueSetName      = "OneOf";
    private const string PoolTakesNoCasing = "a WithChars pool is the whole character definition, so it takes no casing";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.ConstraintAlreadyDefined);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.Any is null || symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        // Analyse each chain once, from its outermost call.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "String") { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        AnalyzeConstraints(context, constraints);
    }

    /// <summary>
    ///     Walks the chain in the order the run time applies it and reports the first call it would refuse as already
    ///     defined — the one its message names first. Nothing after that call is ever applied.
    /// </summary>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints) {
        Dictionary<string, IInvocationOperation> slots = [];
        foreach (IInvocationOperation call in constraints) {
            if (SlotOf(call.TargetMethod.Name) is not string slot) { continue; }

            if (slots.TryGetValue(slot, out IInvocationOperation? first)) {
                if (IsSameDeclaration(first, call) == false) {
                    Report(context, call, first, $"a string takes one {slot}");

                    return;
                }

                continue;
            }

            if (RivalOf(call, slot, slots) is IInvocationOperation rival) {
                Report(context, call, rival, PoolTakesNoCasing);

                return;
            }

            slots.Add(slot, call);
        }
    }

    /// <summary>
    ///     The once-per-generator constraint a method declares, as the message names it, or <c>null</c> for one that
    ///     accumulates or folds instead — a bound, a contained value, a subtraction, an exclusion.
    /// </summary>
    private static string? SlotOf(string name) {
        return name switch {
            "WithLength"                                       => "exact length",
            "StartingWith"                                     => "prefix",
            "EndingWith"                                       => "suffix",
            "InUpperCase" or "InLowerCase"                     => Casing,
            ValueSetName                                       => "value set",
            PoolName                                           => FamilyOrPool,
            _ when CharacterFamilies.PoolFor(name) is not null => FamilyOrPool,
            _                                                  => null
        };
    }

    /// <summary>
    ///     The declaration a casing or a <c>WithChars</c> pool cannot stand beside: the pool is the whole character
    ///     definition, so the run time refuses a casing with it in either order.
    /// </summary>
    private static IInvocationOperation? RivalOf(IInvocationOperation call, string slot, IReadOnlyDictionary<string, IInvocationOperation> slots) {
        if (slot == Casing && slots.TryGetValue(FamilyOrPool, out IInvocationOperation? family) && family.TargetMethod.Name == PoolName) { return family; }
        if (call.TargetMethod.Name == PoolName && slots.TryGetValue(Casing, out IInvocationOperation? casing)) { return casing; }

        return null;
    }

    /// <summary>
    ///     Whether two calls in one slot declare the same thing — <c>true</c> for a repetition the run time ignores,
    ///     <c>false</c> for a second declaration it refuses, <c>null</c> where an argument the compiler cannot resolve
    ///     leaves it open.
    /// </summary>
    private static bool? IsSameDeclaration(IInvocationOperation first, IInvocationOperation second) {
        if (first.TargetMethod.Name != second.TargetMethod.Name) { return false; }
        if (first.TargetMethod.Name == ValueSetName) { return ValueSet(first) is { } before && ValueSet(second) is { } after ? before.SetEquals(after) : null; }

        return ConstantArguments(first) is { } written && ConstantArguments(second) is { } rewritten ? written.SequenceEqual(rewritten) : null;
    }

    /// <summary>The constant value of every argument, in parameter order, or <c>null</c> if any is not a constant.</summary>
    private static List<object?>? ConstantArguments(IInvocationOperation call) {
        List<Optional<object?>> constants = call.Arguments.Select(argument => argument.Value.ConstantValue).ToList();

        return constants.All(constant => constant.HasValue) ? constants.Select(constant => constant.Value).ToList() : null;
    }

    /// <summary>
    ///     The values a <c>OneOf(...)</c> lists inline, as a set — the run time identifies a value set by its values,
    ///     ignoring order and duplicates (ADR-0100) — or <c>null</c> where a collection held elsewhere or a value that
    ///     is not a constant leaves it unknown.
    /// </summary>
    private static HashSet<string>? ValueSet(IInvocationOperation call) {
        if (call.Arguments.Any(argument => argument.ArgumentKind != ArgumentKind.ParamArray
                                        && GeneratorFacts.Unwrap(argument.Value) is not IArrayCreationOperation { Initializer: not null })) {
            return null;
        }

        HashSet<string> values = new(StringComparer.Ordinal);
        foreach (IOperation element in ValueSetFacts.Elements(call)) {
            if (!ConstantFacts.TryGetString(element, out string value)) { return null; }

            values.Add(value);
        }

        return values.Count > 0 ? values : null;
    }

    /// <summary>
    ///     Reports the call the run time refuses, on the chain up to it — where its exception is thrown — naming the
    ///     declaration already in place as the run time does.
    /// </summary>
    private static void Report(OperationAnalysisContext context, IInvocationOperation refused, IInvocationOperation defined, string reason) {
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.ConstraintAlreadyDefined, refused.Syntax.GetLocation(), Rendered(refused), Rendered(defined), reason));
    }

    /// <summary>A call as the caller wrote it — its name and its argument list — the way the run time quotes it.</summary>
    private static string Rendered(IInvocationOperation call) {
        return call.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
                   ? $"{access.Name}{invocation.ArgumentList}"
                   : $"{call.TargetMethod.Name}(...)";
    }

}
