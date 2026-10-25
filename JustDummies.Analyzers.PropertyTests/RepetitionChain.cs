using System.Collections;
using System.Globalization;

using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     A generated chain for JD035, written once as C# for the analyzer and applied call by call by the run time, which
///     is the oracle: a call the run time ignores leaves every draw as it was.
/// </summary>
internal sealed class RepetitionChain {

    /// <summary>The snippet up to the chain, which starts right after it.</summary>
    private const string Prelude = "using System;\n\nusing JustDummies;\n\npublic static class Sample {\n    public static object M() {\n        return ";

    /// <summary>The seeds every prefix of the chain is drawn under.</summary>
    private const int Seeds = 3;

    /// <summary>The draws compared under each seed.</summary>
    private const int Draws = 16;

    /// <summary>What the run time says when a constraint the generator takes once is declared again differently.</summary>
    private const string AlreadyDefined = "is already defined";

    public RepetitionChain(DeclarationVocabulary vocabulary, IReadOnlyList<DeclarationCall> calls) {
        Vocabulary = vocabulary;
        Calls      = calls;
    }

    public DeclarationVocabulary Vocabulary { get; }

    public IReadOnlyList<DeclarationCall> Calls { get; }

    /// <summary>The chain as a whole expression, the shape the rule reads.</summary>
    public string Expression => Vocabulary.Factory + string.Concat(Calls.Select(call => "." + call.Source)) + ".Generate()";

    /// <summary>
    ///     The positions of the calls the run time ignores: applying one leaves every draw, under every seed, as it was
    ///     before it. The chain ends at the first call the run time refuses, since nothing after it is applied.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     The chain's constraints contradict one another, which is neither a repetition nor this rule's to judge; the
    ///     vocabularies are built to spell no such chain.
    /// </exception>
    public ISet<int> IgnoredCalls() {
        List<string> before = Observed(0);
        HashSet<int> ignored = [];
        for (int applied = 1; applied <= Calls.Count; applied++) {
            if (!IsApplied(applied)) { break; }

            List<string> after = Observed(applied);
            if (after.SequenceEqual(before, StringComparer.Ordinal)) { ignored.Add(applied - 1); }
            before = after;
        }

        return ignored;
    }

    /// <summary>
    ///     The positions of the calls <paramref name="analyzer" /> reports JD035 on, read from where each diagnostic starts.
    ///     A value listed twice in one exclusion list is reported on the value, inside a call the run time does apply; the
    ///     oracle here judges whole calls, so that diagnostic is left to the unit cases.
    /// </summary>
    public ISet<int> ReportedCalls(DiagnosticAnalyzer analyzer) {
        string source = Prelude + Expression + ";\n    }\n}\n";
        Dictionary<int, int> positions = [];
        int offset = Prelude.Length + Vocabulary.Factory.Length;
        for (int call = 0; call < Calls.Count; call++) {
            positions[offset + 1] = call;
            offset += 1 + Calls[call].Source.Length;
        }

        return new HashSet<int>(AnalyzerHarness.Diagnose([analyzer], source)
                                               .Where(diagnostic => diagnostic.Id == "JD035" && positions.ContainsKey(diagnostic.Location.SourceSpan.Start))
                                               .Select(diagnostic => positions[diagnostic.Location.SourceSpan.Start]));
    }

    /// <summary>The chains one call shorter, so FsCheck reports the smallest chain that still disagrees.</summary>
    public IEnumerable<RepetitionChain> Shrink() {
        for (int call = 0; call < Calls.Count; call++) {
            int removed = call;

            yield return new RepetitionChain(Vocabulary, Calls.Where((_, index) => index != removed).ToList());
        }
    }

    /// <inheritdoc />
    public override string ToString() {
        return Expression;
    }

    /// <summary>Whether the run time applies the first <paramref name="count" /> calls without refusing one of them.</summary>
    private bool IsApplied(int count) {
        try {
            _ = Prefix(Any.WithSeed(0), count);

            return true;
        } catch (ConflictingAnyConstraintException conflict) when (conflict.Message.Contains(AlreadyDefined, StringComparison.Ordinal)) {
            return false;
        } catch (ConflictingAnyConstraintException conflict) {
            throw new InvalidOperationException($"The vocabulary spells a chain whose constraints contradict one another: {Expression}{Environment.NewLine}{conflict.Message}", conflict);
        } catch (ArgumentException) {
            return false;
        }
    }

    private object Prefix(AnyContext context, int count) {
        return Calls.Take(count).Aggregate(Vocabulary.Create(context), (generator, call) => call.ApplyTo(generator));
    }

    /// <summary>What the first <paramref name="count" /> calls draw, seed after seed — or the refusal the draw meets.</summary>
    private List<string> Observed(int count) {
        List<string> observed = [];
        for (int seed = 0; seed < Seeds; seed++) {
            object generator = Prefix(Any.WithSeed(seed), count);
            System.Reflection.MethodInfo generate = generator.GetType().GetMethod("Generate", Type.EmptyTypes)!;
            for (int draw = 0; draw < Draws; draw++) {
                try {
                    observed.Add(Rendered(generate.Invoke(generator, null)));
                } catch (System.Reflection.TargetInvocationException refused) {
                    observed.Add($"refused: {refused.InnerException?.GetType().Name}");

                    break;
                }
            }
        }

        return observed;
    }

    private static string Rendered(object? value) {
        return value switch {
            null                    => "null",
            string text             => text,
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            IEnumerable sequence    => "[" + string.Join(", ", sequence.Cast<object?>().Select(Rendered)) + "]",
            _                       => value.ToString() ?? string.Empty
        };
    }

}
