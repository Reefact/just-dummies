#region Usings declarations

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

#endregion

namespace JustDummies.Documentation.UnitTests;

/// <summary>
///     Turns a fenced C# sample from a documentation page into a real compilation, against the three shipped packages
///     and nothing else.
/// </summary>
/// <remarks>
///     <para>
///         The references are the reader's references: the runtime, plus <c>JustDummies</c>, <c>JustDummies.Xunit</c>,
///         <c>JustDummies.DiagnosticCatalog</c> and xUnit. A sample therefore cannot compile here by leaning on
///         something only this repository has — if it binds, it binds in a consumer's test project too.
///     </para>
///     <para>
///         This assembly is referenced as well, for the one thing a page is allowed to assume: the illustrative domain
///         in <see cref="Fixtures" /> (<c>OrderReference</c>, <c>Money</c>, <c>Customer</c>, <c>OrderStatus</c>,
///         <c>Permissions</c>). Every other symbol a sample names has to come from the packages.
///     </para>
/// </remarks>
internal static class DocumentationSampleCompiler {

    private const string SamplePrelude = """
                                         #pragma warning disable CS1998
                                         using System;
                                         using System.Collections.Generic;
                                         using System.Diagnostics.CodeAnalysis;
                                         using System.Linq;
                                         using System.Text.RegularExpressions;
                                         using System.Threading.Tasks;

                                         using JustDummies;
                                         using JustDummies.Diagnostics;
                                         using JustDummies.Xunit;
                                         using JustDummies.Documentation.UnitTests.Fixtures;

                                         using Xunit;

                                         namespace JustDummies.Documentation.Samples;


                                         """;

    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    /// <summary>Compiles one sample and returns the compiler ERRORS it produced, already mapped back to the page.</summary>
    public static IReadOnlyList<string> GetErrors(DocumentationPage page, CodeFence fence) {
        CSharpCompilation compilation = Compile(page, fence, out int preludeLines);

        return [
            .. compilation.GetDiagnostics()
                          .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                          .Select(diagnostic => Describe(page, fence, diagnostic, preludeLines))
        ];
    }

    /// <summary>Builds the compilation for one sample; also used by the rule contract, which needs the compilation itself.</summary>
    public static CSharpCompilation Compile(DocumentationPage page, CodeFence fence, out int preludeLines) {
        string source = Wrap(fence, out preludeLines);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));

        return CSharpCompilation.Create(
            assemblyName: "JustDummies.Documentation.Sample",
            syntaxTrees: [syntaxTree],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
    }

    /// <summary>Renders a diagnostic as a message pointing at the page and line a maintainer can open.</summary>
    public static string Describe(DocumentationPage page, CodeFence fence, Diagnostic diagnostic, int preludeLines) {
        // The sample's first line sits one line below the opening fence, and `preludeLines` lines below the top of the
        // generated compilation unit. Mapping it back means a failure names the markdown, never the scaffolding.
        int generatedLine = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
        int pageLine      = fence.StartLine + (generatedLine - preludeLines);

        return $"{page.RelativePath}:{pageLine}: {diagnostic.Id} {diagnostic.GetMessage()}";
    }

    private static string Wrap(CodeFence fence, out int preludeLines) {
        string prelude = SamplePrelude;
        string body;

        if (fence.Mode == SampleMode.Declarations) {
            body = fence.Content;
        } else {
            prelude += """
                       internal static class DocumentationSample {

                           internal static async Task RunAsync() {

                       """;
            body = fence.Content;
        }

        preludeLines = CountLines(prelude);

        string source = prelude + body;
        if (fence.Mode != SampleMode.Declarations) {
            source += """

                          }

                      }
                      """;
        }

        return source;
    }

    private static int CountLines(string text) {
        int count = 1;
        foreach (char character in text) {
            if (character == '\n') { count++; }
        }

        return count;
    }

    private static ImmutableArray<MetadataReference> BuildReferences() {
        List<MetadataReference> references = [];

        // The running runtime, so a sample resolves System types without this suite pinning a reference pack.
        string trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;
        foreach (string path in trustedAssemblies.Split(Path.PathSeparator)) {
            if (string.IsNullOrEmpty(path)) { continue; }
            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch (Exception exception) when (exception is IOException or BadImageFormatException or ArgumentException) {
                // A native or otherwise unloadable entry in the TPA list carries no metadata; skipping it is correct.
            }
        }

        // The shipped packages, exactly as a consumer references them.
        references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Any).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Xunit.ReproducibleAttribute).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Diagnostics.JustDummiesRule).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location));

        // This assembly, for the illustrative domain the samples are allowed to name.
        references.Add(MetadataReference.CreateFromFile(typeof(Fixtures.OrderReference).Assembly.Location));

        return [.. references];
    }

}
