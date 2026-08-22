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

    /// <summary>The same two emptiness checks, spelled as the throw helper that performs them.</summary>
    private static readonly string[] EmptinessThrowHelpers = ["ThrowIfNullOrEmpty", "ThrowIfNullOrWhiteSpace"];

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

            if (statement is IfStatementSyntax { Else: null } guard && ThrowsUnconditionally(guard.Statement)) {
                ReadOne(guard.Condition, model, method, reading);
            } else {
                MarkIfItRejects(statement, model, method, reading);
                MarkIfValidatedElsewhere(statement, model, method, reading);
            }
        }

        return reading;
    }

    /// <summary>
    ///     Whether a leading statement rejects values at all, in a shape §5.3 could not read, and marks every
    ///     parameter it names as unread.
    /// </summary>
    /// <remarks>
    ///     The one thing a <c>throw</c> before the first assignment to state cannot be is ordinary logic: it
    ///     refuses to build the object, which is the definition of a guard. So a statement carrying one is a
    ///     guard whatever its shape, and where the recognised set could not parse that shape — an
    ///     <c>else if</c> chain, a block that logs before it throws, a condition outside the closed set — the
    ///     right answer is the one §9 already gives, <c>unread guards</c>, and not silence.
    ///     <para>
    ///         Silence was what it got: those shapes fall past the recognised-guard branch above, and the call
    ///         rule below only catches them where the body happens to call something naming the parameter. So
    ///         <c>if (v &lt; 0) { throw … } else if (v &gt; 100) { throw … }</c> read exactly like a parameter
    ///         nobody had constrained — a throwing guard, in plain sight, reported as none.
    ///     </para>
    ///     <para>
    ///         A parameter named only inside the <c>nameof</c> of the throw's own message does not count, for
    ///         the reason <see cref="IsNameOf" /> gives: that names the rejected parameter for a reader rather
    ///         than testing anything. Every real guard of this shape names its subject in the condition too.
    ///     </para>
    /// </remarks>
    private static void MarkIfItRejects(StatementSyntax statement, SemanticModel model, IMethodSymbol method, GuardReading reading) {
        if (!Throws(statement)) { return; }

        foreach (IParameterSymbol parameter in Mentioned(statement, model, method)) { reading.MarkUnread(parameter.Name); }
    }

    /// <summary>Whether anything anywhere in <paramref name="statement" /> refuses to go on.</summary>
    private static bool Throws(StatementSyntax statement) {
        return statement.DescendantNodesAndSelf().Any(node => node is ThrowStatementSyntax or ThrowExpressionSyntax);
    }

    /// <summary>
    ///     Whether a leading statement hands a parameter to a call made for its effect alone, and marks every
    ///     parameter it finds that way as unread.
    /// </summary>
    /// <remarks>
    ///     A guard delegated to a helper — <c>Ensure.NotBlank(name);</c>, <c>Validate(name);</c> — throws from
    ///     inside a call the closed set of §5.3 does not parse, so the loop above never sees an <c>if</c> at
    ///     all and passed over the statement in silence: the parameter read exactly like one with no guard on
    ///     it, and the neutral generator it kept violated the invariant the helper enforces on every draw the
    ///     helper would have rejected. §9 already has the right word for "something here could not be read" —
    ///     <c>unread guards</c> — and the developer needs it here as much as on a condition the set fails to
    ///     recognise. A helper the set <b>does</b> know is read first, by
    ///     <see cref="TryRecogniseThrowHelper" />, and never reaches the mark.
    ///     <para>
    ///         <b>The call's result has to be discarded</b>, and that one test is the whole rule. A call whose
    ///         value is used is <i>producing</i> something — <c>_name = name.Trim()</c>,
    ///         <c>_tags = tags.ToList()</c> — and normalising a value or copying a collection says nothing
    ///         about which values are admissible; flagging those blocked the compilation of constructors
    ///         carrying no guard at all, which is most of them. A call whose value is thrown away was made for
    ///         its effect, and the only effect a call on a constructor parameter can have before the first
    ///         assignment is to reject it.
    ///     </para>
    ///     <para>
    ///         Structural rather than a list of names a validator is expected to be spelled with: a set of
    ///         blessed prefixes is a guess about intent that no reader could reproduce, which is the kind of
    ///         mechanism ADR-0046 refuses. The cost is named in §9 — a guard helper that <i>returns</i> the
    ///         value it checked, <c>_name = Ensure.NotBlank(name);</c>, reads as production and is missed.
    ///     </para>
    /// </remarks>
    private static void MarkIfValidatedElsewhere(StatementSyntax statement, SemanticModel model, IMethodSymbol method, GuardReading reading) {
        foreach (ExpressionStatementSyntax discarded in statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>()) {
            if (discarded.Expression is not InvocationExpressionSyntax invocation || IsNameOf(invocation)) { continue; }

            foreach (IParameterSymbol parameter in Mentioned(invocation, model, method)) {
                if (!TryRecogniseThrowHelper(invocation, model, parameter, GeneratorFor.Sizes(parameter.Type),
                                             out GuardConstraint? constraint)) {
                    reading.MarkUnread(parameter.Name);

                    continue;
                }

                if (constraint is not null) { reading.Add(parameter.Name, constraint); }
            }
        }
    }

    /// <summary>
    ///     The closed set of §5.3 again, for the guards it already knows written as a call rather than as an
    ///     <c>if</c>. Returning true with no constraint means the same as it does there: understood, and adds
    ///     nothing the generator does not already guarantee.
    /// </summary>
    /// <remarks>
    ///     <c>ArgumentNullException.ThrowIfNull(value)</c> and <c>if (value is null) { throw … }</c> state one
    ///     invariant in two spellings, and so do <c>ArgumentException.ThrowIfNullOrWhiteSpace(value)</c> and
    ///     the <c>string.IsNullOrWhiteSpace</c> condition. Only the older spelling was read, so the modern one
    ///     fell to the call rule above and blocked the developer's build — over a generator that was already
    ///     exactly right, since a null check adds nothing (ADR-0064 draws no null) and an emptiness check is
    ///     the row's own <c>NonEmpty</c>. Reading a guard the set already understands as one it could not read
    ///     is the worst of both: it neither tightens anything nor lets the file compile.
    ///     <para>
    ///         The first argument has to <b>be</b> the parameter, the same subject-identity discipline the
    ///         comparison rows keep: <c>ThrowIfNull(other.Thing)</c> is about something else.
    ///     </para>
    ///     <para>
    ///         The arithmetic helpers — <c>ArgumentOutOfRangeException.ThrowIfNegative</c> and its siblings —
    ///         are read too, by <see cref="TryRecogniseArithmeticThrowHelper" />: ADR-0082's follow-up, widening
    ///         the closed set rather than recognising a second spelling of what was already in it.
    ///     </para>
    /// </remarks>
    private static bool TryRecogniseThrowHelper(InvocationExpressionSyntax invocation,
                                                SemanticModel model,
                                                IParameterSymbol parameter,
                                                (bool ByCount, int Ceiling, int Floor) sizes,
                                                out GuardConstraint? constraint) {
        constraint = null;

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count == 0 || !IsParameter(arguments[0].Expression, model, parameter)) { return false; }

        if (IsCall(invocation, model, "System.ArgumentNullException", "ThrowIfNull")) { return true; }

        if (IsCall(invocation, model, "System.ArgumentException", EmptinessThrowHelpers)) {
            constraint = Emptiness(sizes.ByCount);

            return true;
        }

        return TryRecogniseArithmeticThrowHelper(invocation, model, parameter, arguments, out constraint);
    }

    /// <summary>
    ///     <c>ArgumentOutOfRangeException</c>'s arithmetic throw helpers, mapped to the same rows
    ///     <see cref="Numeric" /> already builds for the equivalent <c>if</c> condition — the same invariant,
    ///     read from a second spelling.
    /// </summary>
    /// <remarks>
    ///     <c>ThrowIfLessThanOrEqual</c> and <c>ThrowIfGreaterThanOrEqual</c> have no zero-valued shortcut to
    ///     fall back on the way <c>if (v &lt;= 0)</c> does — they need the general exclusive bound at whatever
    ///     value the second argument names, so they are built directly rather than through <see cref="Numeric" />.
    ///     <para>
    ///         <b><c>ThrowIfNegative</c> is not <c>Positive()</c>.</b> It throws on <c>v &lt; 0</c>, so <c>0</c>
    ///         is admissible — that is <c>GreaterThanOrEqualTo(0)</c>. <c>Positive()</c> (<c>v &gt; 0</c>) is
    ///         <c>ThrowIfNegativeOrZero</c>. The two read from different <see cref="Numeric" /> rows for exactly
    ///         that reason.
    ///     </para>
    ///     <para>
    ///         The second argument of a two-argument helper has to be a compile-time constant, the same
    ///         discipline <see cref="TryComparison" /> already keeps for the <c>if</c> spelling.
    ///     </para>
    /// </remarks>
    private static bool TryRecogniseArithmeticThrowHelper(InvocationExpressionSyntax invocation,
                                                           SemanticModel model,
                                                           IParameterSymbol parameter,
                                                           SeparatedSyntaxList<ArgumentSyntax> arguments,
                                                           out GuardConstraint? constraint) {
        constraint = null;

        const string containing = "System.ArgumentOutOfRangeException";

        if (IsCall(invocation, model, containing, "ThrowIfNegative")) {
            constraint = Numeric(SyntaxKind.LessThanExpression, 0m, parameter.Type, Literal(0m, parameter.Type));

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfNegativeOrZero")) {
            constraint = Numeric(SyntaxKind.LessThanOrEqualExpression, 0m, parameter.Type, Literal(0m, parameter.Type));

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfZero")) {
            constraint = Numeric(SyntaxKind.EqualsExpression, 0m, parameter.Type, Literal(0m, parameter.Type));

            return true;
        }

        if (arguments.Count < 2) { return false; }

        Optional<object?> bound = model.GetConstantValue(arguments[1].Expression);

        if (!bound.HasValue || bound.Value is null || !IsNumber(bound.Value) || !TryDecimal(bound.Value, out decimal value)) {
            return false;
        }

        string literal = Literal(bound.Value, parameter.Type);

        if (IsCall(invocation, model, containing, "ThrowIfLessThan")) {
            constraint = Numeric(SyntaxKind.LessThanExpression, value, parameter.Type, literal);

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfGreaterThan")) {
            constraint = Numeric(SyntaxKind.GreaterThanExpression, value, parameter.Type, literal);

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfLessThanOrEqual")) {
            constraint = new GuardConstraint("GreaterThan", literal, Bound.Lower, value, exclusive: true);

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfGreaterThanOrEqual")) {
            constraint = new GuardConstraint("LessThan", literal, Bound.Upper, value, exclusive: true);

            return true;
        }

        return false;
    }

    /// <summary>
    ///     <c>nameof(value)</c> is not a call — there is no method behind it to have thrown — and it is exactly
    ///     what a guard's own throw names its rejected parameter with, so counting it would flag the ordinary
    ///     shape of a guard the loop above already read as one it did not.
    /// </summary>
    private static bool IsNameOf(InvocationExpressionSyntax invocation) {
        return invocation.Expression is IdentifierNameSyntax { Identifier.Text: "nameof" };
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
        // the case §9 names as out of reach — and §9 says how it ends, too: `unread guards`, on every
        // parameter it spans. Silence there measured 5008 throws in 10 000 draws on `Range(min, max)`, under
        // a recap reporting both parameters inferred and nothing to look at.
        //
        // A condition mentioning NO parameter is different and stays silent — one testing the clock, say, is
        // about none of them, so there is nobody to send looking. The loop below says both at once.
        if (mentioned.Length != 1) {
            foreach (IParameterSymbol spanned in mentioned) { reading.MarkUnread(spanned.Name); }

            return;
        }

        IParameterSymbol parameter = mentioned[0];

        if (condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
                     .Any(binary => binary.IsKind(SyntaxKind.LogicalAndExpression)
                                 || binary.IsKind(SyntaxKind.LogicalOrExpression))) {
            reading.MarkUnread(parameter.Name);

            return;
        }

        if (!TryRecognise(condition, model, parameter, GeneratorFor.Sizes(parameter.Type),
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
                                     (bool ByCount, int Ceiling, int Floor) sizes,
                                     out GuardConstraint? constraint) {
        constraint = null;

        // `p is null` — and its negation is not a guard, so the pattern has to be exactly this.
        if (condition is IsPatternExpressionSyntax pattern) {
            return pattern.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal }
                && literal.IsKind(SyntaxKind.NullLiteralExpression);
        }

        if (condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation) {
            // `!Enum.IsDefined(typeof(E), p)` — Any.Enum<E>() already draws only declared members, which holds
            // only where the parameter IS E. On an int-backed status column the row's own justification fails:
            // nothing narrows Any.Int32(), and the outcome is indistinguishable from a guard there was nothing
            // to read — worse than a lost constraint, because it does not even say it lost one.
            return IsCall(negation.Operand, model, "System.Enum", "IsDefined")
                && NamesTheParametersOwnUniverse(negation.Operand, model, parameter);
        }

        if (condition is InvocationExpressionSyntax invocation) {
            if (!IsCall(invocation, model, "System.String", EmptinessChecks)) { return false; }

            constraint = Emptiness(sizes.ByCount);

            return true;
        }

        return condition is BinaryExpressionSyntax comparison
            && TryComparison(comparison, model, parameter, sizes, out constraint);
    }

    private static bool TryComparison(BinaryExpressionSyntax comparison,
                                      SemanticModel model,
                                      IParameterSymbol parameter,
                                      (bool ByCount, int Ceiling, int Floor) sizes,
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
        if (!TryDecimal(constant.Value, out decimal value)) { return false; }

        constraint = sized
                         ? Sized(@operator, value, sizes)
                         : Numeric(@operator, value, parameter.Type, Literal(constant.Value, parameter.Type));

        return constraint is not null;
    }

    /// <summary>
    ///     The constant as a decimal, or the fact that it is not one a bound can be read from.
    /// </summary>
    /// <remarks>
    ///     <see cref="IsNumber" /> admits <c>float</c> and <c>double</c>, whose range runs far past
    ///     <c>decimal</c>'s — and whose NaN and infinities are not points on the number line at all. The
    ///     conversion throws on each, out of a method the public <c>Scaffolder.Scaffold</c> declares no such
    ///     exception for: one <c>if (value &gt; 1e30)</c> in one domain type ended a whole run, leaving the
    ///     types before it on disk, the rest absent, and the shell reporting a command line it had understood
    ///     perfectly well (§10.3).
    /// </remarks>
    private static bool TryDecimal(object constant, out decimal value) {
        value = 0m;

        if (constant is double asDouble && (double.IsNaN(asDouble) || double.IsInfinity(asDouble))) { return false; }
        if (constant is float asSingle && (float.IsNaN(asSingle) || float.IsInfinity(asSingle))) { return false; }

        try {
            value = Convert.ToDecimal(constant, CultureInfo.InvariantCulture);
        } catch (OverflowException) {
            // The range boundary is not expressible as a comparison — (double)decimal.MaxValue rounds ABOVE
            // decimal.MaxValue — so the conversion's own documented failure is the test, turned here into the
            // false every caller of this path already handles.
            return false;
        }

        return true;
    }

    /// <summary>A guard on how long, or how many. The two families share only <c>NonEmpty</c> (§14.3).</summary>
    /// <remarks>
    ///     Every size member takes an <c>int</c> (§14.3), so a constant that does not render as one is not a
    ///     size this family can be written with. <c>text.Length &gt; Budget / 2.0</c> folds to <c>140.5</c>:
    ///     emitted verbatim that is <c>CS1503</c> in the developer's own build, and even spelled as an integer
    ///     it is not the bound the guard states. The numeric branch has carried a type-aware literal since the
    ///     <c>decimal</c> case forced one; this is the same rule, for the family that had no equivalent.
    /// </remarks>
    private static GuardConstraint? Sized(SyntaxKind @operator, decimal value, (bool ByCount, int Ceiling, int Floor) sizes) {
        if (value != decimal.Truncate(value) || value < 0 || value > int.MaxValue) { return null; }

        string exact = sizes.ByCount ? "WithCount" : "WithLength";
        string min   = sizes.ByCount ? "WithMinCount" : "WithMinLength";
        string max   = sizes.ByCount ? "WithMaxCount" : "WithMaxLength";
        string count = ((int)value).ToString(CultureInfo.InvariantCulture);

        // A floor and an exact size ask the generator to PRODUCE that many, so they answer to both limits; a
        // ceiling only asks it not to exceed one, so the element domain has nothing to say about it.
        return @operator switch {
            SyntaxKind.EqualsExpression when value == 0                => Emptiness(sizes.ByCount),
            SyntaxKind.LessThanExpression when value == 1              => Emptiness(sizes.ByCount),
            SyntaxKind.LessThanExpression when value <= sizes.Floor    => new GuardConstraint(min, count, Bound.Lower, value),
            SyntaxKind.GreaterThanExpression when value <= sizes.Ceiling => new GuardConstraint(max, count, Bound.Upper, value),
            SyntaxKind.NotEqualsExpression when value <= sizes.Floor   => new GuardConstraint(exact, count, Bound.Exact, value),
            _                                                          => null
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
            SyntaxKind.LessThanOrEqualExpression when value == 0        => Positive(),
            SyntaxKind.LessThanExpression when value == 1 && integral   => Positive(),
            SyntaxKind.GreaterThanOrEqualExpression when value == 0     => Negative(),
            SyntaxKind.EqualsExpression when value == 0                 => new GuardConstraint("NonZero", null, Bound.Zero),
            SyntaxKind.GreaterThanExpression                            => new GuardConstraint("LessThanOrEqualTo", literal, Bound.Upper, value),
            SyntaxKind.LessThanExpression                               => new GuardConstraint("GreaterThanOrEqualTo", literal, Bound.Lower, value),
            _                                                           => null
        };
    }

    /// <summary>
    ///     A floor at zero that zero does not satisfy, and a ceiling likewise.
    /// </summary>
    /// <remarks>
    ///     Placed on the number line rather than left as a sign, so that composition can see them: a sign
    ///     invisible to the interval arithmetic let <c>Positive()</c> stand beside a ceiling below zero, which
    ///     the library refuses at construction. Exclusive rather than a floor of one, because these rows fire
    ///     on <c>decimal</c> and <c>double</c> too, where a floor of one would declare
    ///     <c>Positive().LessThanOrEqualTo(0.5m)</c> empty — and it draws.
    /// </remarks>
    private static GuardConstraint Positive() {
        return new GuardConstraint("Positive", argument: null, Bound.Lower, value: 0m, exclusive: true);
    }

    private static GuardConstraint Negative() {
        return new GuardConstraint("Negative", argument: null, Bound.Upper, value: 0m, exclusive: true);
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

    /// <summary>
    ///     Whether the universe an <c>Enum.IsDefined</c> guard checks against is the parameter's own type.
    /// </summary>
    /// <remarks>
    ///     Both overloads are read, and only one of them needs the check: <c>IsDefined&lt;TEnum&gt;(TEnum)</c>
    ///     infers the universe from the value it is given, so naming the parameter there settles it, while
    ///     <c>IsDefined(Type, object)</c> takes the two independently and will happily judge an <c>int</c>
    ///     against an enum's members.
    /// </remarks>
    private static bool NamesTheParametersOwnUniverse(ExpressionSyntax call, SemanticModel model, IParameterSymbol parameter) {
        if (call is not InvocationExpressionSyntax invocation) { return false; }

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count == 1) { return IsParameter(arguments[0].Expression, model, parameter); }

        return arguments.Count == 2
            && arguments[0].Expression is TypeOfExpressionSyntax universe
            && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(universe.Type).Type, Underlying(parameter.Type))
            && IsParameter(arguments[1].Expression, model, parameter);
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

    /// <summary>
    ///     The parameters <paramref name="node" /> names for itself, past any <c>nameof</c> inside it.
    /// </summary>
    /// <remarks>
    ///     The exclusion matters once whole statements are read rather than conditions alone: every guard's own
    ///     throw spells its rejected parameter with <c>nameof</c>, so counting it would make the message the
    ///     evidence instead of the test.
    /// </remarks>
    private static IParameterSymbol[] Mentioned(SyntaxNode node, SemanticModel model, IMethodSymbol method) {
        InvocationExpressionSyntax[] spelled = [.. node.DescendantNodesAndSelf()
                                                       .OfType<InvocationExpressionSyntax>()
                                                       .Where(IsNameOf)];

        return node.DescendantNodesAndSelf()
                   .OfType<IdentifierNameSyntax>()
                   .Where(identifier => !spelled.Any(name => name.Contains(identifier)))
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

    /// <summary>
    ///     The type a bound is actually read against — <c>int</c> for an <c>int?</c>.
    /// </summary>
    /// <remarks>
    ///     One definition, because three readers have to agree on it: whether the type is integral, how its
    ///     literal is spelled, and which enum universe a guard may name.
    /// </remarks>
    private static ITypeSymbol Underlying(ITypeSymbol type) {
        return type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
                   ? nullable.TypeArguments[0]
                   : type;
    }

    private static bool IsIntegral(ITypeSymbol type) {
        ITypeSymbol underlying = Underlying(type);

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
        ITypeSymbol underlying = Underlying(type);

        string written = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "0";

        return underlying.SpecialType switch {
            SpecialType.System_Decimal => written + "m",
            SpecialType.System_Single  => written + "f",
            SpecialType.System_Double  => written + "d",
            _                          => written
        };
    }

}
