using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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
///         questions of placement: nothing may have written its parameter where it sits, and nothing may
///         decide whether it runs — neither a construct <b>enclosing</b> it nor a jump <b>above</b> it.
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

    private const string EmptinessCheck = "IsNullOrEmpty";

    /// <summary>
    ///     The blankness check, which is <b>not</b> the emptiness one and no longer shares its row.
    /// </summary>
    /// <remarks>
    ///     Both spellings sat in one table and folded onto <c>NonEmpty</c>, on a premise ADR-0075 falsified: a
    ///     floor of one character does not reject the all-whitespace value this guard exists to refuse, and under
    ///     a short ceiling that draw is ordinary rather than rare. <c>NotBlank</c> states it exactly (ADR-0088).
    /// </remarks>
    private const string BlanknessCheck = "IsNullOrWhiteSpace";

    /// <summary>The same two checks, spelled as the throw helper that performs them.</summary>
    private const string EmptinessThrowHelper = "ThrowIfNullOrEmpty";

    private const string BlanknessThrowHelper = "ThrowIfNullOrWhiteSpace";

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
        return Read(method, compilation, names, []);
    }

    /// <summary>
    ///     The same reading, carrying the hops already underway so a cycle of delegation cannot recurse
    ///     forever.
    /// </summary>
    /// <remarks>
    ///     The fold's first draft argued no guard was needed because C# refuses a constructor that calls
    ///     itself (<c>CS0516</c>). That holds for source that COMPILES, and this engine reads whatever the
    ///     developer currently has open — a half-written <c>: this(...)</c> is exactly the state a scaffold
    ///     gets run in. Measured on the earlier build: the process died with a stack overflow rather than
    ///     scaffolding anything, which is the one failure no <c>unread guards</c> mark can soften.
    /// </remarks>
    private static GuardReading Read(IMethodSymbol method, Compilation compilation, TypeNames names,
                                     IReadOnlyCollection<IMethodSymbol> underway) {
        if (underway.Contains(method, SymbolEqualityComparer.Default)) { return GuardReading.WithoutSource(); }

        BaseMethodDeclarationSyntax? declaration = method.DeclaringSyntaxReferences
                                                         .Select(reference => reference.GetSyntax())
                                                         .OfType<BaseMethodDeclarationSyntax>()
                                                         .FirstOrDefault();

        if (declaration?.Body is null) { return GuardReading.WithoutSource(); }

        Compilation? declaring = Declaring(declaration.SyntaxTree, method, compilation);

        if (declaring is null) { return GuardReading.WithoutSource(); }

        SemanticModel model = declaring.GetSemanticModel(declaration.SyntaxTree);

        // Every symbol a reading compares has to come from one compilation, and `method` is the analysed
        // compilation's view of a constructor declared next door. Roslyn hands over the referenced
        // compilation's own symbols only while both bind identical references; the moment they differ — a
        // library on netstandard2.0 under a test project on net8.0, this repository's own arrangement and
        // most others' — it substitutes a view, whose parameters are not the ones a semantic model over the
        // declaring compilation returns. Read through that pair and every guard names a parameter this
        // method does not recognise: no constraint, and no `unread guards` either, a guard naming nothing
        // being indistinguishable from ordinary logic. Resolving the declaration here is what keeps the two
        // sides comparable at every depth — `Mentioned`, `ParameterWrites`, both merges, and the
        // resolved-symbol reads of ADR-0086 — rather than at the one comparison that happened to be looked
        // at. Nothing leaves this file on it: `GuardReading` is keyed by parameter name.
        if (model.GetDeclaredSymbol(declaration) is not IMethodSymbol declared) { return GuardReading.WithoutSource(); }

        GuardReading    reading = GuardReading.FromSource();
        ParameterWrites writes  = new(declaration, declaration.Body, model);

        // The hops already underway travel with the closure rather than as three more parameters, which is
        // also what keeps both merge methods inside the argument count the profile allows. A delegated
        // constructor is resolved through the model above, so the compilation that owns it is the one that
        // travels too — a `base(...)` reaching a third project resolves from there, never from here.
        IReadOnlyCollection<IMethodSymbol> hops          = [.. underway, declared];
        Func<IMethodSymbol, GuardReading>  readDelegated = target => Read(target, declaring, names, hops);

        MergeDelegatedConstructorGuards(declaration, model, declared, writes, reading, readDelegated);
        MergeConstructedReturnGuards(declaration, model, declared, writes, reading, readDelegated);

        // The other half of the question `RunsUnconditionally` asks. That one looks upward, at what encloses
        // a guard; this one looks along, at what precedes it — and a statement above that can jump past the
        // guards below it leaves every one of them unrun on some path the constructor still returns from.
        // It never lifts again: once a path can skip one statement, it can skip all of them.
        bool unskipped = true;

        SyntaxList<StatementSyntax> statements = declaration.Body.Statements;

        for (int index = 0; index < statements.Count; index++) {
            StatementSyntax statement = statements[index];

            // Asked of every statement, leading or not, and before anything decides whether the scan goes on:
            // the idiomatic shape of a validating value object is `XMm = Guard(xMm, nameof(xMm));`, which is
            // also the first assignment to state, so a check made after that decision would miss the very
            // case it exists for (§5.6).
            ReadOrMarkThrowingHelpers(statement, model, declared, reading, writes, unskipped, readDelegated);

            // A second assignment is exempt for the same reason as ADR-0086's: `Field = param ?? throw new
            // ArgumentNullException(nameof(param));` states the same null-check `ArgumentNullException
            // .ThrowIfNull(param)` and `if (param is null) { throw … }` already read (§5.3) — a third
            // spelling of one invariant, fused into the write rather than standing before it. It is read here
            // rather than falling to the marking pass below, because that pass cannot tell "understood, and
            // satisfied by construction" from "rejects, and the engine does not know why" — both syntactically
            // contain a `throw`, and only the recognised shape is the former.
            if (TryRecogniseCoalesceThrowNullCheck(statement, model, declared, out IParameterSymbol? coalesced)) {
                reading.RefuseNull(coalesced!.Name);
                unskipped &= !Jumps(statement, model);

                continue;
            }

            // "Leading" is what makes a guard a guard: past the first assignment to state, an `if` that throws
            // is ordinary logic and says nothing about what the parameter may be. One assignment is exempt
            // (ADR-0086): a guard-library helper assigned straight to a field or property — the documented
            // spelling of the assigned guard idiom — both validates and stores, writes over no parameter, and
            // so leaves everything below it exactly as leading as it was. The call itself is read by the
            // marking pass below, through the same placement questions as any other.
            if (AssignsState(statement, model) && !IsGuardAssignment(statement, model, declared)) {
                // The scan ends here — nothing below is read as a guard any more. A throw carried inside the
                // assignment's own right side (`Code = code.Length switch { < 8 => throw ..., ... };`) is asked
                // about first: the statement rejects a value on some path, and the developer deserves the mark.
                MarkIfItRejects(statement, model, declared, reading);

                // Past the end of the leading scan nothing is read any more, but the body is still walked: a
                // guard written below a write to state rejects values exactly as much as one above it, and
                // passing over it in silence reported a generator as inferred that the constructor still refuses.
                MarkWhatRejectsBelowTheScan(statements.Skip(index + 1), model, declared, reading, names, writes);

                break;
            }

            if (statement is IfStatementSyntax guard) {
                ReadChain(guard, model, declared, reading, names, writes, unskipped);
            } else {
                MarkIfItRejects(statement, model, declared, reading);
                MarkIfValidatedElsewhere(statement, model, declared, reading, names, writes, unskipped);
            }

            unskipped &= !Jumps(statement, model);
        }

        return reading;
    }

    /// <summary>
    ///     A parameter the initializer hands unchanged to the constructor it delegates to is the same value
    ///     there as it is here — a renamed copy, never a computed one — so a guard the delegated constructor
    ///     reads over its own parameter is a guard over this one too.
    /// </summary>
    /// <remarks>
    ///     Scoped as narrowly as ADR-0086's own call-spelling carve-out: the argument has to <b>be</b> the
    ///     parameter, bar nothing. <c>this(percent + 1, ...)</c> hands the delegated constructor a value this
    ///     one never draws, and merging its guard would state an invariant of the wrong value — precisely
    ///     the defect class this file's own remarks already name for composition (§5.4). A parameter the
    ///     initializer writes — by reference, or through a call in one of its own arguments —
    ///     <see cref="ParameterWrites.WrittenByInitializer" /> already excludes from the body's own reading;
    ///     this is additive to that answer, never a second opinion on it, so nothing here reopens a
    ///     placement question <see cref="ParameterWrites" /> already settled.
    ///     <para>
    ///         Reads the delegated constructor through this same method — the mechanism composition and the
    ///         factory path already use to read a different member's guards (ADR-0059's own precedent, one
    ///         hop of it) — never a second, parallel reader. A chain of initializers folds every one of them
    ///         in turn, and C# itself refuses a cycle (<c>CS0516</c>), so nothing here needs to guard against
    ///         one.
    ///     </para>
    /// </remarks>
    private static void MergeDelegatedConstructorGuards(BaseMethodDeclarationSyntax declaration,
                                                         SemanticModel model,
                                                         IMethodSymbol method,
                                                         ParameterWrites writes,
                                                         GuardReading reading,
                                                         Func<IMethodSymbol, GuardReading> readDelegated) {
        if (declaration is not ConstructorDeclarationSyntax { Initializer: { } initializer }) { return; }
        if (model.GetSymbolInfo(initializer).Symbol is not IMethodSymbol delegatedTo) { return; }

        FoldGuardsFromDelegatedConstructor(initializer.ArgumentList, model, method, delegatedTo, reading,
                                           writes.WrittenByInitializer, readDelegated);
    }

    /// <summary>
    ///     Folds a constructor's own guards onto the outer parameters that hand it their values unchanged —
    ///     the shared half of <see cref="MergeDelegatedConstructorGuards" /> and
    ///     <see cref="MergeConstructedReturnGuards" />, which differ only in WHERE a hop of delegation is
    ///     found and in what "written before reaching it" means there.
    /// </summary>
    private static void FoldGuardsFromDelegatedConstructor(ArgumentListSyntax argumentList,
                                                            SemanticModel model,
                                                            IMethodSymbol method,
                                                            IMethodSymbol delegatedTo,
                                                            GuardReading reading,
                                                            Func<IParameterSymbol, bool> writtenBeforeHandoff,
                                                            Func<IMethodSymbol, GuardReading> readDelegated) {
        SeparatedSyntaxList<ArgumentSyntax> arguments        = argumentList.Arguments;
        GuardReading?                       delegatedReading = null;
        Dictionary<string, string>          handed           = new(StringComparer.Ordinal);

        GuardReading Delegated() {
            return delegatedReading ??= readDelegated(delegatedTo);
        }

        // A call that fills a `params` target in expanded form hands over ELEMENTS, so no argument of it maps
        // to the parameter a guard would be about. Asked once for the whole call rather than per argument.
        bool expanded = FillsAnExpandedParams(argumentList.Parent!, model);

        for (int index = 0; index < arguments.Count; index++) {
            ArgumentSyntax argument = arguments[index];

            IParameterSymbol? handedTo = HandedTo(argument, index, delegatedTo);

            // Not this method's parameter handed over bare. It may still COMPUTE from one — `value + 1`,
            // `value.Trim()` — and then a real conjunct is lost across arithmetic the engine cannot follow.
            // That is the fold's fourth decline, and the one that used to be silent.
            if (HandedFrom(argument, model, method) is not { } handedFrom) {
                MarkComputedHandoff(argument, model, method, handedTo, reading, Delegated);

                continue;
            }

            // Declining is right wherever the value that reaches the delegated constructor is not the one
            // this generator draws, or wherever the engine cannot tell. Declining in SILENCE is what was
            // wrong: the mark goes on exactly when something is lost by the decline, and stays off when
            // nothing is — marking a hand-off the delegated constructor is silent about would refuse shapes
            // that read perfectly, which is the mirror defect.
            if (Declines(expanded, handedTo, writtenBeforeHandoff(handedFrom), Delegated())) {
                if (handedTo is null || SaysSomethingAbout(Delegated(), handedTo)) { reading.MarkUnread(handedFrom.Name, UnreadGuard.At(argument)); }

                continue;
            }

            FoldOnto(handedFrom.Name, handedTo!.Name, Delegated(), reading);

            handed[handedTo.Name] = handedFrom.Name;
        }

        FoldTies(delegatedReading, handed, reading);
    }

    /// <summary>
    ///     Everything the delegated reading says about its own parameter <paramref name="to" />, said of the outer
    ///     parameter <paramref name="from" /> handed to it unchanged: the doubt, the constraints, the refusal of
    ///     null, and a size past the cap on either side.
    /// </summary>
    private static void FoldOnto(string from, string to, GuardReading delegated, GuardReading reading) {
        // Doubt travels with the constraints: a guard the delegated constructor could not read is a guard over
        // THIS parameter too — every one of them, so the count of further guards survives the fold (#373).
        foreach (UnreadGuard at in delegated.UnreadMarks(to)) { reading.MarkUnread(from, at); }

        foreach (GuardConstraint constraint in delegated.For(to)) { reading.Add(from, constraint); }

        if (delegated.RefusesNull(to)) { reading.RefuseNull(from); }
        if (delegated.CeilingBeyondTheCap(to) is { } ceiling) { reading.Add(from, GuardConstraint.CeilingBeyondTheCap(ceiling)); }
        if (delegated.FloorBeyondTheCap(to) is { } floor) { reading.Add(from, GuardConstraint.FloorBeyondTheCap(floor)); }
    }

    /// <summary>
    ///     An order the delegated constructor reads between two of its parameters is one between the outer
    ///     parameters handed to both — and where only one side was handed over unchanged, it is a rule over a value
    ///     this generator does not draw, and the side it does draw is marked, as a declined hand-off is (#405).
    /// </summary>
    private static void FoldTies(GuardReading? delegated, IReadOnlyDictionary<string, string> handed, GuardReading reading) {
        foreach ((OrderedPairing pairing, UnreadGuard at) in delegated?.Ties ?? []) {
            handed.TryGetValue(pairing.First, out string? from);
            handed.TryGetValue(pairing.Second, out string? to);

            if (from is not null && to is not null && from != to) {
                reading.Tie(new OrderedPairing(from, to, pairing.Strict), at);

                continue;
            }

            if (from is not null) { reading.MarkUnread(from, at); }
            if (to is not null) { reading.MarkUnread(to, at); }
        }

        // A rule the delegated constructor could not read between two of its parameters is one between the outer
        // parameters handed to both, so it can still be pointed to the pair that draws them (#459).
        foreach ((string first, string second, UnreadGuard at) in delegated?.RulesBetweenTwo ?? []) {
            if (handed.TryGetValue(first, out string? outerFirst) && handed.TryGetValue(second, out string? outerSecond) && outerFirst != outerSecond) {
                reading.RuleBetween(outerFirst, outerSecond, at);
            }
        }
    }

    /// <summary>
    ///     The outer constructor's own parameter a bare initializer argument names — never one it writes,
    ///     wraps, or computes from, since only an unmodified identifier can carry the same value the
    ///     delegated constructor's guard would be about.
    /// </summary>
    private static IParameterSymbol? HandedFrom(ArgumentSyntax argument, SemanticModel model, IMethodSymbol method) {
        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None)) { return null; }
        if (Unsuppressed(argument.Expression) is not IdentifierNameSyntax identifier) { return null; }
        if (model.GetSymbolInfo(identifier).Symbol is not IParameterSymbol parameter) { return null; }

        return method.Parameters.Contains(parameter, SymbolEqualityComparer.Default) ? parameter : null;
    }

    /// <summary>
    ///     Whether the fold must decline this hand-off — the four shapes where the value the delegated
    ///     constructor guards is not the one this generator draws, or where the engine cannot tell.
    /// </summary>
    /// <remarks>
    ///     In order: the call fills a <c>params</c> target one element at a time, so a guard about the array
    ///     is not about the argument; the target parameter could not be told at all; a leading statement
    ///     rewrites the parameter before the hand-off; and the delegated reading has no source, which is a
    ///     third state — not "no guards" and not "unread guards", but "there was nothing here to read". That
    ///     last one is the channel the first fix left behind, and the cycle guard's own return arrives on it.
    /// </remarks>
    private static bool Declines(bool expanded, IParameterSymbol? handedTo, bool rewritten, GuardReading delegated) {
        return expanded || handedTo is null || rewritten || !delegated.SourceAvailable;
    }

    /// <summary>
    ///     Marks every parameter a declined argument computes from, where the delegated constructor had
    ///     something to say about the position that argument fills.
    /// </summary>
    /// <remarks>
    ///     A literal or another local mentions no parameter of ours and is declined in silence, which is
    ///     right: the delegated guard over that position says nothing about anything this generator draws.
    /// </remarks>
    private static void MarkComputedHandoff(ArgumentSyntax argument,
                                            SemanticModel model,
                                            IMethodSymbol method,
                                            IParameterSymbol? handedTo,
                                            GuardReading reading,
                                            Func<GuardReading> delegated) {
        if (handedTo is null || !SaysSomethingAbout(delegated(), handedTo)) { return; }

        foreach (IParameterSymbol mentioned in Mentioned(argument.Expression, model, method)) {
            reading.MarkUnread(mentioned.Name, UnreadGuard.At(argument));
        }
    }

    /// <summary>
    ///     Whether the delegated reading has anything to say about <paramref name="target" /> — a constraint,
    ///     a mark, or the fact that its own source could not be read at all.
    /// </summary>
    /// <remarks>
    ///     Asked wherever the fold DECLINES, so that a decline speaks exactly when something is lost by it
    ///     and stays quiet when nothing is. Marking a hand-off the delegated constructor is silent about
    ///     would refuse shapes that read perfectly, which is the mirror defect and costs real coverage.
    /// </remarks>
    private static bool SaysSomethingAbout(GuardReading delegated, IParameterSymbol target) {
        return !delegated.SourceAvailable
            || delegated.Unread(target.Name)
            || delegated.For(target.Name).Count > 0
            || delegated.Tied(target.Name)
            || delegated.FloorBeyondTheCap(target.Name) is not null
            || delegated.CeilingBeyondTheCap(target.Name) is not null;
    }

    /// <summary>
    ///     The expression with a null-forgiving <c>!</c> stripped — <c>value!</c> hands over the value
    ///     <c>value</c> holds, since the operator is a compile-time annotation with no run-time effect.
    /// </summary>
    /// <remarks>
    ///     A cast is deliberately NOT unwrapped here even though it looks similar: <c>(byte)value</c> hands
    ///     over a different number, and folding a guard across it would state an invariant of a value the
    ///     generator never draws.
    /// </remarks>
    private static ExpressionSyntax Unsuppressed(ExpressionSyntax expression) {
        return expression is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed
                   ? suppressed.Operand
                   : expression;
    }

    /// <summary>The delegated constructor's own parameter <paramref name="argument" /> fills.</summary>
    private static IParameterSymbol? HandedTo(ArgumentSyntax argument, int index, IMethodSymbol delegatedTo) {
        if (argument.NameColon is not null) {
            string named = argument.NameColon.Name.Identifier.ValueText;

            return delegatedTo.Parameters.FirstOrDefault(parameter => parameter.Name == named);
        }

        if (index >= delegatedTo.Parameters.Length) { return null; }

        return delegatedTo.Parameters[index];
    }

    /// <summary>
    ///     Whether the call fills a <c>params</c> target in its EXPANDED form, where the argument is one
    ///     element of the array rather than the array itself.
    /// </summary>
    /// <remarks>
    ///     A guard about the array — its length, its contents — is not about a single element handed in, so
    ///     an expanded call must not fold. Normal form must still fold: measured, refusing it costs a guard
    ///     the engine reads correctly about exactly the value the generator draws. The compiler already
    ///     decided which form this is and says so on the bound operation, so nothing here re-derives it —
    ///     and asking the semantic model how a call bound is not the control-flow graph ADR-0084 rules out.
    /// </remarks>
    private static bool FillsAnExpandedParams(SyntaxNode call, SemanticModel model) {
        return model.GetOperation(call) is IInvocationOperation or IObjectCreationOperation
            && model.GetOperation(call) is { } operation
            && Arguments(operation).Any(argument => argument.ArgumentKind == ArgumentKind.ParamArray);
    }

    private static IEnumerable<IArgumentOperation> Arguments(IOperation operation) {
        return operation switch {
            IInvocationOperation invocation   => invocation.Arguments,
            IObjectCreationOperation creation => creation.Arguments,
            _                                 => Enumerable.Empty<IArgumentOperation>()
        };
    }

    /// <summary>
    ///     A factory whose body constructs and returns the type through <c>new SomeType(args)</c> — §5.1's
    ///     canonical shape, a private constructor behind a public <c>Create</c> — hands its own parameters to
    ///     that constructor exactly as a <c>: this(...)</c> initializer delegates to one, and the same
    ///     reasoning applies unchanged: a bare, unmodified argument carries the same value there as here, so
    ///     the constructed type's own guard over it is a guard over this parameter too.
    /// </summary>
    /// <remarks>
    ///     Scoped to the body's TOP-LEVEL statements — never one reached through a branch, a loop, or
    ///     anything else the leading scan does not already vouch for running: a <c>return</c> the engine
    ///     cannot show always executes says nothing certain about the value drawn, exactly the placement
    ///     question <see cref="ParameterWrites.Precede" /> already answers for every other guard, asked here
    ///     of the return statement itself rather than of an <c>if</c>. A parameter a leading statement writes
    ///     before reaching that return — <c>value = value.Trim(); return new Reference(value);</c> — is
    ///     excluded by the same check: what the constructed type is guarding is the trimmed value, not the
    ///     one the generator draws.
    /// </remarks>
    private static void MergeConstructedReturnGuards(BaseMethodDeclarationSyntax declaration,
                                                      SemanticModel model,
                                                      IMethodSymbol method,
                                                      ParameterWrites writes,
                                                      GuardReading reading,
                                                      Func<IMethodSymbol, GuardReading> readDelegated) {
        foreach (StatementSyntax statement in declaration.Body!.Statements) {
            if (statement is not ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax creation }) {
                continue;
            }

            if (creation.ArgumentList is not { } argumentList) { continue; }
            if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol constructed) { continue; }

            FoldGuardsFromDelegatedConstructor(argumentList, model, method, constructed, reading,
                                               parameter => writes.Precede(parameter, statement),
                                               readDelegated);
        }
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
                                  ParameterWrites writes,
                                  bool unskipped) {
        if (!ThrowsUnconditionally(branch.Statement)) {
            MarkIfItRejects(branch, model, method, reading);

            // Handed the `if` itself, so every call under it is conditioned by definition and this can only
            // mark. It is called for the marking, which silence would otherwise cost (§9).
            MarkIfValidatedElsewhere(branch, model, method, reading, names, writes, unskipped);

            return;
        }

        ReadOne(branch.Condition, model, method, reading, names, writes, unskipped);

        switch (branch.Else?.Statement) {
            case IfStatementSyntax elseIf:
                ReadChain(elseIf, model, method, reading, names, writes, unskipped);

                break;
            case StatementSyntax terminal:
                // No condition of its own to read, but a plain `else` that still throws is a reject the closed
                // set has nothing to say about — §9's `unread guards`, not silence.
                MarkIfItRejects(terminal, model, method, reading);
                MarkIfValidatedElsewhere(terminal, model, method, reading, names, writes, unskipped);

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

        // What the exception is built from is excluded along with the `nameof`: a parameter interpolated into
        // the message — `$"Row {number} is wrong."` — is named for a reader there, exactly as `nameof(number)`
        // names it, and a message rejects nothing (#370). What decides the throw still counts in full.
        // Pointed at the statement that throws rather than at the leading statement holding it: a guard inside a
        // loop is the `if` in its body, and the loop header the reader was sent to tests nothing (#371).
        StatementSyntax throwing = ThrowingStatementIn(statement);

        string? pattern = RegexPatternIn(throwing, model);

        UnreadGuard        at       = UnreadGuard.At(throwing, pattern: pattern);
        IParameterSymbol[] rejected = [.. Mentioned(statement, model, method, ignoresWhatIsThrown: true).Concat(ReachedThroughCopies(statement, model, method, ignoresWhatIsThrown: true)).Distinct<IParameterSymbol>(SymbolEqualityComparer.Default)];

        foreach (IParameterSymbol parameter in rejected) { reading.MarkUnread(parameter.Name, at); }

        // Recorded like the rule of a plain if-throw, so a pair drawing both can be named in the note (#459).
        if (rejected.Length == 2) { reading.RuleBetween(rejected[0].Name, rejected[1].Name, at); }

        MarkEveryParameterIfTheTrailIsLost(DecidingParts(statement), throwing, model, method, reading);
    }

    /// <summary>
    ///     The statement a reader opens the file at for what <paramref name="statement" /> throws: the innermost
    ///     statement holding its first throw — the <c>if</c> whose branch throws, never the loop, the <c>try</c>
    ///     or the <c>using</c> around it — or <paramref name="statement" /> itself where it holds none.
    /// </summary>
    /// <remarks>
    ///     The mark is earned by the leading statement entire, and so it was pointed at: <c>foreach (int item in
    ///     items)</c>, where the guard sits two lines down. The CLI page promises the opposite — a place the
    ///     developer need not search the constructor from — and the throw's own line is the one that keeps it
    ///     (#371). A <c>throw</c> statement and the block holding it are skipped on the way up: the statement
    ///     worth reading is the one that decides the throw, with its condition on the same line.
    /// </remarks>
    private static StatementSyntax ThrowingStatementIn(StatementSyntax statement) {
        SyntaxNode? thrown = statement.DescendantNodesAndSelf(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                                      .FirstOrDefault(node => node is ThrowStatementSyntax or ThrowExpressionSyntax);

        if (thrown is null) { return statement; }

        foreach (SyntaxNode ancestor in thrown.AncestorsAndSelf()) {
            if (ancestor == statement) { break; }
            if (ancestor is StatementSyntax holding and not (ThrowStatementSyntax or BlockSyntax)) { return holding; }
        }

        return statement;
    }

    /// <summary>
    ///     The parameters a local, field or property that <paramref name="node" /> names was computed from,
    ///     followed back through every copy (ADR-0110).
    /// </summary>
    /// <remarks>
    ///     <c>bool ok = value.StartsWith("LOT-"); if (!ok) { throw … }</c> rejects <c>value</c> exactly as
    ///     much as testing it in place would, and yet the condition names no parameter: read only for what it
    ///     names, it marked nothing, and the recap reported a generator the constructor refused on nearly
    ///     every draw as inferred. <c>_name = name; if (_name.Length &gt; 5) { throw … }</c> is the same guard
    ///     through a member rather than a local, and <c>string trimmed = value.Trim(); string upper =
    ///     trimmed.ToUpperInvariant();</c> the same again, two copies away. The parameter each chain starts from
    ///     is <b>marked</b> — never read: what a copied or computed value admits is not a constraint the closed
    ///     set can state about the parameter, and deriving one would be the propagation ADR-0046 refuses. Marking
    ///     derives nothing, so it follows the copy to the end, which was one step only until ADR-0110 and left a
    ///     chain of two silent. The <c>nameof</c> of a message is excluded here for the reason
    ///     <see cref="Mentioned" /> gives.
    /// </remarks>
    private static IEnumerable<IParameterSymbol> ReachedThroughCopies(SyntaxNode node, SemanticModel model, IMethodSymbol method, bool ignoresWhatIsThrown = false) {
        return ReachedThroughCopies([node], model, method, ignoresWhatIsThrown);
    }

    private static IEnumerable<IParameterSymbol> ReachedThroughCopies(IReadOnlyList<SyntaxNode> nodes, SemanticModel model, IMethodSymbol method, bool ignoresWhatIsThrown = false) {
        return Trail.Follow(nodes, model, method, ignoresWhatIsThrown).Reached();
    }

    /// <summary>
    ///     Marks every parameter unread when a value deciding a throw comes from where the trail is lost
    ///     (ADR-0110).
    /// </summary>
    /// <remarks>
    ///     Where <see cref="Trail" /> cannot follow a value — a getter written by hand, a method of the object
    ///     being built — the engine cannot say which parameters the rejection involves, and silence would read
    ///     "none of them". Only what decides a throw counts, never what the exception is built from: a message
    ///     rejects nothing.
    /// </remarks>
    private static void MarkEveryParameterIfTheTrailIsLost(IEnumerable<SyntaxNode> deciding, SyntaxNode where, SemanticModel model, IMethodSymbol method, GuardReading reading) {
        if (!Trail.Follow(deciding, model, method, ignoresWhatIsThrown: true).Lost) { return; }

        // Said at the place named, because the place may test none of the parameter's own: a reader sent to a
        // loop over `buses` for a mark on `name` would look there for a guard on `name` that is not there (#371).
        foreach (IParameterSymbol parameter in method.Parameters) { reading.MarkUnread(parameter.Name, UnreadGuard.At(where, TrailLost)); }
    }

    /// <summary>What the trail-lost mark of ADR-0110 says beside the place it names (#371).</summary>
    private const string TrailLost = "every parameter is marked here, since what decides this throw could not be traced back to one";

    /// <summary>
    ///     What decides whether each throw of <paramref name="statement" /> runs: the conditions, loop headers,
    ///     switch subjects and labels, and left operands between it and the statement.
    /// </summary>
    /// <remarks>
    ///     A throw inside a lambda or a local function is not one the constructor runs by evaluating this
    ///     statement, and is passed over. A construct whose order is not modelled here — a <c>try</c>, a
    ///     <c>using</c>, a <c>lock</c> — is asked about entire, the polarity ADR-0084 gives placement: a superset
    ///     can only add refusals.
    /// </remarks>
    private static IEnumerable<SyntaxNode> DecidingParts(StatementSyntax statement) {
        return statement.DescendantNodesAndSelf(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                        .Where(node => node is ThrowStatementSyntax or ThrowExpressionSyntax)
                        .SelectMany(thrown => Deciding(thrown, statement));
    }

    /// <summary>One throw of <see cref="DecidingParts" />.</summary>
    private static IReadOnlyList<SyntaxNode> Deciding(SyntaxNode thrown, StatementSyntax statement) {
        if (thrown == statement) { return []; }

        List<SyntaxNode> parts = [];
        SyntaxNode       child = thrown;

        foreach (SyntaxNode ancestor in thrown.Ancestors()) {
            if (!AddWhatDecides(ancestor, child, parts)) { return [statement]; }
            if (ancestor == statement) { break; }

            child = ancestor;
        }

        return parts;
    }

    /// <summary>
    ///     Adds what <paramref name="ancestor" /> decides about reaching <paramref name="child" />, or answers
    ///     false for a construct this does not model.
    /// </summary>
    private static bool AddWhatDecides(SyntaxNode ancestor, SyntaxNode child, List<SyntaxNode> parts) {
        switch (ancestor) {
            case IfStatementSyntax branch:
                if (branch.Condition != child) { parts.Add(branch.Condition); }

                return true;
            case ConditionalExpressionSyntax conditional:
                if (conditional.Condition != child) { parts.Add(conditional.Condition); }

                return true;
            case BinaryExpressionSyntax binary when binary.Right == child
                                                 && (binary.IsKind(SyntaxKind.CoalesceExpression)
                                                  || binary.IsKind(SyntaxKind.LogicalAndExpression)
                                                  || binary.IsKind(SyntaxKind.LogicalOrExpression)):
                parts.Add(binary.Left);

                return true;
            case ConditionalAccessExpressionSyntax access when access.WhenNotNull == child:
                parts.Add(access.Expression);

                return true;
            case SwitchExpressionSyntax switched:
                parts.Add(switched.GoverningExpression);
                parts.AddRange(switched.Arms.SelectMany(arm => arm.ChildNodes().Where(part => part != arm.Expression)));

                return true;
            case SwitchStatementSyntax switched:
                parts.Add(switched.Expression);
                parts.AddRange(switched.Sections.SelectMany(section => section.Labels));

                return true;
            case WhileStatementSyntax loop:
                parts.Add(loop.Condition);

                return true;
            case DoStatementSyntax loop:
                parts.Add(loop.Condition);

                return true;
            case ForStatementSyntax loop:
                parts.AddRange(loop.ChildNodes().Where(part => part != loop.Statement));

                return true;
            case CommonForEachStatementSyntax loop:
                parts.Add(loop.Expression);

                return true;
            case BlockSyntax or ElseClauseSyntax or ExpressionStatementSyntax or LocalDeclarationStatementSyntax
              or ReturnStatementSyntax or ThrowStatementSyntax or SwitchSectionSyntax or SwitchExpressionArmSyntax:
                return true;
            default:
                return ancestor is not (StatementSyntax or CatchClauseSyntax or FinallyClauseSyntax);
        }
    }

    /// <summary>
    ///     A walk back from what an expression names to the parameters its value was computed from, through any
    ///     number of copies (ADR-0110).
    /// </summary>
    /// <remarks>
    ///     Marking through a copy derives no bound — it only declines to vouch — so nothing ADR-0046 refuses is
    ///     built here, and following further can only add refusals. Every write to a local, field or property
    ///     anywhere in the constructor is taken, not only the writes above the expression: a superset, as
    ///     ADR-0084 asks of a region, and the one that covers a value a loop carries back above its own test.
    ///     The walk is <b>lost</b> where it would have to look inside code it does not read: a property whose
    ///     accessors are written by hand, a method or local function of the object being built, the object itself
    ///     handed on, a member a base constructor sets, a local nothing in the constructor writes — and a member
    ///     of the object as soon as code it does not read may write it too: a method of the object the
    ///     constructor calls, or the constructor it delegates to with <c>: this(…)</c>.
    /// </remarks>
    private sealed class Trail {

        private readonly HashSet<ISymbol>          followed = new(SymbolEqualityComparer.Default);
        private readonly bool                      ignoresWhatIsThrown;
        private readonly IMethodSymbol             method;
        private readonly SemanticModel             model;
        private readonly Stack<(SyntaxNode Node, bool Copied)> pending = new();
        private readonly HashSet<IParameterSymbol> reached  = new(SymbolEqualityComparer.Default);
        private readonly SyntaxNode?               within;
        private readonly bool                      writtenUnread;

        private Trail(SemanticModel model, IMethodSymbol method, SyntaxNode? within, bool ignoresWhatIsThrown) {
            this.model               = model;
            this.method              = method;
            this.within              = within;
            this.ignoresWhatIsThrown = ignoresWhatIsThrown;
            writtenUnread            = WritesTheObjectUnread();
        }

        /// <summary>Whether some value on the way comes from where the walk cannot follow it.</summary>
        internal bool Lost { get; private set; }

        /// <summary>
        ///     Follows <paramref name="roots" /> back to the parameters, past the exception a throw builds when
        ///     <paramref name="ignoresWhatIsThrown" /> says only what decides the throw counts.
        /// </summary>
        internal static Trail Follow(IEnumerable<SyntaxNode> roots, SemanticModel model, IMethodSymbol method, bool ignoresWhatIsThrown) {
            SyntaxNode[] starts = [.. roots];
            Trail        trail  = new(model, method, starts.FirstOrDefault()?.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>(), ignoresWhatIsThrown);

            foreach (SyntaxNode start in starts) { trail.pending.Push((start, false)); }

            while (trail.pending.Count > 0) {
                (SyntaxNode node, bool copied) = trail.pending.Pop();

                trail.Visit(node, copied);
            }

            return trail;
        }

        /// <summary>The parameters of the constructor the walk reached through at least one copy.</summary>
        internal IReadOnlyCollection<IParameterSymbol> Reached() {
            return [.. reached];
        }

        /// <summary>
        ///     Steps through every value <paramref name="node" /> names; a parameter named in a starting node
        ///     itself is not reached through a copy, and is <see cref="Mentioned" />'s to report.
        /// </summary>
        private void Visit(SyntaxNode node, bool copied) {
            InvocationExpressionSyntax[] spelled = [.. node.DescendantNodesAndSelf()
                                                           .OfType<InvocationExpressionSyntax>()
                                                           .Where(IsNameOf)];
            SyntaxNode[] counted = [.. node.DescendantNodesAndSelf(descendant => !IsSkipped(descendant, spelled))
                                           .Where(descendant => !IsSkipped(descendant, spelled))];

            Lost |= counted.Any(HandsOnTheObject);

            foreach (IdentifierNameSyntax identifier in counted.OfType<IdentifierNameSyntax>()) { Step(identifier, copied); }
        }

        /// <summary>A <c>nameof</c>, or the exception a throw builds when only what decides it counts.</summary>
        private bool IsSkipped(SyntaxNode node, InvocationExpressionSyntax[] spelled) {
            return spelled.Contains(node) || (ignoresWhatIsThrown && IsWhatIsThrown(node));
        }

        /// <summary>The object being built, handed on as a value rather than reached a member of.</summary>
        private static bool HandsOnTheObject(SyntaxNode node) {
            return node is ThisExpressionSyntax or BaseExpressionSyntax
                && node.Parent is not MemberAccessExpressionSyntax;
        }

        private void Step(IdentifierNameSyntax identifier, bool copied) {
            ISymbol? symbol = model.GetSymbolInfo(identifier).Symbol;

            if (symbol is IParameterSymbol parameter) {
                if (copied && method.Parameters.Contains(parameter, SymbolEqualityComparer.Default)) { reached.Add(parameter); }

                return;
            }

            if (symbol is null || symbol.IsStatic || HasAReceiverOfItsOwn(identifier) || !followed.Add(symbol)) { return; }

            switch (symbol) {
                case ILocalSymbol:
                    Lost |= !PushSources(symbol);

                    break;
                case IFieldSymbol or IPropertySymbol when SymbolEqualityComparer.Default.Equals(symbol.ContainingType, method.ContainingType)
                                                       && HoldsWhatItIsAssigned(symbol):
                    Lost |= writtenUnread;
                    PushSources(symbol);

                    break;
                case IFieldSymbol or IPropertySymbol or IMethodSymbol or IEventSymbol:
                    Lost |= IsOfTheObject(symbol);

                    break;
            }
        }

        /// <summary>
        ///     Whether the constructor runs code the walk does not read that may write the object's members: a
        ///     method or local function of the object it calls, or the constructor it delegates to.
        /// </summary>
        private bool WritesTheObjectUnread() {
            if (within is ConstructorDeclarationSyntax { Initializer: { } initializer }
             && initializer.IsKind(SyntaxKind.ThisConstructorInitializer)) {
                return true;
            }

            return within?.DescendantNodes()
                          .OfType<InvocationExpressionSyntax>()
                          .Select(invocation => invocation.Expression)
                          .Any(called => called is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax }
                                      && model.GetSymbolInfo(called).Symbol is IMethodSymbol { IsStatic: false } target
                                      && (target.MethodKind == MethodKind.LocalFunction || IsOfTheObject(target)))
                ?? false;
        }

        /// <summary>A member reached through some other value, which the walk follows instead.</summary>
        private static bool HasAReceiverOfItsOwn(IdentifierNameSyntax identifier) {
            return identifier.Parent switch {
                MemberAccessExpressionSyntax access => access.Name == identifier && access.Expression is not (ThisExpressionSyntax or BaseExpressionSyntax),
                MemberBindingExpressionSyntax       => true,
                _                                   => false
            };
        }

        /// <summary>A member of the object being built, or of a type it derives from, or a local function.</summary>
        private bool IsOfTheObject(ISymbol symbol) {
            for (INamedTypeSymbol? type = method.ContainingType; type is not null; type = type.BaseType) {
                if (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, type)) { return true; }
            }

            return false;
        }

        /// <summary>Queues every expression <paramref name="symbol" /> takes its value from; false when there is none.</summary>
        private bool PushSources(ISymbol symbol) {
            SyntaxNode[] sources = [.. symbol.DeclaringSyntaxReferences
                                             .Select(reference => reference.GetSyntax())
                                             .Where(declaring => declaring.SyntaxTree == model.SyntaxTree)
                                             .Select(DeclaredFrom)
                                             .OfType<SyntaxNode>()
                                             .Concat(WritesTo(symbol))];

            foreach (SyntaxNode source in sources) { pending.Push((source, true)); }

            return sources.Length > 0;
        }

        /// <summary>The right side of every write to <paramref name="symbol" /> in the constructor, or the call writing it.</summary>
        private IEnumerable<SyntaxNode> WritesTo(ISymbol symbol) {
            if (within is null) { return []; }

            IEnumerable<SyntaxNode> assigned = within.DescendantNodes()
                                                     .OfType<AssignmentExpressionSyntax>()
                                                     .Where(assignment => Names(assignment.Left, symbol))
                                                     .Select(assignment => assignment.Right);
            IEnumerable<SyntaxNode> passed = within.DescendantNodes()
                                                   .OfType<ArgumentSyntax>()
                                                   .Where(argument => !argument.RefKindKeyword.IsKind(SyntaxKind.None) && Names(argument.Expression, symbol))
                                                   .Select(argument => argument.Parent?.Parent)
                                                   .OfType<SyntaxNode>();

            return assigned.Concat(passed);
        }

        /// <summary>Whether <paramref name="target" /> is <paramref name="symbol" />, or a tuple naming it.</summary>
        private bool Names(ExpressionSyntax target, ISymbol symbol) {
            return target is TupleExpressionSyntax tuple
                       ? tuple.Arguments.Any(element => Names(element.Expression, symbol))
                       : SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(target).Symbol, symbol);
        }

        /// <summary>The expression a declaration takes its value from, whatever form the declaration takes.</summary>
        private static SyntaxNode? DeclaredFrom(SyntaxNode declaring) {
            switch (declaring) {
                case VariableDeclaratorSyntax declarator:  return declarator.Initializer?.Value;
                case PropertyDeclarationSyntax property:   return property.Initializer?.Value;
                case CommonForEachStatementSyntax loop:     return loop.Expression;
            }

            foreach (SyntaxNode ancestor in declaring.Ancestors()) {
                switch (ancestor) {
                    case ArgumentSyntax { Parent.Parent: { } call }: return call;
                    case IsPatternExpressionSyntax pattern:         return pattern.Expression;
                    case SwitchExpressionSyntax switched:           return switched.GoverningExpression;
                    case SwitchStatementSyntax switched:            return switched.Expression;
                    case AssignmentExpressionSyntax assignment:     return assignment.Right;
                    case CommonForEachStatementSyntax loop:         return loop.Expression;
                    case StatementSyntax:                           return null;
                }
            }

            return null;
        }

    }

    /// <summary>
    ///     The expressions every local, field or property <paramref name="node" /> names took its value from,
    ///     past any <c>nameof</c> inside it.
    /// </summary>
    private static IEnumerable<ExpressionSyntax> CopySources(SyntaxNode node, SemanticModel model) {
        return NamedSymbols(node, model).SelectMany(symbol => CopiedFrom(symbol, node, model));
    }

    /// <summary>Every symbol <paramref name="node" /> names, past any <c>nameof</c> inside it.</summary>
    private static IEnumerable<ISymbol?> NamedSymbols(SyntaxNode node, SemanticModel model) {
        InvocationExpressionSyntax[] spelled = [.. node.DescendantNodesAndSelf()
                                                       .OfType<InvocationExpressionSyntax>()
                                                       .Where(IsNameOf)];

        return node.DescendantNodesAndSelf()
                   .OfType<IdentifierNameSyntax>()
                   .Where(identifier => !spelled.Any(name => name.Contains(identifier)))
                   .Select(identifier => model.GetSymbolInfo(identifier).Symbol);
    }

    /// <summary>
    ///     Whether reading <paramref name="symbol" /> gives back what was assigned to it: true of anything but a
    ///     property with a hand-written accessor.
    /// </summary>
    /// <remarks>
    ///     An auto-implemented property is the one property the compiler backs with a field of its own, which is
    ///     how it is recognised — whatever spelling its accessors take. Any other property runs code on the way
    ///     in or out, and <c>set { stored = value.Length &gt;= 5 ? value : null; }</c> turns a non-null value
    ///     into null (#333).
    /// </remarks>
    private static bool HoldsWhatItIsAssigned(ISymbol? symbol) {
        return symbol is not IPropertySymbol property
            || property.ContainingType.GetMembers()
                       .OfType<IFieldSymbol>()
                       .Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, property));
    }

    /// <summary>
    ///     Whether <paramref name="source" /> is a parameter itself, unchanged — through parentheses, a built-in
    ///     cast or a null-forgiving <c>!</c>, none of which can turn a non-null value into null, and through any
    ///     number of plain copies.
    /// </summary>
    /// <remarks>
    ///     A user-defined conversion is not unwrapped: <c>explicit operator ProductCode(string raw)</c> is a
    ///     method, free to return null for a value it rejects, so a null-check on its result is a real guard.
    ///     The same holds when the assignment applies it implicitly — <c>Code = raw;</c> through an
    ///     <c>implicit operator</c> — which is the conversion <see cref="SemanticModel" /> reports on the
    ///     right side itself, where no cast is written to look at.
    ///     <para>
    ///         <c>string first = value; string second = first;</c> holds the parameter as surely as one copy
    ///         does: since marking follows a copy to the end (ADR-0110), so does the proof that a null-check on
    ///         it is satisfied by construction, or a chain of two would earn a mark one copy does not.
    ///     </para>
    /// </remarks>
    private static bool IsTheParameterItself(ExpressionSyntax source, SemanticModel model) {
        return IsTheParameterItself(source, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }

    /// <summary>One step of <see cref="IsTheParameterItself(ExpressionSyntax, SemanticModel)" />, past the copies already followed.</summary>
    private static bool IsTheParameterItself(ExpressionSyntax source, SemanticModel model, HashSet<ISymbol> followed) {
        if (model.GetConversion(source).IsUserDefined) { return false; }

        ExpressionSyntax unwrapped = source switch {
            ParenthesizedExpressionSyntax parenthesized                                          => parenthesized.Expression,
            CastExpressionSyntax cast when !IsUserDefinedConversion(cast, model)                 => cast.Expression,
            PostfixUnaryExpressionSyntax forgiven when forgiven.IsKind(SyntaxKind.SuppressNullableWarningExpression) => forgiven.Operand,
            _                                                                                    => source
        };

        return unwrapped == source
                   ? IsAParameterOrAPlainCopy(model.GetSymbolInfo(source).Symbol, source, model, followed)
                   : IsTheParameterItself(unwrapped, model, followed);
    }

    /// <summary>
    ///     Whether <paramref name="symbol" /> is a parameter, or a member holding what it is assigned whose every
    ///     earlier assignment is the parameter itself.
    /// </summary>
    private static bool IsAParameterOrAPlainCopy(ISymbol? symbol, ExpressionSyntax read, SemanticModel model, HashSet<ISymbol> followed) {
        if (symbol is IParameterSymbol) { return true; }

        if (symbol is not (ILocalSymbol or IFieldSymbol or IPropertySymbol) || !HoldsWhatItIsAssigned(symbol) || !followed.Add(symbol)) {
            return false;
        }

        ExpressionSyntax[] sources = [.. CopiedFrom(symbol, read, model)];

        return sources.Length > 0 && sources.All(copied => IsTheParameterItself(copied, model, followed));
    }

    /// <summary>Whether <paramref name="cast" /> calls a conversion operator someone wrote.</summary>
    private static bool IsUserDefinedConversion(CastExpressionSyntax cast, SemanticModel model) {
        return model.GetSymbolInfo(cast).Symbol is IMethodSymbol { MethodKind: MethodKind.Conversion };
    }

    /// <summary>
    ///     The expressions <paramref name="symbol" /> took its value from before <paramref name="node" />: a
    ///     local's initializer, or the right side of each earlier assignment to a field or property.
    /// </summary>
    private static IEnumerable<ExpressionSyntax> CopiedFrom(ISymbol? symbol, SyntaxNode node, SemanticModel model) {
        if (symbol is ILocalSymbol local) {
            return local.DeclaringSyntaxReferences
                        .Select(reference => reference.GetSyntax())
                        .OfType<VariableDeclaratorSyntax>()
                        .Where(declarator => declarator.SyntaxTree == node.SyntaxTree && declarator.Initializer is not null)
                        .Select(declarator => declarator.Initializer!.Value);
        }

        if (symbol is not (IFieldSymbol or IPropertySymbol)
         || node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()?.Body is not { } body) {
            return [];
        }

        return body.DescendantNodes()
                   .OfType<AssignmentExpressionSyntax>()
                   .Where(assignment => assignment.SpanStart < node.SpanStart
                                     && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, symbol))
                   .Select(assignment => assignment.Right);
    }

    /// <summary>
    ///     Marks every parameter a statement below the end of the leading scan would have constrained, had it
    ///     stood above it.
    /// </summary>
    /// <remarks>
    ///     Nothing here is read: a guard below a write to state is not placed where §5.3 can vouch for it, so
    ///     the most it earns is <c>unread guards</c> — a louder refusal, never a new inference (ADR-0046). What
    ///     earns it is a statement that throws, and a call the closed set recognises as a guard; both are run
    ///     through the ordinary reading into a reading of their own, and only a parameter that reading would have
    ///     narrowed or marked is marked here. A null-check, in each of its spellings, narrows no chain and so
    ///     stays silent, as it would above the scan; its one effect — keeping a nullable parameter's default a
    ///     value (#411) — is carried over all the same.
    ///     <para>
    ///         A call the set does not recognise is left alone below the scan, unlike above it: there,
    ///         <c>_items.AddRange(items);</c> is the ordinary filling of state the scan has just ended on, and
    ///         marking it would block the compilation of constructors carrying no guard at all. That residue is
    ///         named in §9.
    ///     </para>
    /// </remarks>
    private static void MarkWhatRejectsBelowTheScan(IEnumerable<StatementSyntax> below,
                                                    SemanticModel model,
                                                    IMethodSymbol method,
                                                    GuardReading reading,
                                                    TypeNames names,
                                                    ParameterWrites writes) {
        foreach (StatementSyntax statement in below) { MarkIfItRejectsBelowTheScan(statement, model, method, reading, names, writes); }
    }

    /// <summary>One statement of <see cref="MarkWhatRejectsBelowTheScan" />.</summary>
    private static void MarkIfItRejectsBelowTheScan(StatementSyntax statement,
                                                    SemanticModel model,
                                                    IMethodSymbol method,
                                                    GuardReading reading,
                                                    TypeNames names,
                                                    ParameterWrites writes) {
        // Refused below the scan as much as above it: the null a nullable parameter would draw is the very value
        // the check throws on, whatever was written to state first (#411).
        if (TryRecogniseCoalesceThrowNullCheck(statement, model, method, out IParameterSymbol? coalesced)) {
            reading.RefuseNull(coalesced!.Name);

            return;
        }

        // The one unrecognised call that is NOT left alone below the scan: a helper of this type whose body
        // throws is told apart from the filling of state by that throw, above the scan and below it alike —
        // marked here, never read, as nothing below the scan is.
        ReadOrMarkThrowingHelpers(statement, model, method, reading, writes, unskipped: true, readDelegated: null);

        GuardReading wouldHaveRead = GuardReading.FromSource();

        ReadRecognisedCalls(statement, model, method, wouldHaveRead, names);

        if (statement is IfStatementSyntax chain && Throws(chain)) {
            ReadChain(chain, model, method, wouldHaveRead, names, writes, unskipped: true);
        } else {
            MarkIfItRejects(statement, model, method, wouldHaveRead);
        }

        foreach (string narrowed in method.Parameters.Select(parameter => parameter.Name)
                                         .Where(name => wouldHaveRead.Unread(name) || wouldHaveRead.For(name).Count > 0 || wouldHaveRead.FloorBeyondTheCap(name) is not null)) {
            reading.MarkUnread(narrowed, wouldHaveRead.UnreadAt(narrowed) ?? UnreadGuard.At(statement));
        }

        foreach (string refused in method.Parameters.Select(parameter => parameter.Name).Where(wouldHaveRead.RefusesNull)) {
            reading.RefuseNull(refused);
        }

        // An order below the scan is placed no better than a bound: both parameters are marked, never paired.
        foreach ((OrderedPairing pairing, UnreadGuard at) in wouldHaveRead.Ties) {
            reading.MarkUnread(pairing.First, at);
            reading.MarkUnread(pairing.Second, at);
        }

        // A rule between two parameters below the scan is still one a pair drawn above it can be named for (#459).
        foreach ((string first, string second, UnreadGuard at) in wouldHaveRead.RulesBetweenTwo) { reading.RuleBetween(first, second, at); }
    }

    /// <summary>Reads every call under <paramref name="statement" /> the closed set recognises as a guard.</summary>
    private static void ReadRecognisedCalls(StatementSyntax statement, SemanticModel model, IMethodSymbol method, GuardReading reading, TypeNames names) {
        foreach (InvocationExpressionSyntax invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()) {
            foreach (IParameterSymbol parameter in Mentioned(invocation, model, method)) {
                if (!TryRecogniseCall(invocation, model, parameter, names, out IReadOnlyList<GuardConstraint> read)) { continue; }

                foreach (GuardConstraint constraint in read) { reading.Add(parameter.Name, constraint); }
            }
        }
    }

    /// <summary>The exception a throw builds — the operand of a <c>throw</c> statement or expression.</summary>
    private static bool IsWhatIsThrown(SyntaxNode node) {
        return node.Parent is ThrowStatementSyntax or ThrowExpressionSyntax;
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
    ///     <para>
    ///         One family of used results crosses that rule (ADR-0086): a helper of a guard library the set
    ///         knows by resolved symbol, whose documented contract is to return its validated input —
    ///         <c>Name = Guard.Against.NullOrWhiteSpace(name);</c> <b>is</b> the documented spelling of those
    ///         libraries, and reading it as production was the largest silent surface the engine had. The
    ///         recognised call is handed to the same reading as a discarded one, placement questions and all;
    ///         an assignment whose right side the set does not recognise stays exactly what it was.
    ///     </para>
    /// </remarks>
    private static void MarkIfValidatedElsewhere(StatementSyntax statement,
                                                 SemanticModel model,
                                                 IMethodSymbol method,
                                                 GuardReading reading,
                                                 TypeNames names,
                                                 ParameterWrites writes,
                                                 bool unskipped) {
        foreach (ExpressionStatementSyntax discarded in statement.DescendantNodesAndSelf().OfType<ExpressionStatementSyntax>()) {
            bool viaConditionalAccess = discarded.Expression is ConditionalAccessExpressionSyntax;

            InvocationExpressionSyntax? invocation = discarded.Expression switch {
                InvocationExpressionSyntax direct => direct,
                AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
                                                        && Unwrapped(assignment.Right) is InvocationExpressionSyntax assigned
                                                        && LibraryGuards.Recognises(assigned, model) => assigned,
                // `policy?.Enforce(value);` — a call the placement questions below cannot see runs at all: it
                // is named here so ReadCall marks every parameter it mentions, never so it is read through.
                ConditionalAccessExpressionSyntax { WhenNotNull: InvocationExpressionSyntax conditional } => conditional,
                _ => null
            };

            if (invocation is null || IsNameOf(invocation)) { continue; }

            // Asked once of the statement rather than of each parameter: whether the call runs at all is a
            // fact about where it sits, and the same for every parameter it names. Both halves of it — that
            // nothing encloses the call deciding it runs, and that nothing above the statement jumps past it.
            // A null-conditional receiver is a third way the call can fail to run, which the mark answers the
            // same as the other two rather than by proving the receiver is never null.
            ReadCall(invocation,
                    !viaConditionalAccess && unskipped && RunsUnconditionally(discarded, statement, model),
                    model, method, reading, names, writes);
        }

        // A recognised library call whose result is USED — returned, or handed to a local declaration's own
        // initializer, argument position included (`return new Rating(Guard.Against.OutOfRange(...));`) — is
        // validation exactly as much as one discarded or assigned to a field (ADR-0086): the documented
        // contract of these two libraries is to validate and return their input, wherever that value then
        // goes. It is never READ here — the field/property carve-out above stays exactly as narrow as it
        // always was — only marked, so the developer is told rather than left with a parameter that reads as
        // though nothing constrained it.
        foreach (ReturnStatementSyntax returned in statement.DescendantNodesAndSelf().OfType<ReturnStatementSyntax>()) {
            MarkEveryRecognisedCallWithin(returned.Expression, model, method, reading);
        }

        foreach (LocalDeclarationStatementSyntax local in statement.DescendantNodesAndSelf().OfType<LocalDeclarationStatementSyntax>()) {
            foreach (VariableDeclaratorSyntax declarator in local.Declaration.Variables) {
                MarkEveryRecognisedCallWithin(declarator.Initializer?.Value, model, method, reading);
            }
        }
    }

    /// <summary>
    ///     Marks every parameter <paramref name="statement" /> hands to a method of the same type whose body
    ///     contains a <c>throw</c> — the guard written in a helper that <b>returns</b> the value it checked (§5.6).
    /// </summary>
    /// <remarks>
    ///     The discarded-result rule of <see cref="MarkIfValidatedElsewhere" /> is shaped by a measured false
    ///     positive — reading every used result as doubt blocked ordinary normalising constructors — and §9
    ///     named its cost: <c>XMm = Guard(xMm, nameof(xMm));</c>, the idiomatic shape of a validating value
    ///     object, read as production and stayed silent, under a recap claiming the parameter inferred, while a
    ///     hundred draws out of a hundred threw (issue #407, CLI-14 of the second developer-UX trial). That is
    ///     the one outcome ADR-0083 exists to prevent.
    ///     <para>
    ///         What tells <c>Guard(v)</c> from <c>Normalise(v)</c> without reading either is a single fact: the
    ///         helper's body contains a <c>throw</c>. A helper that throws nowhere normalises, and stays
    ///         production; one that throws somewhere may be rejecting, and the parameter handed to it earns the
    ///         mark — <c>to verify</c>, the build stopping at the blocking line §5.6 describes, as for any guard the
    ///         engine cannot vouch for. Nothing is inferred from the helper: reading its body, one level down
    ///         and under strict conditions, is a separate step (ADR-0085's field report is this trial).
    ///     </para>
    ///     <para>
    ///         Bounded to a method <b>declared in the same type, with source</b> — a private helper, a static
    ///         one, a local function — which is where the idiom lives. A helper of another type, a helper the
    ///         closed set does not know in a referenced assembly, and a helper calling a helper that throws are
    ///         not looked into; that residue stays named in §9. Asked of every statement rather than of the
    ///         leading ones: the idiom's own statement is the first assignment to state, and a guard below the
    ///         scan rejects values exactly as much as one above it.
    ///     </para>
    /// </remarks>
    private static void ReadOrMarkThrowingHelpers(StatementSyntax statement,
                                                  SemanticModel model,
                                                  IMethodSymbol method,
                                                  GuardReading reading,
                                                  ParameterWrites writes,
                                                  bool unskipped,
                                                  Func<IMethodSymbol, GuardReading>? readDelegated) {
        foreach (InvocationExpressionSyntax invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()) {
            if (IsNameOf(invocation)) { continue; }

            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol callee
             || !SymbolEqualityComparer.Default.Equals(callee.ContainingType, method.ContainingType)
             || !BodyThrows(callee)) {
                continue;
            }

            // The bounded read of #416: one level into a helper that hands its parameter back unchanged, where
            // the call runs on every path, folded onto the parameters handed to it exactly as a delegated
            // constructor's guards are. Anything else keeps the mark of #407.
            StatementSyntax carrier = invocation.FirstAncestorOrSelf<StatementSyntax>() ?? statement;

            if (readDelegated is not null
             && unskipped
             && RunsUnconditionally(carrier, statement, model)
             && ReturnsAParameterUnchanged(callee, model)) {
                // Placement is asked of the statement carrying the call, a region the compiler analyses whole:
                // a write to the argument anywhere in it — before the call or inside its own arguments — declines
                // the fold, which is the superset ADR-0084 asks for.
                FoldGuardsFromDelegatedConstructor(invocation.ArgumentList, model, method, callee, reading,
                                                   parameter => writes.Precede(parameter, carrier), readDelegated);

                continue;
            }

            UnreadGuard        at     = UnreadGuard.At(invocation);
            IParameterSymbol[] handed = [.. Mentioned(invocation, model, method).Distinct<IParameterSymbol>(SymbolEqualityComparer.Default)];

            foreach (IParameterSymbol parameter in handed) { reading.MarkUnread(parameter.Name, at); }

            // Handed two parameters is not yet a rule between them: only a throw in the helper deciding on both is,
            // and only then can a pair drawing both be named for it (#459). Both stay marked either way.
            if (handed.Length == 2 && HelperThrowsOnBoth(invocation, callee, model, method, handed[0], handed[1])) {
                reading.RuleBetween(handed[0].Name, handed[1].Name, at);
            }
        }
    }

    /// <summary>
    ///     Whether the same-type <paramref name="callee" /> throws on a condition testing both the parameter handed
    ///     <paramref name="first" /> and the one handed <paramref name="second" />, each handed bare.
    /// </summary>
    /// <remarks>
    ///     Asked only to word a note, never to read the guard: a helper whose throws test one side alone —
    ///     <c>ValidateStart(start, end)</c> throwing on <c>start &lt; 0</c> — is a guard a recipe can honour on its own,
    ///     and saying it ties the two would send the developer to the wrong place. A helper the engine cannot see the
    ///     body of, in this tree, is answered no.
    /// </remarks>
    private static bool HelperThrowsOnBoth(InvocationExpressionSyntax invocation,
                                           IMethodSymbol callee,
                                           SemanticModel model,
                                           IMethodSymbol method,
                                           IParameterSymbol first,
                                           IParameterSymbol second) {
        MethodDeclarationSyntax? declaration = callee.DeclaringSyntaxReferences
                                                     .Select(reference => reference.GetSyntax())
                                                     .OfType<MethodDeclarationSyntax>()
                                                     .FirstOrDefault();

        if (declaration?.Body is not { } body || body.SyntaxTree != model.SyntaxTree) { return false; }

        HashSet<IParameterSymbol> fromFirst  = HandedBare(invocation, callee, model, method, first);
        HashSet<IParameterSymbol> fromSecond = HandedBare(invocation, callee, model, method, second);

        if (fromFirst.Count == 0 || fromSecond.Count == 0) { return false; }

        foreach (IfStatementSyntax check in body.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                                                .OfType<IfStatementSyntax>()
                                                .Where(Throws)) {
            IParameterSymbol[] tested = Mentioned(check.Condition, model, callee);

            if (tested.Any(fromFirst.Contains) && tested.Any(fromSecond.Contains)) { return true; }
        }

        return false;
    }

    /// <summary>The parameters of <paramref name="callee" /> that <paramref name="invocation" /> hands <paramref name="outer" /> to, bare.</summary>
    private static HashSet<IParameterSymbol> HandedBare(InvocationExpressionSyntax invocation, IMethodSymbol callee, SemanticModel model, IMethodSymbol method, IParameterSymbol outer) {
        HashSet<IParameterSymbol>           inners    = new(SymbolEqualityComparer.Default);
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        for (int index = 0; index < arguments.Count; index++) {
            if (!SymbolEqualityComparer.Default.Equals(HandedFrom(arguments[index], model, method), outer)) { continue; }

            if (HandedTo(arguments[index], index, callee) is { } inner) { inners.Add(inner); }
        }

        return inners;
    }

    /// <summary>
    ///     Whether <paramref name="callee" /> is the helper the bounded read of #416 goes into: a method of the
    ///     same type with a block body of its own — not virtual, abstract or overridden, and not a local function
    ///     — that returns one of its own parameters, unchanged, on every path that returns at all.
    /// </summary>
    /// <remarks>
    ///     "Unchanged" is two facts, both asked of the compiler. Every <c>return</c> hands back the parameter
    ///     itself — bare, in parentheses, or as the surviving arm of a conditional whose other arm throws — and the
    ///     parameter is written nowhere in the body, so what comes out is what went in. A helper returning
    ///     <c>v * 2</c> or <c>Math.Round(v)</c> normalises, and a guard inside it is about a value this
    ///     generator does not draw: that is the case §9 names, and it keeps the mark. A helper whose body is
    ///     only an expression keeps the mark too, by the reading's own shape: the leading scan reads statements,
    ///     and <c>Math.Abs(v) &gt; 2000.0 ? throw … : v</c> has none.
    /// </remarks>
    private static bool ReturnsAParameterUnchanged(IMethodSymbol callee, SemanticModel model) {
        if (callee.IsVirtual || callee.IsAbstract || callee.IsOverride || callee.MethodKind != MethodKind.Ordinary) { return false; }

        MethodDeclarationSyntax? declaration = callee.DeclaringSyntaxReferences
                                                     .Select(reference => reference.GetSyntax())
                                                     .OfType<MethodDeclarationSyntax>()
                                                     .FirstOrDefault();

        if (declaration?.Body is not { } body || body.SyntaxTree != model.SyntaxTree) { return false; }

        ExpressionSyntax?[] handedBack = [.. body.DescendantNodes(node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
                                                  .OfType<ReturnStatementSyntax>()
                                                  .Select(@return => @return.Expression)];

        if (handedBack.Length == 0) { return false; }

        IParameterSymbol? returned = null;

        foreach (ExpressionSyntax? expression in handedBack) {
            if (expression is null || !ReturnsUnchanged(expression, model, callee, ref returned)) { return false; }
        }

        DataFlowAnalysis? flow = model.AnalyzeDataFlow(body);

        return returned is not null
            && flow is { Succeeded: true }
            && !flow.WrittenInside.Contains(returned, SymbolEqualityComparer.Default);
    }

    /// <summary>
    ///     Whether <paramref name="expression" /> is one parameter of <paramref name="callee" /> handed back as it
    ///     is — the same one every other return hands back — or a conditional whose arms each are that or a throw.
    /// </summary>
    private static bool ReturnsUnchanged(ExpressionSyntax expression, SemanticModel model, IMethodSymbol callee, ref IParameterSymbol? returned) {
        ExpressionSyntax bare = Unwrapped(expression);

        if (bare is ConditionalExpressionSyntax conditional) {
            return (conditional.WhenTrue is ThrowExpressionSyntax || ReturnsUnchanged(conditional.WhenTrue, model, callee, ref returned))
                && (conditional.WhenFalse is ThrowExpressionSyntax || ReturnsUnchanged(conditional.WhenFalse, model, callee, ref returned));
        }

        if (bare is not IdentifierNameSyntax identifier
         || model.GetSymbolInfo(identifier).Symbol is not IParameterSymbol parameter
         || !callee.Parameters.Contains(parameter, SymbolEqualityComparer.Default)) {
            return false;
        }

        if (returned is null) { returned = parameter; }

        return SymbolEqualityComparer.Default.Equals(returned, parameter);
    }

    /// <summary>
    ///     Whether <paramref name="callee" />'s own declaration carries a <c>throw</c> anywhere — a statement or
    ///     an expression, in a block body or an expression body. False where there is no source to look at.
    /// </summary>
    private static bool BodyThrows(IMethodSymbol callee) {
        return callee.DeclaringSyntaxReferences
                     .Select(reference => reference.GetSyntax())
                     .Any(declaration => declaration.DescendantNodes().Any(node => node is ThrowStatementSyntax or ThrowExpressionSyntax));
    }

    /// <summary>Marks every parameter a recognised library call anywhere under <paramref name="node" /> names.</summary>
    private static void MarkEveryRecognisedCallWithin(SyntaxNode? node, SemanticModel model, IMethodSymbol method, GuardReading reading) {
        if (node is null) { return; }

        foreach (InvocationExpressionSyntax invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()) {
            if (!LibraryGuards.Recognises(invocation, model)) { continue; }

            foreach (IParameterSymbol parameter in Mentioned(invocation, model, method)) { reading.MarkUnread(parameter.Name, UnreadGuard.At(invocation)); }
        }
    }

    /// <summary>
    ///     Whether a statement is a guard-library helper assigned straight to a field or a property — the one
    ///     assignment to state that does not end the leading scan (ADR-0086).
    /// </summary>
    /// <remarks>
    ///     The right side has to <b>be</b> the recognised call, bar parentheses: wrapped in any further
    ///     expression it is an operand of ordinary production, and the scan ends as it always did. The write
    ///     lands on state, never on a parameter, which is what keeps everything below as leading as it was —
    ///     a right side that also wrote a parameter is caught by the placement questions the reading itself
    ///     asks, not waved past here.
    /// </remarks>
    private static bool IsGuardAssignment(StatementSyntax statement, SemanticModel model, IMethodSymbol method) {
        return statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
            && Unwrapped(assignment.Right) is InvocationExpressionSyntax invocation
            && (LibraryGuards.Recognises(invocation, model) || IsAReadableHelperCall(invocation, model, method));
    }

    /// <summary>
    ///     A call to a helper of the same type the bounded read of #416 goes into — one that throws and hands a
    ///     parameter back unchanged. <c>XMm = Guard(xMm);</c> validates and stores exactly as a guard library's
    ///     assigned spelling does (ADR-0086), writes over no parameter, and so leaves everything below it as
    ///     leading as it was: the value object with two such lines reads both.
    /// </summary>
    private static bool IsAReadableHelperCall(InvocationExpressionSyntax invocation, SemanticModel model, IMethodSymbol method) {
        return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol callee
            && SymbolEqualityComparer.Default.Equals(callee.ContainingType, method.ContainingType)
            && BodyThrows(callee)
            && ReturnsAParameterUnchanged(callee, model);
    }

    /// <summary>
    ///     Whether a statement is <c>Field = param ?? throw new ArgumentNullException(nameof(param));</c> — the
    ///     null-check idiom fused into an assignment, a third spelling of what <c>ArgumentNullException
    ///     .ThrowIfNull(param)</c> and <c>if (param is null) { throw … }</c> already read.
    /// </summary>
    /// <remarks>
    ///     The coalesce's left side has to <b>be</b> a parameter, bar parentheses — the same subject-identity
    ///     discipline every other row of the closed set keeps — and the thrown expression's type has to resolve
    ///     to exactly <see cref="ArgumentNullException" />: a different exception thrown from the same shape
    ///     states an invariant this method does not know how to name, and is left to the ordinary "rejects, and
    ///     the engine cannot tell why" reading below, exactly as an unrecognised <c>if</c> is.
    /// </remarks>
    private static bool TryRecogniseCoalesceThrowNullCheck(StatementSyntax statement,
                                                            SemanticModel model,
                                                            IMethodSymbol method,
                                                            out IParameterSymbol? parameter) {
        parameter = null;

        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
         || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression)
         || model.GetSymbolInfo(assignment.Left).Symbol is not (IFieldSymbol or IPropertySymbol)) {
            return false;
        }

        if (Unwrapped(assignment.Right) is not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } coalesce) {
            return false;
        }

        if (coalesce.Right is not ThrowExpressionSyntax @throw) { return false; }

        if (Unwrapped(coalesce.Left) is not IdentifierNameSyntax identifier
         || model.GetSymbolInfo(identifier).Symbol is not IParameterSymbol candidate
         || !method.Parameters.Contains(candidate, SymbolEqualityComparer.Default)) {
            return false;
        }

        if (model.GetTypeInfo(@throw.Expression).Type is not { } exceptionType
         || exceptionType.ToDisplayString(ByNamespace) != "System.ArgumentNullException") {
            return false;
        }

        parameter = candidate;

        return true;
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
                                 TypeNames names,
                                 ParameterWrites writes) {
        foreach (IParameterSymbol parameter in Mentioned(invocation, model, method)) {
            // Whose invariant a conditioned helper states is the paths that reach it, never the parameter,
            // so the reading owes the developer the same word it owes any guard it could not place.
            if (!unconditional) {
                reading.MarkUnread(parameter.Name, UnreadGuard.At(invocation));

                continue;
            }

            // The rule the condition reader keeps, in the other spelling: a helper the set knows, called
            // where a write to that parameter can already have run, states an invariant of the computed
            // value. This is the placement that matters most — a call sits in the middle of a statement,
            // where the writes above it are as easy to miss as the ones in the statements above that.
            if (writes.Precede(parameter, invocation)) {
                reading.MarkUnread(parameter.Name, UnreadGuard.At(invocation));

                continue;
            }

            if (TryRecogniseCall(invocation, model, parameter, names, out IReadOnlyList<GuardConstraint> read)) {
                foreach (GuardConstraint constraint in read) { reading.Add(parameter.Name, constraint); }
            } else {
                reading.MarkUnread(parameter.Name, UnreadGuard.At(invocation));
            }
        }
    }

    /// <summary>
    ///     The whole closed set for the call spelling: the BCL throw helpers, then the two guard libraries
    ///     read by resolved symbol (ADR-0086). True with no constraint still means understood, adding nothing.
    /// </summary>
    /// <remarks>
    ///     A recognised library's method the table does not carry answers false — declared validation the
    ///     engine cannot vouch for, which earns the mark exactly as an unrecognised call does.
    /// </remarks>
    private static bool TryRecogniseCall(InvocationExpressionSyntax invocation,
                                         SemanticModel model,
                                         IParameterSymbol parameter,
                                         TypeNames names,
                                         out IReadOnlyList<GuardConstraint> read) {
        if (TryRecogniseThrowHelper(invocation, model, parameter, GeneratorFor.Sizes(parameter.Type), names,
                                    out GuardConstraint? constraint)) {
            read = constraint is null ? [] : [constraint];

            return true;
        }

        if (LibraryGuards.TryRead(invocation, model, parameter, GeneratorFor.Sizes(parameter.Type),
                                  out IReadOnlyList<GuardConstraint>? library)
         && library is not null) {
            read = library;

            return true;
        }

        read = [];

        return false;
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
    ///     exactly right, since a null check narrows no chain and an emptiness check is
    ///     a <c>NonEmpty</c> the set already reads. Reading a guard the set already understands as one it could not read
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
                                                TypeNames names,
                                                out GuardConstraint? constraint) {
        constraint = null;

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        if (arguments.Count == 0) { return false; }

        // `ThrowIfGreaterThan(text.Length, 500)` is `if (text.Length > 500)` in the helper spelling, and reads into
        // the same size row (#408): the helpers take the length or the count exactly as a comparison does.
        if (IsSize(arguments[0].Expression, parameter, model)) {
            return TryRecogniseSizeThrowHelper(invocation, model, arguments, sizes, names, out constraint);
        }

        if (!IsParameter(arguments[0].Expression, model, parameter)) { return false; }

        if (IsCall(invocation, model, "System.ArgumentNullException", "ThrowIfNull")) {
            constraint = GuardConstraint.NullRefused;

            return true;
        }

        if (IsCall(invocation, model, "System.ArgumentException", EmptinessThrowHelper)) {
            constraint = Emptiness(sizes.ByCount).RefusingNull();

            return true;
        }

        if (IsCall(invocation, model, "System.ArgumentException", BlanknessThrowHelper)) {
            constraint = Blankness().RefusingNull();

            return true;
        }

        return TryRecogniseArithmeticThrowHelper(invocation, model, parameter, arguments, names, out constraint);
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
                                                           TypeNames names,
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

        decimal value;
        string  literal;

        // A TimeSpan bound is not a constant to the compiler, and is read from its closed list of spellings (#408).
        if (TryTimeSpanConstant(arguments[1].Expression, model, parameter, names, out decimal ticks, out string? spelled)) {
            value   = ticks;
            literal = spelled!;
        } else {
            Optional<object?> bound = model.GetConstantValue(arguments[1].Expression);

            if (!bound.HasValue || bound.Value is null || !IsNumber(bound.Value) || !TryDecimal(bound.Value, out value)) {
                return false;
            }

            literal = Named(arguments[1].Expression, model, Underlying(parameter.Type), names) ?? Literal(bound.Value, parameter.Type);
        }

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
    ///     The arithmetic throw helpers on <c>p.Length</c> or <c>p.Count</c>, mapped to the size rows the equivalent
    ///     comparison reads into — each helper's own condition, written as the operator <see cref="Sized" /> reads.
    /// </summary>
    /// <remarks>
    ///     A helper whose edge <see cref="Sized" /> has no row for is moved by one onto a row it has:
    ///     <c>ThrowIfGreaterThanOrEqual(p.Length, N)</c> throws on <c>N</c> and above, which is a ceiling of
    ///     <c>N - 1</c>. A bound so moved is folded to its value rather than referenced, since the constant named
    ///     is not the bound written. <c>ThrowIfNegative</c> on a size holds by construction: understood, adding
    ///     nothing to the chain. Every row refuses null as the comparison rows do, that one included, since
    ///     reading the size dereferences the parameter — a nullable parameter handed to it draws a value (#411).
    /// </remarks>
    private static bool TryRecogniseSizeThrowHelper(InvocationExpressionSyntax invocation,
                                                    SemanticModel model,
                                                    SeparatedSyntaxList<ArgumentSyntax> arguments,
                                                    (bool ByCount, int Ceiling, int Floor) sizes,
                                                    TypeNames names,
                                                    out GuardConstraint? constraint) {
        constraint = null;

        const string containing = "System.ArgumentOutOfRangeException";

        if (IsCall(invocation, model, containing, "ThrowIfNegative")) {
            constraint = GuardConstraint.NullRefused;

            return true;
        }

        if (IsCall(invocation, model, containing, "ThrowIfZero", "ThrowIfNegativeOrZero")) {
            constraint = Emptiness(sizes.ByCount).RefusingNull();

            return true;
        }

        if (arguments.Count < 2) { return false; }

        Optional<object?> bound = model.GetConstantValue(arguments[1].Expression);

        if (!bound.HasValue || bound.Value is null || !IsNumber(bound.Value) || !TryDecimal(bound.Value, out decimal value)) { return false; }

        string? named = Named(arguments[1].Expression, model, model.Compilation.GetSpecialType(SpecialType.System_Int32), names);

        (SyntaxKind @operator, decimal edge, string? spelled)? read = null;

        if (IsCall(invocation, model, containing, "ThrowIfGreaterThan")) { read = (SyntaxKind.GreaterThanExpression, value, named); }
        if (IsCall(invocation, model, containing, "ThrowIfLessThan")) { read = (SyntaxKind.LessThanExpression, value, named); }
        if (IsCall(invocation, model, containing, "ThrowIfGreaterThanOrEqual")) { read = (SyntaxKind.GreaterThanExpression, value - 1, null); }
        if (IsCall(invocation, model, containing, "ThrowIfLessThanOrEqual")) { read = (SyntaxKind.LessThanExpression, value + 1, null); }

        if (read is not { } row) { return false; }

        constraint = Sized(row.@operator, row.edge, sizes, row.spelled)?.RefusingNull();

        return constraint is not null;
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
    ///     <c>Any.String()</c> for a constructor that refuses the empty string.
    /// </remarks>
    private static Compilation? Declaring(SyntaxTree tree, IMethodSymbol method, Compilation compilation) {
        if (compilation.ContainsSyntaxTree(tree)) { return compilation; }

        return compilation.GetMetadataReference(method.ContainingAssembly) is CompilationReference referenced
            && referenced.Compilation.ContainsSyntaxTree(tree)
                   ? referenced.Compilation
                   : null;
    }

    /// <summary>
    ///     Reads a guard's condition — or, where the condition is a disjunction, each of its branches as a guard
    ///     of its own (ADR-0128).
    /// </summary>
    /// <remarks>
    ///     <c>if (A || B) { throw … }</c> is exactly <c>if (A) { throw … } if (B) { throw … }</c>, which this
    ///     reader already reads: a throw on either branch is a throw, so each branch rejects on its own. That is a
    ///     rewrite holding by construction (ADR-0046), not a heuristic, and it is why a disjunction splits where a
    ///     conjunction does not — <c>if (A &amp;&amp; B) { throw … }</c> rejects only where both hold, and
    ///     reading <c>A</c> alone would narrow the draw past what the constructor demands, the one direction a
    ///     reading must never err in. A pattern <c>p is P1 or P2</c> is the same disjunction in the other spelling.
    ///     Both trials met the gap — ten domains of the first, eighteen of the second's twenty contexts — and
    ///     that is the field report ADR-0085 asks for (#364).
    /// </remarks>
    private static void ReadOne(ExpressionSyntax condition,
                                SemanticModel model,
                                IMethodSymbol method,
                                GuardReading reading,
                                TypeNames names,
                                ParameterWrites writes,
                                bool unskipped) {
        foreach (Branch branch in Branches(condition)) { ReadBranch(branch, model, method, reading, names, writes, unskipped); }
    }

    /// <summary>
    ///     One branch of a guard's condition: the whole condition where it is no disjunction; one operand of a
    ///     <c>||</c>; or one alternative of an <c>or</c> pattern, carried with the expression it tests.
    /// </summary>
    private sealed class Branch {

        internal Branch(ExpressionSyntax whole, ExpressionSyntax condition, ExpressionSyntax subject, PatternSyntax? pattern) {
            Whole     = whole;
            Condition = condition;
            Subject   = subject;
            Pattern   = pattern;
        }

        /// <summary>The guard's whole condition, which every branch is evaluated where it is.</summary>
        internal ExpressionSyntax Whole { get; }

        /// <summary>The expression this branch is, or the <c>is</c> expression a pattern branch belongs to.</summary>
        internal ExpressionSyntax Condition { get; }

        /// <summary>The expression tested: the condition itself, or what the pattern is matched against.</summary>
        internal ExpressionSyntax Subject { get; }

        /// <summary>The one alternative this branch tests, where the branch is a pattern.</summary>
        internal PatternSyntax? Pattern { get; }

        /// <summary>What the branch names: the subject, and the pattern where there is one.</summary>
        internal IReadOnlyList<SyntaxNode> Nodes => Pattern is null ? [Subject] : [Subject, Pattern];

    }

    /// <summary>The branches of <paramref name="condition" />, flattened through nested <c>||</c> and <c>or</c>.</summary>
    private static IReadOnlyList<Branch> Branches(ExpressionSyntax condition) {
        List<Branch> branches = [];

        Collect(condition, condition, branches);

        return branches;
    }

    private static void Collect(ExpressionSyntax whole, ExpressionSyntax condition, List<Branch> branches) {
        switch (Unwrapped(condition)) {
            case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } disjunction:
                Collect(whole, disjunction.Left, branches);
                Collect(whole, disjunction.Right, branches);

                break;
            case IsPatternExpressionSyntax matched:
                Collect(whole, matched, matched.Expression, matched.Pattern, branches);

                break;
            default:
                branches.Add(new Branch(whole, condition, condition, pattern: null));

                break;
        }
    }

    private static void Collect(ExpressionSyntax whole, ExpressionSyntax condition, ExpressionSyntax subject, PatternSyntax pattern, List<Branch> branches) {
        while (pattern is ParenthesizedPatternSyntax parenthesised) { pattern = parenthesised.Pattern; }

        if (pattern is BinaryPatternSyntax { RawKind: (int)SyntaxKind.OrPattern } alternatives) {
            Collect(whole, condition, subject, alternatives.Left, branches);
            Collect(whole, condition, subject, alternatives.Right, branches);

            return;
        }

        branches.Add(new Branch(whole, condition, subject, pattern));
    }

    private static void ReadBranch(Branch branch,
                                   SemanticModel model,
                                   IMethodSymbol method,
                                   GuardReading reading,
                                   TypeNames names,
                                   ParameterWrites writes,
                                   bool unskipped) {
        IParameterSymbol[] mentioned = MentionedIn(branch.Nodes, model, method);

        if (!IsANullCheckOnAPlainCopy(branch, model)) {
            foreach (IParameterSymbol reached in ReachedThroughCopies(branch.Nodes, model, method)) { reading.MarkUnread(reached.Name, UnreadGuard.At(branch.Condition)); }

            MarkEveryParameterIfTheTrailIsLost(branch.Nodes, branch.Condition, model, method, reading);
        } else {
            // Silent, since the check narrows no chain — but not without effect: a nullable parameter draws null
            // by default (#411), a member holding it unchanged then holds null, and this check refuses it.
            foreach (IParameterSymbol reached in ReachedThroughCopies(branch.Nodes, model, method)) { reading.RefuseNull(reached.Name); }
        }

        // Exactly one, or the engine cannot say whose invariant this is. A cross-parameter rule is precisely
        // the case §9 names as out of reach — and §9 says how it ends, too: `unread guards`, on every
        // parameter it spans. Silence there measured 5008 throws in 10 000 draws on `Range(min, max)`, under
        // a recap reporting both parameters inferred and nothing to look at.
        //
        // A condition mentioning NO parameter is different and stays silent — one testing the clock, say, is
        // about none of them, so there is nobody to send looking. The loop below says both at once.
        if (mentioned.Length != 1) {
            TieOrMark(branch.Condition, model, mentioned, reading, writes, unskipped);

            return;
        }

        IParameterSymbol parameter = mentioned[0];

        // Read correctly, about a value that need never have reached this test: a `return` above it leaves
        // the constructor building the object without ever evaluating the condition. The mark is the same
        // one a conditioned helper earns, since it is the same question asked along the body rather than up
        // through it.
        if (!unskipped) {
            reading.MarkUnread(parameter.Name, UnreadGuard.At(branch.Condition));

            return;
        }

        // Read correctly, about the wrong value. Every other gap in §9 is a guard the engine cannot see; this
        // is the one shape where it sees the guard, parses it, and attributes it to a value the generator no
        // longer draws — a write above a `percent < 0` test yields `GreaterThanOrEqualTo(0)` over a real
        // domain of 0..100, under a recap reporting it inferred. So the guard is not this parameter's to
        // read, and §9 already has the word for that (ADR-0083). Asked of the whole condition rather than of
        // the branch: every branch is evaluated where the condition is, and a write in a sibling branch —
        // `int.TryParse(text, out value) || value > 5` — has run by the time the next one is tested.
        if (writes.Precede(parameter, branch.Whole)) {
            reading.MarkUnread(parameter.Name, UnreadGuard.At(branch.Condition));

            return;
        }

        // A conjunction stays unread — `A && B` rejects only where both hold, and reading either alone would
        // narrow the draw past what the constructor demands — and so does a disjunction nested where it is no
        // longer the whole condition, `!(A || B)`, which is a conjunction in disguise (ADR-0128).
        if (branch.Condition.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>()
                            .Any(binary => binary.IsKind(SyntaxKind.LogicalAndExpression)
                                        || binary.IsKind(SyntaxKind.LogicalOrExpression))) {
            reading.MarkUnread(parameter.Name, UnreadGuard.At(branch.Condition));

            return;
        }

        // The one row that reads into two constraints: a magnitude is a floor and a ceiling at once (#416).
        if (branch.Pattern is null
         && Unwrapped(branch.Condition) is BinaryExpressionSyntax magnitude
         && TryMagnitude(magnitude, model, parameter, out GuardConstraint? floor, out GuardConstraint? ceiling)) {
            reading.Add(parameter.Name, floor!);
            reading.Add(parameter.Name, ceiling!);

            return;
        }

        if (!TryRecogniseBranch(branch, model, parameter, GeneratorFor.Sizes(parameter.Type), names,
                                out GuardConstraint? constraint)) {
            reading.MarkUnread(parameter.Name, UnreadGuard.At(branch.Condition, pattern: RegexPatternIn(branch.Condition, model)));

            return;
        }

        if (constraint is not null) { reading.Add(parameter.Name, constraint); }
    }

    /// <summary>The closed set, asked of a branch: of its pattern where it has one, of its condition otherwise.</summary>
    private static bool TryRecogniseBranch(Branch branch,
                                     SemanticModel model,
                                     IParameterSymbol parameter,
                                     (bool ByCount, int Ceiling, int Floor) sizes,
                                     TypeNames names,
                                     out GuardConstraint? constraint) {
        return branch.Pattern is null
                   ? TryRecognise(branch.Condition, model, parameter, sizes, names, out constraint)
                   : TryPattern(branch.Subject, branch.Pattern, model, parameter, sizes, names, out constraint);
    }

    /// <summary>
    ///     A relational pattern, <c>p is &gt; N</c>, read as the comparison it spells, and a constant pattern,
    ///     <c>p is null</c>, <c>p is 0</c>, <c>p is E.Member</c>, as the equality it spells — on the parameter, or
    ///     on its length or count (ADR-0128).
    /// </summary>
    /// <remarks>
    ///     A relational pattern is allowed on the built-in numeric types alone, where the comparison it spells is
    ///     the built-in one, so no user-defined operator question arises. Any other pattern — a type, a property,
    ///     a conjunction, a negation — is outside the set and leaves the branch unread.
    /// </remarks>
    private static bool TryPattern(ExpressionSyntax subject,
                                   PatternSyntax pattern,
                                   SemanticModel model,
                                   IParameterSymbol parameter,
                                   (bool ByCount, int Ceiling, int Floor) sizes,
                                   TypeNames names,
                                   out GuardConstraint? constraint) {
        constraint = null;

        while (pattern is ParenthesizedPatternSyntax parenthesised) { pattern = parenthesised.Pattern; }

        if (!IsSubject(subject, model, parameter)) { return false; }

        switch (pattern) {
            case RelationalPatternSyntax relational when ComparisonSpelledBy(relational.OperatorToken.Kind()) is { } written:
                return TryBound(new Comparison(null, subject, relational.Expression, written), model, parameter, sizes, names, out constraint);
            case ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } when literal.IsKind(SyntaxKind.NullLiteralExpression):
                constraint = GuardConstraint.NullRefused;

                return true;
            case ConstantPatternSyntax constant:
                return TryBound(new Comparison(null, subject, constant.Expression, SyntaxKind.EqualsExpression), model, parameter, sizes, names, out constraint);
            default:
                return false;
        }
    }

    /// <summary>The comparison a relational pattern's operator token spells, or null for a token that is none.</summary>
    private static SyntaxKind? ComparisonSpelledBy(SyntaxKind token) {
        return token switch {
            SyntaxKind.LessThanToken          => SyntaxKind.LessThanExpression,
            SyntaxKind.LessThanEqualsToken    => SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.GreaterThanToken       => SyntaxKind.GreaterThanExpression,
            SyntaxKind.GreaterThanEqualsToken => SyntaxKind.GreaterThanOrEqualExpression,
            _                                 => null
        };
    }

    /// <summary>
    ///     A condition about several parameters: the order between two that <see cref="TryOrdering" /> reads, or
    ///     <c>unread guards</c> on every one it spans.
    /// </summary>
    /// <remarks>
    ///     The one rule between parameters that is read rather than marked (#405): a direct comparison of two
    ///     parameters of one type is the order <c>Any.Pair</c> draws by construction, placed by the same
    ///     questions a guard on one parameter answers. Whether the type has a pair to draw is the scaffolder's
    ///     question, and it marks both, as here, where it has none.
    /// </remarks>
    private static void TieOrMark(ExpressionSyntax condition,
                                  SemanticModel model,
                                  IParameterSymbol[] mentioned,
                                  GuardReading reading,
                                  ParameterWrites writes,
                                  bool unskipped) {
        if (mentioned.Length == 2
         && unskipped
         && !mentioned.Any(each => writes.Precede(each, condition))
         && TryOrdering(condition, model, mentioned, out OrderedPairing? pairing)) {
            reading.Tie(pairing!, UnreadGuard.At(condition));

            return;
        }

        // Called out as a rule between parameters (#373): a developer who narrows one of them alone has not
        // resolved it, and the trial measured exactly that — (204 days, 3 months) still throwing after both
        // named guards were fixed.
        string?     ties = mentioned.Length < 2 ? null : "ties " + Listed(mentioned);
        UnreadGuard at   = UnreadGuard.At(condition, ties);

        foreach (IParameterSymbol spanned in mentioned) { reading.MarkUnread(spanned.Name, at); }

        // Kept so that, once the scaffolder knows which orders it draws as pairs, a rule between the same two can say
        // where it belongs — the pair's method, the one place both are drawn together (#459).
        if (mentioned.Length == 2) { reading.RuleBetween(mentioned[0].Name, mentioned[1].Name, at); }
    }

    /// <summary><c>a to b</c>, or <c>a, b and c</c>.</summary>
    private static string Listed(IParameterSymbol[] parameters) {
        string[] names = [.. parameters.Select(parameter => parameter.Name)];

        return names.Length == 2
                   ? names[0] + " to " + names[1]
                   : string.Join(", ", names.Take(names.Length - 1)) + " and " + names[names.Length - 1];
    }

    /// <summary>
    ///     The constant pattern a regex guard tests the parameter against, where the condition holds one, so the
    ///     comment can quote it (#374): <c>Regex.IsMatch(p, "…")</c>, a <c>[GeneratedRegex]</c> method's
    ///     <c>IsMatch</c>, or the <c>IsMatch</c> of a field or property initialised with <c>new Regex("…")</c>.
    /// </summary>
    /// <remarks>
    ///     Quoted, never read: the guard stays unread for the reasons §5.3 gives, and the pattern is what the
    ///     developer copies into <c>Any.StringMatching</c> if it lies in the library's regular subset.
    /// </remarks>
    private static string? RegexPatternIn(SyntaxNode condition, SemanticModel model) {
        foreach (InvocationExpressionSyntax invocation in condition.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()) {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.Text: "IsMatch" } access) { continue; }
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingType: { } owner } || owner.ToDisplayString(ByNamespace) != "System.Text.RegularExpressions.Regex") { continue; }

            // The static overload carries the pattern as its second argument.
            if (invocation.ArgumentList.Arguments.Count >= 2 && model.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression).Value is string written) { return written; }

            if (PatternOf(access.Expression, model) is { } pattern) { return pattern; }
        }

        return null;
    }

    /// <summary>The pattern the <c>Regex</c> instance <paramref name="receiver" /> was built from, where that is a constant.</summary>
    private static string? PatternOf(ExpressionSyntax receiver, SemanticModel model) {
        ISymbol? symbol = model.GetSymbolInfo(receiver).Symbol;

        // A [GeneratedRegex] method carries its pattern as the attribute's first argument.
        if (symbol is IMethodSymbol generated) {
            AttributeData? attribute = generated.GetAttributes()
                                                .FirstOrDefault(candidate => candidate.AttributeClass?.ToDisplayString(ByNamespace) == "System.Text.RegularExpressions.GeneratedRegexAttribute");

            return attribute?.ConstructorArguments.FirstOrDefault().Value as string;
        }

        if (symbol is not (IFieldSymbol or IPropertySymbol)) { return null; }

        foreach (SyntaxNode declaring in symbol.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax())) {
            ExpressionSyntax? initializer = declaring switch {
                VariableDeclaratorSyntax declarator => declarator.Initializer?.Value,
                PropertyDeclarationSyntax property  => property.Initializer?.Value ?? (property.ExpressionBody?.Expression),
                _                                   => null
            };

            if (initializer is ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: >= 1 } built
             && built.SyntaxTree == model.SyntaxTree
             && model.GetConstantValue(built.ArgumentList.Arguments[0].Expression).Value is string pattern) {
                return pattern;
            }
        }

        return null;
    }

    /// <summary>
    ///     Whether <paramref name="condition" /> compares the two <paramref name="mentioned" /> parameters directly —
    ///     <c>a &gt; b</c>, <c>a &gt;= b</c>, or the mirrored <c>b &lt; a</c>, <c>b &lt;= a</c> — over one
    ///     non-nullable value type, and the order the constructor then demands.
    /// </summary>
    /// <remarks>
    ///     Each side has to <b>be</b> a parameter, bar parentheses and a <c>!</c>: <c>a + 1 &gt; b</c> or
    ///     <c>a.Date &gt; b</c> is a relation the pair does not draw. A nullable value type is left out, since a
    ///     lifted comparison lets <c>null</c> through on either side, and a reference type has no pair at all.
    /// </remarks>
    private static bool TryOrdering(ExpressionSyntax condition, SemanticModel model, IParameterSymbol[] mentioned, out OrderedPairing? pairing) {
        pairing = null;

        if (Unwrapped(condition) is not BinaryExpressionSyntax comparison) { return false; }

        if (Unwrapped(comparison.Left) is not IdentifierNameSyntax left
         || Unwrapped(comparison.Right) is not IdentifierNameSyntax right
         || model.GetSymbolInfo(left).Symbol is not IParameterSymbol low
         || model.GetSymbolInfo(right).Symbol is not IParameterSymbol high
         || !mentioned.Contains(low, SymbolEqualityComparer.Default)
         || !mentioned.Contains(high, SymbolEqualityComparer.Default)
         || SymbolEqualityComparer.Default.Equals(low, high)) {
            return false;
        }

        if (!SymbolEqualityComparer.Default.Equals(low.Type, high.Type)
         || !low.Type.IsValueType
         || low.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }) {
            return false;
        }

        pairing = comparison.Kind() switch {
            SyntaxKind.GreaterThanExpression        => new OrderedPairing(low.Name, high.Name, strict: false),
            SyntaxKind.GreaterThanOrEqualExpression => new OrderedPairing(low.Name, high.Name, strict: true),
            SyntaxKind.LessThanExpression           => new OrderedPairing(high.Name, low.Name, strict: false),
            SyntaxKind.LessThanOrEqualExpression    => new OrderedPairing(high.Name, low.Name, strict: true),
            _                                       => null
        };

        return pairing is not null;
    }

    /// <summary>
    ///     Whether <paramref name="branch" /> is a null-check on members that hold a parameter unchanged — one
    ///     that narrows nothing.
    /// </summary>
    /// <remarks>
    ///     A member holding the value unchanged is null exactly when the parameter is, so the check is the
    ///     parameter's own null-check: it narrows no chain, and keeps a nullable parameter's default a value (#411). Each
    ///     half of "unchanged" is asked of the compiler: the right side of every copy is the parameter itself,
    ///     and every member followed holds what it is assigned — a local, a field or an auto-implemented
    ///     property, never one whose <c>set</c> accessor can rewrite the value (#333). A value COMPUTED from
    ///     the parameter can be null — <c>name.Length &lt; 5 ? null : name</c> — and its null-check is a real guard.
    /// </remarks>
    private static bool IsANullCheckOnAPlainCopy(Branch branch, SemanticModel model) {
        return IsNullCheck(branch, model)
            && NamedSymbols(branch.Subject, model).All(HoldsWhatItIsAssigned)
            && CopySources(branch.Subject, model).All(source => IsTheParameterItself(source, model));
    }

    /// <summary>Whether a branch tests one value against null and nothing else, in either spelling.</summary>
    private static bool IsNullCheck(Branch branch, SemanticModel model) {
        if (branch.Pattern is null) { return IsNullCheck(branch.Condition, model); }

        return branch.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } && literal.IsKind(SyntaxKind.NullLiteralExpression);
    }

    /// <summary>
    ///     Whether <paramref name="condition" /> tests one value against null and nothing else.
    /// </summary>
    /// <remarks>
    ///     <c>is null</c> cannot be overloaded. <c>== null</c> can, and a user-defined operator is a method, free to
    ///     answer <c>true</c> for a non-null value it rejects — so that spelling is a null-check only when the
    ///     comparison the compiler resolved is the built-in one (#333).
    /// </remarks>
    private static bool IsNullCheck(ExpressionSyntax condition, SemanticModel model) {
        return condition switch {
            IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } } =>
                literal.IsKind(SyntaxKind.NullLiteralExpression),
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) =>
                (IsNull(binary.Left) || IsNull(binary.Right)) && !IsUserDefinedOperator(binary, model),
            _ => false
        };
    }

    /// <summary>
    ///     Whether <paramref name="comparison" /> calls an operator someone wrote. A pattern has none to call, and
    ///     answers false.
    /// </summary>
    private static bool IsUserDefinedOperator(BinaryExpressionSyntax? comparison, SemanticModel model) {
        return comparison is not null
            && model.GetSymbolInfo(comparison).Symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator };
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
            if (pattern.Pattern is not ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal }
             || !literal.IsKind(SyntaxKind.NullLiteralExpression)) {
                return false;
            }

            constraint = GuardConstraint.NullRefused;

            return true;
        }

        if (condition is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation) {
            // `!(p > 0)` is `p <= 0` and reads as that row (#408): a comparison negated whole is the opposite
            // comparison, with no third value for the two to disagree on — NaN, the one such value, is never drawn.
            if (Unwrapped(negation.Operand) is BinaryExpressionSyntax negated && Negated(negated.Kind()) is { } opposite) {
                return TryComparison(negated, opposite, model, parameter, sizes, names, out constraint);
            }

            // `!Enum.IsDefined(typeof(E), p)` — Any.Enum<E>() already draws only declared members, which holds
            // only where the parameter IS E. On an int-backed status column the row's own justification fails:
            // nothing narrows Any.Int32(), and the outcome is indistinguishable from a guard there was nothing
            // to read — worse than a lost constraint, because it does not even say it lost one.
            //
            // `!double.IsFinite(p)` is the same case for a value type: Any.Double() and Any.Single() draw within an
            // ordinary magnitude and never NaN or an infinity (ADR-0031), which again holds only where the
            // parameter IS the type whose IsFinite is called.
            return (IsCall(negation.Operand, model, "System.Enum", "IsDefined")
                 && NamesTheParametersOwnUniverse(negation.Operand, model, parameter))
                || IsAFloatingPointCheckOnTheParameter(negation.Operand, model, parameter, "IsFinite");
        }

        if (condition is InvocationExpressionSyntax invocation) {
            return TryNullElementCheck(invocation, model, parameter, out constraint)
                || TryCallCondition(invocation, model, parameter, sizes, out constraint);
        }

        if (condition is not BinaryExpressionSyntax comparison) { return false; }

        if (comparison.IsKind(SyntaxKind.IsExpression)) { return TryIsConstant(comparison, model, parameter, sizes, names, out constraint); }

        return TryScale(comparison, model, parameter, out constraint)
            || TryCharset(comparison, model, parameter, out constraint)
            || TryComparison(comparison, comparison.Kind(), model, parameter, sizes, names, out constraint);
    }

    /// <summary>
    ///     <c>Math.Abs(p) &gt; N</c> and <c>Math.Abs(p) &gt;= N</c>, in either orientation, on a signed number: the
    ///     value lies outside <c>[-N, N]</c>, which is the symmetric range the two bounds state (#416).
    /// </summary>
    /// <remarks>
    ///     Exact where <c>N</c> is a non-negative constant: <c>|p| &gt; N</c> throws exactly where <c>p &lt; -N</c>
    ///     or <c>p &gt; N</c>, the disjunction of two rows the set already reads, so this is the rewrite of
    ///     ADR-0128 written out for the one arithmetic call the field met. <c>Math.Abs</c> of an unsigned type
    ///     does not exist, and a negative <c>N</c> rejects everything, which no bound states; both fall through
    ///     to the comparison reading, which refuses a subject that is not the parameter.
    /// </remarks>
    private static bool TryMagnitude(BinaryExpressionSyntax comparison, SemanticModel model, IParameterSymbol parameter, out GuardConstraint? floor, out GuardConstraint? ceiling) {
        floor   = null;
        ceiling = null;

        bool flipped = Unwrapped(comparison.Right) is InvocationExpressionSyntax;

        ExpressionSyntax call  = flipped ? comparison.Right : comparison.Left;
        ExpressionSyntax other = flipped ? comparison.Left : comparison.Right;

        if (Unwrapped(call) is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } magnitude
         || !IsCall(magnitude, model, "System.Math", "Abs")
         || !IsParameter(magnitude.ArgumentList.Arguments[0].Expression, model, parameter)
         || !IsNumeric(parameter.Type)
         || IsUnsigned(parameter.Type)) {
            return false;
        }

        Optional<object?> constant = model.GetConstantValue(other);

        if (!constant.HasValue || constant.Value is null || !IsNumber(constant.Value) || !TryDecimal(constant.Value, out decimal value) || value < 0) { return false; }

        string literal = Literal(constant.Value, parameter.Type);
        string negated = "-" + literal;

        switch (Operator(comparison.Kind(), flipped)) {
            case SyntaxKind.GreaterThanExpression:
                floor   = new GuardConstraint("GreaterThanOrEqualTo", negated, Bound.Lower, -value);
                ceiling = new GuardConstraint("LessThanOrEqualTo", literal, Bound.Upper, value);

                return true;
            case SyntaxKind.GreaterThanOrEqualExpression:
                floor   = new GuardConstraint("GreaterThan", negated, Bound.Lower, -value, exclusive: true);
                ceiling = new GuardConstraint("LessThan", literal, Bound.Upper, value, exclusive: true);

                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     <c>p is E.Member</c>, parsed as a type test since the member's spelling is a qualified name, and bound as
    ///     the constant pattern it is: the equality it spells, where the name resolves to a constant (ADR-0128).
    /// </summary>
    private static bool TryIsConstant(BinaryExpressionSyntax test,
                                      SemanticModel model,
                                      IParameterSymbol parameter,
                                      (bool ByCount, int Ceiling, int Floor) sizes,
                                      TypeNames names,
                                      out GuardConstraint? constraint) {
        constraint = null;

        return IsSubject(test.Left, model, parameter)
            && model.GetSymbolInfo(test.Right).Symbol is IFieldSymbol { HasConstantValue: true }
            && TryBound(new Comparison(null, test.Left, test.Right, SyntaxKind.EqualsExpression), model, parameter, sizes, names, out constraint);
    }

    /// <summary>
    ///     <c>items.Any(x =&gt; x is null)</c>, <c>items.Any(x =&gt; x == null)</c> or <c>items.Contains(null)</c> on a
    ///     collection parameter whose element type is not nullable: satisfied by construction, since every element
    ///     recipe the scaffold writes — the table's, or the generator the element type owns — draws a value, so the
    ///     guard is understood and adds nothing; the receiver is dereferenced, so the parameter itself refuses null
    ///     (#403).
    /// </summary>
    /// <remarks>
    ///     Where the element type is nullable — <c>IReadOnlyList&lt;Run?&gt;</c> — the row does not apply and the
    ///     guard keeps the mark: the element recipe is the parameter's own business there, and may draw <c>null</c>
    ///     into it one day. The receiver has to <b>be</b> the parameter, the lambda's test has to be the null-check
    ///     of its own parameter and nothing else, and the argument of <c>Contains</c> the null literal.
    /// </remarks>
    private static bool TryNullElementCheck(InvocationExpressionSyntax invocation, SemanticModel model, IParameterSymbol parameter, out GuardConstraint? constraint) {
        constraint = null;

        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: { } receiver, Name.Identifier.Text: "Any" or "Contains" } access
         || !IsParameter(receiver, model, parameter)
         || invocation.ArgumentList.Arguments.Count != 1
         || ElementTypeOf(parameter.Type) is not { } element
         || IsNullable(element)
         || !TestsAnElementForNull(access.Name.Identifier.Text, invocation.ArgumentList.Arguments[0].Expression, model)) {
            return false;
        }

        constraint = GuardConstraint.NullRefused;

        return true;
    }

    /// <summary>A reference type annotated <c>?</c>, or a <c>Nullable&lt;T&gt;</c>.</summary>
    private static bool IsNullable(ITypeSymbol type) {
        return type.NullableAnnotation == NullableAnnotation.Annotated
            || type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
    }

    /// <summary>
    ///     <c>Contains(null)</c>, or <c>Any(x =&gt; x is null)</c> and <c>Any(x =&gt; x == null)</c> — a lambda whose
    ///     body is the null-check of its own parameter and names nothing else.
    /// </summary>
    private static bool TestsAnElementForNull(string method, ExpressionSyntax argument, SemanticModel model) {
        if (method == "Contains") { return IsNull(argument); }

        return argument is SimpleLambdaExpressionSyntax { Body: ExpressionSyntax test } lambda
            && IsNullCheck(test, model)
            && NamedSymbols(test, model).All(symbol => symbol is IParameterSymbol named && named.Name == lambda.Parameter.Identifier.ValueText);
    }

    /// <summary>The element type of an array or of a generic collection, or null where the type holds none.</summary>
    private static ITypeSymbol? ElementTypeOf(ITypeSymbol type) {
        return type switch {
            IArrayTypeSymbol array                                       => array.ElementType,
            INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } collection => collection.TypeArguments[0],
            _                                                            => null
        };
    }

    /// <summary>The rows of the closed set whose condition is a call on the parameter, standing alone.</summary>
    private static bool TryCallCondition(InvocationExpressionSyntax invocation,
                                         SemanticModel model,
                                         IParameterSymbol parameter,
                                         (bool ByCount, int Ceiling, int Floor) sizes,
                                         out GuardConstraint? constraint) {
        constraint = null;

        // The subject has to BE the parameter, exactly as every other row of the closed set demands.
        // `IsCall` answers only "which method is this", so without this test `IsNullOrEmpty(value.Trim())`
        // read as a guard about `value` — an invariant of the trimmed value stated over the drawn one,
        // which the domain then rejects. The throw-helper row (`TryRecogniseThrowHelper`) and both guard
        // libraries (`LibraryGuards.TryRead`) already test it; this was the one spelling that did not.
        if (!IsCallOnTheParameter(invocation, model, parameter)) { return false; }

        if (IsCall(invocation, model, "System.String", BlanknessCheck)) {
            constraint = Blankness().RefusingNull();

            return true;
        }

        // `double.IsNaN(p)`, `double.IsInfinity(p)` and their one-sided halves, standing alone: the reading
        // of `!IsFinite` with its sign turned round, understood and adding nothing, since neither NaN nor an
        // infinity is ever drawn (#408).
        if (IsAFloatingPointCheckOnTheParameter(invocation, model, parameter, "IsNaN", "IsInfinity", "IsPositiveInfinity", "IsNegativeInfinity")) {
            return true;
        }

        if (!IsCall(invocation, model, "System.String", EmptinessCheck)) { return false; }

        constraint = Emptiness(sizes.ByCount).RefusingNull();

        return true;
    }

    /// <summary>The comparison that holds exactly when <paramref name="written" /> does not, or null for an operator that is not one.</summary>
    private static SyntaxKind? Negated(SyntaxKind written) {
        return written switch {
            SyntaxKind.GreaterThanExpression        => SyntaxKind.LessThanOrEqualExpression,
            SyntaxKind.LessThanOrEqualExpression    => SyntaxKind.GreaterThanExpression,
            SyntaxKind.LessThanExpression           => SyntaxKind.GreaterThanOrEqualExpression,
            SyntaxKind.GreaterThanOrEqualExpression => SyntaxKind.LessThanExpression,
            SyntaxKind.EqualsExpression             => SyntaxKind.NotEqualsExpression,
            SyntaxKind.NotEqualsExpression          => SyntaxKind.EqualsExpression,
            _                                       => null
        };
    }

    /// <summary>
    ///     <c>decimal.Round(p, N) != p</c>, or <c>Math.Round(p, N) != p</c>, on a <c>decimal</c> parameter: the
    ///     value has more than <c>N</c> decimal places, which is exactly what <c>WithScale(N)</c> draws against (#408).
    /// </summary>
    /// <remarks>
    ///     Either side may carry the call; the other has to be the parameter itself. A rounding mode argument, a
    ///     non-constant scale or a scale outside what the generator accepts (0 to 28) is not this row, and falls to
    ///     the ordinary comparison reading — which refuses it, since the parameter sits on both sides.
    /// </remarks>
    private static bool TryScale(BinaryExpressionSyntax comparison, SemanticModel model, IParameterSymbol parameter, out GuardConstraint? constraint) {
        constraint = null;

        if (!comparison.IsKind(SyntaxKind.NotEqualsExpression) || Underlying(parameter.Type).SpecialType != SpecialType.System_Decimal) { return false; }

        ExpressionSyntax rounded = IsParameter(comparison.Right, model, parameter) ? comparison.Left : comparison.Right;
        ExpressionSyntax plain   = ReferenceEquals(rounded, comparison.Left) ? comparison.Right : comparison.Left;

        if (!IsParameter(plain, model, parameter)) { return false; }
        if (Unwrapped(rounded) is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 2 } call) { return false; }
        if (!IsCall(call, model, "System.Decimal", "Round") && !IsCall(call, model, "System.Math", "Round")) { return false; }
        if (!IsParameter(call.ArgumentList.Arguments[0].Expression, model, parameter)) { return false; }

        Optional<object?> scale = model.GetConstantValue(call.ArgumentList.Arguments[1].Expression);

        if (scale.Value is not int places || places < 0 || places > 28) { return false; }

        constraint = new GuardConstraint("WithScale", places.ToString(CultureInfo.InvariantCulture), Bound.Exact);

        return true;
    }

    /// <summary>
    ///     <c>p.IndexOfAnyExcept("…") >= 0</c> — some character of <c>p</c> is outside the pool — read as
    ///     <c>WithChars("…")</c>, the pool the generator then draws every character from (#408).
    /// </summary>
    /// <remarks>
    ///     The receiver has to be the parameter itself, the pool a compile-time constant with at least one
    ///     character, and the test one of the three spellings of "found": <c>>= 0</c>, <c>> -1</c>, <c>!= -1</c>,
    ///     on either side. The pool is written as a literal in the emitted call, escaped by the compiler's own
    ///     rules, so a pool naming a constant the file cannot reach still compiles.
    /// </remarks>
    private static bool TryCharset(BinaryExpressionSyntax comparison, SemanticModel model, IParameterSymbol parameter, out GuardConstraint? constraint) {
        constraint = null;

        if (parameter.Type.SpecialType != SpecialType.System_String) { return false; }

        bool flipped = Unwrapped(comparison.Right) is InvocationExpressionSyntax;

        ExpressionSyntax call  = flipped ? comparison.Right : comparison.Left;
        ExpressionSyntax other = flipped ? comparison.Left : comparison.Right;

        if (Unwrapped(call) is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } search
         || !IsCall(search, model, "System.MemoryExtensions", "IndexOfAnyExcept")
         || search.Expression is not MemberAccessExpressionSyntax { Expression: { } receiver }
         || !IsTheParameterAsASpan(receiver, model, parameter)) {
            return false;
        }

        if (model.GetConstantValue(search.ArgumentList.Arguments[0].Expression).Value is not string pool || pool.Length == 0) { return false; }

        Optional<object?> bound = model.GetConstantValue(other);

        if (bound.Value is not int edge) { return false; }

        bool found = Operator(comparison.Kind(), flipped) switch {
            SyntaxKind.GreaterThanOrEqualExpression => edge == 0,
            SyntaxKind.GreaterThanExpression        => edge == -1,
            SyntaxKind.NotEqualsExpression          => edge == -1,
            _                                       => false
        };

        if (!found) { return false; }

        constraint = new GuardConstraint("WithChars", SymbolDisplay.FormatLiteral(pool, quote: true), Bound.Exact);

        return true;
    }

    /// <summary>
    ///     <c>p.AsSpan()</c>, the receiver a span search over a string has to be written with — an extension
    ///     method's receiver takes no implicit conversion — or the parameter itself.
    /// </summary>
    private static bool IsTheParameterAsASpan(ExpressionSyntax receiver, SemanticModel model, IParameterSymbol parameter) {
        if (IsParameter(receiver, model, parameter)) { return true; }

        return Unwrapped(receiver) is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } span
            && IsCall(span, model, "System.MemoryExtensions", "AsSpan")
            && span.Expression is MemberAccessExpressionSyntax { Expression: { } spanned }
            && IsParameter(spanned, model, parameter);
    }

    private static bool TryComparison(BinaryExpressionSyntax comparison,
                                      SyntaxKind written,
                                      SemanticModel model,
                                      IParameterSymbol parameter,
                                      (bool ByCount, int Ceiling, int Floor) sizes,
                                      TypeNames names,
                                      out GuardConstraint? constraint) {
        constraint = null;

        if (!TrySides(comparison, model, parameter, out ExpressionSyntax subject, out ExpressionSyntax other, out bool flipped)) {
            return false;
        }

        return TryBound(new Comparison(comparison, subject, other, Operator(written, flipped)), model, parameter, sizes, names, out constraint);
    }

    /// <summary>
    ///     A comparison of the subject against another operand, with its sides told apart and its operator read
    ///     from the subject's side — written as a binary expression, or as a relational pattern, which has no
    ///     expression of its own to ask a user-defined operator of.
    /// </summary>
    private sealed class Comparison {

        internal Comparison(BinaryExpressionSyntax? written, ExpressionSyntax subject, ExpressionSyntax other, SyntaxKind kind) {
            Written = written;
            Subject = subject;
            Other   = other;
            Kind    = kind;
        }

        internal BinaryExpressionSyntax? Written { get; }

        internal ExpressionSyntax Subject { get; }

        internal ExpressionSyntax Other { get; }

        /// <summary>The operator, read from the subject's side.</summary>
        internal SyntaxKind Kind { get; }

    }

    /// <summary>The rows of the closed set for one <see cref="Comparison" />.</summary>
    private static bool TryBound(Comparison compared,
                                 SemanticModel model,
                                 IParameterSymbol parameter,
                                 (bool ByCount, int Ceiling, int Floor) sizes,
                                 TypeNames names,
                                 out GuardConstraint? constraint) {
        constraint = null;

        BinaryExpressionSyntax? comparison = compared.Written;
        ExpressionSyntax        other      = compared.Other;
        SyntaxKind              @operator  = compared.Kind;
        bool                    sized      = IsSize(compared.Subject, parameter, model);

        // `p == null`, `p == Guid.Empty`, `p == default` and `p == E.Member` are the comparisons whose right side
        // is not a number.
        if (@operator == SyntaxKind.EqualsExpression && TryEquality(comparison, other, model, parameter, names, out constraint)) {
            return constraint is not null;
        }

        if (TryTimeSpanConstant(other, model, parameter, names, out decimal ticks, out string? spelled)) {
            constraint = Numeric(@operator, ticks, parameter.Type, spelled!);

            return constraint is not null;
        }

        Optional<object?> constant = model.GetConstantValue(other);

        if (!constant.HasValue || constant.Value is null || !IsNumber(constant.Value)) { return false; }
        if (!TryDecimal(constant.Value, out decimal value)) { return false; }

        // A size is read through `p.Length` or `p.Count`, which dereferences the parameter: the guard throws on
        // null as surely as a null-check does, so a nullable reference parameter keeps drawing a value (#411).
        constraint = sized
                         ? Sized(@operator, value, sizes, Named(other, model, model.Compilation.GetSpecialType(SpecialType.System_Int32), names))?.RefusingNull()
                         : Numeric(@operator, value, parameter.Type, Named(other, model, Underlying(parameter.Type), names) ?? Literal(constant.Value, parameter.Type));

        return constraint is not null;
    }

    /// <summary>
    ///     The equality rows whose other side is not a number: null, the empty <c>Guid</c>, an enum member, and
    ///     the <c>default</c> of a struct. True means the row was recognised, with its constraint — or with none,
    ///     for a user-defined <c>==</c> the row declines to read (#333).
    /// </summary>
    private static bool TryEquality(BinaryExpressionSyntax? comparison,
                                    ExpressionSyntax other,
                                    SemanticModel model,
                                    IParameterSymbol parameter,
                                    TypeNames names,
                                    out GuardConstraint? constraint) {
        constraint = null;

        if (IsNull(other)) {
            constraint = IsUserDefinedOperator(comparison, model) ? null : GuardConstraint.NullRefused;

            return true;
        }

        if (IsEmptyGuid(other, model) || (IsDefault(other, model, parameter) && IsGuid(parameter.Type))) {
            constraint = new GuardConstraint("NonEmpty", argument: null, Bound.Emptiness);

            return true;
        }

        if (TryEnumMember(other, model, parameter, names, out string? excluded)) {
            constraint = new GuardConstraint("DifferentFrom", excluded, Bound.Excluded);

            return true;
        }

        // `p == default` on a struct the number line does not place — a DateTimeOffset, a date, a duration — is
        // the exclusion of that one value (#408). A number's `default` is its zero, read by the numeric rows as
        // the constant it is; a nullable's is null, and reads as the null-check above. Only a struct of the
        // framework: its `==` is value equality by contract, where a struct of the developer's own carries a
        // user-defined `==` free to answer true for a value that is not `default` (#333).
        if (IsDefault(other, model, parameter) && !IsNumeric(parameter.Type)) {
            constraint = IsOfTheFramework(parameter.Type) ? new GuardConstraint("DifferentFrom", "default", Bound.Excluded) : null;

            return true;
        }

        return false;
    }

    /// <summary>A type declared in the <c>System</c> namespace itself — the BCL's own structs.</summary>
    private static bool IsOfTheFramework(ITypeSymbol type) {
        return type.ContainingNamespace is { IsGlobalNamespace: false } @namespace
            && @namespace.ToDisplayString() == "System";
    }

    /// <summary>
    ///     Whether <paramref name="expression" /> is <c>default</c>, or <c>default(T)</c> for the parameter's own
    ///     type, compared against a non-nullable value-type parameter — the one value a struct cannot refuse to
    ///     hold, and the commonest guard on a date or an identifier that was never set.
    /// </summary>
    /// <remarks>
    ///     A nullable parameter's <c>default</c> is <c>null</c>, and a reference parameter's too: both are the
    ///     null-check, not this. A <c>default(Other)</c> is a different value altogether.
    /// </remarks>
    private static bool IsDefault(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        if (!parameter.Type.IsValueType || parameter.Type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }) { return false; }

        return Unwrapped(expression) switch {
            LiteralExpressionSyntax literal   => literal.IsKind(SyntaxKind.DefaultLiteralExpression),
            DefaultExpressionSyntax @default => SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(@default.Type).Type, parameter.Type),
            _                                 => false
        };
    }

    private static bool IsGuid(ITypeSymbol type) {
        return type.ToDisplayString(ByNamespace) == "System.Guid";
    }

    /// <summary>A type the number line places: integral, floating-point or decimal, nullable or not.</summary>
    private static bool IsNumeric(ITypeSymbol type) {
        return Underlying(type).SpecialType is SpecialType.System_SByte or SpecialType.System_Byte
                                            or SpecialType.System_Int16 or SpecialType.System_UInt16
                                            or SpecialType.System_Int32 or SpecialType.System_UInt32
                                            or SpecialType.System_Int64 or SpecialType.System_UInt64
                                            or SpecialType.System_Single or SpecialType.System_Double
                                            or SpecialType.System_Decimal
            || Underlying(type).ToDisplayString(ByNamespace) is "System.Int128" or "System.UInt128" or "System.Half";
    }

    /// <summary>
    ///     A <c>TimeSpan</c> bound written as a member of a closed list — <c>TimeSpan.Zero</c>, <c>MinValue</c>,
    ///     <c>MaxValue</c>, or <c>TimeSpan.FromX(constant)</c> — as the ticks it stands for, and as the emitted
    ///     chain spells it (#408).
    /// </summary>
    /// <remarks>
    ///     Not a constant to the compiler — a struct never is — so the ordinary constant reading passes it over
    ///     and the guard fell unread. These six factories and three fields are the whole of how a duration is
    ///     written in a guard; the ticks are computed by the same <see cref="TimeSpan" /> the domain will call,
    ///     and the argument is spelled as the comparison rows spell theirs: a reachable constant by name, anything
    ///     else by value. Only on a <c>TimeSpan</c> parameter, nullable or not: the bound has to be of the
    ///     parameter's type for the chain to bind.
    /// </remarks>
    private static bool TryTimeSpanConstant(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter, TypeNames names, out decimal ticks, out string? spelled) {
        ticks   = 0m;
        spelled = null;

        if (Underlying(parameter.Type).ToDisplayString(ByNamespace) != "System.TimeSpan") { return false; }

        ExpressionSyntax bare = Unwrapped(expression);

        if (bare is MemberAccessExpressionSyntax field
         && model.GetSymbolInfo(field).Symbol is IFieldSymbol { IsStatic: true } member
         && member.ContainingType.ToDisplayString(ByNamespace) == "System.TimeSpan"
         && member.Name is "Zero" or "MinValue" or "MaxValue") {
            ticks   = member.Name switch { "Zero" => 0m, "MinValue" => long.MinValue, _ => long.MaxValue };
            spelled = names.Of(member.ContainingType) + "." + member.Name;

            return true;
        }

        return bare is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } call
            && model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsStatic: true } factory
            && factory.ContainingType.ToDisplayString(ByNamespace) == "System.TimeSpan"
            && TryTimeSpanFactory(call, factory, model, names, out ticks, out spelled);
    }

    /// <summary><c>TimeSpan.FromX(constant)</c>, for the six factories a duration is written with.</summary>
    private static bool TryTimeSpanFactory(InvocationExpressionSyntax call, IMethodSymbol factory, SemanticModel model, TypeNames names, out decimal ticks, out string? spelled) {
        ticks   = 0m;
        spelled = null;

        if (factory.Name is not ("FromDays" or "FromHours" or "FromMinutes" or "FromSeconds" or "FromMilliseconds" or "FromTicks")) { return false; }

        ExpressionSyntax  argument = call.ArgumentList.Arguments[0].Expression;
        Optional<object?> amount   = model.GetConstantValue(argument);

        if (!amount.HasValue || amount.Value is null || !IsNumber(amount.Value) || !TryDecimal(amount.Value, out decimal count)) { return false; }

        try {
            ticks = factory.Name switch {
                "FromDays"         => TimeSpan.FromDays((double)count).Ticks,
                "FromHours"        => TimeSpan.FromHours((double)count).Ticks,
                "FromMinutes"      => TimeSpan.FromMinutes((double)count).Ticks,
                "FromSeconds"      => TimeSpan.FromSeconds((double)count).Ticks,
                "FromMilliseconds" => TimeSpan.FromMilliseconds((double)count).Ticks,
                _                  => TimeSpan.FromTicks((long)count).Ticks
            };
        } catch (OverflowException) {
            // A duration no TimeSpan holds is not a bound; the guard is left unread, as a number past decimal is.
            return false;
        }

        ITypeSymbol argumentType = model.GetTypeInfo(argument).Type ?? factory.Parameters[0].Type;

        spelled = names.Of(factory.ContainingType) + "." + factory.Name + "("
                + (Named(argument, model, argumentType, names) ?? Literal(amount.Value, argumentType)) + ")";

        return true;
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

        spelled = names.Of(universe) + "." + Spelled(member.Name);

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
    internal static bool TryDecimal(object constant, out decimal value) {
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
    internal static GuardConstraint? Sized(SyntaxKind @operator, decimal value, (bool ByCount, int Ceiling, int Floor) sizes, string? named = null) {
        if (value != decimal.Truncate(value) || value < 0 || value > int.MaxValue) { return null; }

        string exact = sizes.ByCount ? "WithCount" : "WithLength";
        string min   = sizes.ByCount ? "WithMinCount" : "WithMinLength";
        string max   = sizes.ByCount ? "WithMaxCount" : "WithMaxLength";
        string count = named ?? ((int)value).ToString(CultureInfo.InvariantCulture);

        // A floor and an exact size ask the generator to PRODUCE that many, so they answer to both limits; a
        // ceiling only asks it not to exceed one, so the element domain has nothing to say about it. Past the
        // cap itself, a ceiling holds by construction and a floor admits no draw (#362) — the cap is the one
        // limit a size never crosses, where a floor between a distinct count and the cap is only refused.
        return @operator switch {
            SyntaxKind.EqualsExpression when value == 0                  => Emptiness(sizes.ByCount),
            SyntaxKind.LessThanExpression when value == 1                => Emptiness(sizes.ByCount),
            SyntaxKind.LessThanExpression when value <= sizes.Floor      => new GuardConstraint(min, count, Bound.Lower, value),
            SyntaxKind.LessThanExpression when value > sizes.Ceiling     => GuardConstraint.FloorBeyondTheCap(value),
            SyntaxKind.GreaterThanExpression when value <= sizes.Ceiling => new GuardConstraint(max, count, Bound.Upper, value),
            SyntaxKind.GreaterThanExpression                             => GuardConstraint.CeilingBeyondTheCap(value),
            SyntaxKind.NotEqualsExpression when value <= sizes.Floor     => new GuardConstraint(exact, count, Bound.Exact, value),
            SyntaxKind.NotEqualsExpression when value > sizes.Ceiling    => GuardConstraint.FloorBeyondTheCap(value),
            _                                                            => null
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
    ///     The same rule keeps <c>p &lt;= 0</c> and <c>p &gt;= 0</c> on their sign rows ahead of the general
    ///     strict bounds below them, which <c>ThrowIfLessThanOrEqual</c> and <c>ThrowIfGreaterThanOrEqual</c>
    ///     already read into.
    /// </remarks>
    internal static GuardConstraint? Numeric(SyntaxKind @operator, decimal value, ITypeSymbol type, string literal) {
        bool integral = IsIntegral(type);

        return @operator switch {
            SyntaxKind.LessThanOrEqualExpression when value == 0        => Positive(type),
            SyntaxKind.LessThanExpression when value == 1 && integral   => Positive(type),
            SyntaxKind.GreaterThanOrEqualExpression when value == 0     => Negative(type),
            SyntaxKind.EqualsExpression when value == 0                 => new GuardConstraint("NonZero", null, Bound.Zero),
            SyntaxKind.GreaterThanExpression                            => new GuardConstraint("LessThanOrEqualTo", literal, Bound.Upper, value),
            SyntaxKind.LessThanExpression                               => new GuardConstraint("GreaterThanOrEqualTo", literal, Bound.Lower, value),
            SyntaxKind.GreaterThanOrEqualExpression                     => new GuardConstraint("LessThan", literal, Bound.Upper, value, exclusive: true),
            SyntaxKind.LessThanOrEqualExpression                        => new GuardConstraint("GreaterThan", literal, Bound.Lower, value, exclusive: true),
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

    /// <summary>
    ///     The row <c>IsNullOrWhiteSpace</c> reads as. String-only by construction — neither spelling of the guard
    ///     exists for a collection — so unlike <see cref="Emptiness" /> there is no size family to choose between.
    /// </summary>
    private static GuardConstraint Blankness() {
        return new GuardConstraint("NotBlank", argument: null, Bound.Emptiness);
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

    /// <summary>
    ///     Whether <paramref name="call" /> is one of <paramref name="checks" /> — <c>IsFinite</c>, <c>IsNaN</c>,
    ///     <c>IsInfinity</c> and its halves — of <c>double</c> on a <c>double</c> parameter itself, or of
    ///     <c>float</c> on a <c>float</c> one.
    /// </summary>
    private static bool IsAFloatingPointCheckOnTheParameter(ExpressionSyntax call, SemanticModel model, IParameterSymbol parameter, params string[] checks) {
        string? owner = parameter.Type.SpecialType switch {
            SpecialType.System_Double => "System.Double",
            SpecialType.System_Single => "System.Single",
            _                         => null
        };

        return owner is not null
            && call is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation
            && IsCall(invocation, model, owner, checks)
            && IsParameter(invocation.ArgumentList.Arguments[0].Expression, model, parameter);
    }

    /// <summary>The parameter itself, or the one derived form the table has rows for: its length or its count.</summary>
    private static bool IsSubject(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        return IsParameter(expression, model, parameter) || IsSize(expression, parameter, model);
    }

    internal static bool IsParameter(ExpressionSyntax expression, SemanticModel model, IParameterSymbol parameter) {
        ExpressionSyntax bare = Unwrapped(expression);

        return bare is IdentifierNameSyntax
            && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(bare).Symbol, parameter);
    }

    /// <summary>
    ///     The expression itself, past the parentheses a writer is free to add around it and the
    ///     null-forgiving <c>!</c> a writer is free to add after it.
    /// </summary>
    /// <remarks>
    ///     <c>!</c> is a compile-time annotation with no run-time effect, so <c>value!</c> and <c>value</c> are
    ///     the same value at every call site this reads — exactly the reasoning <see cref="HandedFrom" />
    ///     already applies one hop into a delegated constructor. Stripping it here reaches the same case where
    ///     the guard is read directly: <c>string.IsNullOrEmpty(value!)</c> is a guard about <c>value</c>, not a
    ///     shape the closed set declines to recognise.
    /// </remarks>
    private static ExpressionSyntax Unwrapped(ExpressionSyntax expression) {
        ExpressionSyntax bare = expression;

        while (true) {
            if (bare is ParenthesizedExpressionSyntax parenthesised) {
                bare = parenthesised.Expression;
            } else if (bare is PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed) {
                bare = suppressed.Operand;
            } else {
                return bare;
            }
        }
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
    ///     How long, or how many — the parameter's own, never that of something read off it, and only where
    ///     the engine's own size vocabulary means anything for the parameter's type at all.
    /// </summary>
    /// <remarks>
    ///     The receiver has to <b>be</b> the parameter. <c>p[0].Length</c>, <c>p.Split(',').Length</c> and
    ///     <c>p.Trim().Length</c> are the length of something else, and the family the constraint is written in
    ///     comes from the parameter's own type — so an element's length reads as the collection's count, a
    ///     different invariant emitted with a straight face.
    ///     <para>
    ///         <b>The parameter's type has to be one the engine's size families mean something for</b> — a
    ///         string, or a type <see cref="GeneratorFor.SizedByCount" /> already recognises. <c>tags.Count</c>
    ///         on a composed <c>Tags</c> parameter is neither: the family this guard would be written in is
    ///         chosen from a fallback (§14.3's length family, the one every other size defaults to), not from
    ///         anything the type actually is, and the composed generator that fallback lands on — the
    ///         factory's own string argument, say — happens to carry a same-named member too, which is what
    ///         let the misattribution through in silence rather than as a drop. Refused rather than guessed, on
    ///         the same footing as the numeric family's arithmetic conditions §9 already names as out of reach:
    ///         <see cref="IsParameter" /> alone answers false for it too, so it falls to the same unread mark
    ///         a condition the closed set fails to recognise already earns.
    ///     </para>
    /// </remarks>
    private static bool IsSize(ExpressionSyntax subject, IParameterSymbol parameter, SemanticModel model) {
        return Unwrapped(subject) is MemberAccessExpressionSyntax access
            && access.Name.Identifier.Text is "Length" or "Count"
            && IsParameter(access.Expression, model, parameter)
            && (parameter.Type.SpecialType == SpecialType.System_String || GeneratorFor.SizedByCount(parameter.Type));
    }

    private static bool IsNull(ExpressionSyntax expression) {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression);
    }

    private static bool IsEmptyGuid(ExpressionSyntax expression, SemanticModel model) {
        return expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Empty" } access
            && model.GetTypeInfo(access).Type?.ToDisplayString(ByNamespace) == "System.Guid";
    }

    /// <summary>
    ///     Whether the invocation's first argument <b>is</b> <paramref name="parameter" /> — the
    ///     subject-identity discipline, asked of a call written as a condition.
    /// </summary>
    private static bool IsCallOnTheParameter(InvocationExpressionSyntax invocation, SemanticModel model, IParameterSymbol parameter) {
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        return arguments.Count > 0 && IsParameter(arguments[0].Expression, model, parameter);
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
    ///     The parameters <paramref name="node" /> names for itself, past any <c>nameof</c> inside it — and, when
    ///     <paramref name="ignoresWhatIsThrown" /> says only what decides a throw counts, past the exception a
    ///     throw builds.
    /// </summary>
    /// <remarks>
    ///     The exclusion matters once whole statements are read rather than conditions alone: every guard's own
    ///     throw spells its rejected parameter with <c>nameof</c>, so counting it would make the message the
    ///     evidence instead of the test. A parameter interpolated into the message is the same evidence in another
    ///     spelling, and a guard that names its subject only there rejects nothing about it (#370).
    /// </remarks>
    private static IParameterSymbol[] Mentioned(SyntaxNode node, SemanticModel model, IMethodSymbol method, bool ignoresWhatIsThrown = false) {
        InvocationExpressionSyntax[] spelled = [.. node.DescendantNodesAndSelf()
                                                       .OfType<InvocationExpressionSyntax>()
                                                       .Where(IsNameOf)];

        return node.DescendantNodesAndSelf(descendant => !ignoresWhatIsThrown || !IsWhatIsThrown(descendant))
                   .Where(descendant => !ignoresWhatIsThrown || !IsWhatIsThrown(descendant))
                   .OfType<IdentifierNameSyntax>()
                   .Where(identifier => !spelled.Any(name => name.Contains(identifier)))
                   .Select(identifier => model.GetSymbolInfo(identifier).Symbol as IParameterSymbol)
                   .Where(symbol => symbol is not null && method.Parameters.Contains(symbol, SymbolEqualityComparer.Default))
                   .Select(symbol => symbol!)
                   .Distinct(SymbolEqualityComparer.Default)
                   .OfType<IParameterSymbol>()
                   .ToArray();
    }

    /// <summary>The parameters any of <paramref name="nodes" /> names, each once — a branch's subject and pattern together.</summary>
    private static IParameterSymbol[] MentionedIn(IReadOnlyList<SyntaxNode> nodes, SemanticModel model, IMethodSymbol method) {
        return [.. nodes.SelectMany(node => Mentioned(node, model, method))
                        .Distinct(SymbolEqualityComparer.Default)
                        .OfType<IParameterSymbol>()];
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
    ///         walk of three lines rather than a second model of the language.
    ///         <see cref="Read(IMethodSymbol, Compilation, TypeNames)" /> hands a
    ///         leading statement, so the whole nesting inside it is asked about; <see cref="ReadChain" />
    ///         hands the <c>if</c> whose branch does not throw, so that branch refuses; and it hands the body
    ///         of a terminal <c>else</c>, which stops the walk before the clause — correctly, since reaching
    ///         the <c>else</c> of a branch that throws unconditionally is what every construction that
    ///         survives did. <see cref="ThrowsUnconditionally" /> is therefore not restated here: it stays
    ///         where it is, deciding which <c>else</c> bodies reach this method at all.
    ///     </para>
    ///     <para>
    ///         This is one half of whether a guard runs, and <see cref="Jumps" /> is the other: a walk of
    ///         ancestors can show what encloses a call, never what precedes the statement it sits in. So the
    ///         walk asks it too, at every level it climbs through: the leading-statement loop already ends
    ///         the reading of everything below a jump at the top level, and a block a <c>using</c> or a
    ///         <c>lock</c> always runs is the same question one nesting level down —
    ///         <c>lock (this) { if (lenient) { … return; } ThrowIfLessThan(value, 50); }</c> reaches the
    ///         helper on no path the <c>return</c> took, and neither an ancestor of the helper nor a sibling
    ///         of the <c>lock</c> can say so. Applying the rule the walk already claims, at the nesting
    ///         levels it already covers.
    ///     </para>
    /// </remarks>
    private static bool RunsUnconditionally(StatementSyntax carrier, StatementSyntax boundary, SemanticModel model) {
        SyntaxNode node = carrier;

        while (!ReferenceEquals(node, boundary)) {
            if (node.Parent is not SyntaxNode parent || !AlwaysRuns(parent, node)) { return false; }
            if (SkippedByAPrecedingSibling(parent, node, model)) { return false; }

            node = parent;
        }

        return true;
    }

    /// <summary>
    ///     Whether a statement written above <paramref name="child" /> in the block holding it can send
    ///     execution past it.
    /// </summary>
    /// <remarks>
    ///     A block is the one construct <see cref="AlwaysRuns" /> admits that holds more than one statement,
    ///     so it is the only level where "always runs its child" needs the second half of the question. The
    ///     others each scope a single body, and a <c>switch</c> section — the other multi-statement container
    ///     — never reaches here, refused by <see cref="AlwaysRuns" /> before the walk can ask.
    /// </remarks>
    private static bool SkippedByAPrecedingSibling(SyntaxNode parent, SyntaxNode child, SemanticModel model) {
        if (parent is not BlockSyntax block) { return false; }

        foreach (StatementSyntax preceding in block.Statements) {
            if (ReferenceEquals(preceding, child)) { return false; }
            if (Jumps(preceding, model)) { return true; }
        }

        return false;
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
    ///         <c>return</c>, a <c>goto</c> — which no ancestor of the call can show. <see cref="Jumps" />
    ///         answers that one, along the body rather than up through it.
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

    /// <summary>
    ///     Whether <paramref name="statement" /> can send execution past the statements written below it,
    ///     while the constructor still returns an object.
    /// </summary>
    /// <remarks>
    ///     The other half of <see cref="RunsUnconditionally" />, and the half no ancestor of a guard can
    ///     show. <c>if (lenient) { kept = value; return; } ThrowIfNegative(value);</c> has nothing enclosing
    ///     the guard at all, and yet <c>new Subject(lenient: true, value: -5)</c> constructs happily while
    ///     the reading emitted <c>GreaterThanOrEqualTo(0)</c> — inferred, with nothing to look at. It is not
    ///     the call spelling's own: the same <c>return</c> above <c>if (value &lt; 0) { throw … }</c> was
    ///     measured reading exactly as wrongly.
    ///     <para>
    ///         <b>Asked of the compiler, never of the syntax</b>, the same discipline
    ///         <see cref="ParameterWrites" /> keeps for writes.
    ///         <see cref="Microsoft.CodeAnalysis.ControlFlowAnalysis.ExitPoints" /> answers for
    ///         <c>return</c> and <c>goto</c> at once, and for the jumps nobody listed; it excludes a
    ///         <c>return</c> inside a lambda or a local function, which leaves that body rather than this
    ///         one, so an ordinary helper declared among the leading statements costs nothing. A list of
    ///         spellings would have had to be right about all three.
    ///     </para>
    ///     <para>
    ///         <b>Not the graph ADR-0084 declines.</b> That one is
    ///         <c>Microsoft.CodeAnalysis.FlowAnalysis.ControlFlowGraph</c>, asked for guard <b>placement</b>,
    ///         and refused because an edge it does not carry reads as <i>no write ran</i> — the unsafe
    ///         direction. This is <c>SemanticModel.AnalyzeControlFlow</c>, the region query that sits beside
    ///         the data-flow one <see cref="ParameterWrites" /> already asks, put to a different question;
    ///         and where it declines to answer, the statement counts as jumping, so the default points the
    ///         way that decision requires.
    ///     </para>
    ///     <para>
    ///         <b>A <c>throw</c> is not a jump.</b> It refuses to build the object, which is what makes it a
    ///         guard rather than a way around one — and counting it would refuse every guard written below
    ///         another. <see cref="Microsoft.CodeAnalysis.ControlFlowAnalysis.ExitPoints" /> does not report
    ///         it, which is the second reason the question goes there rather than to a walk of the tree.
    ///     </para>
    /// </remarks>
    private static bool Jumps(StatementSyntax statement, SemanticModel model) {
        ControlFlowAnalysis? flow = model.AnalyzeControlFlow(statement);

        // A region the compiler declines to analyse says nothing, and silence would be read here as "nothing
        // jumped" — the one answer that turns a guard the engine cannot reach into one it emits.
        return flow is not { Succeeded: true } || flow.ExitPoints.Length > 0;
    }

    private static bool AssignsState(StatementSyntax statement, SemanticModel model) {
        return statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment }
            && model.GetSymbolInfo(assignment.Left).Symbol is IFieldSymbol or IPropertySymbol;
    }

    internal static bool IsNumber(object value) {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    /// <summary>
    ///     The type a bound is actually read against — <c>int</c> for an <c>int?</c>.
    /// </summary>
    /// <remarks>
    ///     One definition, because three readers have to agree on it: whether the type is integral, how its
    ///     literal is spelled, and which enum universe a guard may name.
    /// </remarks>
    internal static ITypeSymbol Underlying(ITypeSymbol type) {
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
    ///     The bound as the guard wrote it, when it names a constant the scaffolded file can reach; null, for the
    ///     caller to fold it to its value, otherwise.
    /// </summary>
    /// <remarks>
    ///     The file is scaffolded once (§1), so a copied value goes stale in silence the day the domain changes
    ///     the constant; a reference moves with it (#377). Only a <c>const</c> field of exactly the type the
    ///     constraint takes, written on its own rather than inside an expression, and <c>public</c> at every level
    ///     of its nesting: the scaffolded file lives in the test project, which reaches nothing less without a
    ///     friend assembly the engine does not look for. Anything else — a private constant, a local one,
    ///     <c>Max - 1</c> — is folded, as it always was.
    /// </remarks>
    private static string? Named(ExpressionSyntax expression, SemanticModel model, ITypeSymbol type, TypeNames names) {
        while (expression is ParenthesizedExpressionSyntax parenthesized) { expression = parenthesized.Expression; }

        if (model.GetSymbolInfo(expression).Symbol is not IFieldSymbol { IsConst: true } field) { return null; }
        if (!SymbolEqualityComparer.Default.Equals(field.Type, type)) { return null; }
        if (field.ContainingType is not { TypeKind: not TypeKind.Enum, IsGenericType: false } owner) { return null; }

        for (ISymbol? level = field; level is not null and not INamespaceSymbol; level = level.ContainingSymbol) {
            if (level.DeclaredAccessibility != Accessibility.Public) { return null; }
        }

        return names.Of(owner) + "." + Spelled(field.Name);
    }

    /// <summary>
    ///     A member name as the emitted file has to write it: Roslyn reports <c>@checked</c> as <c>checked</c>, and
    ///     the <c>@</c> has to go back on a reserved word or the reference does not parse.
    /// </summary>
    private static string Spelled(string member) {
        return SyntaxFacts.GetKeywordKind(member) == SyntaxKind.None ? member : "@" + member;
    }

    /// <summary>
    ///     The constant, spelled so it binds to the constraint's parameter.
    /// </summary>
    /// <remarks>
    ///     A <c>decimal</c> bound written as <c>9.99</c> is a <c>double</c> literal, and there is no implicit
    ///     conversion — the emitted chain would not compile. The suffix is not decoration.
    /// </remarks>
    internal static string Literal(object value, ITypeSymbol type) {
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
