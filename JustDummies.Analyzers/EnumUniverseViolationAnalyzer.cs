using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD017 — reports an enum constraint naming a value the type does not define. <c>Any.Enum&lt;T&gt;()</c> yields
///     a <b>declared</b> member, or on a <c>[Flags]</c> enum a combination of declared members where <c>OneOf</c>
///     names one; an undeclared numeric value the CLR would still let you write is never among them.
/// </summary>
/// <remarks>
///     Kept apart from the interval rules because the domain is metadata — what the type declares — rather than
///     arithmetic, and because the mistake has its own teachable model: the generator yields values the type
///     defines, so a value that is not one is not a narrowing but a category error.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EnumUniverseViolationAnalyzer : DiagnosticAnalyzer {

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
        if (ReportedAValueTheTypeDoesNotDefine(context, constraints, declared, enumType)) { return; }

        ReportAnExclusionNothingSurvives(context, invocation, constraints, declared, enumType);
    }

    /// <summary>
    ///     Reports the first constrained value the enum type does not define, and whether it did.
    /// </summary>
    /// <remarks>
    ///     On a <see cref="FlagsAttribute">[Flags]</see> enum a combination of declared members is defined even
    ///     without the opt-in, because writing one is asking for it — which is what the generator accepts.
    /// </remarks>
    private static bool ReportedAValueTheTypeDoesNotDefine(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints,
                                                          HashSet<object?> declared, INamedTypeSymbol enumType) {
        bool isFlags = enumType.GetAttributes().Any(IsFlagsAttribute);

        foreach ((string _, IOperation value, object? constant) in ConstrainedValues(constraints)) {
            if (declared.Contains(constant)) { continue; }
            if (isFlags && IsCombinationOfDeclared(constant, declared)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.EnumUniverseViolation, value.Syntax.GetLocation(),
                $"{constant} is {(isFlags ? $"neither a declared member of {enumType.Name} nor a combination of its declared members" : $"not a declared member of {enumType.Name}")}"));

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Reports a set of exclusions that leaves the generator nothing to draw.
    /// </summary>
    /// <remarks>
    ///     An allow-list is the pool, so the declared members it does not name were never going to be drawn: what
    ///     decides is whether anything the caller allowed survives, not whether the declared set was emptied — and
    ///     an allow-list holding one entry the rule cannot read is a pool it cannot decide, not an absent one.
    /// </remarks>
    private static void ReportAnExclusionNothingSurvives(OperationAnalysisContext context, IInvocationOperation invocation,
                                                        IReadOnlyList<IInvocationOperation> constraints, HashSet<object?> declared,
                                                        INamedTypeSymbol enumType) {
        HashSet<object?> excluded = [];
        HashSet<object?> allowed  = [];
        foreach ((string name, IOperation _, object? constant) in ConstrainedValues(constraints)) {
            if (name is "Except" or "DifferentFrom") { excluded.Add(constant); }
            if (name is "OneOf") { allowed.Add(constant); }
        }

        if (excluded.Count == 0 || !declared.All(excluded.Contains)) { return; }
        if (NamesAValueItCannotRead(constraints, "OneOf")) { return; }
        if (allowed.Count > 0 && allowed.Any(value => !excluded.Contains(value))) { return; }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.EnumUniverseViolation, invocation.Syntax.GetLocation(),
            $"no declared {enumType.Name} member remains once every exclusion is applied"));
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
    ///     Whether <paramref name="constant" /> can be obtained by OR-ing declared members — <c>Read | Write</c> yes,
    ///     <c>99</c> no. Mirrors <c>AnyEnum.IsCombinationOfDeclaredMembers</c>, which is what the generator applies.
    /// </summary>
    /// <remarks>
    ///     Decided arithmetically rather than by enumerating the closure: OR-ing every declared member whose bits the
    ///     value already carries gives the largest reachable value within it, so the value is reachable exactly when
    ///     that OR is the value itself. A member the rule cannot read as bits makes the answer <c>false</c>, which
    ///     only ever leaves the value to be reported as before — the rule never invents a permission it cannot prove.
    /// </remarks>
    private static bool IsCombinationOfDeclared(object? constant, HashSet<object?> declared) {
        if (!TryBits(constant, out ulong bits)) { return false; }
        // The empty combination is a value only where the enum declares it.
        if (bits == 0UL) { return declared.Any(member => TryBits(member, out ulong zero) && zero == 0UL); }

        ulong reachable = 0UL;
        foreach (object? member in declared) {
            if (!TryBits(member, out ulong memberBits) || memberBits == 0UL) { continue; }
            if ((memberBits & bits) == memberBits) { reachable |= memberBits; }
        }

        return reachable == bits;
    }

    /// <summary>The constant's underlying bits, whatever the enum's underlying type — signed members included.</summary>
    private static bool TryBits(object? constant, out ulong bits) {
        // Reinterpreted at each signed width rather than converted, exactly as the runtime stores a negative member.
        switch (constant) {
            case sbyte value: bits  = unchecked((ulong)value); return true;
            case short value: bits  = unchecked((ulong)value); return true;
            case int value: bits    = unchecked((ulong)value); return true;
            case long value: bits   = unchecked((ulong)value); return true;
            case byte value: bits   = value; return true;
            case ushort value: bits = value; return true;
            case uint value: bits   = value; return true;
            case ulong value: bits  = value; return true;
            default: bits           = 0UL; return false;
        }
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
            if (name is not ("OneOf" or "Except" or "DifferentFrom")) { continue; }

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

    private static bool IsFlagsAttribute(AttributeData attribute) {
        return attribute.AttributeClass?.Name == "FlagsAttribute";
    }

}
