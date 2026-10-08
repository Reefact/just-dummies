using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD014 — reports a constraint argument that is a compile-time constant the generator's own guard rejects, so the
///     call throws every time it runs. The mistake is fully determined by a literal at the call site, yet it survives
///     the build and only fires when that arrange line executes — often deep inside a helper shared by many tests,
///     where the failure reads as a library problem rather than as the transposition typo it usually is.
/// </summary>
/// <remarks>
///     One rule over one table rather than a rule per method: the library validates these in one place, with one
///     message shape, and a reader who learns "a constant the guard rejects" has learned all of them. The table
///     mirrors <c>SizeGuard</c> and the per-generator guards exactly; where it cannot be certain it stays silent.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
[SuppressMessage(SonarRule.S3267.Category, SonarRule.S3267.Id, Justification = SuppressionJustification.S3267.ArgumentIsTheOperation)]
public sealed class RejectedConstantArgumentAnalyzer : DiagnosticAnalyzer {

    // SizeGuard.MaxProducibleSize — a size the generator must actually produce is capped here.
    private const int MaxProducibleSize = 1_000_000;
    private const int MaxDecimalScale   = 28;
    private const int MinPort           = 1;
    private const int MaxPort           = 65535;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.RejectedConstantArgument);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context) {
        KnownSymbols symbols = KnownSymbols.From(context.Compilation);
        if (symbols.IAny is null) { return; }

        context.RegisterOperationAction(operationContext => Analyze(operationContext, symbols), OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context, KnownSymbols symbols) {
        IInvocationOperation invocation = (IInvocationOperation)context.Operation;

        if (!IsJustDummiesMember(invocation.TargetMethod, symbols)) { return; }

        // A test asserting that the guard rejects the argument writes the illegal call as the whole body of a lambda
        // argument. This repository alone holds hundreds of them.
        if (NegativeTestGuard.IsSoleBodyOfLambdaArgument(invocation.Syntax)) { return; }

        if (TryFindViolation(invocation, context.Compilation, out IOperation? offending, out string? reason)) {
            context.ReportDiagnostic(Diagnostic.Create(Descriptors.RejectedConstantArgument, offending!.Syntax.GetLocation(), invocation.TargetMethod.Name, reason));
        }
    }

    private static bool TryFindViolation(IInvocationOperation invocation, Compilation compilation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        string name = invocation.TargetMethod.Name;

        switch (name) {
            case "WithLength" or "WithMinLength" or "WithCount" or "WithMinCount":
                return TryCheckSize(invocation, capped: true, out offending, out reason);

            // A ceiling steers the draw exactly as a floor does — every size in range is one the generator may
            // have to produce — so ADR-0076 caps it too: one rule for every size argument, no exception to
            // remember. This row said otherwise, a leftover from the per-generator caps ADR-0029 had, and the
            // consequence was the worst kind: a call the analyzer blessed and the library refused at run time.
            case "WithMaxLength" or "WithMaxCount":
                return TryCheckSize(invocation, capped: true, out offending, out reason);

            // Genuinely uncapped, and verified rather than assumed: UriSpec.RequireSegmentCount refuses a
            // negative count and nothing else.
            case "WithPathSegments":
                return TryCheckSize(invocation, capped: false, out offending, out reason);

            case "WithLengthBetween" or "WithCountBetween":
                return TryCheckSizeRange(invocation, out offending, out reason);

            case "Between":
                return TryCheckOrderedPair(invocation, out offending, out reason);

            case "MultipleOf":
                return TryCheckStrictlyPositive(invocation, out offending, out reason);

            case "WithGranularity":
                return TryCheckGranularity(invocation, compilation, out offending, out reason);

            case "WithScale":
                return TryCheckRange(invocation, 0, MaxDecimalScale, "the scale must be in the inclusive range [0, 28]", out offending, out reason);

            case "WithPort":
                return TryCheckRange(invocation, MinPort, MaxPort, "the port must be between 1 and 65535", out offending, out reason);

            case "StartingWith" or "EndingWith" or "Containing" or "WithChars" or "WithHost":
                return TryCheckNonEmptyText(invocation, out offending, out reason);

            case "OneOf" or "Except":
                return TryCheckNonEmptyPool(invocation, out offending, out reason);

            default:
                return false;
        }
    }

    private static bool TryCheckSize(IInvocationOperation invocation, bool capped, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in NumericArguments(invocation)) {
            if (!ConstantFacts.TryGetInt32(argument.Value, out int value)) { continue; }

            if (value < 0) {
                offending = argument.Value;
                reason    = "it must not be negative";

                return true;
            }

            if (capped && value > MaxProducibleSize) {
                offending = argument.Value;
                reason    = $"it must not exceed {MaxProducibleSize:N0}, the largest size the generator will produce";

                return true;
            }
        }

        return false;
    }

    private static bool TryCheckSizeRange(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        IArgumentOperation[] arguments = NumericArguments(invocation).ToArray();
        if (arguments.Length != 2) { return false; }

        if (ConstantFacts.TryGetInt32(arguments[0].Value, out int minimum) && minimum < 0) {
            offending = arguments[0].Value;
            reason    = "it must not be negative";

            return true;
        }

        if (ConstantFacts.TryGetInt32(arguments[0].Value, out int min) && min > MaxProducibleSize) {
            offending = arguments[0].Value;
            reason    = $"it must not exceed {MaxProducibleSize:N0}, the largest size the generator will produce";

            return true;
        }

        if (ConstantFacts.TryGetInt32(arguments[1].Value, out int maximum) && maximum < 0) {
            offending = arguments[1].Value;
            reason    = "it must not be negative";

            return true;
        }

        return TryCheckOrderedPair(invocation, out offending, out reason);
    }

    // Every numeric overload carries the same guard — Between on a decimal, a double or a long throws for transposed
    // bounds exactly as it does on an int — so the pair is read at the parameter's own type, whichever it is.
    private static bool TryCheckOrderedPair(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        IArgumentOperation[] arguments = ScalarArguments(invocation).ToArray();
        if (arguments.Length != 2) { return false; }

        if (!TryGetComparable(arguments[0].Value, out IComparable minimum)) { return false; }
        if (!TryGetComparable(arguments[1].Value, out IComparable maximum)) { return false; }
        if (minimum.GetType() != maximum.GetType() || minimum.CompareTo(maximum) <= 0) { return false; }

        offending = arguments[0].Value;
        reason    = $"the minimum ({Render(minimum)}) must be less than or equal to the maximum ({Render(maximum)}) — the two arguments look transposed";

        return true;
    }

    // MultipleOf exists on the integer builders only, and every one of them refuses a step that is not strictly
    // positive, whichever integer type it is declared over.
    private static bool TryCheckStrictlyPositive(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in ScalarArguments(invocation)) {
            if (!TryGetComparable(argument.Value, out IComparable value) || !IsNotPositive(value)) { continue; }

            offending = argument.Value;
            reason    = "it must be strictly positive";

            return true;
        }

        return false;
    }

    private static bool TryCheckGranularity(IInvocationOperation invocation, Compilation compilation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (!ConstantFacts.IsNonPositiveTimeSpan(argument.Value, compilation)) { continue; }

            offending = argument.Value;
            reason    = "the granularity must be strictly positive";

            return true;
        }

        return false;
    }

    private static bool TryCheckRange(IInvocationOperation invocation, int minimum, int maximum, string requirement, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in NumericArguments(invocation)) {
            if (!ConstantFacts.TryGetInt32(argument.Value, out int value)) { continue; }
            if (value >= minimum && value <= maximum) { continue; }

            offending = argument.Value;
            reason    = requirement;

            return true;
        }

        return false;
    }

    private static bool TryCheckNonEmptyText(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.Parameter?.Type.SpecialType != SpecialType.System_String) { continue; }
            if (!ConstantFacts.TryGetString(argument.Value, out string text)) { continue; }

            if (text.Length == 0) {
                offending = argument.Value;
                reason    = "it must not be empty";

                return true;
            }

            if (invocation.TargetMethod.Name == "WithChars" && text.Any(char.IsSurrogate)) {
                offending = argument.Value;
                reason    = "a character pool must not contain a surrogate: an astral code point spans two UTF-16 units, which the draw would split. Use OneOf(...) to draw such values as whole strings";

                return true;
            }
        }

        return false;
    }

    private static bool TryCheckNonEmptyPool(IInvocationOperation invocation, out IOperation? offending, out string? reason) {
        offending = null;
        reason    = null;

        foreach (IArgumentOperation argument in invocation.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { continue; }
            if (!initializer.ElementValues.IsEmpty) { continue; }

            offending = invocation;
            reason    = "at least one value is required";

            return true;
        }

        return false;
    }

    // Only the arguments a size or bound guard inspects: an int parameter. This keeps Containing(TItem) on a
    // collection, or Between(DateTime, DateTime), out of the integer checks rather than misreading them.
    private static System.Collections.Generic.IEnumerable<IArgumentOperation> NumericArguments(IInvocationOperation invocation) {
        return invocation.Arguments.Where(argument => argument.Parameter?.Type.SpecialType == SpecialType.System_Int32);
    }

    // The arguments of a numeric bound or step, at whatever numeric type the overload declares. Dates, durations
    // and the 128-bit integers stay out: they are not primitive numbers, and their own guards are not this rule's.
    private static System.Collections.Generic.IEnumerable<IArgumentOperation> ScalarArguments(IInvocationOperation invocation) {
        return invocation.Arguments.Where(argument => argument.Parameter?.Type.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                                                                                               or SpecialType.System_Int16 or SpecialType.System_UInt16
                                                                                               or SpecialType.System_Int32 or SpecialType.System_UInt32
                                                                                               or SpecialType.System_Int64 or SpecialType.System_UInt64
                                                                                               or SpecialType.System_Single or SpecialType.System_Double
                                                                                               or SpecialType.System_Decimal);
    }

    // The argument's constant at the parameter's own type — the conversion Roslyn inserts already folds an int literal
    // into a long or a decimal. A non-finite floating-point bound is refused by a guard of its own, with its own
    // message, so it is left to that guard rather than misreported as transposed.
    private static bool TryGetComparable(IOperation operation, out IComparable value) {
        value = operation.ConstantValue is { HasValue: true, Value: IComparable constant } ? constant : null!;

        return value switch {
            null     => false,
            double d => !double.IsNaN(d) && !double.IsInfinity(d),
            float f  => !float.IsNaN(f) && !float.IsInfinity(f),
            _        => true
        };
    }

    private static bool IsNotPositive(IComparable value) {
        return value switch {
            double d  => d <= 0,
            float f   => f <= 0,
            decimal m => m <= 0,
            ulong u   => u == 0,
            _         => Convert.ToInt64(value, CultureInfo.InvariantCulture) <= 0
        };
    }

    private static string Render(IComparable value) {
        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static bool IsJustDummiesMember(IMethodSymbol method, KnownSymbols symbols) {
        return SymbolEqualityComparer.Default.Equals(method.ContainingType?.ContainingAssembly, symbols.IAny!.ContainingAssembly);
    }

}
