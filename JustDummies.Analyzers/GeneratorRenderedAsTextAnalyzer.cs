using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD005 — reports a generator rendered as text instead of the value it would draw. No JustDummies generator
///     overrides <see cref="object.ToString" />, so an interpolation hole, a string concatenation or an explicit
///     <c>ToString()</c> over a recipe yields the builder's type name — the literal text <c>"JustDummies.AnyString"</c>
///     — which is non-empty, plausible, constant on every run, and flows into the assertion as if it were a value.
///     The concatenation includes <c>+=</c>, and the interpolation includes an interpolated expression passed to an
///     interpolated-string handler, as in <c>StringBuilder.Append($"...")</c> and <c>Debug.Assert(condition, $"...")</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GeneratorRenderedAsTextAnalyzer : DiagnosticAnalyzer {

    private const string ToStringMethodName = "ToString";

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.GeneratorRenderedAsText);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        INamedTypeSymbol iAny = symbols.IAny;

        context.RegisterOperationAction(operationContext => AnalyzeInterpolation(operationContext, iAny), OperationKind.Interpolation);
        context.RegisterOperationAction(operationContext => AnalyzeHandlerHole(operationContext, iAny), OperationKind.InterpolatedStringAppendFormatted);
        context.RegisterOperationAction(operationContext => AnalyzeConcatenation(operationContext, iAny), OperationKind.Binary);
        context.RegisterOperationAction(operationContext => AnalyzeAppendingAssignment(operationContext, iAny), OperationKind.CompoundAssignment);
        context.RegisterOperationAction(operationContext => AnalyzeToString(operationContext, iAny), OperationKind.Invocation);
    }

    private static void AnalyzeInterpolation(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IInterpolationOperation interpolation = (IInterpolationOperation)context.Operation;
        IOperation              expression    = GeneratorFacts.Unwrap(interpolation.Expression);

        Report(context, expression, iAny);
    }

    private static void AnalyzeHandlerHole(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        // An interpolated string converted to a handler type lowers each hole to an AppendFormatted(value, ...) call
        // rather than to an interpolation operation; the value is the call's first parameter.
        IInterpolatedStringAppendOperation append = (IInterpolatedStringAppendOperation)context.Operation;
        if (append.AppendCall is not IInvocationOperation call) { return; }

        IArgumentOperation? value = call.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
        if (value is null) { return; }

        Report(context, GeneratorFacts.Unwrap(value.Value), iAny);
    }

    private static void AnalyzeConcatenation(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IBinaryOperation binary = (IBinaryOperation)context.Operation;

        if (binary.OperatorKind != BinaryOperatorKind.Add) { return; }
        if (binary.Type?.SpecialType != SpecialType.System_String) { return; }

        Report(context, GeneratorFacts.Unwrap(binary.LeftOperand), iAny);
        Report(context, GeneratorFacts.Unwrap(binary.RightOperand), iAny);
    }

    private static void AnalyzeAppendingAssignment(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        ICompoundAssignmentOperation assignment = (ICompoundAssignmentOperation)context.Operation;

        if (assignment.OperatorKind != BinaryOperatorKind.Add) { return; }
        if (assignment.Type?.SpecialType != SpecialType.System_String) { return; }

        Report(context, GeneratorFacts.Unwrap(assignment.Value), iAny);
    }

    private static void AnalyzeToString(OperationAnalysisContext context, INamedTypeSymbol iAny) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol        method     = invocation.TargetMethod;

        if (method.Name != ToStringMethodName || !method.Parameters.IsEmpty) { return; }

        // Only the inherited object.ToString() is a defect. A consumer's own generator that meaningfully overrides
        // ToString() resolves to its own override, and is deliberately left alone.
        if (method.ContainingType?.SpecialType != SpecialType.System_Object) { return; }
        if (invocation.Instance is null) { return; }

        Report(context, GeneratorFacts.Unwrap(invocation.Instance), iAny);
    }

    private static void Report(OperationAnalysisContext context, IOperation expression, INamedTypeSymbol iAny) {
        if (!GeneratorFacts.IsGenerator(expression.Type, iAny)) { return; }

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.GeneratorRenderedAsText, expression.Syntax.GetLocation(), expression.Type!.Name));
    }

}
