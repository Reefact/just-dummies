using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.GenAny;

/// <summary>
///     Reads a constructor's or factory's leading guard clauses (§5.3).
/// </summary>
/// <remarks>
///     This is the feature that makes the tool worth building rather than templating. A constructor that
///     refuses an empty string is stating an invariant the scaffolded generator has to respect, and reading it
///     is the difference between a chain that works and one measured throwing about one draw in seventeen.
///     <para>
///         Deliberately conservative, mirroring how the library's own analyzers under-report rather than
///         misfire: a statement counts as a guard only when it is an <c>if</c> with no <c>else</c> whose body
///         throws unconditionally, it appears before the first assignment to state, its condition mentions
///         exactly one parameter and carries no <c>&amp;&amp;</c> or <c>||</c>, and every other operand is a
///         compile-time constant. Anything else is left alone and reported as unread.
///     </para>
///     <para>
///         <b>Regex guards are not read</b>, and that is a decision rather than an omission. The library builds
///         values from the regular subset of the pattern language only, and an unsupported pattern throws at
///         <b>construction</b> — the emitted parameterless constructor runs the whole recipe, so the generated
///         type would be unusable rather than merely imprecise, and no call the developer could write would
///         rescue it. ADR-0063 also keeps the engine from asking the library whether a pattern is supported.
///         The rule that follows generalises: the engine never emits an expression whose validity depends on a
///         value it cannot check.
///     </para>
/// </remarks>
internal static class Guards {

    private static readonly string[] EmptinessChecks = ["IsNullOrEmpty", "IsNullOrWhiteSpace"];

    /// <summary>
    ///     Names types by namespace, never by keyword.
    /// </summary>
    /// <remarks>
    ///     The default display renders <c>System.String</c> as <c>string</c>, so a guard calling
    ///     <c>string.IsNullOrEmpty</c> was compared against the wrong name and read as unrecognised — silently,
    ///     since an unread guard is a legitimate outcome. Measured, not guessed.
    /// </remarks>
    private static readonly SymbolDisplayFormat ByNamespace =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>Reads <paramref name="method" />'s guards, or reports that there was no body to read.</summary>
    internal static GuardReading Read(IMethodSymbol method, Compilation compilation) {
        BaseMethodDeclarationSyntax? declaration = method.DeclaringSyntaxReferences
                                                         .Select(reference => reference.GetSyntax())
                                                         .OfType<BaseMethodDeclarationSyntax>()
                                                         .FirstOrDefault();

        if (declaration?.Body is null) { return GuardReading.WithoutSource(); }

        Compilation? declaring = Declaring(declaration.SyntaxTree, method, compilation);

        if (declaring is null) { return GuardReading.WithoutSource(); }

        SemanticModel model   = declaring.GetSemanticModel(declaration.SyntaxTree);
        GuardReading  reading = GuardReading.FromSource();

        foreach (StatementSyntax statement in declaration.Body.Statements) {
            // "Leading" is what makes a guard a guard: past the first assignment to state, an `if` that throws
            // is ordinary logic and says nothing about what the parameter may be.
            if (AssignsState(statement, model)) { break; }

            if (statement is not IfStatementSyntax guard || guard.Else is not null) { continue; }
            if (!ThrowsUnconditionally(guard.Statement)) { continue; }

            ReadOne(guard.Condition, model, method, reading);
        }

        return reading;
    }

    /// <summary>
    ///     The compilation that owns <paramref name="tree" />, which is not always the one being scaffolded.
    /// </summary>
    /// <remarks>
    ///     §3.1 runs the tool from the test project, so the target type usually comes from the production
    ///     project next door — and a workspace hands that over as a <b>compilation</b> reference, not as
    ///     metadata. Its constructor then has a real body, in a tree the analyzed compilation does not own, and
    ///     asking that compilation for a semantic model over it throws. The reference carries the compilation
    ///     that does own it, so the guards of §5.3 are read from the source they were written in rather than
    ///     reported as absent — which is the difference between <c>Any.String().NonEmpty()</c> and a plain
    ///     <c>Any.String()</c> for most of the types this tool exists to scaffold.
    /// </remarks>
    private static Compilation? Declaring(SyntaxTree tree, IMethodSymbol method, Compilation compilation) {
        if (compilation.ContainsSyntaxTree(tree)) { return compilation; }

        return compilation.GetMetadataReference(method.ContainingAssembly) is CompilationReference referenced
            && referenced.Compilation.ContainsSyntaxTree(tree)
                   ? referenced.Compilation
                   : null;
    }

    private static void ReadOne(ExpressionSyntax condition,
                                SemanticModel model,
                                IMethodSymbol method,
                                GuardReading reading) {
        IParameterSymbol[] mentioned = Mentioned(condition, model, method);

        // Exactly one, or the engine cannot say whose invariant this is. A cross-parameter rule is precisely
        // the case §9 names as out of reach, and it is left alone rather than half-read.
        if (mentioned.Length != 1) { return; }

        IParameterSymbol parameter = mentioned[0];

        if (condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
                     .Any(binary => binary.IsKind(SyntaxKind.LogicalAndExpression)
                                 || binary.IsKind(SyntaxKind.LogicalOrExpression))) {
            reading.MarkUnread(parameter.Name);

            return;
        }

        if (!TryRecognise(condition, model, parameter, GeneratorFor.SizedByCount(parameter.Type),
                          out GuardConstraint? constraint)) {
            reading.MarkUnread(parameter.Name);

            return;
        }

        if (constraint is not null) { reading.Add(parameter.Name, constraint); }
    }

    /// <summary>
    ///     The closed set. Returning true with no constraint means the guard was understood and adds nothing —
    ///     a null check, or an enum universe check the generator already satisfies.
    /// </summary>
    private static bool TryRecognise(ExpressionSyntax condition,
                                     SemanticModel model,
                                     IParameterSymbol parameter,
                                     bool sizedByCount,
                                     out GuardConstraint? constraint) {
        constraint = null;

        // `p is null` — and its negation is not a guard, so the pattern has to be exactly this.
        if (condition is IsPatternExpressionSyntax pattern) {
            return pattern.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal }
                && literal.IsKind(SyntaxKind.NullLiteralExpression);
        }

        if (condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation) {
            // `!Enum.IsDefined(typeof(E), p)` — Any.Enum<E>() already draws only declared members.
            return IsCall(negation.Operand, model, "System.Enum", "IsDefined");
        }

        if (condition is InvocationExpressionSyntax invocation) {
            if (!IsCall(invocation, model, "System.String", EmptinessChecks)) { return false; }

            constraint = Emptiness(sizedByCount);

            return true;
        }

        return condition is BinaryExpressionSyntax comparison
            && TryComparison(comparison, model, parameter, sizedByCount, out constraint);
    }

    private static bool TryComparison(BinaryExpressionSyntax comparison,
                                      SemanticModel model,
                                      IParameterSymbol parameter,
                                      bool sizedByCount,
                                      out GuardConstraint? constraint) {
        constraint = null;

        if (!TrySides(comparison, model, parameter, out ExpressionSyntax subject, out ExpressionSyntax other, out bool flipped)) {
            return false;
        }

        SyntaxKind @operator = Operator(comparison.Kind(), flipped);
        bool       sized     = IsSize(subject, parameter, model);

        // `p == null` and `p == Guid.Empty` are the two comparisons whose right side is not a number.
        if (@operator == SyntaxKind.EqualsExpression && IsNull(other)) { return true; }
        if (@operator == SyntaxKind.EqualsExpression && IsEmptyGuid(other, model)) {
            constraint = new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness);

            return true;
        }

        Optional<object?> constant = model.GetConstantValue(other);

        if (!constant.HasValue || constant.Value is null || !IsNumber(constant.Value)) { return false; }

        decimal value = Convert.ToDecimal(constant.Value, CultureInfo.InvariantCulture);

        constraint = sized
                         ? Sized(@operator, value, sizedByCount)
                         : Numeric(@operator, value, parameter.Type, Literal(constant.Value, parameter.Type));

        return constraint is not null;
    }

    /// <summary>A guard on how long, or how many. The two families share only <c>NonEmpty</c> (§14.3).</summary>
    private static GuardConstraint? Sized(SyntaxKind @operator, decimal value, bool byCount) {
        string exact = byCount ? "WithCount" : "WithLength";
        string min   = byCount ? "WithMinCount" : "WithMinLength";
        string max   = byCount ? "WithMaxCount" : "WithMaxLength";
        string count = value.ToString(CultureInfo.InvariantCulture);

        return @operator switch {
            SyntaxKind.EqualsExpression when value == 0            => Emptiness(byCount),
            SyntaxKind.LessThanExpression when value == 1          => Emptiness(byCount),
            SyntaxKind.LessThanExpression                          => new GuardConstraint(min, count, Bound.Lower, value),
            SyntaxKind.GreaterThanExpression                       => new GuardConstraint(max, count, Bound.Upper, value),
            SyntaxKind.NotEqualsExpression                         => new GuardConstraint(exact, count, Bound.Exact, value),
            _                                                      => null
        };
    }

    /// <summary>
    ///     A guard on the value itself.
    /// </summary>
    /// <remarks>
    ///     Where two rows both match, the more specific wins. <c>p &lt; 1</c> is <c>Positive</c> on an integral
    ///     type; on <c>decimal</c>, <c>double</c> or <c>float</c> it is a floor of one, because
    ///     <c>Positive</c> would admit the values between zero and one that the guard rejects — a rare draw for
    ///     an otherwise unconstrained decimal, and a common one as soon as the parameter carries another bound.
    /// </remarks>
    private static GuardConstraint? Numeric(SyntaxKind @operator, decimal value, ITypeSymbol type, string literal) {
        bool integral = IsIntegral(type);

        return @operator switch {
            SyntaxKind.LessThanOrEqualExpression when value == 0        => new GuardConstraint("Positive", null, Bound.Sign),
            SyntaxKind.LessThanExpression when value == 1 && integral   => new GuardConstraint("Positive", null, Bound.Sign),
            SyntaxKind.GreaterThanOrEqualExpression when value == 0     => new GuardConstraint("Negative", null, Bound.Sign),
            SyntaxKind.EqualsExpression when value == 0                 => new GuardConstraint("NonZero", null, Bound.Zero),
            SyntaxKind.GreaterThanExpression                            => new GuardConstraint("LessThanOrEqualTo", literal, Bound.Upper, value),
            SyntaxKind.LessThanExpression                               => new GuardConstraint("GreaterThanOrEqualTo", literal, Bound.Lower, value),
            _                                                           => null
        };
    }

    private static GuardConstraint Emptiness(bool byCount) {
        // The one member spelled the same for both families — which is why reading a size guard against the
        // wrong one would emit a member ADR-0059 drops silently, losing a real constraint without a trace.
        _ = byCount;

        return new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness);
    }

    /// <summary>
    ///     The side the guard is about, and the side it compares against.
    /// </summary>
    /// <remarks>
    ///     The subject side has to <b>be</b> the parameter, or the one derived form the table has rows for — its
    ///     length or its count. Merely mentioning it is not enough, and the difference is not academic:
    ///     <c>Math.Abs(p) &gt; 90</c>, <c>p * 2 &gt; 100</c> and <c>p.TotalMinutes &lt; 5</c> all mention the
    ///     parameter while saying nothing about <c>p</c> itself, which is what every row of the table is written
    ///     about. Read as bounds on <c>p</c> they produce a generator whose every draw the guard rejects — or,
    ///     where the member belongs to a type the constraint's argument cannot bind to, a chain that does not
    ///     compile. §9 names the arithmetic condition as out of reach, so a side that is not the parameter is
    ///     left unread and reported as such.
    /// </remarks>
    private static bool TrySides(BinaryExpressionSyntax comparison,
                                 SemanticModel model,
                                 IParameterSymbol parameter,
                                 out ExpressionSyntax subject,
                                 out ExpressionSyntax other,
                                 out bool flipped) {
        subject = comparison.Left;
        other   = comparison.Right;
        flipped = false;

        if (IsSubject(comparison.Left, model, parameter)) { return !Mentions(comparison.Right, model, parameter); }

        if (IsSubject(comparison.Right, model, parameter)) {
            subject = comparison.Right;
            other   = comparison.Left;
            flipped = true;

            return !Mentions(comparison.Left, model, parameter);
        }

        return false;
    }

    /// <summary>The parameter itself, or the one derived form the table has rows for: its length or its count.</summary>
    private static bool IsSubject(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        return IsParameter(expression, model, parameter) || IsSize(expression, parameter, model);
    }

    private static bool IsParameter(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        ExpressionSyntax bare = Unwrapped(expression);

        return bare is IdentifierNameSyntax
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(bare).Symbol, parameter);
    }

    /// <summary>The expression itself, past the parentheses a writer is free to add around it.</summary>
    private static ExpressionSyntax Unwrapped(ExpressionSyntax expression) {
        ExpressionSyntax bare = expression;

        while (bare is ParenthesizedExpressionSyntax parenthesised) { bare = parenthesised.Expression; }

        return bare;
    }

    /// <summary>Reads a comparison written the other way round as the one the table lists.</summary>
    private static SyntaxKind Operator(SyntaxKind written, bool flipped) {
        if (!flipped) { return written; }

        return written switch {
            SyntaxKind.LessThanExpression           => SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanExpression        => SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanOrEqualExpression    => SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanOrEqualExpression,
            _                                       => written
        };
    }

    /// <summary>
    ///     How long, or how many — the parameter's own, never that of something read off it.
    /// </summary>
    /// <remarks>
    ///     The receiver has to <b>be</b> the parameter. <c>p[0].Length</c>, <c>p.Split(',').Length</c> and
    ///     <c>p.Trim().Length</c> are the length of something else, and the family the constraint is written in
    ///     comes from the parameter's own type — so an element's length reads as the collection's count, a
    ///     different invariant emitted with a straight face.
    /// </remarks>
    private static bool IsSize(ExpressionSyntax subject, IParameterSymbol parameter, SemanticModel model) {
        return Unwrapped(subject) is MemberAccessExpressionSyntax access
            && access.Name.Identifier.Text is "Length" or "Count"
            && IsParameter(access.Expression, model, parameter);
    }

    private static bool IsNull(ExpressionSyntax expression) {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression);
    }

    private static bool IsEmptyGuid(ExpressionSyntax expression, SemanticModel model) {
        return expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Empty" } access
            && model.GetTypeInfo(access).Type?.ToDisplayString(ByNamespace) == "System.Guid";
    }

    private static bool IsCall(ExpressionSyntax expression, SemanticModel model, string containing, params string[] names) {
        return expression is InvocationExpressionSyntax invocation
            && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
            && method.ContainingType?.ToDisplayString(ByNamespace) == containing
            && names.Contains(method.Name, StringComparer.Ordinal);
    }

    private static bool Mentions(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        return expression.DescendantNodesAndSelf()
                         .OfType<IdentifierNameSyntax>()
                         .Any(identifier => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier).Symbol,
                                                                                  parameter));
    }

    private static IParameterSymbol[] Mentioned(ExpressionSyntax condition, SemanticModel model, IMethodSymbol method) {
        return condition.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Select(identifier => model.GetSymbolInfo(identifier).Symbol as IParameterSymbol)
                        .Where(symbol => symbol is not null && method.Parameters.Contains(symbol, SymbolEqualityComparer.Default))
                        .Select(symbol => symbol!)
                        .Distinct(SymbolEqualityComparer.Default)
                        .OfType<IParameterSymbol>()
                        .ToArray();
    }

    /// <summary>A body that throws whatever happens, rather than one that merely might.</summary>
    private static bool ThrowsUnconditionally(StatementSyntax body) {
        return body switch {
            ThrowStatementSyntax                     => true,
            BlockSyntax { Statements.Count: 1 } block => block.Statements[0] is ThrowStatementSyntax,
            _                                        => false
        };
    }

    private static bool AssignsState(StatementSyntax statement, SemanticModel model) {
        return statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            && model.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol or IPropertySymbol;
    }

    private static bool IsNumber(object value) {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static bool IsIntegral(ITypeSymbol type) {
        ITypeSymbol underlying = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                                     ? nullable.TypeArguments[0]
                                     : type;

        return underlying.SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                                      or SpecialType.System_Int16 or SpecialType.System_UInt16
                                      or SpecialType.System_Int32 or SpecialType.System_UInt32
                                      or SpecialType.System_Int64 or SpecialType.System_UInt64;
    }

    /// <summary>
    ///     The constant, spelled so it binds to the constraint's parameter.
    /// </summary>
    /// <remarks>
    ///     A <c>decimal</c> bound written as <c>9.99</c> is a <c>double</c> literal, and there is no implicit
    ///     conversion — the emitted chain would not compile. The suffix is not decoration.
    /// </remarks>
    private static string Literal(object value, ITypeSymbol type) {
        ITypeSymbol underlying = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                                     ? nullable.TypeArguments[0]
                                     : type;

        string written = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";

        return underlying.SpecialType switch {
            SpecialType.System_Decimal => written + "m",
            SpecialType.System_Single  => written + "f",
            SpecialType.System_Double  => written + "d",
            _                          => written
        };
    }

}
