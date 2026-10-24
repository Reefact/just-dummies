using System.Collections.Immutable;

using JustDummies;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.Analyzers.PropertyTests;

/// <summary>
///     Compiles a generated snippet against the running runtime and the JustDummies core — the same assembly the
///     oracle executes — and returns what one analyzer reports on it.
/// </summary>
internal static class AnalyzerHarness {

    /// <summary>The diagnostic Roslyn raises in place of an analyzer that threw.</summary>
    private const string AnalyzerFailure = "AD0001";

    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    #region Statics members declarations

    public static ImmutableArray<Diagnostic> Diagnose(DiagnosticAnalyzer analyzer, string source) {
        SyntaxTree         syntaxTree  = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        CSharpCompilation compilation = CSharpCompilation.Create("JustDummies.Analyzers.PropertyTests.Snippet", [syntaxTree], References,
                                                                  new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // A snippet that does not compile binds no operations, so the rule stands down and its silence would read as
        // agreement with an oracle that refused the chain. Fail loudly instead.
        ImmutableArray<Diagnostic> errors = [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
        if (errors.Length > 0) {
            throw new InvalidOperationException($"The generated snippet does not compile:{Environment.NewLine}{source}{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        // An analyzer that throws is reported as AD0001 in place of its own diagnostics, so its silence would read as
        // agreement too. Fail loudly on that as well.
        ImmutableArray<Diagnostic> diagnostics = compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        if (diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == AnalyzerFailure) is Diagnostic failure) {
            throw new InvalidOperationException($"The analyzer failed on the generated snippet:{Environment.NewLine}{source}{Environment.NewLine}{failure}");
        }

        return diagnostics;
    }

    private static ImmutableArray<MetadataReference> BuildReferences() {
        List<MetadataReference> references = [];

        string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator).Where(path => path.Length > 0)) {
            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch (IOException) {
                // A native or otherwise unloadable entry in the platform list carries no metadata to reference.
            } catch (BadImageFormatException) {
                // Same.
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(Any).Assembly.Location));

        return [.. references];
    }

    #endregion

}
