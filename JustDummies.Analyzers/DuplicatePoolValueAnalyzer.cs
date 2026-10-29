using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace JustDummies.Analyzers;

/// <summary>
///     JD025 — reports a constant listed twice in the same pool. <c>Any.OneOf(a, b, a)</c> is deduplicated when the
///     generator is built, so the pool is one value smaller than it reads, and nothing anywhere says so. A string's
///     <c>WithChars(...)</c> pool is deduplicated the same way, so a character written twice is read too.
/// </summary>
/// <remarks>
///     Weighting is the reading this rule exists to refuse: listing a value twice looks like "draw this one more
///     often", and the library declines to weight a pool on purpose. The consequence surfaces somewhere else
///     entirely — a distinct collection over the pool gates against the real distinct count and names a number the
///     author cannot find in their source.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicatePoolValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DuplicatePoolValue);

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

        if (invocation.TargetMethod.Name is not ("OneOf" or "ElementOf" or "WithChars")) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out _, out IInvocationOperation? factory) || factory is null) { return; }

        Location? duplicate = invocation.TargetMethod.Name == "WithChars" ? RepeatedCharacter(invocation) : RepeatedValue(invocation);
        if (duplicate is not null) { context.ReportDiagnostic(Diagnostic.Create(Descriptors.DuplicatePoolValue, duplicate)); }
    }

    /// <summary>
    ///     Where the first value listed a second time sits in a <c>OneOf(...)</c> or an <c>ElementOf(...)</c> written
    ///     inline — as arguments, an array or a collection expression — or <c>null</c> when there is none.
    /// </summary>
    private static Location? RepeatedValue(IInvocationOperation invocation) {
        // A pool held in a variable or built by a query is not this rule's business: only the values written at the call
        // site are visible.
        if (!ValueSetFacts.IsWrittenInline(invocation)) { return null; }

        Type? elementType = ValueSetFacts.ElementType(invocation.TargetMethod);
        HashSet<object?> seen = [];
        foreach (IOperation element in ValueSetFacts.Elements(invocation)) {
            // One unfoldable element and the pool stops being knowable: a later duplicate of THAT value would go
            // unseen, and reporting the ones this walk can see would claim a completeness the walk does not have.
            if (!element.ConstantValue.HasValue) { return null; }

            // The value the run time compares: a collection expression's element is read before its conversion to the
            // pool's element type, which is what decides whether two literals written differently are the same value.
            object? value = elementType is not null && element.ConstantValue.Value is not null ? ValueSetFacts.RunTimeValue(element, elementType) : element.ConstantValue.Value;
            if (!seen.Add(value)) { return element.Syntax.GetLocation(); }
        }

        return null;
    }

    /// <summary>
    ///     Where the first character listed a second time sits in a <c>WithChars(...)</c> pool, which is deduplicated just
    ///     as a value set is (issue #280), or <c>null</c> when there is none.
    /// </summary>
    private static Location? RepeatedCharacter(IInvocationOperation invocation) {
        if (invocation.TargetMethod.ContainingType.Name != "AnyString" || invocation.Arguments.Length != 1) { return null; }

        IOperation argument = invocation.Arguments[0].Value;
        if (argument.ConstantValue is not { HasValue: true, Value: string pool }) { return null; }

        for (int index = 1; index < pool.Length; index++) {
            if (pool.IndexOf(pool[index]) < index) { return CharacterLocation(argument, index); }
        }

        return null;
    }

    /// <summary>
    ///     The repeated character itself when the pool is a plain string literal, whose characters sit one per column;
    ///     the whole argument otherwise — an escape, a verbatim or raw literal, or a constant declared elsewhere.
    /// </summary>
    private static Location CharacterLocation(IOperation argument, int index) {
        if (argument.Syntax is LiteralExpressionSyntax { Token: SyntaxToken token } && token.Text.Length == token.ValueText.Length + 2 && token.Text[0] == '"') {
            return Location.Create(argument.Syntax.SyntaxTree, new TextSpan(token.SpanStart + 1 + index, 1));
        }

        return argument.Syntax.GetLocation();
    }

}
