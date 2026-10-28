using System.Linq;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Finding the type a developer named on the command line (§3.2).
/// </summary>
public sealed class TypeLookupTests {

    /// <summary>Two types called <c>Order</c>, one nested type, and a near-miss to suggest.</summary>
    private const string Domain = """
                                  namespace Shop.Sales;

                                  public sealed class Order {

                                      public Order(int quantity) { }

                                      public sealed class Line {
                                          public Line(int quantity) { }
                                      }

                                  }

                                  public sealed class Basket {
                                      public Basket(int size) { }
                                  }
                                  """;

    private const string Elsewhere = """
                                     namespace Shop.Archive;

                                     public sealed class Order {
                                         public Order(int quantity) { }
                                     }
                                     """;

    /// <summary>What a test project's own source looks like: tests, and not one domain type.</summary>
    private const string Nearby = """
                                  namespace Shop.Tests;

                                  public sealed class BasketTests {
                                      public BasketTests(int nothing) { }
                                  }
                                  """;

    /// <summary>The production type, guarded, as the test project meets it: through a reference.</summary>
    private const string NextDoor = """
                                    namespace Shop.Production;

                                    public sealed class Warehouse {

                                        private readonly int kept;

                                        public Warehouse(int size) {
                                            if (size <= 0) { throw new System.ArgumentOutOfRangeException(nameof(size)); }

                                            kept = size;
                                        }

                                    }
                                    """;

    /// <summary>
    ///     A referenced assembly declaring the domain's <c>Measurement</c> beside two that cannot be scaffolded
    ///     under that name: a generic one, which the bare name does not spell, and an internal one, which the
    ///     analysed project cannot reach.
    /// </summary>
    private const string Crowded = """
                                   namespace Shop {
                                       public sealed class Measurement {
                                           public Measurement(int value) { }
                                       }
                                   }

                                   namespace Other {
                                       public readonly struct Measurement<T> {
                                           public Measurement(T value) { }
                                       }
                                   }

                                   namespace Other.Hidden {
                                       internal sealed class Measurement {
                                           public Measurement(int value) { }
                                       }
                                   }
                                   """;

    [Theory(DisplayName = "A type is found by the name a developer would type.")]
    [InlineData("Basket", "AnyBasket.cs")]
    [InlineData("Shop.Sales.Basket", "AnyBasket.cs")]
    public void ATypeIsFoundByTheNameADeveloperWouldType(string argument, string file) {
        ScaffoldOutcome outcome = Scaffold(argument, Domain);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.File!.FileName).IsEqualTo(file);
    }

    /// <summary>
    ///     A nested type is written the way a developer types it, and translated before the lookup.
    /// </summary>
    /// <remarks>
    ///     Metadata spells nesting with <c>+</c>. Handing <c>Order.Line</c> straight to a metadata lookup
    ///     returns nothing, which would report a real type as missing — the one bug §3.2 calls out by name.
    /// </remarks>
    [Theory(DisplayName = "A nested type is found from its dotted spelling.")]
    [InlineData("Order.Line")]
    [InlineData("Shop.Sales.Order.Line")]
    public void ANestedTypeIsFoundFromItsDottedSpelling(string argument) {
        ScaffoldOutcome outcome = Scaffold(argument, Domain);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.File!.FileName).IsEqualTo("AnyLine.cs");
    }

    // Which one the developer meant is theirs to say, so both are named and neither is picked.
    [Fact(DisplayName = "A name matching two types is refused, with both named.")]
    public void ANameMatchingTwoTypesIsRefused() {
        ScaffoldOutcome outcome = Scaffold("Order", Domain, Elsewhere);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.TypeAmbiguous);
        Check.That(outcome.Candidates).ContainsExactly("Shop.Archive.Order", "Shop.Sales.Order");
    }

    /// <summary>
    ///     A short name is matched against the types it could actually name.
    /// </summary>
    /// <remarks>
    ///     <c>Measurement</c> does not spell <c>Measurement&lt;T&gt;</c> — C# would need a type argument — and an
    ///     internal type of another assembly is one the emitted file could not name at all. Counting either made
    ///     a real domain type ambiguous with the framework's <c>System.Diagnostics.Metrics.Measurement&lt;T&gt;</c>.
    /// </remarks>
    [Fact(DisplayName = "A short name ignores generic and inaccessible types it cannot name.")]
    public void AShortNameIgnoresTypesItCannotName() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing(Crowded, "Measurement", Nearby);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.Plan!.Target.FullName).IsEqualTo("Shop.Measurement");
    }

    // Nameable first, source second: a generic type of the developer's own does not hide a referenced type the
    // bare name does spell — the preference for source is between types the name can reach, not before it.
    [Fact(DisplayName = "A generic type in source does not hide a nameable referenced one.")]
    public void AGenericTypeInSourceDoesNotHideANameableReferencedOne() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing("""
                                                                    namespace Shop;

                                                                    public sealed class Measurement {
                                                                        public Measurement(int value) { }
                                                                    }
                                                                    """,
                                                                    "Measurement",
                                                                    "namespace Shop.Tests; public sealed class Measurement<T> { public Measurement(T value) { } }");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.Plan!.Target.FullName).IsEqualTo("Shop.Measurement");
    }

    // The same name on its own still meets the refusal written for it: a generic type is told why, rather
    // than that it does not exist.
    [Fact(DisplayName = "A generic type named alone is still refused as generic.")]
    public void AGenericTypeNamedAloneIsStillRefusedAsGeneric() {
        ScaffoldOutcome outcome = Scaffold("Crate", "namespace Shop; public sealed class Crate<T> { public Crate(T item) { } }");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.TypeIsGeneric);
    }

    // The header tells the developer how to re-run the tool; a short name that is ambiguous would send them
    // into the very refusal the full name was typed to avoid.
    [Fact(DisplayName = "The header names a command that resolves to the scaffolded type.")]
    public void TheHeaderNamesACommandThatResolves() {
        ScaffoldOutcome outcome = Scaffold("Shop.Archive.Order", Domain, Elsewhere);

        Check.That(outcome.File!.SourceText).Contains("// `any generate Shop.Archive.Order --force` overwrites it.");
    }

    [Fact(DisplayName = "The header keeps the short name where it resolves.")]
    public void TheHeaderKeepsTheShortNameWhereItResolves() {
        ScaffoldOutcome outcome = Scaffold("Shop.Sales.Basket", Domain, Elsewhere);

        Check.That(outcome.File!.SourceText).Contains("// `any generate Basket --force` overwrites it.");
    }

    // Fully qualified, the same name settles it.
    [Fact(DisplayName = "The full name settles what the simple one could not.")]
    public void TheFullNameSettlesIt() {
        Check.That(Scaffold("Shop.Archive.Order", Domain, Elsewhere).Status).IsEqualTo(ScaffoldStatus.Scaffolded);
    }

    // An answer that only says no costs the developer a search; one that offers the near-miss costs them a
    // keystroke.
    [Fact(DisplayName = "A name matching nothing is refused, with the closest ones offered.")]
    public void ANameMatchingNothingIsRefusedWithSuggestions() {
        ScaffoldOutcome outcome = Scaffold("Baskett", Domain);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.TypeNotFound);
        Check.That(outcome.Candidates).Contains("Basket");
    }

    [Fact(DisplayName = "A name nothing resembles is refused without inventing a suggestion.")]
    public void ANameNothingResemblesOffersNothing() {
        ScaffoldOutcome outcome = Scaffold("Zzzzzzzzzzzz", Domain);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.TypeNotFound);
        Check.That(outcome.Candidates).IsEmpty();
    }

    /// <summary>
    ///     The type a developer points the tool at is normally not in the source it is run over.
    /// </summary>
    /// <remarks>
    ///     §3.1 has <c>any</c> run from the <b>test</b> project, so <c>Warehouse</c> reaches that compilation
    ///     through a project reference and its own source declares nothing of the sort. Both halves of this
    ///     failed against a real project before this case was written down: the lookup answered "no such type"
    ///     for every type the tool exists to scaffold, and once it found one, reading its guards threw —
    ///     a workspace hands a referenced project over as a compilation, whose syntax trees the analyzed
    ///     compilation does not own.
    /// </remarks>
    [Fact(DisplayName = "A type in a referenced assembly is found, and its guards are read from its own source.")]
    public void ATypeFromAReferencedAssemblyIsFoundAndRead() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing(NextDoor, "Warehouse", Nearby);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.File!.FileName).IsEqualTo("AnyWarehouse.cs");

        ScaffoldedParameter size = outcome.Plan!.Parameters.Single();

        Check.That(size.Expression).IsEqualTo("Any.Int32().Positive()");
        Check.That(size.Provenance).IsEqualTo(Provenance.Guard);
    }

    /// <summary>
    ///     And the referenced project is normally on another target framework, which is a harder case.
    /// </summary>
    /// <remarks>
    ///     The case above is the same shape with the two compilations bound to identical references, and that
    ///     identity is what made it pass: they share their symbols outright, so a guard read through the
    ///     declaring compilation's semantic model names parameters the analysed compilation recognises. A
    ///     library on <c>netstandard2.0</c> under a test project on <c>net8.0</c> — this repository's own
    ///     arrangement, and most others' — has no such luck, and the reading came back empty: not a wrong
    ///     constraint, no constraint at all, and no <c>unread guards</c> either, since a guard naming no
    ///     parameter is indistinguishable from ordinary logic. A recap reporting every parameter inferred over
    ///     an invariant nobody honoured is the silent failure ADR-0083 exists to stop, and it took pointing
    ///     <c>any</c> at real repositories to find it: every shape the generative sweep draws lives in one
    ///     compilation, so no number of them can reach this.
    /// </remarks>
    [Fact(DisplayName = "A type in a referenced assembly built on another framework has its guards read too.")]
    public void ATypeFromAReferencedAssemblyOnAnotherFrameworkIsRead() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencingAcrossFrameworks(NextDoor, "Warehouse", Nearby);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        ScaffoldedParameter size = outcome.Plan!.Parameters.Single();

        Check.That(size.Expression).IsEqualTo("Any.Int32().Positive()");
        Check.That(size.Provenance).IsEqualTo(Provenance.Guard);
    }

    // The other half of the same rule: widening happens only when the developer's own types have nothing to
    // say. A domain type sharing a name with a referenced one is the one they meant, not an ambiguity.
    [Fact(DisplayName = "A type in source wins over one of the same name in a reference.")]
    public void ATypeInSourceWinsOverAReferencedOne() {
        ScaffoldOutcome outcome = Subject.ScaffoldByNameReferencing(Elsewhere, "Order", Domain);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
    }

    [Fact(DisplayName = "A blank type argument is a programming error, not an outcome.")]
    public void ABlankTypeArgumentIsAProgrammingError() {
        Check.ThatCode(() => Scaffold("   ", Domain)).Throws<System.ArgumentException>();
    }

    private static ScaffoldOutcome Scaffold(string argument, params string[] sources) {
        return Subject.ScaffoldByName(argument, sources);
    }

}
