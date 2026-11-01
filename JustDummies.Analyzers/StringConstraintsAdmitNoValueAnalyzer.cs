using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

// A length a constraint declares, the call it is rendered as, and where in the chain that call sits.
using DeclaredLength = (int Length, string Rendered, int Position);

namespace JustDummies.Analyzers;

/// <summary>
///     JD015 — reports an <c>AnyString</c> chain whose constant constraints admit no value: two declared lengths no
///     length satisfies, anchored fragments that cannot fit the declared length, or declared constraints that admit
///     none of the values a constant <c>OneOf(...)</c> supplies.
/// </summary>
/// <remarks>
///     <para>
///         This is the case ADR-0014 names by hand as the one an analyzer should carry and the type system cannot:
///         <c>WithLength(3).StartingWith("ORD-")</c> conflicts while <c>WithLength(12).StartingWith("ORD-")</c> does
///         not, from identical call sites and identical static types. Only the argument's value tells them apart,
///         which is exactly what makes it value-dependent — and what puts it on the analyzer's side of the ADR's line.
///     </para>
///     <para>
///         The rule used to check the fragments against the declared family, subtractions and casing as well. It no
///         longer does, and must not: a character constraint governs what the generator <b>draws</b>, and an anchored
///         fragment is a literal the caller wrote, so <c>AlphaNumeric().StartingWith("ORD-")</c> is legal and draws no
///         hyphen anywhere but that prefix (ADR-0079). Reporting it here would refuse at build time what the run time
///         honours.
///     </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StringConstraintsAdmitNoValueAnalyzer : DiagnosticAnalyzer {

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.StringConstraintsAdmitNoValue);

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

        // Analyse each chain once, from its outermost call.
        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || factory.TargetMethod.Name != "String") { return; }

        AnalyzeConstraints(context, invocation, constraints);
    }

    /// <summary>
    ///     Reads what the chain declares about the string's layout — its anchored fragments and its length bounds —
    ///     then reports the pair no value can satisfy.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints) {
        IInvocationOperation? valueSet   = null;
        IOperation?           lastAnchor = null;
        DeclaredLength?       exact      = null;
        DeclaredLength?       minimum    = null;
        DeclaredLength?       maximum    = null;
        // What the calls declared before the value set had made of the string by the time it arrived.
        int             valueSetAt   = -1;
        IOperation?     anchorBefore = null;
        DeclaredLength? capBefore    = null;

        for (int position = 0; position < constraints.Count; position++) {
            IInvocationOperation constraint = constraints[position];

            switch (constraint.TargetMethod.Name) {
                // A terminal value set changes what the fragments are checked against: they are matched against the
                // pooled values rather than laid out side by side, so the length budget below no longer applies to
                // anything declared after it.
                case "OneOf":
                    if (valueSet is null) {
                        valueSetAt   = position;
                        anchorBefore = lastAnchor;
                        capBefore    = exact ?? maximum;
                    }

                    valueSet = constraint;

                    break;

                case "StartingWith" or "EndingWith" or "Containing" when constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string _):
                    lastAnchor = constraint.Arguments[0].Value;

                    break;

                case "WithLength" when TryGetLength(constraint, 0, out int length):
                    exact = (length, $"WithLength({length})", position);

                    break;

                case "WithMinLength" when TryGetLength(constraint, 0, out int min):
                    minimum = Longer(minimum, (min, $"WithMinLength({min})", position));

                    break;

                case "WithMaxLength" when TryGetLength(constraint, 0, out int max):
                    maximum = Shorter(maximum, (max, $"WithMaxLength({max})", position));

                    break;

                // The range form sets the same two bounds the single forms do, under its own one name — the name the
                // run time renders, and the call the reader edits. A range whose ends are reversed throws an argument
                // error of its own before any bound is set, so it is not read as one.
                case "WithLengthBetween" when TryGetLength(constraint, 0, out int low) && TryGetLength(constraint, 1, out int high) && low <= high:
                    minimum = Longer(minimum, (low, $"WithLengthBetween({low}, {high})", position));
                    maximum = Shorter(maximum, (high, $"WithLengthBetween({low}, {high})", position));

                    break;
            }
        }

        // The bounds are weighed against one another before anything else, value set or not: the run time checks
        // them first, on every constraint it applies.
        if (ReportContradictoryBounds(context, constraints, exact, minimum, maximum)) { return; }

        if (valueSet is not null) {
            if (ReportDeclaredBeforeTheValueSet(context, invocation, constraints.Take(valueSetAt).ToList(), anchorBefore, capBefore)) { return; }

            ReportEmptiedValueSet(context, constraints, valueSet);

            return;
        }

        if (ReportEmptiedAlphabet(context, invocation, constraints)) { return; }

        // An exact length below a maximum is the tighter cap, and one above it was reported just now.
        if (ReportLengthBudget(context, invocation, constraints, lastAnchor, exact ?? maximum, atTheDraw: true)) { return; }

        ReportBlankAlphabet(context, invocation, constraints);
    }

    /// <summary>
    ///     Reports what the calls declared before a value set make impossible on their own. Until the value set
    ///     arrives, the run time judges each call as a shaped string the moment it is declared, so such a chain is
    ///     refused there and then — the value set cannot reinterpret it.
    /// </summary>
    /// <remarks>
    ///     Only the draw asks for the position <c>NotBlank()</c> is owed, and the draw sees the value set, so that
    ///     position is not counted here.
    /// </remarks>
    private static bool ReportDeclaredBeforeTheValueSet(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> declaredBefore,
                                                        IOperation? lastAnchor, DeclaredLength? cap) {
        return ReportEmptiedAlphabet(context, invocation, declaredBefore)
            || ReportLengthBudget(context, invocation, declaredBefore, lastAnchor, cap, atTheDraw: false);
    }

    /// <summary>
    ///     Reports a shaped string its declared family or pool and its subtractions leave with no character to draw —
    ///     <c>Numeric().WithoutNumeric()</c> — which the run time refuses at the call that empties it.
    /// </summary>
    /// <remarks>
    ///     Named the way the run time names it: every subtraction that removes a character the family offers, since
    ///     naming only one would point at a call whose removal can leave the alphabet just as empty.
    /// </remarks>
    private static bool ReportEmptiedAlphabet(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints) {
        if (DeclaredAlphabet(constraints) is not (string family, string offered)) { return false; }

        List<(string Rendered, string Removed)> subtractions = Subtractions(constraints).ToList();
        if (Filler(offered, subtractions).Length > 0) { return false; }

        List<string> removers = subtractions.Where(subtraction => offered.Any(subtraction.Removed.Contains)).Select(subtraction => subtraction.Rendered).ToList();
        string       remove   = removers.Count == 1 ? "removes" : "remove";

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, invocation.Syntax.GetLocation(),
            $"{Conjoin(removers)} {remove} every character {family} offers"));

        return true;
    }

    /// <summary>
    ///     Reports a <c>NotBlank()</c> whose non-whitespace character has nowhere to come from: no anchored literal
    ///     carries one, and the filler has only whitespace to draw — <c>Whitespaces().NotBlank()</c>. The run time
    ///     refuses it at the draw. Where the filler has no room at all, the length budget has already said so.
    /// </summary>
    private static void ReportBlankAlphabet(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints) {
        if (!StringShapeFacts.FillerMustCarryNonBlank(constraints) || StringShapeFacts.HasUnreadableAnchor(constraints)) { return; }
        if (DeclaredAlphabet(constraints) is not (string family, string offered)) { return; }

        List<(string Rendered, string Removed)> subtractions = Subtractions(constraints).ToList();
        if (Filler(offered, subtractions).Any(character => !char.IsWhiteSpace(character))) { return; }

        string   nonBlank = new(offered.Where(character => !char.IsWhiteSpace(character)).ToArray());
        string[] removers = subtractions.Where(subtraction => nonBlank.Any(subtraction.Removed.Contains)).Select(subtraction => subtraction.Rendered).ToArray();
        string   remove   = removers.Length == 1 ? "removes" : "remove";
        string   reason   = removers.Length == 0 ? $"{family} offers none" : $"{Conjoin(removers)} {remove} every one {family} offers";

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, invocation.Syntax.GetLocation(),
            $"NotBlank() needs a character that is not whitespace, and {reason}"));
    }

    /// <summary>
    ///     The family or pool the chain draws from, as the caller wrote it, and the characters it offers under the
    ///     declared casing — or <c>null</c> where the rule cannot say: no family at all (the whole of ASCII, which no
    ///     subtraction empties), a pool that is not a constant, or an empty one, which the call rejects as an argument.
    ///     It is <c>null</c> too where the declarations conflict rather than the values, which JD034 reports: two
    ///     different calls in the one family or casing slot, or a casing beside a <c>WithChars</c> pool, which cannot
    ///     combine. The same call repeated is not a conflict — the run time ignores the repetition — so it counts once.
    /// </summary>
    /// <remarks>
    ///     What is returned therefore always offers a character, so an empty filler is always some subtraction's doing.
    ///     Reading a casing beside a pool as narrowing it would break that: <c>WithChars("abc").InUpperCase()</c> left
    ///     nothing, with no subtraction to name.
    /// </remarks>
    private static (string Family, string Offered)? DeclaredAlphabet(IReadOnlyList<IInvocationOperation> constraints) {
        List<(string Family, string Offered)> slot   = [];
        bool                                  pooled = false;
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;

            if (CharacterFamilies.PoolFor(name) is string family) {
                slot.Add(($"{name}()", family));
            } else if (name == "WithChars") {
                if (constraint.Arguments.Length != 1 || !ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string pool) || pool.Length == 0) { return null; }

                pooled = true;
                slot.Add(($"WithChars(\"{pool}\")", pool));
            }
        }

        bool upper = StringShapeFacts.Declares(constraints, "InUpperCase");
        bool lower = StringShapeFacts.Declares(constraints, "InLowerCase");
        List<(string Family, string Offered)> distinct = slot.Distinct().ToList();
        if (distinct.Count != 1 || (upper && lower) || (pooled && (upper || lower))) { return null; }

        (string declared, string offered) = distinct[0];
        if (upper) { offered = new string(offered.Where(character => CharacterFamilies.LowerLetters.IndexOf(character) < 0).ToArray()); }
        if (lower) { offered = new string(offered.Where(character => CharacterFamilies.UpperLetters.IndexOf(character) < 0).ToArray()); }

        return (declared, offered);
    }

    /// <summary>The subtractions the chain declares, each with the family it removes.</summary>
    private static IEnumerable<(string Rendered, string Removed)> Subtractions(IReadOnlyList<IInvocationOperation> constraints) {
        foreach (IInvocationOperation constraint in constraints) {
            string name = constraint.TargetMethod.Name;
            if (name is "WithoutAlpha" or "WithoutNumeric" && CharacterFamilies.PoolFor(name.Substring("Without".Length)) is string removed) {
                yield return ($"{name}()", removed);
            } else if (name == "WithoutChars" && constraint.Arguments.Length == 1 && ConstantFacts.TryGetString(constraint.Arguments[0].Value, out string chars)) {
                // A list the compiler cannot fold is left out, which can only leave the filler wider than the run time
                // draws it: the rule under-reports rather than claiming an emptied pool it cannot see.
                yield return ($"WithoutChars(\"{chars}\")", chars);
            }
        }
    }

    private static string Filler(string offered, IEnumerable<(string Rendered, string Removed)> subtractions) {
        return new string(offered.Where(character => subtractions.All(subtraction => subtraction.Removed.IndexOf(character) < 0)).ToArray());
    }

    /// <summary>
    ///     Reports two declared lengths no value can satisfy together: a minimum above a maximum, or an exact length
    ///     outside either.
    /// </summary>
    /// <remarks>
    ///     Only the bounds a length constraint declares are weighed here. The floor of one <c>NonEmpty()</c> and
    ///     <c>NotBlank()</c> each set belongs with the anchored fragments, in <see cref="ReportLengthBudget" />, where
    ///     the sentence can say which of them demands it.
    /// </remarks>
    private static bool ReportContradictoryBounds(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints,
                                                  DeclaredLength? exact, DeclaredLength? minimum, DeclaredLength? maximum) {
        if (exact is { } fixedAt && minimum is { } atLeast && fixedAt.Length < atLeast.Length) {
            return ReportOnTheLater(context, constraints, fixedAt, atLeast, $"{atLeast.Rendered} needs at least {Characters(atLeast.Length)}, which {fixedAt.Rendered} cannot hold");
        }

        if (exact is { } fixedTo && maximum is { } atMost && fixedTo.Length > atMost.Length) {
            return ReportOnTheLater(context, constraints, fixedTo, atMost, $"{fixedTo.Rendered} needs exactly {Characters(fixedTo.Length)}, which {atMost.Rendered} cannot hold");
        }

        if (minimum is { } floor && maximum is { } ceiling && floor.Length > ceiling.Length) {
            return ReportOnTheLater(context, constraints, floor, ceiling, $"{floor.Rendered} needs at least {Characters(floor.Length)}, which {ceiling.Rendered} cannot hold");
        }

        return false;
    }

    /// <summary>
    ///     Reports on whichever of the two constraints was declared last — the one the run time is applying when it
    ///     refuses the chain, and so the one its message names first.
    /// </summary>
    private static bool ReportOnTheLater(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints, DeclaredLength first, DeclaredLength second, string reason) {
        IInvocationOperation applying = constraints[System.Math.Max(first.Position, second.Position)];

        context.ReportDiagnostic(Diagnostic.Create(Descriptors.StringConstraintsAdmitNoValue, applying.Syntax.GetLocation(), reason));

        return true;
    }

    /// <summary>
    ///     Keeps the tighter of two minimums, and the earlier of two equal ones: the run time ignores a bound no
    ///     tighter than the one in force, so the earlier is the one it names.
    /// </summary>
    private static DeclaredLength Longer(DeclaredLength? current, DeclaredLength candidate) {
        return current is { } kept && kept.Length >= candidate.Length ? kept : candidate;
    }

    /// <summary>The counterpart of <see cref="Longer" /> for a maximum.</summary>
    private static DeclaredLength Shorter(DeclaredLength? current, DeclaredLength candidate) {
        return current is { } kept && kept.Length <= candidate.Length ? kept : candidate;
    }

    /// <summary>
    ///     Reads a constant length argument. A negative one is left unread: the call throws an argument error of its
    ///     own, which is not a claim about the chain.
    /// </summary>
    private static bool TryGetLength(IInvocationOperation constraint, int ordinal, out int length) {
        length = 0;

        IArgumentOperation? argument = constraint.Arguments.FirstOrDefault(candidate => candidate.Parameter?.Ordinal == ordinal);

        return argument is not null && ConstantFacts.TryGetInt32(argument.Value, out length) && length >= 0;
    }

    /// <summary>
    ///     Reports a declared constraint that admits none of the values a constant <c>OneOf(...)</c> supplies, which
    ///     the run time refuses at declaration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Every constraint the run time applies to an allow-list is weighed — a character family, a length, an
    ///         anchored fragment, an exclusion — through the predicates JD029 reads too, so the pool this rule calls
    ///         emptied is exactly the pool that rule stands down on.
    ///     </para>
    ///     <para>
    ///         For a character constraint this is the counterpart of the exemption ADR-0079 grants an anchored
    ///         literal, and the reason the two read differently. A literal claims its own region of a shaped string
    ///         and the family claims the rest, so the two never meet; a value set claims the WHOLE string and leaves
    ///         no filler, so the family's region is that supplied value itself and the two must agree. When they
    ///         cannot, the constraint contributes nothing and the chain throws — the remedy is to drop it.
    ///     </para>
    ///     <para>
    ///         Reported only when every value the pool writes inline is refused. A pool one value survives is a
    ///         narrowing rather than a contradiction, and JD029 reports the values it removed, at the severity a
    ///         still-working chain deserves.
    ///     </para>
    /// </remarks>
    private static void ReportEmptiedValueSet(OperationAnalysisContext context, IReadOnlyList<IInvocationOperation> constraints, IInvocationOperation valueSet) {
        List<string> values = [];
        foreach (IOperation element in ValueSetFacts.Elements(valueSet)) {
            // A value the walk cannot fold leaves the pool partly unknown, and a claim about ALL of them would be a
            // guess. Under-reporting is the safe direction.
            if (!ConstantFacts.TryGetString(element, out string value)) { return; }

            values.Add(value);
        }

        if (values.Count == 0) { return; }

        List<(string Rendered, Func<string, bool> Admits)> tests = StringValueFacts.Tests(constraints);

        foreach ((string rendered, Func<string, bool> admits) in tests) {
            if (values.Any(admits)) { continue; }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.StringConstraintsAdmitNoValue, valueSet.Syntax.GetLocation(),
                $"{rendered} allows none of the values it offers"));

            return;
        }

        ReportEmptiedByTheirConjunction(context, valueSet, tests, values);
    }

    /// <summary>
    ///     Reports a pool no value survives, where no single constraint is answerable for it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A value must satisfy every declared constraint, so the pool the generator draws from is the
    ///         intersection — and an intersection can be empty while each set entering it is not.
    ///         <c>WithoutNumeric().InLowerCase().OneOf("1", "A")</c> is the shape: the first admits <c>"A"</c>, the
    ///         second admits <c>"1"</c>, and no value satisfies both, which the run time refuses at declaration.
    ///         Asked one constraint at a time — as the pass above asks — nothing is wrong with either.
    ///     </para>
    ///     <para>
    ///         Only the constraints that actually refuse a value are named. One every value passes takes no part in
    ///         emptying the pool, so naming it would send the reader to a constraint whose removal changes nothing —
    ///         the same discipline the exhaustion messages on the run-time side follow. Which of the survivors is the
    ///         <i>smallest</i> set answering for it is a set cover, and this rule does not go there (ADR-0046): every
    ///         constraint that refuses something is named, and the reader picks.
    ///     </para>
    /// </remarks>
    private static void ReportEmptiedByTheirConjunction(OperationAnalysisContext context, IInvocationOperation valueSet,
                                                        IReadOnlyList<(string Rendered, Func<string, bool> Admits)> tests, IReadOnlyList<string> values) {
        // One constraint is what the pass above already answered for, and answered better: it names what to remove.
        if (tests.Count < 2) { return; }
        if (values.Any(value => tests.All(test => test.Admits(value)))) { return; }

        List<string> culprits = tests.Where(test => !values.All(test.Admits)).Select(test => test.Rendered).ToList();

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, valueSet.Syntax.GetLocation(),
            $"{Conjoin(culprits)} together allow none of the values it offers"));
    }

    /// <summary>Renders a list of names as a reader would say it — <c>A and B</c>, <c>A, B and C</c>.</summary>
    private static string Conjoin(IReadOnlyList<string> names) {
        if (names.Count == 1) { return names[0]; }

        return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
    }

    /// <summary>
    ///     Reports anchored fragments, or the floor of one <c>NonEmpty()</c> and <c>NotBlank()</c> each set, that the
    ///     cap cannot hold. <paramref name="atTheDraw" /> adds the position <c>NotBlank()</c> is owed where no literal
    ///     carries its character — a question the run time asks only of a string it is about to draw.
    /// </summary>
    private static bool ReportLengthBudget(OperationAnalysisContext context, IInvocationOperation invocation, IReadOnlyList<IInvocationOperation> constraints,
                                           IOperation? lastAnchor, DeclaredLength? cap, bool atTheDraw) {
        if (cap is not { } capped) { return false; }

        int required = atTheDraw ? StringShapeFacts.Floor(constraints) : StringShapeFacts.DeclaredFloor(constraints);
        if (required <= capped.Length) { return false; }

        string capName = capped.Rendered;

        // The last anchored literal is what the reader is most likely to shorten; with no anchor at all there is
        // nothing in the chain more specific than the chain itself.
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.StringConstraintsAdmitNoValue, (lastAnchor ?? invocation).Syntax.GetLocation(),
            $"{Claimant(constraints, lastAnchor is not null, atTheDraw)} at least {Characters(required)}, which {capName} cannot hold"));

        return true;
    }

    /// <summary>
    ///     What the sentence names as demanding the length, and the verb it takes. A constraint owed a position of
    ///     its own is named beside the anchors rather than folded into them, because it is the one the reader can
    ///     remove.
    /// </summary>
    private static string Claimant(IReadOnlyList<IInvocationOperation> constraints, bool anchored, bool atTheDraw) {
        if (anchored) {
            bool notBlankOwnsAPosition = atTheDraw && StringShapeFacts.FillerMustCarryNonBlank(constraints);

            return notBlankOwnsAPosition ? "the anchored fragments and NotBlank() need" : "the anchored fragments need";
        }

        // With no anchor, the floor is the one character NonEmpty() or NotBlank() sets; the latter names it when declared.
        return StringShapeFacts.Declares(constraints, "NotBlank") ? "NotBlank() needs" : "NonEmpty() needs";
    }

    private static string Characters(int count) {
        return count == 1 ? "1 character" : $"{count} characters";
    }

}
