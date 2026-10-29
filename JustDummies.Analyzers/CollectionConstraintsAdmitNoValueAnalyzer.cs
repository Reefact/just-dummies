using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace JustDummies.Analyzers;

/// <summary>
///     JD016 — reports a collection chain whose constant count constraints cannot all hold, or which asks for more
///     distinct elements than its element generator can produce. Both throw at declaration time, so this moves an
///     arrange-time red to a build-time red — worth it because the chain usually sits in a helper several call frames
///     away from the test that dies on it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionConstraintsAdmitNoValueAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    ///     How many values <c>Any.Boolean()</c> can produce. Restated here rather than read from the library: an
    ///     analyzer ships without it and reasons over symbols, so a fact about a generator's domain has to be
    ///     written down on this side too.
    /// </summary>
    private const int BooleanValueCount = 2;

    /// <summary>How many characters the unconstrained character row draws — the ASCII pool of ADR-0075.</summary>
    private const int AsciiValueCount = 128;

    /// <summary>The whole of a byte, signed or not.</summary>
    private const int ByteValueCount = 256;

    /// <summary>The whole of a 16-bit integer, signed or not.</summary>
    private const int Int16ValueCount = 65536;

    /// <summary>The distinct finite halves, the two zeros counted once because they compare equal.</summary>
    private const int HalfValueCount = 63487;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.CollectionConstraintsAdmitNoValue);

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

        if (invocation.Parent is IInvocationOperation) { return; }
        if (!AnyChainFacts.TryGetChain(invocation, symbols, out IReadOnlyList<IInvocationOperation> constraints, out IInvocationOperation? factory)) { return; }
        if (factory is null || !IsCollectionFactory(factory.TargetMethod.Name)) { return; }

        AnalyzeConstraints(context, factory, constraints, symbols);
    }

    /// <summary>
    ///     Reads the counts, the distinctness and the contained elements the chain declares, then reports the first
    ///     combination of them no collection can satisfy.
    /// </summary>
    /// <remarks>
    ///     Split from <see cref="Analyze" />, which answers a different question: whether this chain is one the rule
    ///     reasons about at all. Everything below assumes that answer is yes.
    /// </remarks>
    private static void AnalyzeConstraints(OperationAnalysisContext context, IInvocationOperation factory, IReadOnlyList<IInvocationOperation> constraints, KnownSymbols symbols) {
        int?  exact    = null;
        int?  minimum  = null;
        int?  maximum  = null;
        bool  distinct = factory.TargetMethod.Name is "SetOf" or "DictionaryOf";
        int   contained = 0;
        // Every value the chain pins, fixed or drawn: one lying outside the element domain fills its own slot.
        List<IOperation> pinned = [];
        IOperation? at  = null;

        foreach (IInvocationOperation constraint in constraints) {
            at = constraint;

            switch (constraint.TargetMethod.Name) {
                case "Empty":       exact   = 0;                                                          break;
                case "NonEmpty":    minimum = System.Math.Max(minimum ?? 0, 1);                           break;
                case "Distinct":    distinct = true;                                                      break;
                case "Containing" or "ContainingKey" or "ContainingEntry": contained++; Pin(constraint, pinned); break;
                case "ContainingAny" or "ContainingAnyKey":                               Pin(constraint, pinned); break;

                case "WithCount" when TryConstant(constraint, out int count):      exact   = count;       break;
                case "WithMinCount" when TryConstant(constraint, out int min):     minimum = System.Math.Max(minimum ?? 0, min); break;
                case "WithMaxCount" when TryConstant(constraint, out int max):     maximum = System.Math.Min(maximum ?? int.MaxValue, max); break;

                case "WithCountBetween" when constraint.Arguments.Length == 2
                                          && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out int low)
                                          && ConstantFacts.TryGetInt32(constraint.Arguments[1].Value, out int high):
                    minimum = System.Math.Max(minimum ?? 0, low);
                    maximum = System.Math.Min(maximum ?? int.MaxValue, high);

                    break;
            }
        }

        if (at is null) { return; }

        int effectiveMin = System.Math.Max(minimum ?? 0, exact ?? 0);
        int effectiveMax = System.Math.Min(maximum ?? int.MaxValue, exact ?? int.MaxValue);

        if (effectiveMin > effectiveMax) {
            Report(context, at, $"the declared counts require at least {effectiveMin} element(s) and at most {effectiveMax}");

            return;
        }

        if (contained > effectiveMax) {
            Report(context, at, $"{contained} element(s) are required to be contained, which cannot fit in a collection of at most {effectiveMax}");

            return;
        }

        // The cardinality gate (ADR-0004): a distinct collection cannot hold more elements than its element
        // generator has distinct values to give, plus the pinned values that lie outside what it gives. A pin is
        // credited unless it provably sits inside the domain, which mirrors the runtime's own count and can only
        // defer to it: a value the compiler cannot place must never make the domain look smaller than it is.
        if (!distinct) { return; }
        if (!TryGetProvableCardinality(factory, symbols, out int cardinality, out Func<IOperation, bool> mayExtend)) { return; }

        int credited      = CountCredited(pinned.Where(mayExtend));
        int fromGenerator = effectiveMin - credited;
        if (fromGenerator <= cardinality) { return; }

        string required = credited == 0
                              ? $"{effectiveMin} distinct element(s) are required"
                              : $"{fromGenerator} distinct element(s) must come from the element generator once the {credited} pinned outside its domain are counted";

        Report(context, at, $"{required}, but the element generator can produce only {cardinality}");
    }

    private static void Pin(IInvocationOperation constraint, List<IOperation> pinned) {
        if (constraint.Arguments.Length > 0) { pinned.Add(GeneratorFacts.Unwrap(constraint.Arguments[0].Value)); }
    }

    // The same constant pinned twice is one value — and a distinct collection refuses the repeat outright — so it
    // fills one slot. A pin the compiler cannot evaluate is credited on its own: nothing proves it equals another.
    private static int CountCredited(IEnumerable<IOperation> pins) {
        HashSet<object?> constants = [];

        return pins.Count(pin => !pin.ConstantValue.HasValue || constants.Add(pin.ConstantValue.Value));
    }

    private static void Report(OperationAnalysisContext context, IOperation at, string reason) {
        context.ReportDiagnostic(Diagnostic.Create(Descriptors.CollectionConstraintsAdmitNoValue, at.Syntax.GetLocation(), reason));
    }

    private static bool TryConstant(IInvocationOperation constraint, out int value) {
        value = 0;

        return constraint.Arguments.Length == 1 && ConstantFacts.TryGetInt32(constraint.Arguments[0].Value, out value);
    }

    private static bool IsCollectionFactory(string name) {
        return name is "ListOf" or "ArrayOf" or "SequenceOf" or "SetOf" or "DictionaryOf";
    }

    /// <summary>
    ///     An upper bound on the element generator's distinct domain, for the shapes the compiler can settle. Anything
    ///     else answers <c>false</c>: an unprovable domain must never be treated as a small one.
    /// </summary>
    /// <remarks>
    ///     <paramref name="mayExtend" /> answers, for a value the chain pins, whether it may lie outside that domain —
    ///     <c>true</c> unless it provably sits inside. A domain bounded by its type (a <c>bool</c>, a <c>byte</c>, a
    ///     16-bit integer, a <c>Half</c>) holds every value of the type, so nothing pinned can extend it; a pool, an
    ///     enum and the ASCII row are extended by a constant they do not hold, and by anything the compiler cannot fold.
    /// </remarks>
    private static bool TryGetProvableCardinality(IInvocationOperation factory, KnownSymbols symbols, out int cardinality, out Func<IOperation, bool> mayExtend) {
        cardinality = 0;
        mayExtend   = _ => true;

        if (factory.Arguments.Length == 0) { return false; }
        if (GeneratorFacts.Unwrap(factory.Arguments[0].Value) is not IInvocationOperation element) { return false; }

        // Walk the element chain back to its own factory, watching what the constraints along the way do to the
        // domain. AllowingCombinations() WIDENS an enum's universe to the OR-closure of its declared members — eight
        // values for four flags — so counting declared members there would under-report the domain and condemn a legal
        // chain. An unprovable domain must never be treated as a small one, so the rule stands down instead of
        // computing the closure: a deliberate false negative, not an oversight.
        //
        // A typed OneOf(...) is the opposite shape: its pool is the caller's own, so walking through it to the
        // factory beneath answers too WIDE rather than too narrow -- a set of two accented letters would read as if it
        // drew from the whole ASCII row, and Any.Int32().OneOf(1, 2) as if it drew from no provable domain at all, so
        // a count of three over either went unreported (issue #326). Counted exactly instead, the same way the
        // top-level Any.OneOf(...)/ElementOf(...) case is. Only while every hop above it stays on the same generator:
        // a call that changes the type, OrNull() or As(...), may add a value the pool does not hold, and is walked
        // through rather than trusted, and so is a OneOf declared outside JustDummies, whose meaning is its own. A
        // value the collection pins outside the pool -- the widening #188 made legal,
        // SetOf(Any.Int32().OneOf(1, 2)).WithCount(3).Containing(3) -- still reaches the count through mayExtend.
        IInvocationOperation root          = element;
        bool                 sameGenerator = true;
        while (root.Instance is not null && GeneratorFacts.Unwrap(root.Instance) is IInvocationOperation inner) {
            if (root.TargetMethod.Name == "AllowingCombinations") { return false; }

            sameGenerator &= SymbolEqualityComparer.Default.Equals(root.TargetMethod.ContainingType, element.Type);
            // A pool the compiler cannot count -- a sequence, a value it cannot fold -- is still bounded by the row
            // beneath it, so the walk goes on rather than giving up.
            if (sameGenerator && TryCountTypedPool(root, symbols, out cardinality, out mayExtend)) { return true; }

            root = inner;
        }

        // The rows below are named after JustDummies' own factories; a project's same-named method proves nothing.
        return symbols.IsJustDummiesMember(root.TargetMethod) && TryGetRowCardinality(root, out cardinality, out mayExtend);
    }

    /// <summary>The distinct domain of the JustDummies factory the element walk ended on, where the compiler settles it.</summary>
    private static bool TryGetRowCardinality(IInvocationOperation root, out int cardinality, out Func<IOperation, bool> mayExtend) {
        cardinality = 0;
        mayExtend   = _ => true;

        switch (root.TargetMethod.Name) {
            case "Boolean":
                cardinality = BooleanValueCount;
                mayExtend   = _ => false;

                return true;

            // DISTINCT constant values, never declared members: `enum Grade { Low = 1, …, Min = 1 }` declares five
            // names for three values, and counting names would bless a floor the element row can never reach.
            case "Enum" when root.TargetMethod.TypeArguments.Length == 1 && root.TargetMethod.TypeArguments[0] is INamedTypeSymbol enumType:
                HashSet<object?> declared = [.. enumType.GetMembers()
                                                        .OfType<IFieldSymbol>()
                                                        .Where(field => field.HasConstantValue)
                                                        .Select(field => field.ConstantValue)];
                cardinality = declared.Count;
                mayExtend   = pin => !IsConstantIn(pin, declared);

                return cardinality > 0;

            // The small primitive rows. Their domains are settled and reachable by an ordinary floor — the
            // unconstrained character row draws the ASCII pool of ADR-0075, not the 16 bits a `char` holds — so a
            // count above them is provably unsatisfiable rather than merely large.
            case "Char":
                cardinality = AsciiValueCount;
                mayExtend   = pin => !(pin.ConstantValue is { HasValue: true, Value: char character } && character < AsciiValueCount);

                return true;

            case "Byte" or "SByte":
                cardinality = ByteValueCount;
                mayExtend   = _ => false;

                return true;

            // The one floating-point row narrow enough to matter. `Half` holds this many distinct finite values --
            // the two zeros compare equal, so a set keeps one of them -- and the row states the count itself
            // rather than inheriting the "unbounded" answer a floating-point range otherwise gives. It is a
            // bound rather than a reachable target, which is the right side to err on: a count above it is
            // provably unsatisfiable, and one below it is left to the draw.
            case "Half":
                cardinality = HalfValueCount;
                mayExtend   = _ => false;

                return true;

            case "Int16" or "UInt16":
                cardinality = Int16ValueCount;
                mayExtend   = _ => false;

                return true;

            case "OneOf" or "ElementOf":
                return TryCountDistinctConstants(root, out cardinality, out mayExtend);

            default:
                return false;
        }
    }

    // A JustDummies OneOf(...) whose values the compiler can count.
    private static bool TryCountTypedPool(IInvocationOperation hop, KnownSymbols symbols, out int cardinality, out Func<IOperation, bool> mayExtend) {
        cardinality = 0;
        mayExtend   = _ => true;

        return hop.TargetMethod.Name == "OneOf" && symbols.IsJustDummiesMember(hop.TargetMethod) && TryCountDistinctConstants(hop, out cardinality, out mayExtend);
    }

    private static bool TryCountDistinctConstants(IInvocationOperation pool, out int cardinality, out Func<IOperation, bool> mayExtend) {
        cardinality = 0;
        mayExtend   = _ => true;

        foreach (IArgumentOperation argument in pool.Arguments) {
            if (argument.ArgumentKind != ArgumentKind.ParamArray) { continue; }
            if (argument.Value is not IArrayCreationOperation { Initializer: { } initializer }) { return false; }

            HashSet<object?> distinct = [];
            foreach (IOperation element in initializer.ElementValues) {
                Optional<object?> constant = GeneratorFacts.Unwrap(element).ConstantValue;
                if (!constant.HasValue) { return false; }

                distinct.Add(constant.Value);
            }

            cardinality = distinct.Count;
            mayExtend   = pin => !IsConstantIn(pin, distinct);

            return cardinality > 0;
        }

        return false;
    }

    private static bool IsConstantIn(IOperation pin, HashSet<object?> values) {
        return pin.ConstantValue.HasValue && values.Contains(pin.ConstantValue.Value);
    }

}
