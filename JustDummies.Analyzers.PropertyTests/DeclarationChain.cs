using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generated chain of declarations on any generator but <c>Any.String()</c>, written once as C# for the analyzer
///     and executed once by the run time, which is the oracle.
/// </summary>
internal sealed class DeclarationChain {

    /// <summary>What the run time says when a constraint the generator takes once is declared again differently.</summary>
    private const string AlreadyDefined = "is already defined";

    public DeclarationChain(DeclarationVocabulary vocabulary, IReadOnlyList<DeclarationCall> calls) {
        Vocabulary = vocabulary;
        Calls      = calls;
    }

    public DeclarationVocabulary Vocabulary { get; }

    public IReadOnlyList<DeclarationCall> Calls { get; }

    /// <summary>The chain as a whole expression, the shape the rule reads: one expression, every declaration in it.</summary>
    public string Expression => Vocabulary.Factory + string.Concat(Calls.Select(call => "." + call.Source)) + ".Generate()";

    /// <summary>
    ///     Whether the first call the run time refuses is refused as already defined. A call refused on its own argument
    ///     stops the chain just as surely, and nothing after it is applied, so a declaration it hides is not refused.
    ///     Nothing is drawn: the refusal JD034 claims is the one thrown at the call.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     The chain's constraints contradict one another. That is not this rule's to report, and the vocabulary is built
    ///     to spell no such chain, so it fails loudly rather than pass as a chain the rule rightly leaves alone.
    /// </exception>
    public bool RuntimeRefusesAsAlreadyDefined() {
        object generator = Vocabulary.Create(Any.WithSeed(0));
        try {
            foreach (DeclarationCall call in Calls) {
                generator = call.ApplyTo(generator);
            }

            return false;
        } catch (ConflictingAnyConstraintException conflict) when (conflict.Message.Contains(AlreadyDefined, StringComparison.Ordinal)) {
            return true;
        } catch (ConflictingAnyConstraintException conflict) {
            throw new InvalidOperationException($"The vocabulary spells a chain whose constraints contradict one another: {Expression}{Environment.NewLine}{conflict.Message}", conflict);
        } catch (ArgumentException) {
            return false;
        }
    }

    /// <summary>The ids <paramref name="analyzers" /> report anywhere on the chain.</summary>
    public ISet<string> ReportedBy(params DiagnosticAnalyzer[] analyzers) {
        string source = $$"""
            using System;

            using JustDummies;

            public static class Sample {
                public static object M() {
                    return {{Expression}};
                }
            }
            """;

        return new HashSet<string>(AnalyzerHarness.Diagnose([.. analyzers], source).Select(diagnostic => diagnostic.Id), StringComparer.Ordinal);
    }

    /// <summary>The chains one call shorter, so FsCheck reports the smallest chain that still disagrees.</summary>
    public IEnumerable<DeclarationChain> Shrink() {
        for (int call = 0; call < Calls.Count; call++) {
            int removed = call;

            yield return new DeclarationChain(Vocabulary, Calls.Where((_, index) => index != removed).ToList());
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return Expression;
    }

}
