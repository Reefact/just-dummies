using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     The whole loop, once: a real type in, a file out, and that file compiled against the real library with
///     the real rules running on it.
/// </summary>
/// <remarks>
///     Every other suite here checks one half. The base table proves an expression is the right string; the
///     golden files prove the emitter lays a plan out correctly. Neither would notice a row whose string is
///     right and whose <b>type</b> is wrong — a collection assigned to an interface field that covariance does
///     not in fact reach, a nullable hop written the wrong way round. Only the compiler notices that, which is
///     why the loop is closed here rather than trusted.
/// </remarks>
public sealed class ResolvedCodeCompilesTests {

    /// <summary>
    ///     A type carrying one parameter from most rows of §5.2 at once — the shape the tool exists for.
    /// </summary>
    private const string Domain = """
                                  using System;
                                  using System.Collections.Generic;

                                  namespace Shop.Fulfilment;

                                  public enum Fulfilment { Pending, Shipped }

                                  public sealed class Order {

                                      public Order(Guid id,
                                                   string reference,
                                                   int quantity,
                                                   decimal total,
                                                   Fulfilment status,
                                                   IReadOnlyList<string> tags,
                                                   Dictionary<string, int> lines,
                                                   int[] revisions,
                                                   Uri source,
                                                   DateTime placedAt,
                                                   TimeSpan window,
                                                   int? priority,
                                                   string? note) { }

                                  }
                                  """;

    [Fact(DisplayName = "A resolved file compiles against the library, with no error.")]
    public void AResolvedFileCompiles() {
        CSharpCompilation compilation = EmittedCodeCompiler.CompileWith(Emitted(), Domain);

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();
    }

    // Warnings only, for the reason EmittedCodeCompilesTests spells out: the scaffolder hands the file to the
    // developer (ADR-0056), so an informational rule pointing at what is still undeclared is that rule working,
    // not a defect in what was emitted.
    [Fact(DisplayName = "A resolved file raises no warning from the library's own rules.")]
    public async Task AResolvedFileRaisesNoWarningFromTheLibrarysRules() {
        CSharpCompilation          compilation = EmittedCodeCompiler.CompileWith(Emitted(), Domain);
        ImmutableArray<Diagnostic> raised      = await compilation.WithAnalyzers(EmittedCodeCompiler.Analyzers)
                                                                  .GetAnalyzerDiagnosticsAsync(TestContext.Current.CancellationToken);

        string[] justDummies = raised.Where(diagnostic => diagnostic.Id.StartsWith("JD", StringComparison.Ordinal))
                                     .Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)
                                     .Select(diagnostic => diagnostic.Id + " at " + diagnostic.Location.GetLineSpan())
                                     .ToArray();

        Check.That(justDummies).IsEmpty();
    }

    // Thirteen parameters, none of them open: if a row ever stops resolving, this says so before the compiler
    // does, and names the parameter rather than a line number.
    [Fact(DisplayName = "Every parameter of that type is resolved.")]
    public void EveryParameterIsResolved() {
        ScaffoldOutcome outcome = Scaffold();

        string[] open = outcome.Plan!.Parameters
                                     .Where(parameter => parameter.IsUnresolved)
                                     .Select(parameter => parameter.Name)
                                     .ToArray();

        Check.That(open).IsEmpty();
        Check.That(outcome.Plan.Parameters.Count).IsEqualTo(13);
    }

    private static string Emitted() {
        return Scaffold().File!.SourceText;
    }

    private static ScaffoldOutcome Scaffold() {
        ScaffoldOutcome outcome = Subject.Scaffold(Domain, metadataName: "Shop.Fulfilment.Order");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome;
    }

}
