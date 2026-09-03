using NFluent;

namespace JustDummies.GenDummy.UnitTests;

/// <summary>
///     How a type the base table has no row for is drawn anyway (§5.4).
/// </summary>
/// <remarks>
///     One rule, since ADR-0089: through the generator that type owns, named as a blind code-generation
///     convention — <c>new DummyX()</c> — never as a lookup. Composition never asks the compilation whether a
///     type of that name exists, qualifies, or is ambiguous; whether the call resolves is the developer's own
///     compiler's verdict. What the type's own factories look like, or what answers to the same name and
///     whether it could serve, never reaches this decision.
/// </remarks>
public sealed class CompositionTests {

    /// <summary>
    ///     The call is written the same way whether a real, valid generator already answers to the name, a
    ///     same-named type exists but could never serve as one, several same-named types tie, or nothing
    ///     answers to the name at all. Composition looks at none of it.
    /// </summary>
    [Theory(DisplayName = "The call is written blind, regardless of what answers to the name.")]
    [InlineData("")]
    [InlineData("public sealed class DummyEmail : IDummy<Email> { public Email Generate() { return new Email(); } }")]
    [InlineData("public static class DummyEmail { public static Email Generate() { return new Email(); } }")]
    [InlineData("public abstract class DummyEmail : IDummy<Email> { public Email Generate() { return new Email(); } }")]
    [InlineData("""
                public sealed class DummyEmail : IDummy<Email> {
                    public DummyEmail(int seed) { }
                    public Email Generate() { return new Email(); }
                }
                """)]
    [InlineData("""
                public sealed class DummyEmail : IDummy<Email> {
                    public DummyEmail(int seed = 0) { }
                    public DummyEmail(string seed = "") { }
                    public Email Generate() { return new Email(); }
                }
                """)]
    public void TheCallIsWrittenBlindRegardlessOfWhatAnswersToTheName(string anyEmail) {
        ScaffoldedParameter parameter = Composed($$"""
                                                  public sealed class Email { public Email() { } }

                                                  {{anyEmail}}
                                                  """,
                                                  "Email");

        Check.That(parameter.IsUnresolved).IsFalse();
        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Scaffolded)).IsTrue();
    }

    /// <summary>
    ///     The generator is named whether or not it exists yet, which is the whole of ADR-0089.
    /// </summary>
    /// <remarks>
    ///     <c>CS0246</c> at that line is not a failure to resolve the parameter — it is the resolution, carried
    ///     to the one place the developer cannot miss it, naming the type to scaffold. That is ADR-0060's
    ///     mechanism, spelled as a type name rather than as an invented identifier, so the parameter is
    ///     <b>not</b> open: nothing about it is left for §5.5 to answer.
    /// </remarks>
    [Fact(DisplayName = "A type with no generator yet is named anyway, for the compiler to report.")]
    public void ATypeWithNoGeneratorYetIsNamedAnyway() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Create(string value) { return new Email(); }
                                                 }
                                                 """,
                                                 "Email");

        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Scaffolded)).IsTrue();
        Check.That(parameter.IsUnresolved).IsFalse();
    }

    /// <summary>
    ///     The shape of the type's own members stopped being a question here.
    /// </summary>
    /// <remarks>
    ///     Every row below used to route somewhere different — recognised by name, <c>Create</c> winning a tie,
    ///     several qualifying and the parameter left open, a factory taking its own type. They now share one
    ///     answer, and the ones that used to be refusals are the point: a type whose factories the engine could
    ///     not choose between was left open, and the developer met a sentinel instead of the generator they
    ///     were going to have to write either way.
    /// </remarks>
    [Theory(DisplayName = "What the type's own factories look like no longer reaches the decision.")]
    [InlineData("public static Email Create(string value) { return new Email(); }")]
    [InlineData("public static Email Parse(string value) { return new Email(); }")]
    [InlineData("public static Email Of(string value) { return new Email(); } public static Email From(string value) { return new Email(); }")]
    [InlineData("public static Email Build(string value) { return new Email(); }")]
    [InlineData("public static Email Create(Email value) { return value; }")]
    [InlineData("")]
    public void TheTypesOwnFactoriesNoLongerReachTheDecision(string members) {
        ScaffoldedParameter parameter = Composed($$"""
                                                  public sealed class Email {
                                                      {{members}}
                                                  }
                                                  """,
                                                  "Email");

        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
    }

    /// <summary>
    ///     A generic type is the one composed shape §5.5 still answers for.
    /// </summary>
    /// <remarks>
    ///     The naming function works from <c>type.Name</c>, which drops the arguments: <c>Repository&lt;Order&gt;</c>
    ///     and <c>Repository&lt;Line&gt;</c> would both be told to write <c>DummyRepository</c>, and neither is the
    ///     name to write. A sentinel that says nothing beats a name that says the wrong thing.
    /// </remarks>
    [Fact(DisplayName = "A generic type comes back open: its name would drop its arguments.")]
    public void AGenericTypeComesBackOpen() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Repository<T> { public Repository() { } }

                                                 public sealed class Email { public Email() { } }
                                                 """,
                                                 "Repository<Email>");

        Check.That(parameter.IsUnresolved).IsTrue();
    }

    [Fact(DisplayName = "A composed element inside a collection is composed too.")]
    public void AComposedElementInsideACollectionIsComposedToo() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Create(string value) { return new Email(); }
                                                 }
                                                 """,
                                                 "IReadOnlyList<Email>");

        Check.That(parameter.Expression).IsEqualTo("Dummy.ListOf(new DummyEmail())");
    }

    /// <summary>
    ///     Where the recipe went, and the reason the trade is worth making.
    /// </summary>
    /// <remarks>
    ///     The guards on <c>OrderReference.Create</c> used to be read at every site composing an
    ///     <c>OrderReference</c>, once per site, each copy free to drift from the constructor it described.
    ///     They are read once now, by the generator for the type that declares them — the same chain, at the
    ///     one address that owns it. The reported case is pinned here rather than deleted with the path that
    ///     used to carry it: <c>WithLengthBetween(8, 20)</c>, the interval spelled once, with
    ///     <c>NotBlank</c> kept beside it rather than absorbed — a floor of eight admits eight spaces, which
    ///     <c>NotBlank</c> alone still refuses.
    /// </remarks>
    [Fact(DisplayName = "The recipe a factory's guards describe belongs to the value object's own generator.")]
    public void TheRecipeAFactorysGuardsDescribeBelongsToTheValueObject() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class OrderReference {

                                                       private OrderReference() { }

                                                       public static OrderReference Create(string value) {
                                                           if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                                                           if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                                                           if (value.Length > 20) { throw new ArgumentException(nameof(value)); }

                                                           return new OrderReference();
                                                       }

                                                   }
                                                   """,
                                                   metadataName: "Shop.Domain.OrderReference");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo("Dummy.String().NotBlank().WithLengthBetween(8, 20)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     The field case: several same-named types, none of them usable and none of them looked at.
    ///     Reproduces <c>Reefact/justdummies.io</c>'s <c>tools/snippet-validation</c>, where three unrelated
    ///     <c>static class DummyOrderReference</c> narrative snippets share a name with the composed type's real
    ///     generator without being one.
    /// </summary>
    /// <remarks>
    ///     None of the three earns a <c>using</c> — nothing here is ever consulted to decide that, since the
    ///     call is written the same way regardless of how many same-named types the compilation carries.
    /// </remarks>
    [Fact(DisplayName = "Several same-named types in different namespaces change nothing about the call.")]
    public void SeveralSameNamedTypesInDifferentNamespacesChangeNothingAboutTheCall() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain {

                                                       public sealed class Email { public Email() { } }

                                                       public sealed class Subject {
                                                           public Subject(Email value) { }
                                                       }

                                                   }

                                                   namespace Shop.SnippetsCareless {

                                                       public static class DummyEmail {
                                                           public static Email Generate() { return new Email(); }
                                                       }

                                                   }

                                                   namespace Shop.SnippetsHandwritten {

                                                       public static class DummyEmail {
                                                           public static Email Generate() { return new Email(); }
                                                       }

                                                   }

                                                   namespace Shop.SnippetsConstrained {

                                                       public static class DummyEmail {
                                                           public static Email Generate() { return new Email(); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.IsUnresolved).IsFalse();
        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Scaffolded)).IsTrue();

        string sourceText = outcome.File!.SourceText;

        Check.That(sourceText).Not.Contains("Shop.SnippetsCareless");
        Check.That(sourceText).Not.Contains("Shop.SnippetsHandwritten");
        Check.That(sourceText).Not.Contains("Shop.SnippetsConstrained");
    }

    /// <summary>
    ///     Even a real, valid generator sitting in another namespace earns no <c>using</c>: composition never
    ///     opens a namespace for anything but the composed type's own (ADR-0062), which is deterministic from
    ///     the type being composed and never from a lookup. If the developer's own convention does not put
    ///     <c>DummyEmail</c> where the composed type lives, the resulting <c>CS0246</c> is the answer, not a bug —
    ///     adding the <c>using</c> by hand is the developer's call, never dum's.
    /// </summary>
    [Fact(DisplayName = "A real generator in another namespace earns no using: the call stays blind.")]
    public void ARealGeneratorInAnotherNamespaceEarnsNoUsing() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain {

                                                       public sealed class Email { public Email() { } }

                                                       public sealed class Subject {
                                                           public Subject(Email value) { }
                                                       }

                                                   }

                                                   namespace Shop.Generators {

                                                       using JustDummies;
                                                       using Shop.Domain;

                                                       public sealed class DummyEmail : IDummy<Email> {
                                                           public Email Generate() { return new Email(); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(outcome.File!.SourceText).Not.Contains("using Shop.Generators;");
    }

    /// <summary>
    ///     A null-check on a composed parameter adds nothing to verify: the generator it draws through never
    ///     returns <c>null</c> (§5.3), so the parameter is exactly as clean as one with no guard at all — inline,
    ///     with no method of its own (§4.2).
    /// </summary>
    [Fact(DisplayName = "A composed parameter's own recognised null-check needs no verification.")]
    public void ARecognisedNullCheckOnAComposedParameterNeedsNoVerification() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain;

                                                   using System;
                                                   using JustDummies;

                                                   public sealed class Email { public Email() { } }

                                                   public sealed class Subject {
                                                       public Subject(Email value) {
                                                           Value = value ?? throw new ArgumentNullException(nameof(value));
                                                       }

                                                       public Email Value { get; }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(parameter.RequiresVerification).IsFalse();
        Check.That(parameter.DrawnInline).IsTrue();
        Check.That(outcome.File!.SourceText).Not.Contains("DummyValidValue");
    }

    /// <summary>
    ///     The recognised null-check reads only what it recognises: a second, unclassified guard on the same
    ///     composed parameter still blocks compilation, exactly as it would on a parameter with no null-check at
    ///     all.
    /// </summary>
    [Fact(DisplayName = "A composed parameter's guard beyond a null-check still blocks compilation.")]
    public void AComposedParametersGuardBeyondANullCheckStillBlocksCompilation() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain;

                                                   using System;
                                                   using JustDummies;

                                                   public sealed class Email { public string Text => "x"; }

                                                   public sealed class Subject {
                                                       public Subject(Email value) {
                                                           Value = value ?? throw new ArgumentNullException(nameof(value));

                                                           if (value.Text == "forbidden") { throw new ArgumentException(nameof(value)); }
                                                       }

                                                       public Email Value { get; }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo("new DummyEmail()");
        Check.That(parameter.RequiresVerification).IsTrue();
        Check.That(parameter.DrawnInline).IsFalse();
        Check.That(outcome.File!.SourceText).Contains("DummyValidValue");
    }

    /// <summary>Scaffolds a <c>Subject</c> whose single parameter is of <paramref name="parameterType" />.</summary>
    private static ScaffoldedParameter Composed(string declarations, string parameterType) {
        ScaffoldOutcome outcome = Subject.Scaffold($$"""
                                                   namespace Shop.Domain;

                                                   using System;
                                                   using System.Collections.Generic;

                                                   using JustDummies;

                                                   {{declarations}}

                                                   public sealed class Subject {
                                                       public Subject({{parameterType}} value) { }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters[0];
    }

}
