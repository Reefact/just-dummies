using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using JustDummies.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Compiles an emitted file the way the developer's own project will, with the library referenced and the
///     JustDummies analyzers wired in.
/// </summary>
/// <remarks>
///     Wiring the analyzers is the point, and it is what ADR-0058 buys: the scaffolded file is deliberately not
///     marked as generated code, so the rules run on it in the developer's build. If the emitter ever wrote a
///     chain the library itself warns about, the developer would meet that warning in their own error list —
///     which is why this suite meets it first.
/// </remarks>
internal static class EmittedCodeCompiler {

    /// <summary>
    ///     The types the approved files name. A minimum domain: enough to bind, never enough to distract.
    /// </summary>
    private const string Domain = """
                                  namespace Shop.Domain {

                                      public sealed class Order {
                                          public Order(OrderReference reference, Customer customer, int quantity,
                                                       OrderStatus status, System.Collections.Generic.IReadOnlyList<string> tags,
                                                       System.DateTime placedAt) { }
                                      }

                                      public sealed class Customer {
                                          public Customer(string name) { }
                                      }

                                      public sealed class OrderReference {
                                          public static OrderReference Create(string value) { return new OrderReference(); }
                                      }

                                      public enum OrderStatus { Draft, Placed }

                                      public sealed class Money {
                                          public Money(decimal amount) { }
                                      }

                                      public sealed record Address(string Street, string City);

                                      public sealed class Email {
                                          private Email() { }
                                          public static Email Create(string value) { return new Email(); }
                                      }

                                      // Already scaffolded, which is why AnyOrder composes them rather than leaving them open.
                                      public sealed class AnyCustomer : JustDummies.IAny<Customer> {
                                          public Customer Generate() { return new Customer("name"); }
                                      }

                                      public sealed class AnyOrderReference : JustDummies.IAny<OrderReference> {
                                          public OrderReference Generate() { return OrderReference.Create("R-1"); }
                                      }

                                  }

                                  namespace Shop.Legacy {

                                      // The name that shadows JustDummies.AnyPattern once a generator is scaffolded for it.
                                      public sealed class Pattern {
                                          public Pattern(string text) { }
                                      }

                                  }

                                  public sealed class Session {
                                      public Session() { }
                                  }
                                  """;

    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    /// <summary>Every JustDummies analyzer, instantiated, exactly as a consumer's build loads them.</summary>
    internal static ImmutableArray<DiagnosticAnalyzer> Analyzers { get; } = LoadAnalyzers();

    /// <summary>
    ///     The informational rules the engine stands behind on its own output.
    /// </summary>
    /// <remarks>
    ///     One entry, and it earns its place: <c>JD030</c> reports a string whose length nobody has declared,
    ///     and nobody has — the domain says nothing about it and the engine will not invent a bound to quieten
    ///     a rule. Every other <c>Info</c> reports something the engine chose, and a choice it cannot defend is
    ///     one it should not have made: <c>JD031</c> means it wrote two bounds where it meant a range, and
    ///     <c>JD024</c> means it wrote a constraint that narrows nothing.
    ///     <para>
    ///         Declared beside the analyzers rather than inside one suite because two benches now hold output
    ///         to it — the named corpus and the generative sweep — and a rule copied is a rule that drifts.
    ///     </para>
    /// </remarks>
    internal static ImmutableHashSet<string> Assumed { get; } = ImmutableHashSet.Create("JD030");

    /// <summary>Compiles <paramref name="emitted" /> together with the domain it names.</summary>
    internal static CSharpCompilation Compile(string emitted) {
        return CompileWith(emitted, Domain);
    }

    /// <summary>
    ///     Compiles several emitted files together, against the fixture domain.
    /// </summary>
    /// <remarks>
    ///     What an entry point needs (§4.5): it names the generator it reaches, so it has no meaning apart from
    ///     it, and the pair is what lands in the developer's project.
    /// </remarks>
    internal static CSharpCompilation CompileTogether(params string[] emitted) {
        return CSharpCompilation.Create(
            assemblyName: "JustDummies.GenAny.Emitted",
            syntaxTrees: [.. emitted.Select(Parse), Parse(Domain)],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    ///     Compiles <paramref name="emitted" /> against <paramref name="domain" /> rather than the fixture one.
    /// </summary>
    /// <remarks>
    ///     What the resolution tests need: their domain is the snippet they scaffolded from, so the file under
    ///     compilation is one the engine produced end to end rather than one written by hand.
    /// </remarks>
    internal static CSharpCompilation CompileWith(string emitted, string domain) {
        return CompileAllWith([emitted], domain);
    }

    /// <summary>
    ///     Compiles several emitted files against <paramref name="domain" /> rather than the fixture one.
    /// </summary>
    /// <remarks>
    ///     What the generative sweep needs: a composed parameter draws through the generator its own type owns
    ///     (ADR-0089), so a scaffolded file routinely names one or more others, and what lands in a
    ///     developer's project is the set rather than the file.
    /// </remarks>
    internal static CSharpCompilation CompileAllWith(IEnumerable<string> emitted, string domain) {
        return CSharpCompilation.Create(
            assemblyName: "JustDummies.GenAny.Emitted",
            syntaxTrees: [.. emitted.Select(Parse), Parse(domain)],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>The errors that compilation produced, rendered for a reader.</summary>
    internal static IReadOnlyList<string> ErrorsIn(CSharpCompilation compilation) {
        return compilation.GetDiagnostics()
                          .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                          .Select(diagnostic => diagnostic.Id + ": " + diagnostic.GetMessage())
                          .ToArray();
    }

    private static SyntaxTree Parse(string source) {
        return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
    }

    private static ImmutableArray<DiagnosticAnalyzer> LoadAnalyzers() {
        DiagnosticAnalyzer[] analyzers = typeof(AsyncBodyPassedToReproduciblyAnalyzer)
                                        .Assembly
                                        .GetTypes()
                                        .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                                        .OrderBy(type => type.Name, StringComparer.Ordinal)
                                        .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
                                        .ToArray();

        return [.. analyzers];
    }

    private static ImmutableArray<MetadataReference> BuildReferences() {
        List<MetadataReference> references = [];

        // The running runtime, so the emitted file resolves System types without this suite pinning a
        // reference pack of its own.
        string trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

        foreach (string path in trusted.Split(Path.PathSeparator)) {
            if (string.IsNullOrEmpty(path)) { continue; }

            try {
                references.Add(MetadataReference.CreateFromFile(path));
            } catch (Exception exception) when (exception is IOException or BadImageFormatException or ArgumentException) {
                // A native or otherwise unloadable entry in the TPA list carries no metadata; skipping it is correct.
            }
        }

        // The library, exactly as the developer's test project references it — and the only JustDummies
        // assembly here, since the emitted code names no other.
        references.Add(MetadataReference.CreateFromFile(typeof(global::JustDummies.Any).Assembly.Location));

        return [.. references];
    }

}
