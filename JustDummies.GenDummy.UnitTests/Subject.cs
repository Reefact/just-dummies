using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     A compilation to scaffold against, built in memory from a snippet.
/// </summary>
/// <remarks>
///     Fast, and closer to the truth than a hand-built symbol model would be: these are the symbols the
///     developer's own compiler produces, so a row of §5.2 that only works against a mock cannot pass here.
/// </remarks>
internal static class Subject {

    /// <summary>The domain every snippet may lean on, so a snippet only shows what it is about.</summary>
    private const string Domain = """
                                  using System;
                                  using System.Collections.Generic;

                                  namespace Shop.Domain;

                                  public enum OrderStatus { Draft, Placed }

                                  public sealed class Customer {
                                      public Customer(string name) { }
                                  }
                                  """;

    // Lazily, so a suite that never asks for the downlevel asset never needs it on disk.
    private static readonly Lazy<ImmutableArray<MetadataReference>> Modern = new(() => References(downlevel: false));

    private static readonly Lazy<ImmutableArray<MetadataReference>> Downlevel = new(() => References(downlevel: true));

    /// <summary>The expression §5.2 produces for a constructor parameter of that type, or null.</summary>
    internal static string? ExpressionFor(string parameterType, bool downlevel = false) {
        ScaffoldOutcome outcome = Scaffold($$"""
                                            public sealed class Subject {
                                                public Subject({{parameterType}} value) { }
                                            }
                                            """,
                                           downlevel: downlevel);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters.Single().Expression;
    }

    /// <summary>
    ///     The single parameter of a <c>Subject</c> whose constructor carries <paramref name="body" />.
    /// </summary>
    /// <remarks>
    ///     The guard is written as a developer would write it, inside a real constructor with a real assignment
    ///     after it, so the reading is exercised against the syntax and the semantic model rather than against a
    ///     shape invented here.
    /// </remarks>
    internal static ScaffoldedParameter GuardedBy(string parameterType, string body) {
        ScaffoldOutcome outcome = Scaffold($$"""
                                            public sealed class Subject {

                                                private readonly {{parameterType}} kept;

                                                public Subject({{parameterType}} value) {
                                            {{body}}
                                                    kept = value;
                                                }

                                            }
                                            """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters.Single();
    }

    /// <summary>
    ///     Scaffolds by the name a developer would type, over the sources given (§3.2).
    /// </summary>
    internal static ScaffoldOutcome ScaffoldByName(string typeArgument, params string[] sources) {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Subject",
            [.. sources.Select(Parse)],
            Referenced(downlevel: false, withLibrary: true),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                         nullableContextOptions: NullableContextOptions.Enable));

        return Scaffolder.Scaffold(compilation, typeArgument, ScaffoldOptions.Default);
    }

    /// <summary>
    ///     Scaffolds by name, over sources compiled here and a type that lives in a <b>referenced</b> assembly.
    /// </summary>
    /// <remarks>
    ///     Which is where the target type usually is: the tool is run from the test project (§3.1), so the type
    ///     it is pointed at reaches the compilation through a reference and is not in its source at all.
    /// </remarks>
    internal static ScaffoldOutcome ScaffoldByNameReferencing(string referencedSource,
                                                              string typeArgument,
                                                              params string[] sources) {
        return ScaffoldReferencing(referencedSource, Referenced(downlevel: false, withLibrary: true),
                                   typeArgument, sources);
    }

    /// <summary>
    ///     The same, with the referenced project bound to a reference set of its <b>own</b> — which is what a
    ///     project on another target framework is.
    /// </summary>
    /// <remarks>
    ///     A library on <c>netstandard2.0</c> under a test project on <c>net8.0</c> is the ordinary shape of a
    ///     .NET repository, this one included, and it is not the same case as the one above: two compilations
    ///     that bind the same references share their symbols outright, and Roslyn hands the referencing one a
    ///     <b>view</b> of the referenced assembly the moment they do not — a view whose parameters are not the
    ///     ones a semantic model over the declaring compilation returns. A reading that mixes the two then
    ///     recognises none of them.
    ///     <para>
    ///         The narrower set below stands in for the framework difference rather than reproducing it: what
    ///         produces the view is that the two sets differ at all, so binding the referenced source against
    ///         the corlib alone is the same condition, without this suite resolving a reference pack.
    ///     </para>
    /// </remarks>
    internal static ScaffoldOutcome ScaffoldByNameReferencingAcrossFrameworks(string referencedSource,
                                                                             string typeArgument,
                                                                             params string[] sources) {
        return ScaffoldReferencing(referencedSource, ItsOwnReferenceSet(), typeArgument, sources);
    }

    private static ScaffoldOutcome ScaffoldReferencing(string referencedSource,
                                                       ImmutableArray<MetadataReference> referencedAgainst,
                                                       string typeArgument,
                                                       IEnumerable<string> sources) {
        CSharpCompilation referenced = CSharpCompilation.Create(
            "Referenced",
            [Parse(referencedSource)],
            referencedAgainst,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                         nullableContextOptions: NullableContextOptions.Enable));

        Check.WithCustomMessage("The referenced source does not compile on its own.")
             .That(referenced.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                             .Select(diagnostic => diagnostic.GetMessage()))
             .IsEmpty();

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Subject",
            [.. sources.Select(Parse)],
            [.. Referenced(downlevel: false, withLibrary: true), referenced.ToMetadataReference()],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                         nullableContextOptions: NullableContextOptions.Enable));

        return Scaffolder.Scaffold(compilation, typeArgument, ScaffoldOptions.Default);
    }

    /// <summary>
    ///     Enough to bind a domain type, and not what the analysed compilation binds — which is the whole of
    ///     the condition, whichever entries the two sets happen to differ by.
    /// </summary>
    private static ImmutableArray<MetadataReference> ItsOwnReferenceSet() {
        List<MetadataReference> references = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];

        string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

        foreach (string path in trusted.Split(Path.PathSeparator)) {
            if (Path.GetFileName(path) is "System.Runtime.dll" or "netstandard.dll") {
                references.Add(MetadataReference.CreateFromFile(path));
            }
        }

        return [.. references];
    }

    /// <summary>Scaffolds <c>Shop.Domain.Subject</c>, declared by <paramref name="declarations" />.</summary>
    internal static ScaffoldOutcome Scaffold(string declarations,
                                             ScaffoldOptions? options = null,
                                             string metadataName = "Shop.Domain.Subject",
                                             bool downlevel = false,
                                             bool withLibrary = true) {
        ImmutableArray<MetadataReference> references = Referenced(downlevel, withLibrary);

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Subject",
            [Parse(Domain), Parse(declarations)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                         nullableContextOptions: NullableContextOptions.Enable));

        INamedTypeSymbol? target = compilation.GetTypeByMetadataName(metadataName);

        Check.WithCustomMessage($"The snippet does not declare {metadataName}.").That(target).IsNotNull();

        return Scaffolder.Scaffold(compilation, target!, options ?? ScaffoldOptions.Default);
    }

    /// <summary>Which asset the snippet compiles against — the whole point of the downlevel case.</summary>
    private static ImmutableArray<MetadataReference> Referenced(bool downlevel, bool withLibrary) {
        if (!withLibrary) { return References(downlevel: false, withLibrary: false); }

        return downlevel ? Downlevel.Value : Modern.Value;
    }

    /// <summary>
    ///     Parses a snippet, giving it <c>Shop.Domain</c> and the usual usings unless it opens its own.
    /// </summary>
    /// <remarks>
    ///     A snippet that declares a namespace — or deliberately declares none, to sit in the global one — is
    ///     left exactly as written: those two are what the namespace-form cases of §4.4 are about, and wrapping
    ///     them would test the wrapper.
    /// </remarks>
    private static SyntaxTree Parse(string source) {
        bool declaresItsOwn = source.StartsWith("using", StringComparison.Ordinal)
                           || source.StartsWith("namespace", StringComparison.Ordinal)
                           || source.StartsWith("//", StringComparison.Ordinal);

        return CSharpSyntaxTree.ParseText(declaresItsOwn
                                              ? source
                                              : "namespace Shop.Domain;\n\nusing System;\nusing System.Collections.Generic;\n\n" + source,
                                          new CSharpParseOptions(LanguageVersion.Latest));
    }

    private static ImmutableArray<MetadataReference> References(bool downlevel, bool withLibrary = true) {
        List<MetadataReference> references = [];

        string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

        foreach (string path in trusted.Split(Path.PathSeparator)) {
            if (string.IsNullOrEmpty(path)) { continue; }

            // The suite's own output carries JustDummies.dll, and the runtime lists it here. Left in, a
            // compilation built WITHOUT the library would still see it, and the refusal of §7 could never be
            // tested — nor could the downlevel asset, which would be shadowed by the modern one.
            if (Path.GetFileName(path).StartsWith("JustDummies", StringComparison.Ordinal)) { continue; }

            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch (Exception exception) when (exception is IOException or BadImageFormatException or ArgumentException) {
                // A native or otherwise unloadable entry in the TPA list carries no metadata; skipping it is correct.
            }
        }

        if (withLibrary) { references.Add(MetadataReference.CreateFromFile(downlevel ? DownlevelAsset : ModernAsset)); }

        return [.. references];
    }

    private static string ModernAsset => typeof(global::JustDummies.Dummy).Assembly.Location;

    /// <summary>
    ///     The <c>netstandard2.0</c> asset of the same build — the one a project below .NET 8 resolves.
    /// </summary>
    /// <remarks>
    ///     It sits INSIDE this project's own output, put there by the build: what lands beside the output by
    ///     itself is the net8.0 leg, copied, so there is no second asset to find there. Reading a sibling's
    ///     <c>bin</c> instead is what this replaces — a build of this project alone leaves that directory
    ///     holding whatever some earlier build put there, and a mutation run that rebuilds only the test
    ///     project empties it. It is what makes ADR-0059 checkable — the same parameter, two assets, two
    ///     answers — from state this project produced.
    /// </remarks>
    private static string DownlevelAsset {
        get {
            AssemblyMetadataAttribute relative = typeof(Subject).Assembly
                                                                .GetCustomAttributes<AssemblyMetadataAttribute>()
                                                                .Single(metadata => metadata.Key == "DownlevelLibrary");
            string path = Path.Combine(AppContext.BaseDirectory, relative.Value!);

            Check.WithCustomMessage($"The netstandard2.0 asset is missing, at {path}. "
                                  + "The build copies it there from the library's netstandard2.0 leg; a clean rebuild should restore it.")
                 .That(File.Exists(path)).IsTrue();

            return path;
        }
    }

}
