using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     How a type the base table has no row for is drawn anyway (§5.4).
/// </summary>
/// <remarks>
///     One rule, since ADR-0089: through the generator that type owns. What the type's own factories look like
///     no longer reaches this decision. A value object's recipe belongs to the generator scaffolded for it, so
///     every site composing that type calls it rather than deriving a copy free to drift from the original.
/// </remarks>
public sealed class CompositionTests {

    /// <summary>
    ///     A scaffolded generator is used, and that is how aggregates compose in cascade: scaffold
    ///     <c>Customer</c>, re-run <c>--force</c> on <c>Order</c>, and the parameter closes.
    /// </summary>
    [Fact(DisplayName = "A generator already scaffolded for the type is used.")]
    public void AGeneratorAlreadyScaffoldedIsUsed() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Customer { public Customer(string name) { } }

                                                 public sealed class AnyCustomer : IAny<Customer> {
                                                     public Customer Generate() { return new Customer("name"); }
                                                 }
                                                 """,
                                                 "Customer");

        Check.That(parameter.Expression).IsEqualTo("new AnyCustomer()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Scaffolded)).IsTrue();
    }

    // It is the developer's own answer to the question, so it outranks anything the engine could infer.
    [Fact(DisplayName = "A scaffolded generator wins over a static factory.")]
    public void AScaffoldedGeneratorWinsOverAFactory() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Create(string value) { return new Email(); }
                                                 }

                                                 public sealed class AnyEmail : IAny<Email> {
                                                     public Email Generate() { return Email.Create("a@b.c"); }
                                                 }
                                                 """,
                                                 "Email");

        Check.That(parameter.Expression).IsEqualTo("new AnyEmail()");
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

        Check.That(parameter.Expression).IsEqualTo("new AnyEmail()");
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

        Check.That(parameter.Expression).IsEqualTo("new AnyEmail()");
    }

    /// <summary>
    ///     A generic type is the one composed shape §5.5 still answers for.
    /// </summary>
    /// <remarks>
    ///     The naming function works from <c>type.Name</c>, which drops the arguments: <c>Repository&lt;Order&gt;</c>
    ///     and <c>Repository&lt;Line&gt;</c> would both be told to write <c>AnyRepository</c>, and neither is the
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

        Check.That(parameter.Expression).IsEqualTo("Any.ListOf(new AnyEmail())");
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

        Check.That(parameter.Expression).IsEqualTo("Any.String().NotBlank().WithLengthBetween(8, 20)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     The four ways a same-named type fails to be a usable <c>AnyX</c> — static, not <c>IAny&lt;T&gt;</c>,
    ///     abstract, and missing a public parameterless constructor — and the nominal case that must keep
    ///     working: a single <c>AnyX : IAny&lt;X&gt;</c> that qualifies on every count.
    /// </summary>
    /// <remarks>
    ///     Every disqualified row is treated as though nothing named <c>AnyEmail</c> existed at all: composed
    ///     as an open parameter, never as <c>new AnyEmail()</c> — which would collide with the very declaration
    ///     that failed to qualify and send the developer chasing a compiler error at the wrong culprit.
    /// </remarks>
    [Theory(DisplayName = "A same-named type that is not usable as a generator is not a candidate.")]
    [InlineData("public static class AnyEmail { public static Email Generate() { return new Email(); } }")]
    [InlineData("public sealed class AnyEmail { public Email Generate() { return new Email(); } }")]
    [InlineData("public abstract class AnyEmail : IAny<Email> { public Email Generate() { return new Email(); } }")]
    [InlineData("public sealed class AnyEmail : IAny<Email> { public AnyEmail(int seed) { } public Email Generate() { return new Email(); } }")]
    public void ASameNamedTypeThatIsNotUsableIsNotACandidate(string anyEmail) {
        ScaffoldedParameter parameter = Composed($$"""
                                                  public sealed class Email { public Email() { } }

                                                  {{anyEmail}}
                                                  """,
                                                  "Email");

        Check.That(parameter.IsUnresolved).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Scaffolded)).IsFalse();
        Check.That(parameter.AmbiguousGeneratorCandidates).IsEmpty();
    }

    /// <summary>
    ///     The nominal case a disqualified same-named type must never be confused with: exactly one usable
    ///     generator, reached from another namespace, whose namespace the emitted file therefore has to open.
    /// </summary>
    [Fact(DisplayName = "A single usable generator in another namespace is used, and its namespace is opened.")]
    public void ASingleUsableGeneratorInAnotherNamespaceIsUsedAndItsNamespaceIsOpened() {
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

                                                       public sealed class AnyEmail : IAny<Email> {
                                                           public Email Generate() { return new Email(); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.Expression).IsEqualTo("new AnyEmail()");
        Check.That(outcome.File!.SourceText).Contains("using Shop.Generators;");
    }

    /// <summary>
    ///     Two usable generators answer to the same name, in two different namespaces: the discipline §5.1.2
    ///     already holds a tied static factory to — list them, choose neither.
    /// </summary>
    [Fact(DisplayName = "Two usable generators in two namespaces are listed, and neither is chosen.")]
    public void TwoUsableGeneratorsInTwoNamespacesAreListedAndNeitherIsChosen() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   namespace Shop.Domain {

                                                       public sealed class Email { public Email() { } }

                                                       public sealed class Subject {
                                                           public Subject(Email value) { }
                                                       }

                                                   }

                                                   namespace Shop.GeneratorsOne {

                                                       using JustDummies;
                                                       using Shop.Domain;

                                                       public sealed class AnyEmail : IAny<Email> {
                                                           public Email Generate() { return new Email(); }
                                                       }

                                                   }

                                                   namespace Shop.GeneratorsTwo {

                                                       using JustDummies;
                                                       using Shop.Domain;

                                                       public sealed class AnyEmail : IAny<Email> {
                                                           public Email Generate() { return new Email(); }
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters[0];

        Check.That(parameter.IsUnresolved).IsTrue();
        Check.That(parameter.AmbiguousGeneratorCandidates)
             .ContainsExactly("Shop.GeneratorsOne.AnyEmail", "Shop.GeneratorsTwo.AnyEmail");

        string sourceText = outcome.File!.SourceText;

        Check.That(sourceText).Contains("Shop.GeneratorsOne.AnyEmail");
        Check.That(sourceText).Contains("Shop.GeneratorsTwo.AnyEmail");
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
