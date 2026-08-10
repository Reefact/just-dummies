using System;

using Microsoft.CodeAnalysis;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The engine's entry point: what it produces, and what it refuses to produce.
/// </summary>
public sealed class ScaffolderTests {

    /// <summary>
    ///     Without the library there is not one expression the engine could write, so it says so instead of
    ///     emitting a file of TODOs that would read as "your types are unusual".
    /// </summary>
    [Fact(DisplayName = "A project that does not reference JustDummies is refused, and says why.")]
    public void AProjectWithoutTheLibraryIsRefused() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(int one) { } }",
                                                   withLibrary: false);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.LibraryNotReferenced);
        Check.That(outcome.File).IsNull();
        Check.That(outcome.Plan).IsNull();
    }

    [Fact(DisplayName = "A scaffold produces the file, named after the generator.")]
    public void AScaffoldProducesTheFile() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(int one) { } }");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.File!.FileName).IsEqualTo("AnySubject.cs");
        Check.That(outcome.File.SourceText).Contains("public sealed partial class AnySubject : IAny<Subject> {");
        Check.That(outcome.File.ContainsTodo).IsFalse();
    }

    // An open parameter is a success (§7): the file is written, and the developer's own build reports the rest.
    [Fact(DisplayName = "A parameter the table cannot resolve is still a scaffold.")]
    public void AnOpenParameterIsStillAScaffold() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(Customer one) { } }");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.File!.ContainsTodo).IsTrue();
        Check.That(outcome.File.SourceText).Contains("TODO_supply_a_generator_for_one");
    }

    [Fact(DisplayName = "The file opens the namespaces its short names lean on, and no others.")]
    public void TheFileOpensTheNamespacesItsNamesLeanOn() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(IReadOnlyList<string> tags, DateTime at) { }
                                                   }
                                                   """);

        Check.That(outcome.Plan!.Usings).IsEquivalentTo("JustDummies", "System", "System.Collections.Generic");
    }

    // A type spelled as a keyword names no namespace at the point of use, so an int parameter alone must not
    // drag `using System;` into a file that has no other reason for it.
    [Fact(DisplayName = "A parameter the compiler spells as a keyword opens nothing.")]
    public void AParameterSpelledAsAKeywordOpensNothing() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(int one, string two) { } }");

        Check.That(outcome.Plan!.Usings).IsEquivalentTo("JustDummies");
    }

    // The target's own namespace costs no using — until the file is emitted somewhere else, and then it does.
    [Fact(DisplayName = "A namespace override opens the target's own namespace.")]
    public void ANamespaceOverrideOpensTheTargetsNamespace() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(Customer one) { } }",
                                                   ScaffoldOptions.Default.InNamespace("Shop.Tests"));

        Check.That(outcome.Plan!.Target.Namespace).IsEqualTo("Shop.Tests");
        Check.That(outcome.Plan.Usings).IsEquivalentTo("JustDummies", "Shop.Domain");
        Check.That(outcome.File!.SourceText).Contains("namespace Shop.Tests;");
    }

    /// <summary>
    ///     §4.4: the namespace form is copied from the target type's own file, so the scaffolded file looks
    ///     like its neighbours — and so a project below C# 10 never meets a file-scoped namespace it cannot
    ///     compile.
    /// </summary>
    [Fact(DisplayName = "A block namespace in the target's file is copied into the emitted one.")]
    public void ABlockNamespaceIsCopied() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Legacy {
                                                       public sealed class Subject {
                                                           public Subject(int one) { }
                                                       }
                                                   }
                                                   """,
                                                   metadataName: "Shop.Legacy.Subject");

        Check.That(outcome.Plan!.Target.Style).IsEqualTo(NamespaceStyle.Block);
        Check.That(outcome.File!.SourceText).Contains("namespace Shop.Legacy {");
    }

    [Fact(DisplayName = "A file-scoped namespace in the target's file is copied into the emitted one.")]
    public void AFileScopedNamespaceIsCopied() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject(int one) { } }");

        Check.That(outcome.Plan!.Target.Style).IsEqualTo(NamespaceStyle.FileScoped);
        Check.That(outcome.File!.SourceText).Contains("namespace Shop.Domain;");
    }

    [Fact(DisplayName = "A target in the global namespace emits no namespace at all.")]
    public void ATargetInTheGlobalNamespaceEmitsNone() {
        // The leading comment is how a snippet says "leave me where I am": every other one is given
        // Shop.Domain, and this case is about having no namespace at all.
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   // Declared in the global namespace, on purpose.
                                                   public sealed class Subject {
                                                       public Subject(int one) { }
                                                   }
                                                   """,
                                                   metadataName: "Subject");

        Check.That(outcome.Plan!.Target.Style).IsEqualTo(NamespaceStyle.None);
        Check.That(outcome.File!.SourceText).Not.Contains("namespace ");
    }

    [Fact(DisplayName = "A nested type scaffolds a top-level generator named after itself alone.")]
    public void ANestedTypeScaffoldsATopLevelGenerator() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject() { }

                                                       public sealed class Line {
                                                           public Line(int quantity) { }
                                                       }
                                                   }
                                                   """,
                                                   metadataName: "Shop.Domain.Subject+Line");

        Check.That(outcome.File!.FileName).IsEqualTo("AnyLine.cs");
        Check.That(outcome.File.SourceText).Contains("public sealed partial class AnyLine : IAny<Subject.Line> {");
    }

    [Fact(DisplayName = "Every argument is required, on both overloads.")]
    public void EveryArgumentIsRequired() {
        Check.ThatCode(() => Scaffolder.Scaffold(null!, (INamedTypeSymbol)null!, null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Scaffolder.Scaffold(null!, (string)null!, null!)).Throws<ArgumentNullException>();
    }

}
