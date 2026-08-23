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
///         exactly one parameter the body has not written over, and carries no <c>&amp;&amp;</c> or
///         <c>||</c>, and every other operand is a compile-time constant. Anything else is left alone and
///         reported as unread. A guard written as a call rather than as an <c>if</c> answers the same two
///         questions of placement: nothing may have written its parameter where it sits, and nothing
///         <b>enclosing</b> it may decide whether it runs. A jump past it from above — an early
///         <c>return</c>, a <c>goto</c> — is a third question §9 names as out of reach, for both spellings.
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
    /// <param name="method">The constructor or factory to read leading guards from.</param>
    /// <param name="compilation">The compilation <paramref name="method" /> is resolved against.</param>
    /// <param name="names">
    ///     Spells an enum member in the target file's namespace context — <c>Status.None</c>, possibly
    ///     qualified — for the one constraint §5.3 reads whose argument is not a number (enum exclusion).
    /// </param>
    internal static GuardReading Read(IMethodSymbol method, Compilation compilation, TypeNames names) {
        BaseMethodDeclarationSyntax? declaration = method.DeclaringSyntaxReferences
                                                         .Select(reference => reference.GetSyntax())
                                                         .OfType<BaseMethodDeclarationSyntax>()
                                                         .FirstOrDefault();

        if (declaration?.Body is null) { return GuardReading.WithoutSource(); }

        Compilation? declaring = Declaring(declaration.SyntaxTree, method, compilation);

        if (declaring is null) { return GuardReading.WithoutSource(); }

        SemanticModel   model   = declaring.GetSemanticModel(declaration.SyntaxTree);
        GuardReading    reading = GuardReading.FromSource();
        ParameterWrites writes  = new(declaration, declaration.Body, model);

        foreach (StatementSyntax statement in declaration.Body.Statements) {
            // "Leading" is what makes a guard a guard: past the first assignment to state, an `if` that throws
            // is ordinary logic and says nothing about what the parameter may be.
            if (AssignsState(statement, model)) { break; }

            if (statement is IfStatementSyntax guard) {
                ReadChain(guard, model, method, reading, names, writes);
            } else {
                MarkIfItRejects(statement, model, method, reading);
                MarkIfValidatedElsewhere(statement, model, method, reading, writes);
            }
        }

        return reading;
    }

    /// <summary>
    ///     Walks an <c>if</c>/<c>else if</c> chain, reading each branch's condition as its own guard for as
    ///     long as every branch before it throws unconditionally.
    /// </summary>
    /// <remarks>
    ///     An <c>else</c> branch says only what happens when its condition is <b>false</b> — exactly the case
    ///     where the branches before it let the value through — so it can never weaken what they reject:
    ///     <c>if (v &lt; 0) { throw … } else { … }</c> means <c>v &gt;= 0</c> holds whatever the <c>else</c>
    ///     contains, and the condition is read the same as it would be with no <c>else</c> at all.
    ///     <para>
    ///         An <c>else if</c> needs one more step, and it is why this used to stop at the first <c>else</c>
    ///         rather than read through it: <c>if (a &lt; 0) { throw … } else if (b &gt; 100) { throw … }</c> is
    ///         readable on both — reaching <c>b</c>'s test presupposes only that <c>a</c>'s already rejected the
    ///         value, and <c>b</c>'s own guard holds regardless of <c>a</c>. But
    ///         <c>if (a &lt; 0) { _x = 1; } else if (b &gt; 100) { throw … }</c> is not: reaching <c>b</c>'s test
    ///         now presupposes <c>a &gt;= 0</c> too, a cross-parameter rule §9 already names as out of reach.
    ///         The rule that falls out is exactly the one this method walks: keep reading while every branch so
    ///         far throws unconditionally, and hand the first branch that does not — condition, body and
    ///         whatever follows it — to <see cref="MarkIfItRejects" />, the same as any other shape the closed
    ///         set cannot parse.
    ///     </para>
    /// </remarks>
    private static void ReadChain(IfStatementSyntax branch,
                                  SemanticModel model,
                                  IMethodSymbol method,
                                  GuardReading reading,
                                  TypeNames names,
                                  ParameterWrites writes) {
        if (!ThrowsUnconditionally(branch.Statement)) {
            MarkIfItRejects(branch, model, method, reading);

            // Handed the `if` itself, so every call under it is conditioned by definition and this can only
            // mark. It is called for the marking, which silence would otherwise cost (§9).
            MarkIfValidatedElsewhere(branch, model, method, reading, writes);

            return;
        }

        ReadOne(branch.Condition, model, method, reading, names, writes);

        switch (branch.Else?.Statement) {
            case IfStatementSyntax elseIf:
                ReadChain(elseIf, model, method, reading, names, writes);

                break;
            case StatementSyntax terminal:
                // No condition of its own to read, but a plain `else` that still throws is a reject the closed
                // set has nothing to say about — §9's `unread guards`, not silence.
                MarkIfItRejects(terminal, model, method, reading);
                MarkIfValidatedElsewhere(terminal, model, method, reading, writes);

                break;
        }
    }

    /// <summary>
    ///     Whether a leading statement rejects values at all, in a shape §5.3 could not read, and marks every
    ///     parameter it names as unread.
    /// </summary>
    /// <remarks>
    ///     The one thing a <c>throw</c> before the first assignment to state cannot be is ordinary logic: it
    ///     refuses to build the object, which is the definition of a guard. So a statement carrying one is a
    ///     guard whatever its shape, and where the recognised set could not parse that shape — a block that logs
    ///     before it throws, a condition outside the closed set, an <c>else if</c> chain whose reachability
    ///     depends on an earlier branch that does not throw — the right answer is the one §9 already gives,
    ///     <c>unread guards</c>, and not silence.
    ///     <para>
    ///         Silence was what it got: those shapes fall past the recognised-guard branch above, and the call
    ///         rule below only catches them where the body happens to call something naming the parameter. So
    ///         <c>if (v &lt; 0) { throw … } else if (v &gt; 100) { throw … }</c> used to read exactly like a
    ///         parameter nobody had constrained — a throwing guard, in plain sight, reported as none.
    ///         <see cref="ReadChain" /> now reads both conditions instead; this rule catches only the chains it
    ///         hands off, and every other shape that rejects without matching the closed set.
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
    private static void MarkIfValidatedElsewhere(StatementSyntax statement,
                                                 SemanticModel model,
                                                 IMethodSymbol method,
                                                 GuardReading reading,
                                                 ParameterWrites writes) {
        foreach (ExpressionStatementSyntax discarded in statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>()) {
            if (discarded.Expression is not InvocationExpressionSyntax invocation || IsNameOf(invocation)) { continue; }

            // Asked once of the statement rather than of each parameter: whether the call runs at all is a
            // fact about where it sits, and the same for every parameter it names.
            ReadCall(invocation, RunsUnconditionally(discarded, statement), model, method, reading, writes);
        }
    }

    /// <summary>
    ///     Reads one discarded call as a guard over each parameter it names — <see cref="ReadOne" />'s
    ///     counterpart for the spelling that has no condition to parse.
    /// </summary>
    /// <remarks>
    ///     A helper that runs only when something else holds states an invariant of the paths that reach it,
    ///     never of the parameter. <c>if (strict) { ThrowIfNegative(value); }</c> admits every negative a
    ///     <c>strict: false</c> caller passes, and read as <c>GreaterThanOrEqualTo(0)</c> all the same. The
    ///     <c>if</c> spelling has always demanded an unconditional throw before its condition is read
    ///     (<see cref="ThrowsUnconditionally" />), and <see cref="RunsUnconditionally" /> is that same demand
    ///     asked of the statement carrying the call.
    /// </remarks>
    private static void ReadCall(InvocationExpressionSyntax invocation,
                                 bool unconditional,
                                 SemanticModel model,
                                 IMethodSymbol method,
                                 GuardReading reading,
                                 ParameterWrites writes) {
        foreach (IParameterSymbol parameter in Mentioned(invocation, model, method)) {
            // Whose invariant a conditioned helper states is the paths that reach it, never the parameter,
            // so the reading owes the developer the same word it owes any guard it could not place.
            if (!unconditional) {
                reading.MarkUnread(parameter.Name);

                continue;
            }

            // The rule the condition reader keeps, in the other spelling: a helper the set knows, called
            // where a write to that parameter can already have run, states an invariant of the computed
            // value. This is the placement that matters most — a call sits in the middle of a statement,
            // where the writes above it are as easy to miss as the ones in the statements above that.
            if (writes.Precede(parameter, invocation)) {
                reading.MarkUnread(parameter.Name);

                continue;
            }

            if (!TryRecogniseThrowHelper(invocation, model, parameter, GeneratorFor.Sizes(parameter.Type),
                                         out GuardConstraint? constraint)) {
                reading.MarkUnread(parameter.Name);

                continue;
            }

            if (constraint is not null) { reading.Add(parameter.Name, constraint); }
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
                                GuardReading reading,
                                TypeNames names,
                                ParameterWrites writes) {
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

        // Read correctly, about the wrong value. Every other gap in §9 is a guard the engine cannot see; this
        // is the one shape where it sees the guard, parses it, and attributes it to a value the generator no
        // longer draws — a write above a `percent < 0` test yields `GreaterThanOrEqualTo(0)` over a real
        // domain of 0..100, under a recap reporting it inferred. So the guard is not this parameter's to
        // read, and §9 already has the word for that (ADR-0083).
        if (writes.Precede(parameter, condition)) {
            reading.MarkUnread(parameter.Name);

            return;
        }

        if (condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
                     .Any(binary => binary.IsKind(SyntaxKind.LogicalAndExpression)
                                 || binary.IsKind(SyntaxKind.LogicalOrExpression))) {
            reading.MarkUnread(parameter.Name);

            return;
        }

        if (!TryRecognise(condition, model, parameter, GeneratorFor.Sizes(parameter.Type), names,
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
                                     TypeNames names,
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
            && TryComparison(comparison, model, parameter, sizes, names, out constraint);
    }

    private static bool TryComparison(BinaryExpressionSyntax comparison,
                                      SemanticModel model,
                                      IParameterSymbol parameter,
                                      (bool ByCount, int Ceiling, int Floor) sizes,
                                      TypeNames names,
                                      out GuardConstraint? constraint) {
        constraint = null;

        if (!TrySides(comparison, model, parameter, out ExpressionSyntax subject, out ExpressionSyntax other, out bool flipped)) {
            return false;
        }

        SyntaxKind @operator = Operator(comparison.Kind(), flipped);
        bool       sized     = IsSize(subject, parameter, model);

        // `p == null`, `p == Guid.Empty` and `p == E.Member` are the comparisons whose right side is not a
        // number.
        if (@operator == SyntaxKind.EqualsExpression && IsNull(other)) { return true; }
        if (@operator == SyntaxKind.EqualsExpression && IsEmptyGuid(other, model)) {
            constraint = new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness);

            return true;
        }

        if (@operator == SyntaxKind.EqualsExpression
         && TryEnumMember(other, model, parameter, names, out string? excluded)) {
            constraint = new GuardConstraint("DifferentFrom", excluded, Bound.Excluded);

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
    ///     Whether <paramref name="expression" /> names a declared member of <paramref name="parameter" />'s own
    ///     enum type, spelled the way the emitted file's namespace context needs it.
    /// </summary>
    /// <remarks>
    ///     The same discipline as <see cref="NamesTheParametersOwnUniverse" />: <c>status == OrderStatus.None</c>
    ///     on an <c>int</c>-backed status column would read as a bound the parameter's own type cannot carry, so
    ///     the universe the member belongs to has to be the parameter's own — never merely an enum.
    /// </remarks>
    private static bool TryEnumMember(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter, TypeNames names, out string? spelled) {
        spelled = null;

        if (model.GetSymbolInfo(expression).Symbol is not IFieldSymbol { HasConstantValue: true } member) { return false; }
        if (member.ContainingType is not { TypeKind: TypeKind.Enum } universe) { return false; }
        if (!SymbolEqualityComparer.Default.Equals(universe, Underlying(parameter.Type))) { return false; }

        spelled = names.Of(universe) + "." + member.Name;

        return true;
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
            SyntaxKind.LessThanOrEqualExpression when value == 0        => Positive(type),
            SyntaxKind.LessThanExpression when value == 1 && integral   => Positive(type),
            SyntaxKind.GreaterThanOrEqualExpression when value == 0     => Negative(type),
            SyntaxKind.EqualsExpression when value == 0                 => new GuardConstraint("NonZero", null, Bound.Zero),
            SyntaxKind.GreaterThanExpression                            => new GuardConstraint("LessThanOrEqualTo", literal, Bound.Upper, value),
            SyntaxKind.LessThanExpression                               => new GuardConstraint("GreaterThanOrEqualTo", literal, Bound.Lower, value),
            _                                                           => null
        };
    }

    /// <summary>
    ///     A floor at zero that zero does not satisfy, spelled so the parameter's own generator carries it.
    /// </summary>
    /// <remarks>
    ///     Placed on the number line rather than left as a sign, so that composition can see them: a sign
    ///     invisible to the interval arithmetic let <c>Positive()</c> stand beside a ceiling below zero, which
    ///     the library refuses at construction. Exclusive rather than a floor of one, because these rows fire
    ///     on <c>decimal</c> and <c>double</c> too, where a floor of one would declare
    ///     <c>Positive().LessThanOrEqualTo(0.5m)</c> empty — and it draws.
    ///     <para>
    ///         <b>An unsigned generator carries no <c>Positive</c></b> — §14.3 gives the unsigned families the
    ///         signed surface less <c>Positive</c> and <c>Negative</c> — so emitting it there resolves to
    ///         nothing and ADR-0059 drops it, leaving an unnarrowed draw under a file that still compiles.
    ///         Zero is the floor of an unsigned type, so <i>above zero</i> is exactly <i>not zero</i>: the
    ///         constraint is the same one, in the only spelling the generator has for it. Not an
    ///         approximation, and not a widening — <c>NonZero</c> admits precisely what <c>Positive</c> would.
    ///     </para>
    /// </remarks>
    private static GuardConstraint Positive(ITypeSymbol type) {
        if (IsUnsigned(type)) { return new GuardConstraint("NonZero", argument: null, Bound.Zero); }

        return new GuardConstraint("Positive", argument: null, Bound.Lower, value: 0m, exclusive: true);
    }

    /// <summary>
    ///     A ceiling at zero that zero does not satisfy, or nothing where the type leaves no value below it.
    /// </summary>
    /// <remarks>
    ///     <c>if (v &gt;= 0) { throw … }</c> on an unsigned parameter rejects every value the type can hold, so
    ///     there is no constraint to write and no draw that would satisfy it. Returning nothing sends it to
    ///     <c>unread guards</c>, which blocks the developer's build (ADR-0083) and says so — the loud refusal
    ///     ADR-0046 asks for, rather than a <c>Negative</c> the generator would drop on its way out.
    /// </remarks>
    private static GuardConstraint? Negative(ITypeSymbol type) {
        if (IsUnsigned(type)) { return null; }

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

    /// <summary>
    ///     Whether <paramref name="carrier" /> runs every time <paramref name="boundary" /> does, or
    ///     something enclosing it between the two decides whether it runs.
    /// </summary>
    /// <remarks>
    ///     A guard delegated to a helper was read wherever it was found among the leading statements, with no
    ///     check on whether the statement carrying it can be skipped.
    ///     <c>if (strict) { ThrowIfNegative(value); }</c> read <c>GreaterThanOrEqualTo(0)</c> over a
    ///     constructor that accepts every negative a <c>strict: false</c> caller passes — narrower than the
    ///     domain it really admits, reported as inferred, and silent: every draw still compiles and still
    ///     constructs, so nothing sent the developer looking, which is what makes it worse than a crash.
    ///     <para>
    ///         <b>The boundary is the statement the reading was handed</b>, and that is what keeps this a
    ///         walk of three lines rather than a second model of the language. <see cref="Read" /> hands a
    ///         leading statement, so the whole nesting inside it is asked about; <see cref="ReadChain" />
    ///         hands the <c>if</c> whose branch does not throw, so that branch refuses; and it hands the body
    ///         of a terminal <c>else</c>, which stops the walk before the clause — correctly, since reaching
    ///         the <c>else</c> of a branch that throws unconditionally is what every construction that
    ///         survives did. <see cref="ThrowsUnconditionally" /> is therefore not restated here: it stays
    ///         where it is, deciding which <c>else</c> bodies reach this method at all.
    ///     </para>
    /// </remarks>
    private static bool RunsUnconditionally(StatementSyntax carrier, StatementSyntax boundary) {
        SyntaxNode node = carrier;

        while (!ReferenceEquals(node, boundary)) {
            if (node.Parent is not SyntaxNode parent || !AlwaysRuns(parent, node)) { return false; }

            node = parent;
        }

        return true;
    }

    /// <summary>Whether <paramref name="parent" /> runs <paramref name="child" /> every time it runs itself.</summary>
    /// <remarks>
    ///     <b>Only a construct that scopes its body passes, and every other one refuses.</b> A <c>using</c>,
    ///     a <c>lock</c>, a <c>checked</c> or <c>unchecked</c> block and an <c>unsafe</c> block run the body
    ///     they wrap every time, and so does a <c>finally</c>, whose rejection propagates like any other. A
    ///     loop may run its body no times, a <c>switch</c> picks one section among several, a <c>catch</c>
    ///     runs only when something threw, a <c>try</c> may sit under a handler able to swallow the very
    ///     rejection the guard makes, and a lambda body runs where it is called rather than where it is
    ///     written.
    ///     <para>
    ///         That default is the safe side of this question, and it is the mirror of the one
    ///         <see cref="ParameterWrites" /> keeps for writes: there an unlisted construct is asked about
    ///         <b>entire</b>, here it refuses. A construct left out of this list therefore costs a
    ///         constraint and can never produce a wrong one, which is what lets the list stay short instead
    ///         of pretending to be complete.
    ///     </para>
    ///     <para>
    ///         What it does not answer is a jump <b>past</b> the carrier from above — an early
    ///         <c>return</c>, a <c>goto</c> — which no ancestor of the call can show. §9 names that as out
    ///         of reach for both spellings of a guard rather than leaving it unsaid.
    ///     </para>
    /// </remarks>
    private static bool AlwaysRuns(SyntaxNode parent, SyntaxNode child) {
        return parent switch {
            BlockSyntax or CheckedStatementSyntax or UnsafeStatementSyntax or FinallyClauseSyntax => true,
            UsingStatementSyntax scoped                                                           => ReferenceEquals(child, scoped.Statement),
            LockStatementSyntax held                                                              => ReferenceEquals(child, held.Statement),
            TryStatementSyntax attempt                                                            => ReferenceEquals(child, attempt.Finally),
            _                                                                                     => false
        };
    }

    private static bool AssignsState(StatementSyntax statement, SemanticModel model) {
        return statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            && model.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol or IPropertySymbol;
    }

    /// <summary>
    ///     Where a body writes over its own parameters, and whether one of those writes can already have run
    ///     when a given guard is evaluated.
    /// </summary>
    /// <remarks>
    ///     Only an assignment to a field or a property ends the leading-guard scan, so a body that writes over
    ///     a parameter and then guards it used to have that guard read as a bound on the drawn value. That is
    ///     the one shape where the engine is confidently wrong rather than blind, and the fix is a question of
    ///     <b>placement</b>: a guard states something about the drawn value exactly when no write to its
    ///     parameter can have run before it.
    ///     <para>
    ///         <b>Which writes exist is asked of the compiler, never of the syntax.</b> An enumeration of the
    ///         spellings — <c>=</c>, the compound forms, <c>++</c>, <c>--</c> — reads as complete and is not:
    ///         a deconstruction writes through a tuple whose left side resolves to no parameter at all, and an
    ///         <c>out</c> argument writes with no assignment node anywhere. Both were measured being read as
    ///         bounds on the drawn value. <see cref="Microsoft.CodeAnalysis.DataFlowAnalysis.WrittenInside" />
    ///         answers for every spelling at once, including the ones nobody thought to list, which is what
    ///         ADR-0046 asks of a boundary: it holds against what was not foreseen.
    ///     </para>
    ///     <para>
    ///         <b>Where they sit is a question about execution, not about statements.</b> A write and a guard
    ///         share a statement as readily as they occupy two — <c>else { v = 100 - v; ThrowIf…(v); }</c> is
    ///         one statement carrying both — so the regions asked about are the ones that have finished by the
    ///         time the guard is evaluated.
    ///     </para>
    ///     <para>
    ///         <b>One order is read, and every other construct is asked about entire.</b> The order read is
    ///         statement sequencing, plus the fact that reaching either branch of an <c>if</c> means its
    ///         condition ran first — which is what keeps the <c>else</c> rule intact, a condition having no
    ///         region of its own statement above it. Everything else answers whole: a loop runs its body
    ///         again, a <c>finally</c> runs after a <c>try</c> that wrote, a <c>switch</c> evaluates its
    ///         governing expression before the section it picked, a <c>using</c> its resource before the body
    ///         it scopes. This was a list of walkable parents that yielded nothing for the rest, and all four
    ///         of those were measured reading a guard as a bound on a value the constructor had replaced.
    ///         A superset region can only add refusals and never remove one, so asking entire is the safe
    ///         default and silence was the unsafe one — and it holds for the constructs nobody listed,
    ///         including the ones C# has not grown yet.
    ///     </para>
    ///     <para>
    ///         Two writes sit outside that walk altogether and are refused wherever they are written: one
    ///         inside a local function or a lambda, which runs when it is called rather than where it is
    ///         declared, and any write at all in a body carrying a <c>goto</c>, which can send execution back
    ///         above a guard the source puts above it.
    ///     </para>
    /// </remarks>
    private sealed class ParameterWrites {

        private readonly BlockSyntax body;

        /// <summary>The <c>: this(…)</c> or <c>: base(…)</c> the body runs after, where there is one.</summary>
        private readonly ConstructorInitializerSyntax? initializer;

        private readonly SemanticModel model;

        /// <summary>The bodies that run when something calls them, rather than where they are written.</summary>
        private readonly SyntaxNode[] deferred;

        /// <summary>Whether the body runs in the order it is written, which a <c>goto</c> is the end of.</summary>
        private readonly bool ordered;

        internal ParameterWrites(BaseMethodDeclarationSyntax declaration, BlockSyntax body, SemanticModel model) {
            this.body   = body;
            this.model  = model;
            initializer = (declaration as ConstructorDeclarationSyntax)?.Initializer;

            // Over the declaration rather than the body: the initializer is as able to carry a lambda that
            // writes, or the parameter of a `goto`-bearing body, as the statements below it are.
            ordered  = !declaration.DescendantNodes().OfType<GotoStatementSyntax>().Any();
            deferred = [.. declaration.DescendantNodes()
                                      .Where(node => node is LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax)];
        }

        /// <summary>
        ///     Whether a write to <paramref name="parameter" /> can already have run when
        ///     <paramref name="guard" /> is evaluated.
        /// </summary>
        internal bool Precede(IParameterSymbol parameter, SyntaxNode guard) {
            // A write inside something the body calls runs when it is called, not where it is written: a
            // local function declared below a guard and invoked above it writes before that guard, and the
            // engine follows no call to see it (§9). So position says nothing about it, and it is refused
            // wherever it sits.
            if (deferred.Any(called => Written(called, parameter))) { return true; }

            // A modifier on the argument sits outside the expression the data-flow question below analyses,
            // so that question answers "read" for a parameter the delegation is free to replace.
            if (HandedByReference(parameter)) { return true; }

            return (ordered ? Before(guard) : Everything()).Any(region => Written(region, parameter));
        }

        /// <summary>
        ///     Whether the constructor initializer hands <paramref name="parameter" /> to the constructor it
        ///     delegates to by reference, which is a write the region walk cannot see.
        /// </summary>
        /// <remarks>
        ///     <c>: this(Normalise(ref value))</c> is answered by data flow like any other write — the
        ///     modifier is inside an expression the analysis covers. <c>: this(ref value, true)</c> is not:
        ///     the modifier belongs to the <b>argument</b>, and the region analysed is the bare identifier
        ///     under it, which the compiler reports as read rather than written. Measured: the guard below
        ///     such an initializer read <c>GreaterThanOrEqualTo(0)</c> over a constructor whose delegation
        ///     had already replaced the drawn value, which is the shape a guard must never be read from.
        ///     <para>
        ///         Asked of the invoked symbol rather than of the <c>ref</c> and <c>out</c> keywords, for the
        ///         same reason the write question goes to data flow at all: what the callee may write is a
        ///         fact about the constructor being delegated to, not about how the call is spelled. A named
        ///         argument is matched by name and every other by position.
        ///     </para>
        ///     <para>
        ///         Anything but <see cref="RefKind.None" /> counts, <c>in</c> included, although <c>in</c>
        ///         cannot write: naming the kinds that can would have to be right about every kind the
        ///         language grows, and being wrong there costs a guard read about a value the constructor
        ///         replaced. Being wrong the other way costs one confirmation on an initializer nobody
        ///         writes.
        ///     </para>
        /// </remarks>
        private bool HandedByReference(IParameterSymbol parameter) {
            if (initializer is null || model.GetSymbolInfo(initializer).Symbol is not IMethodSymbol delegated) { return false; }

            SeparatedSyntaxList<ArgumentSyntax> arguments = initializer.ArgumentList.Arguments;

            for (int index = 0; index < arguments.Count; index++) {
                if (Handed(delegated, arguments[index], index) is not { RefKind: not RefKind.None }) { continue; }

                if (model.GetSymbolInfo(arguments[index].Expression).Symbol is IParameterSymbol handed
                 && SymbolEqualityComparer.Default.Equals(handed, parameter)) { return true; }
            }

            return false;
        }

        /// <summary>The parameter of <paramref name="delegated" /> that <paramref name="argument" /> fills.</summary>
        private static IParameterSymbol? Handed(IMethodSymbol delegated, ArgumentSyntax argument, int index) {
            if (argument.NameColon is not null) {
                string named = argument.NameColon.Name.Identifier.ValueText;

                return delegated.Parameters.FirstOrDefault(parameter => parameter.Name == named);
            }

            return index < delegated.Parameters.Length ? delegated.Parameters[index] : null;
        }

        /// <summary>Every region the constructor runs, for when its order cannot be read at all.</summary>
        private IEnumerable<SyntaxNode> Everything() {
            yield return body;

            foreach (SyntaxNode region in Initializer()) { yield return region; }
        }

        /// <summary>
        ///     The arguments of the constructor initializer, which run entire before the body begins.
        /// </summary>
        /// <remarks>
        ///     The one place a write can reach a parameter before the region the walk below covers even
        ///     starts. <c>: this(Normalise(ref value))</c> is an ordinary delegation to the widest overload,
        ///     and it had already replaced the drawn value by the time the first guard of the body ran — read,
        ///     before this, as a bound on what the generator draws. The arguments rather than the initializer
        ///     itself because those are expressions, which is what the compiler will analyse.
        /// </remarks>
        private IEnumerable<SyntaxNode> Initializer() {
            return initializer?.ArgumentList.Arguments.Select(argument => argument.Expression) ?? [];
        }

        /// <summary>The regions that have finished running by the time <paramref name="guard" /> is evaluated.</summary>
        private IEnumerable<SyntaxNode> Before(SyntaxNode guard) {
            // Every guard this is asked about sits in the body, and the initializer has finished by then.
            foreach (SyntaxNode region in Initializer()) { yield return region; }

            SyntaxNode node = guard;

            while (!ReferenceEquals(node, body) && node.Parent is SyntaxNode parent) {
                switch (parent) {
                    // Sequencing, and the one order this walk claims to read: what ran is what is written
                    // above, and a block has no way back to a statement it has left.
                    case BlockSyntax or SwitchSectionSyntax:
                        foreach (StatementSyntax earlier in Earlier(parent, node)) { yield return earlier; }

                        break;

                    // Reaching either branch means the condition was evaluated first, and it alone — the
                    // branch not taken did not run, so it is not a region that finished.
                    case IfStatementSyntax branch when !ReferenceEquals(node, branch.Condition):
                        yield return branch.Condition;

                        break;

                    // Nothing of an `if` runs before its own condition; a clause runs nothing of its own, and
                    // the statement it belongs to answers for it below.
                    case IfStatementSyntax or ElseClauseSyntax
                      or CatchClauseSyntax or CatchFilterClauseSyntax or FinallyClauseSyntax:
                        break;

                    // Everything else, and this is the rule rather than a fallback: a construct whose order
                    // this walk does not claim to read is asked about entire. A loop runs its body again, a
                    // `finally` runs after a `try` that wrote, a `switch` evaluates its governing expression
                    // before the section it picked, a `using` its resource before the body it scopes — one
                    // answer covers all four, and covers the constructs nobody listed here, including the
                    // ones C# has not grown yet. Asking about a superset can only add refusals, never remove
                    // one, which is what makes it the safe default and silence the unsafe one.
                    default:
                        yield return parent;

                        break;
                }

                node = parent;
            }
        }

        /// <summary>The statements of <paramref name="parent" /> that finish before <paramref name="reached" /> begins.</summary>
        private static IEnumerable<StatementSyntax> Earlier(SyntaxNode parent, SyntaxNode reached) {
            return parent.ChildNodes()
                         .OfType<StatementSyntax>()
                         .TakeWhile(statement => statement.Span.End <= reached.SpanStart);
        }

        /// <summary>Whether <paramref name="region" /> writes <paramref name="parameter" />, in any spelling.</summary>
        /// <remarks>
        ///     A region the compiler declines to analyse says nothing, and silence would be read here as
        ///     "not written" — the one answer that turns a guard the engine cannot place into one it emits.
        ///     <para>
        ///         Which is also why a node that is neither a statement nor an expression answers yes rather
        ///         than being skipped: only those two are regions at all, so anything else is a shape the walk
        ///         above did not expect, and an unexpected construct must refuse a guard rather than wave it
        ///         through. It is what keeps the case list up there a matter of precision rather than of
        ///         soundness — forgetting one costs a constraint, never a wrong one.
        ///     </para>
        /// </remarks>
        private bool Written(SyntaxNode region, IParameterSymbol parameter) {
            if (region is not (StatementSyntax or ExpressionSyntax)) { return true; }

            DataFlowAnalysis flow = model.AnalyzeDataFlow(region);

            return !flow.Succeeded || flow.WrittenInside.Contains(parameter, SymbolEqualityComparer.Default);
        }

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
    ///     A type whose values are never below zero, which is what decides how a sign is spelled.
    /// </summary>
    /// <remarks>
    ///     <c>UInt128</c> is named rather than read off <see cref="SpecialType" />, which has no member for it:
    ///     it is an ordinary named type to the compiler, and reading it as signed would put a <c>Positive</c>
    ///     on the one unsigned generator the downlevel asset does not even carry.
    /// </remarks>
    private static bool IsUnsigned(ITypeSymbol type) {
        ITypeSymbol underlying = Underlying(type);

        return underlying.SpecialType is SpecialType.System_Byte or SpecialType.System_UInt16
                                      or SpecialType.System_UInt32 or SpecialType.System_UInt64
            || underlying.ToDisplayString(ByNamespace) == "System.UInt128";
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
