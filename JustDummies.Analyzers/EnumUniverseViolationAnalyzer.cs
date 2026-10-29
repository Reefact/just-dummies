using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD017 — reports an allow-list naming a value the enum type does not define, and exclusions that leave nothing
///     to draw. <c>Any.Enum&lt;T&gt;()</c> yields a <b>declared</b> member, or on a <c>[Flags]</c> enum a combination
///     of declared members where <c>OneOf</c> names one; an undeclared numeric value the CLR would still let you write
///     is never among them.
/// </summary>
/// <remarks>
///     Kept apart from the interval rules because the domain is metadata — what the type declares — rather than
///     arithmetic, and because the mistake has its own teachable model: the generator yields values the type
///     defines, so a value that is not one is not a narrowing but a category error.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumUniverseViolationAnalyzer : DiagnosticAnalyzer {

    private const string OneOfMethodName = "OneOf";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.EnumUniverseViolation);

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

        if (symbols.IsJustDummiesMember(invocation.TargetMethod)) { ReportAValueTheTypeDoesNotDefine(context, invocation); }

        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "Enum") { return; }
        if (factory.TargetMethod.TypeArguments.Length != 1 || factory.TargetMethod.TypeArguments[0] is not INamedTypeSymbol enumType) { return; }
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        HashSet<object?> declared = [.. enumType.GetMembers()
                                                .OfType<IFieldSymbol>()
                                                .Where(field => field.HasConstantValue)
                                                .Select(field => field.ConstantValue)];

        if (declared.Count == 0) { return; }

        // AllowingCombinations widens the universe to the OR-closure of the declared members, which no longer matches
        // a declared value one for one — so both rules below stand down rather than approximate it.
        if (constraints.Any(constraint => constraint.TargetMethod.Name == "AllowingCombinations")) { return; }
        // A value the type does not define is reported on its own OneOf, above; the chain is not judged twice.
        if (constraints.Any(constraint => ArgumentGuardFacts.TryFindUndefinedEnumElement(constraint, out _, out _))) { return; }

        ReportAnExclusionNothingSurvives(context, invocation, constraints, declared, enumType);
    }

    /// <summary>
    ///     Reports the first value a <c>OneOf</c> on <c>Any.Enum&lt;T&gt;()</c> names that the enum type does not define.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Judged on the <c>OneOf</c> call itself, wherever its receiver comes from, with the reading of the guard
    ///         JD014 uses for every other value set: this rule is the one that reports the membership, and JD014 stands
    ///         down on it, so one mistake draws one diagnostic (ADR-0078, issue #321). <c>AllowingCombinations</c> does
    ///         not change the verdict — <c>OneOf</c> refuses a value no combination produces either way.
    ///     </para>
    ///     <para>
    ///         On a <see cref="FlagsAttribute">[Flags]</see> enum a combination of declared members is defined even
    ///         without the opt-in, because writing one is asking for it — which is what the generator accepts.
    ///     </para>
    ///     <para>
    ///         Only <c>OneOf</c> is judged. An exclusion of a value the type does not define removes nothing the
    ///         generator could draw, and the library accepts it as a no-op — the reading ADR-0101 gives a non-finite
    ///         exclusion on a floating-point builder. Reporting it would refuse at build time what the run time honours.
    ///     </para>
    /// </remarks>
    private static void ReportAValueTheTypeDoesNotDefine(OperationAnalysisContext context, IInvocationOperation invocation) {
        if (!ArgumentGuardFacts.TryFindUndefinedEnumElement(invocation, out IOperation? offending, out string? reason)) { return; }
        if (NegativeTestGuard.IsLinkOfSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.EnumUniverseViolation, offending!.Syntax.GetLocation(), reason));
    }

    /// <summary>
    ///     Reports a set of exclusions that leaves the generator nothing to draw.
    /// </summary>
    /// <remarks>
    ///     An allow-list is the pool, so the declared members it does not name were never going to be drawn: what
    ///     decides is whether anything the caller allowed survives, not whether the declared set was emptied. With an
    ///     allow-list the pool alone is weighed, however many declared members lie outside it; without one, the
    ///     declared set is the pool. An allow-list holding one entry the rule cannot read is a pool it cannot decide,
    ///     not an absent one.
    /// </remarks>
    private static void ReportAnExclusionNothingSurvives(OperationAnalysisContext context, IInvocationOperation invocation,
                                                        IReadOnlyList<IInvocationOperation> constraints, HashSet<object?> declared,
                                                        INamedTypeSymbol enumType) {
        HashSet<object?> excluded = [];
        HashSet<object?> allowed  = [];
        foreach ((string name, IOperation _, object? constant) in ConstrainedValues(constraints)) {
            if (name is "Except" or "DifferentFrom") { excluded.Add(constant); }
            if (name is OneOfMethodName) { allowed.Add(constant); }
        }

        if (excluded.Count == 0) { return; }
        if (NamesAValueItCannotRead(constraints, OneOfMethodName)) { return; }

        HashSet<object?> pool = allowed.Count > 0 ? allowed : declared;
        if (pool.Any(value => !excluded.Contains(value))) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EnumUniverseViolation, invocation.Syntax.GetLocation(),
            allowed.Count > 0
                ? $"no {enumType.Name} value the allow-list names remains once every exclusion is applied"
                : $"no declared {enumType.Name} member remains once every exclusion is applied"));
    }

    /// <summary>
    ///     Whether <paramref name="name" /> was given an argument the rule cannot read as a constant.
    /// </summary>
    /// <remarks>
    ///     <see cref="ConstrainedValues" /> skips such an argument, which is the safe direction for an exclusion —
    ///     a smaller excluded set only ever reports less. An allow-list is the other way round: it <b>is</b> the
    ///     pool, so a skipped entry makes the surviving pool smaller than it really is, and on a <c>[Flags]</c> enum
    ///     that entry may hold a combination no exclusion names — one the generator draws.
    /// </remarks>
    private static bool NamesAValueItCannotRead(IReadOnlyList<IInvocationOperation> constraints, string name) {
        foreach (IInvocationOperation constraint in constraints) {
            if (constraint.TargetMethod.Name != name) { continue; }

            foreach (IOperation value in ConstantArguments(constraint)) {
                if (!value.ConstantValue.HasValue) { return true; }
            }
        }

        return false;
    }

    /// <summary>
    ///     The constant values the universe rules reason about, each paired with the constraint that declared it —
    ///     <c>OneOf</c>, <c>Except</c> and <c>DifferentFrom</c>, in the order they were written, and only where the
    ///     argument is a constant the rule can compare against a declared member.
    /// </summary>
    /// <remarks>
    ///     Flattened here rather than inline so the rule states what it does <i>with</i> a value without also spelling
    ///     out how to reach one: which constraints carry values, how a <c>params</c> array unfolds, and that a
    ///     non-constant argument is skipped are all one concern, and it is not the universe check.
    /// </remarks>
    private static IEnumerable<(string Name, IOperation Value, object? Constant)> ConstrainedValues(IReadOnlyList<IInvocationOperation> constraints) {
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (name is not (OneOfMethodName or "Except" or "DifferentFrom")) { continue; }

            foreach (IOperation value in ConstantArguments(constraint)) {
                Optional<object?> constant = value.ConstantValue;
                if (constant.HasValue) { yield return (name, value, constant.Value); }
            }
        }
    }

    private static IEnumerable<IOperation> ConstantArguments(IInvocationOperation constraint) {
        foreach (IArgumentOperation argument in constraint.Arguments) {
            if (argument.ArgumentKind == ArgumentKind.ParamArray) {
                if (argument.Value is IArrayCreationOperation { Initializer: { } initializer }) {
                    foreach (IOperation element in initializer.ElementValues) { yield return GeneratorFacts.Unwrap(element); }
                }

                continue;
            }

            yield return GeneratorFacts.Unwrap(argument.Value);
        }
    }

}
