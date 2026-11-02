using System.Reflection;

using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generated chain of constraints on one of the exact-type generators JD023 and JD038 read, written once as C#
///     for the analyzer and executed once by the run time, which is the oracle: whether it refuses the chain, and
///     when it does not, how many values the generator can produce.
/// </summary>
internal sealed class ScalarChain {

    /// <summary>What the run time says when a declaration another rule owns is declared again: JD034's refusal, not JD023's.</summary>
    private const string AlreadyDefined = "is already defined";

    private readonly DeclarationChain _chain;

    public ScalarChain(DeclarationVocabulary vocabulary, IReadOnlyList<DeclarationCall> calls) {
        _chain = new DeclarationChain(vocabulary, calls);
    }

    /// <summary>
    ///     The run time's verdict on the chain: refused as admitting no value, which JD023 claims; refused on an
    ///     argument or as already defined, which JD014 and JD034 own and JD023 leaves alone; or built, with the
    ///     number of distinct values it can produce where the generator can count them.
    /// </summary>
    public ScalarOutcome RunTime() {
        object generator = _chain.Vocabulary.Create(Any.WithSeed(0));
        try {
            foreach (DeclarationCall call in _chain.Calls) {
                generator = call.ApplyTo(generator);
            }

            return new ScalarOutcome(ScalarVerdict.Built, Cardinality(generator));
        } catch (ConflictingAnyConstraintException conflict) when (conflict.Message.Contains(AlreadyDefined, StringComparison.Ordinal)) {
            return new ScalarOutcome(ScalarVerdict.OwnedByAnotherRule, null);
        } catch (ConflictingAnyConstraintException) {
            return new ScalarOutcome(ScalarVerdict.AdmitsNoValue, null);
        } catch (ArgumentException) {
            return new ScalarOutcome(ScalarVerdict.OwnedByAnotherRule, null);
        }
    }

    /// <summary>The ids <paramref name="analyzers" /> report anywhere on the chain.</summary>
    public ISet<string> ReportedBy(params DiagnosticAnalyzer[] analyzers) {
        return _chain.ReportedBy(analyzers);
    }

    /// <summary>The chains one call shorter, so FsCheck reports the smallest chain that still disagrees.</summary>
    public IEnumerable<ScalarChain> Shrink() {
        return _chain.Shrink().Select(shorter => new ScalarChain(shorter.Vocabulary, shorter.Calls));
    }

    /// <inheritdoc />
    public override string ToString() {
        return _chain.ToString();
    }

    /// <summary>
    ///     The generator's own count of its distinct values, read through the cardinality hint every exact-type
    ///     generator implements. The hint is internal to the library, so it is reached by name.
    /// </summary>
    private static long? Cardinality(object generator) {
        Type? hint = generator.GetType().GetInterfaces().FirstOrDefault(implemented => implemented.Name == "ICardinalityHint`1");

        return hint?.GetProperty("DistinctCardinality", BindingFlags.Public | BindingFlags.Instance)?.GetValue(generator) as long?;
    }

}
