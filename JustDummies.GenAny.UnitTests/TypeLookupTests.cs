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
    ///     §3.1 has <c>dum</c> run from the <b>test</b> project, so <c>Warehouse</c> reaches that compilation
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
    ///     <c>dum</c> at real repositories to find it: every shape the generative sweep draws lives in one
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
