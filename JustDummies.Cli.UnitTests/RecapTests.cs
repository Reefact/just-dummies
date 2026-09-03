using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using JustDummies.GenDummy;

using NFluent;

namespace JustDummies.Cli.UnitTests;

/// <summary>
///     The console recap of §6, rendered from a result model rather than from a run.
/// </summary>
/// <remarks>
///     Which is the point of the model existing: "provenance is data, not output — the engine returns it, the
///     CLI renders it", and that is what makes the recap checkable without a compilation, a project on disk or
///     a terminal. The expected text below is §6's own worked example, copied from the specification.
/// </remarks>
[SuppressMessage(SonarRule.S1135.Category, SonarRule.S1135.Id, Justification = "Names the marker the tool emits by design (§5.5) and prints in the recap (§6), not unfinished work here.")]
public sealed class RecapTests {

    /// <summary>
    ///     §6's run, verbatim: the <c>Order</c> of §4.1, every parameter inferred.
    /// </summary>
    /// <remarks>
    ///     Both composed parameters read <c>DummyX</c>, and the recap says the same thing whether or not the
    ///     compilation carries those two generators yet: the name is the answer either way, and where one is
    ///     missing the developer's own build reports it (ADR-0089). Not a silence — the file does not compile.
    /// </remarks>
    private const string Expected = """
                                    Analyzing Shop.Domain.Order
                                      constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

                                      reference  OrderReference         new DummyOrderReference()                DummyX
                                      customer   Customer               new DummyCustomer()                      DummyX
                                      quantity   int                    Dummy.Int32().Positive()                 guard
                                      status     OrderStatus            Dummy.Enum<OrderStatus>()
                                      tags       IReadOnlyList<string>  Dummy.ListOf(Dummy.String().NonEmpty())
                                      placedAt   DateTime               Dummy.DateTime()

                                    ✓ DummyOrder.cs — 6 of 6 parameters inferred.
                                    """;

    [Fact(DisplayName = "The recap is the one §6 writes out, to the space.")]
    public void TheRecapIsTheOneTheSpecificationWritesOut() {
        string rendered = Rendered(WorkedExample());

        // The tool's own line ending is part of its contract, so it is asserted rather than normalised away.
        Check.That(rendered).Not.Contains("\r");
        Check.That(rendered).IsEqualTo(AsWritten(Expected));
    }

    /// <summary>
    ///     The expected text with its line endings normalised — the <b>expected</b> side only.
    /// </summary>
    /// <remarks>
    ///     A C# raw string literal carries the line endings of the file it is written in, and Git hands a
    ///     Windows checkout that file with CRLF. So this fixture arrives platform-dependent while the recap it
    ///     describes is not, and the two disagreed on Windows alone — the tool being right and the fixture
    ///     being local. Normalising the expectation says which of the two is the contract; normalising the
    ///     rendered text would have hidden it.
    /// </remarks>
    private static string AsWritten(string expected) {
        return expected.Replace("\r\n", "\n");
    }

    // A file with nothing left open says so without the two lines that explain a TODO — there is none to
    // explain, and a sentence about deliberate failure under a clean run would read as a warning.
    [Fact(DisplayName = "A scaffold with nothing left open closes without the TODO sentence.")]
    public void AScaffoldWithNothingOpenClosesWithoutTheTodoSentence() {
        string rendered = Rendered(Plan([Inferred("quantity", "int", "Dummy.Int32().Positive()", Provenance.Guard)]));

        Check.That(rendered).Contains("✓ DummySubject.cs — 1 of 1 parameters inferred.");
        Check.That(rendered).Not.Contains("That is deliberate");
    }

    // A guard the engine cannot vouch for closes the recap the same way an open parameter does — the file
    // will not compile — but counted separately, since a generator WAS inferred here and stays as the base.
    [Fact(DisplayName = "A parameter requiring verification closes with its own count and the compile sentence.")]
    public void AParameterRequiringVerificationClosesWithItsOwnCount() {
        string rendered = Rendered(Plan([Inferred("name", "string", "Dummy.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("✓ DummySubject.cs — 1 of 1 parameters inferred, 1 to verify.");
        Check.That(rendered).Contains("The file will not compile until you resolve it. That is deliberate.");
    }

    // The row and the closing line describe the same parameter, so they say the same word about it. A row
    // reading TODO under a count reading `1 to verify, 0 TODO` is the recap contradicting itself.
    [Fact(DisplayName = "A parameter requiring verification reads `to verify` in its row too, never TODO.")]
    public void AParameterRequiringVerificationReadsToVerifyInItsRow() {
        string rendered = Rendered(Plan([Inferred("name", "string", "Dummy.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("to verify, unread guards");
        Check.That(rendered).Not.Contains("TODO");
    }

    // Both at once, the ordinary case where one parameter is wholly open and another only doubtful: the two
    // counts read side by side, in the order a developer acts on them — supply one, verify the other.
    [Fact(DisplayName = "An open parameter and one requiring verification are both counted, TODO first.")]
    public void AnOpenParameterAndOneRequiringVerificationAreBothCounted() {
        string rendered = Rendered(Plan([ScaffoldedParameter.Unresolved("customer", "Customer"),
                                         Inferred("name", "string", "Dummy.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("✓ DummySubject.cs — 1 of 2 parameters inferred, 1 TODO, 1 to verify.");
    }

    // A generator for a type with a parameterless constructor is still worth having — it composes into
    // Dummy.ListOf(…) where `new Subject()` does not — so the recap says so rather than counting to zero.
    [Fact(DisplayName = "A generator with no parameters has its own closing line.")]
    public void AGeneratorWithNoParametersHasItsOwnClosingLine() {
        Check.That(Rendered(Plan([]))).Contains("✓ DummySubject.cs — no constructor parameters to infer.");
    }

    /// <summary>
    ///     The shadowing case of §7: a warning, both names, and the file written anyway.
    /// </summary>
    [Fact(DisplayName = "A shadowed name is named on both sides, and does not stop the scaffold.")]
    public void AShadowedNameIsNamedOnBothSides() {
        ScaffoldOutcome outcome = Outcome(Plan([Inferred("text", "string", "Dummy.String().NonEmpty()")]),
                                          [ScaffoldWarning.Shadows("DummyPattern", "JustDummies.DummyPattern")]);

        string rendered = Rendered(outcome);

        Check.That(rendered).Contains("DummyPattern shadows JustDummies.DummyPattern inside its own namespace.");
        Check.That(rendered).Contains("✓ DummySubject.cs");
        Check.That(ExitCode.For(outcome)).IsEqualTo(0);
    }

    [Theory(DisplayName = "Every provenance the engine can report has a word in the column.")]
    [InlineData(Provenance.Guard, "guard")]
    [InlineData(Provenance.Scaffolded, "DummyX")]
    [InlineData(Provenance.GuardsNotCombined, "guards not combined")]
    [InlineData(Provenance.UnreadGuards, "unread guards")]
    [InlineData(Provenance.NoSource, "no source")]
    [InlineData(Provenance.Unavailable, "unavailable")]
    public void EveryProvenanceHasAWord(Provenance provenance, string word) {
        Check.That(Rendered(Plan([Inferred("value", "string", "Dummy.String().NonEmpty()", provenance)]))).Contains(word);
    }

    // The base table has nothing to say, and saying nothing is the point: a column that always spoke would
    // stop meaning anything.
    [Fact(DisplayName = "A parameter straight from the base table leaves the column empty.")]
    public void AParameterFromTheBaseTableLeavesTheColumnEmpty() {
        string rendered = Rendered(Plan([Inferred("value", "string", "Dummy.String().NonEmpty()")]));

        Check.That(rendered).Contains("  value  string  Dummy.String().NonEmpty()\n");
    }

    // The chosen construction is always printed (§5.1), and for a type built through its own factory the
    // construction Generate() makes is that factory call — so that is what the line names.
    [Fact(DisplayName = "A factory-built target prints the factory it is built through.")]
    public void AFactoryBuiltTargetPrintsItsFactory() {
        ScaffoldPlan plan = new(new TargetType("Email", "Shop.Domain", NamespaceStyle.FileScoped),
                                "DummyEmail",
                                ["JustDummies"],
                                [Inferred("value", "string", "Dummy.String().NonEmpty()", Provenance.Guard)],
                                factory: "Email.Create");

        string rendered = Rendered(plan);

        Check.That(rendered).Contains("  factory Email.Create(string)");
        Check.That(rendered).Not.Contains("constructor");
    }

    private static ScaffoldPlan WorkedExample() {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "DummyOrder",
                                ["System", "System.Collections.Generic", "JustDummies"],
                                [
                                    Inferred("reference", "OrderReference", "new DummyOrderReference()",
                                             Provenance.Scaffolded),
                                    Inferred("customer", "Customer", "new DummyCustomer()", Provenance.Scaffolded),
                                    Inferred("quantity", "int", "Dummy.Int32().Positive()", Provenance.Guard),
                                    Inferred("status", "OrderStatus", "Dummy.Enum<OrderStatus>()"),
                                    Inferred("tags", "IReadOnlyList<string>", "Dummy.ListOf(Dummy.String().NonEmpty())"),
                                    Inferred("placedAt", "DateTime", "Dummy.DateTime()")
                                ]);
    }

    private static ScaffoldedParameter Inferred(string name,
                                                string type,
                                                string expression,
                                                Provenance provenance = Provenance.None) {
        return ScaffoldedParameter.DrawnFrom(name, type, expression, provenance);
    }

    private static ScaffoldPlan Plan(IReadOnlyList<ScaffoldedParameter> parameters) {
        return new ScaffoldPlan(new TargetType("Subject", "Shop.Domain", NamespaceStyle.FileScoped),
                                "DummySubject",
                                ["JustDummies"],
                                parameters);
    }

    private static ScaffoldOutcome Outcome(ScaffoldPlan plan, IReadOnlyList<ScaffoldWarning>? warnings = null) {
        return ScaffoldOutcome.Scaffolded(plan, GeneratorEmitter.Emit(plan), warnings);
    }

    private static string Rendered(ScaffoldPlan plan) {
        return Rendered(Outcome(plan));
    }

    private static string Rendered(ScaffoldOutcome outcome) {
        StringWriter written = new();

        Recap.Render(outcome, ToolConsole.On(written));

        // Spectre pads each line to the console width; the recap's own layout is what is under test, not that.
        return string.Join("\n", written.ToString().TrimEnd().Split('\n')).TrimEnd();
    }

}
