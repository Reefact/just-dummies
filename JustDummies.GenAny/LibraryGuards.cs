using System.Collections.Generic;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.GenAny;

/// <summary>
///     The validating helpers of the two guard libraries §5.3 reads by resolved symbol (ADR-0086).
/// </summary>
/// <remarks>
///     Not a list of blessed name prefixes — the mechanism §5.3 refuses. A row here is a specific documented
///     method of a specific package, resolved against the developer's own compilation (ADR-0063), whose
///     semantics — which values it rejects, the inclusivity of each bound, that it returns its input
///     unchanged — was <b>measured</b> against the pinned version the test suite references. A helper of a
///     recognised library that the table cannot carry this way is answered with <c>unread guards</c> rather
///     than approximated, and the two range guards are the standing reason: Ardalis's <c>OutOfRange</c>
///     admits both of its bounds while the Toolkit's <c>IsInRange</c> rejects its upper one, and a row
///     written from memory would have been confidently wrong on one of them.
/// </remarks>
internal static class LibraryGuards {

    /// <summary>Names types by namespace, never by keyword — the same discipline as the guard reader's.</summary>
    private static readonly SymbolDisplayFormat ByNamespace =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>
    ///     Whether <paramref name="invocation" /> resolves into a recognised guard library at all — whatever
    ///     the method, mapped or not.
    /// </summary>
    /// <remarks>
    ///     This is the question the leading scan asks of an assignment's right side: a call into a guard
    ///     library is validation whichever of its methods it is, so the statement is a guard in the assigned
    ///     spelling and must not end the reading the way ordinary production does (§5.3). Whether the call
    ///     also yields a constraint is <see cref="TryRead" />'s separate question.
    /// </remarks>
    internal static bool Recognises(InvocationExpressionSyntax invocation, SemanticModel model) {
        return Resolve(invocation, model) is not null;
    }

    /// <summary>
    ///     Reads one recognised library call as a guard over <paramref name="parameter" /> —
    ///     <c>TryRecogniseThrowHelper</c>'s counterpart for the two libraries.
    /// </summary>
    /// <returns>
    ///     False where the call is not a recognised library guard whose subject <b>is</b> the parameter — the
    ///     same subject-identity discipline every row of §5.3 keeps. True with constraints (possibly none,
    ///     meaning understood and adding nothing) where a mapped row carries the call's measured semantics;
    ///     true with <b>null</b> where the library is recognised and the method is not mapped, or its bound is
    ///     not a compile-time constant the table can carry — a guard the engine cannot vouch for, which the
    ///     caller marks rather than reads.
    /// </returns>
    internal static bool TryRead(InvocationExpressionSyntax invocation,
                                 SemanticModel model,
                                 IParameterSymbol parameter,
                                 (bool ByCount, int Ceiling, int Floor) sizes,
                                 out IReadOnlyList<GuardConstraint>? constraints) {
        constraints = null;

        if (Resolve(invocation, model) is not { } resolved) { return false; }

        (IMethodSymbol method, bool ardalis) = resolved;

        IReadOnlyList<ExpressionSyntax?> arguments = ArgumentsByParameter(method, invocation);
        int                              subject   = SubjectIndex(method);

        if (subject < 0 || arguments[subject] is not { } input || !Guards.IsParameter(input, model, parameter)) {
            return false;
        }

        constraints = ardalis
                          ? Ardalis(method, arguments, subject, model, parameter, sizes)
                          : Toolkit(method, arguments, model, parameter);

        return true;
    }

    /// <summary>The measured rows of <c>Ardalis.GuardClauses</c>, or null for a method outside them.</summary>
    private static IReadOnlyList<GuardConstraint>? Ardalis(IMethodSymbol method,
                                                           IReadOnlyList<ExpressionSyntax?> arguments,
                                                           int subject,
                                                           SemanticModel model,
                                                           IParameterSymbol parameter,
                                                           (bool ByCount, int Ceiling, int Floor) sizes) {
        switch (method.Name) {
            // The generator never draws null (ADR-0064), so the null half of every row below is already
            // guaranteed; what each row adds is the other half. NonEmpty is the one member both size
            // families spell the same way, which is what lets one row serve the string, the Guid and the
            // collection overloads alike.
            case "Null": return [];

            case "NullOrEmpty":
            case "NullOrWhiteSpace":
                return [NonEmpty()];

            case "Negative":       return One(Guards.Numeric(SyntaxKind.LessThanExpression, 0m, parameter.Type, Guards.Literal(0m, parameter.Type)));
            case "NegativeOrZero": return One(Guards.Numeric(SyntaxKind.LessThanOrEqualExpression, 0m, parameter.Type, Guards.Literal(0m, parameter.Type)));
            case "Zero":           return One(Guards.Numeric(SyntaxKind.EqualsExpression, 0m, parameter.Type, Guards.Literal(0m, parameter.Type)));

            // Measured inclusive at BOTH ends — 0 and 100 pass OutOfRange(0, 100) — so the pair reads as the
            // two inclusive bounds it is, and the range fold writes them as one Between. The enumerable
            // overload bounds the ELEMENTS, not the parameter, which is what the scalar-subject test keeps
            // out: there the input's type is not the bounds' own.
            case "OutOfRange" when IsScalarRange(method, subject, "rangeFrom"):
                return TryBound(method, arguments, "rangeFrom", model, out decimal from)
                    && TryBound(method, arguments, "rangeTo", model, out decimal to)
                           ? Pair(Guards.Numeric(SyntaxKind.LessThanExpression, from, parameter.Type, Guards.Literal(from, parameter.Type)),
                                  Guards.Numeric(SyntaxKind.GreaterThanExpression, to, parameter.Type, Guards.Literal(to, parameter.Type)))
                           : null;

            // Measured admitting the boundary length on both sides, so each is the matching Sized row —
            // caps, integer rendering and distinct floors included.
            case "StringTooShort":
                return TryBound(method, arguments, "minLength", model, out decimal shortest)
                           ? One(Guards.Sized(SyntaxKind.LessThanExpression, shortest, sizes))
                           : null;

            case "StringTooLong":
                return TryBound(method, arguments, "maxLength", model, out decimal longest)
                           ? One(Guards.Sized(SyntaxKind.GreaterThanExpression, longest, sizes))
                           : null;

            case "LengthOutOfRange":
                return TryBound(method, arguments, "minLength", model, out decimal floor)
                    && TryBound(method, arguments, "maxLength", model, out decimal ceiling)
                           ? Pair(Guards.Sized(SyntaxKind.LessThanExpression, floor, sizes),
                                  Guards.Sized(SyntaxKind.GreaterThanExpression, ceiling, sizes))
                           : null;

            // The generator already draws only declared members — but only where the parameter IS the enum,
            // the same subject discipline Enum.IsDefined keeps: the int-backed overload says nothing about
            // what Any.Int32() draws.
            case "EnumOutOfRange" when Guards.Underlying(parameter.Type).TypeKind == TypeKind.Enum:
                return [];

            // Rejecting default(T) is NonEmpty on a Guid and NonZero on a number — measured on both — and an
            // unmapped shape on anything else, where default is no single row of this table.
            case "Default" when Guards.Underlying(parameter.Type).ToDisplayString(ByNamespace) == "System.Guid":
                return [NonEmpty()];

            case "Default" when IsNumericType(parameter.Type):
                return One(Guards.Numeric(SyntaxKind.EqualsExpression, 0m, parameter.Type, Guards.Literal(0m, parameter.Type)));

            default: return null;
        }
    }

    /// <summary>The measured rows of <c>CommunityToolkit.Diagnostics.Guard</c>, or null outside them.</summary>
    private static IReadOnlyList<GuardConstraint>? Toolkit(IMethodSymbol method,
                                                           IReadOnlyList<ExpressionSyntax?> arguments,
                                                           SemanticModel model,
                                                           IParameterSymbol parameter) {
        switch (method.Name) {
            case "IsNotNull": return [];

            case "IsNotNullOrEmpty":
            case "IsNotNullOrWhiteSpace":
                return [NonEmpty()];

            // The comparisons are strict where the name says so — measured: IsGreaterThan(5, 5) throws — and
            // the strict pair builds the general exclusive bound, exactly like the BCL's
            // ThrowIfLessThanOrEqual and ThrowIfGreaterThanOrEqual.
            case "IsGreaterThan":
                return TryBound(method, arguments, "minimum", model, out decimal above)
                           ? One(new GuardConstraint("GreaterThan", Guards.Literal(above, parameter.Type), Bound.Lower, above, exclusive: true))
                           : null;

            case "IsGreaterThanOrEqualTo":
                return TryBound(method, arguments, "minimum", model, out decimal least)
                           ? One(Guards.Numeric(SyntaxKind.LessThanExpression, least, parameter.Type, Guards.Literal(least, parameter.Type)))
                           : null;

            case "IsLessThan":
                return TryBound(method, arguments, "maximum", model, out decimal below)
                           ? One(new GuardConstraint("LessThan", Guards.Literal(below, parameter.Type), Bound.Upper, below, exclusive: true))
                           : null;

            case "IsLessThanOrEqualTo":
                return TryBound(method, arguments, "maximum", model, out decimal most)
                           ? One(Guards.Numeric(SyntaxKind.GreaterThanExpression, most, parameter.Type, Guards.Literal(most, parameter.Type)))
                           : null;

            // Measured HALF-OPEN: IsInRange(0, 0, 100) passes and IsInRange(100, 0, 100) throws. The floor
            // is the ordinary inclusive row; the ceiling stays exclusive, which is what keeps this row honest
            // where a remembered "in range" would have admitted the one value the library rejects.
            case "IsInRange":
                return TryBound(method, arguments, "minimum", model, out decimal lowest)
                    && TryBound(method, arguments, "maximum", model, out decimal past)
                           ? Pair(Guards.Numeric(SyntaxKind.LessThanExpression, lowest, parameter.Type, Guards.Literal(lowest, parameter.Type)),
                                  new GuardConstraint("LessThan", Guards.Literal(past, parameter.Type), Bound.Upper, past, exclusive: true))
                           : null;

            default: return null;
        }
    }

    /// <summary>The call resolved into one of the two libraries, or nothing.</summary>
    /// <remarks>
    ///     Staticness is asked of the method <b>as declared</b>: the reduced form a
    ///     <c>Guard.Against.Null(…)</c> spelling resolves to models an instance-style call and reports itself
    ///     non-static, and requiring it there silently unread every reduced call while the spelled-out static
    ///     form passed.
    /// </remarks>
    private static (IMethodSymbol Method, bool Ardalis)? Resolve(InvocationExpressionSyntax invocation, SemanticModel model) {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || !Declared(method).IsStatic) { return null; }

        if (method.ContainingNamespace?.ToDisplayString() == "Ardalis.GuardClauses"
         && Declared(method).Parameters.Length > 0
         && Declared(method).Parameters[0].Type.ToDisplayString(ByNamespace) == "Ardalis.GuardClauses.IGuardClause") {
            return (method, Ardalis: true);
        }

        if (method.ContainingType?.ToDisplayString(ByNamespace) == "CommunityToolkit.Diagnostics.Guard") {
            return (method, Ardalis: false);
        }

        return null;
    }

    /// <summary>The method as declared — the receiver back in first place for a reduced call.</summary>
    private static IMethodSymbol Declared(IMethodSymbol method) {
        return method.ReducedFrom ?? method;
    }

    /// <summary>
    ///     Where the checked value sits in <see cref="IMethodSymbol.Parameters" /> as the call resolved them:
    ///     past the receiver where the receiver is still a parameter, first otherwise.
    /// </summary>
    private static int SubjectIndex(IMethodSymbol method) {
        for (int position = 0; position < method.Parameters.Length; position++) {
            if (method.Parameters[position].Type.ToDisplayString(ByNamespace) != "Ardalis.GuardClauses.IGuardClause") {
                return position;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Whether the input the call checks is a value of the bounds' own kind, not a collection of them.
    /// </summary>
    private static bool IsScalarRange(IMethodSymbol method, int subject, string boundName) {
        IParameterSymbol? bound = null;

        foreach (IParameterSymbol candidate in method.Parameters) {
            if (candidate.Name == boundName) { bound = candidate; }
        }

        return bound is not null
            && SymbolEqualityComparer.Default.Equals(method.Parameters[subject].Type, bound.Type);
    }

    /// <summary>
    ///     The compile-time constant bound to the parameter <paramref name="name" /> names, or nothing.
    /// </summary>
    /// <remarks>
    ///     By the declared parameter's name rather than by position, so a named argument reads the same as a
    ///     positional one — and a version that renamed the parameter stops matching, which fails toward the
    ///     mark rather than toward a bound read off the wrong argument. The same constant discipline as a
    ///     comparison's other side: a number, on the line, inside <c>decimal</c>.
    /// </remarks>
    private static bool TryBound(IMethodSymbol method,
                                 IReadOnlyList<ExpressionSyntax?> arguments,
                                 string name,
                                 SemanticModel model,
                                 out decimal value) {
        value = 0m;

        for (int position = 0; position < method.Parameters.Length; position++) {
            if (method.Parameters[position].Name != name) { continue; }

            if (arguments[position] is not { } expression) { return false; }

            Optional<object?> constant = model.GetConstantValue(expression);

            if (!constant.HasValue || constant.Value is null || !Guards.IsNumber(constant.Value)) { return false; }

            return Guards.TryDecimal(constant.Value, out value);
        }

        return false;
    }

    /// <summary>
    ///     The argument expressions slotted by the parameter each one fills — a named argument where its name
    ///     says, every other where it was written.
    /// </summary>
    private static IReadOnlyList<ExpressionSyntax?> ArgumentsByParameter(IMethodSymbol method, InvocationExpressionSyntax invocation) {
        ExpressionSyntax?[] slots = new ExpressionSyntax?[method.Parameters.Length];

        SeparatedSyntaxList<ArgumentSyntax> written = invocation.ArgumentList.Arguments;

        for (int position = 0; position < written.Count; position++) {
            ArgumentSyntax argument = written[position];

            int index = argument.NameColon is null ? position : IndexOf(method, argument.NameColon.Name.Identifier.ValueText);

            if (index >= 0 && index < slots.Length) { slots[index] = argument.Expression; }
        }

        return slots;
    }

    private static int IndexOf(IMethodSymbol method, string name) {
        for (int position = 0; position < method.Parameters.Length; position++) {
            if (method.Parameters[position].Name == name) { return position; }
        }

        return -1;
    }

    /// <summary>A type the numeric rows are written about — where a rejected <c>default</c> is a zero.</summary>
    private static bool IsNumericType(ITypeSymbol type) {
        return Guards.Underlying(type).SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                                                   or SpecialType.System_Int16 or SpecialType.System_UInt16
                                                   or SpecialType.System_Int32 or SpecialType.System_UInt32
                                                   or SpecialType.System_Int64 or SpecialType.System_UInt64
                                                   or SpecialType.System_Single or SpecialType.System_Double
                                                   or SpecialType.System_Decimal;
    }

    private static GuardConstraint NonEmpty() {
        return new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness);
    }

    /// <summary>One row's single constraint, or the not-vouched answer where the row had none to give.</summary>
    private static IReadOnlyList<GuardConstraint>? One(GuardConstraint? constraint) {
        return constraint is null ? null : [constraint];
    }

    /// <summary>Both halves of a range row, or the not-vouched answer where either half fell out.</summary>
    private static IReadOnlyList<GuardConstraint>? Pair(GuardConstraint? floor, GuardConstraint? ceiling) {
        if (floor is null || ceiling is null) { return null; }

        return [floor, ceiling];
    }

}
