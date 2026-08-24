using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JustDummies.GenAny;

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
internal sealed class ParameterWrites {

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
    ///     Whether the constructor initializer writes <paramref name="parameter" /> — by reference, or
    ///     through a call in one of its own arguments — which is the same question <see cref="Precede" />
    ///     asks of the initializer's own regions, in isolation from where in the body a guard sits.
    /// </summary>
    internal bool WrittenByInitializer(IParameterSymbol parameter) {
        return HandedByReference(parameter) || Initializer().Any(region => Written(region, parameter));
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
