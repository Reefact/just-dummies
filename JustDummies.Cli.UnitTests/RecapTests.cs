using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using JustDummies.GenAny;

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
    ///     §6's run, verbatim: the <c>Order</c> of §4.1 <b>before</b> <c>AnyCustomer</c> was scaffolded, which
    ///     is why <c>customer</c> is the one parameter left open.
    /// </summary>
    private const string Expected = """
                                    Analyzing Shop.Domain.Order
                                      constructor Order(OrderReference, Customer, int, OrderStatus, IReadOnlyList<string>, DateTime)

                                      reference  OrderReference         Any.String().NonEmpty().As(OrderReference.Create)  factory, guard
                                      customer   Customer               —                                                  TODO
                                      quantity   int                    Any.Int32().Positive()                             guard
                                      status     OrderStatus            Any.Enum<OrderStatus>()
                                      tags       IReadOnlyList<string>  Any.ListOf(Any.String().NonEmpty())
                                      placedAt   DateTime               Any.DateTime()

                                    ✓ AnyOrder.cs — 5 of 6 parameters inferred, 1 TODO.
                                      The file will not compile until you resolve it. That is deliberate.
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
        string rendered = Rendered(Plan([Inferred("quantity", "int", "Any.Int32().Positive()", Provenance.Guard)]));

        Check.That(rendered).Contains("✓ AnySubject.cs — 1 of 1 parameters inferred.");
        Check.That(rendered).Not.Contains("That is deliberate");
    }

    // A guard the engine cannot vouch for closes the recap the same way an open parameter does — the file
    // will not compile — but counted separately, since a generator WAS inferred here and stays as the base.
    [Fact(DisplayName = "A parameter requiring verification closes with its own count and the compile sentence.")]
    public void AParameterRequiringVerificationClosesWithItsOwnCount() {
        string rendered = Rendered(Plan([Inferred("name", "string", "Any.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("✓ AnySubject.cs — 1 of 1 parameters inferred, 1 to verify.");
        Check.That(rendered).Contains("The file will not compile until you resolve it. That is deliberate.");
    }

    // The row and the closing line describe the same parameter, so they say the same word about it. A row
    // reading TODO under a count reading `1 to verify, 0 TODO` is the recap contradicting itself.
    [Fact(DisplayName = "A parameter requiring verification reads `to verify` in its row too, never TODO.")]
    public void AParameterRequiringVerificationReadsToVerifyInItsRow() {
        string rendered = Rendered(Plan([Inferred("name", "string", "Any.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("to verify, unread guards");
        Check.That(rendered).Not.Contains("TODO");
    }

    // Both at once, the ordinary case where one parameter is wholly open and another only doubtful: the two
    // counts read side by side, in the order a developer acts on them — supply one, verify the other.
    [Fact(DisplayName = "An open parameter and one requiring verification are both counted, TODO first.")]
    public void AnOpenParameterAndOneRequiringVerificationAreBothCounted() {
        string rendered = Rendered(Plan([ScaffoldedParameter.Unresolved("customer", "Customer"),
                                         Inferred("name", "string", "Any.String().NonEmpty()", Provenance.UnreadGuards)]));

        Check.That(rendered).Contains("✓ AnySubject.cs — 1 of 2 parameters inferred, 1 TODO, 1 to verify.");
    }

    // A generator for a type with a parameterless constructor is still worth having — it composes into
    // Any.ListOf(…) where `new Subject()` does not — so the recap says so rather than counting to zero.
    [Fact(DisplayName = "A generator with no parameters has its own closing line.")]
    public void AGeneratorWithNoParametersHasItsOwnClosingLine() {
        Check.That(Rendered(Plan([]))).Contains("✓ AnySubject.cs — no constructor parameters to infer.");
    }

    /// <summary>
    ///     The shadowing case of §7: a warning, both names, and the file written anyway.
    /// </summary>
    [Fact(DisplayName = "A shadowed name is named on both sides, and does not stop the scaffold.")]
    public void AShadowedNameIsNamedOnBothSides() {
        ScaffoldOutcome outcome = Outcome(Plan([Inferred("text", "string", "Any.String().NonEmpty()")]),
                                          [ScaffoldWarning.Shadows("AnyPattern", "JustDummies.AnyPattern")]);

        string rendered = Rendered(outcome);

        Check.That(rendered).Contains("AnyPattern shadows JustDummies.AnyPattern inside its own namespace.");
        Check.That(rendered).Contains("✓ AnySubject.cs");
        Check.That(ExitCode.For(outcome)).IsEqualTo(0);
    }

    // §5.4 leaves the parameter open where several factories qualify and names them here, because which one
    // the developer meant is theirs to say.
    [Fact(DisplayName = "Several qualifying factories are named in the recap.")]
    public void SeveralQualifyingFactoriesAreNamed() {
        ScaffoldedParameter open = ScaffoldedParameter.Unresolved("value", "Email", Provenance.None,
                                                                  ["Email.Of", "Email.From"]);

        Check.That(Rendered(Plan([open]))).Contains("value: several factories qualify — Email.Of, Email.From.");
    }

    [Theory(DisplayName = "Every provenance the engine can report has a word in the column.")]
    [InlineData(Provenance.Guard, "guard")]
    [InlineData(Provenance.Factory, "factory")]
    [InlineData(Provenance.Scaffolded, "AnyX")]
    [InlineData(Provenance.GuardsNotCombined, "guards not combined")]
    [InlineData(Provenance.UnreadGuards, "unread guards")]
    [InlineData(Provenance.NoSource, "no source")]
    [InlineData(Provenance.Unavailable, "unavailable")]
    public void EveryProvenanceHasAWord(Provenance provenance, string word) {
        Check.That(Rendered(Plan([Inferred("value", "string", "Any.String().NonEmpty()", provenance)]))).Contains(word);
    }

    // The base table has nothing to say, and saying nothing is the point: a column that always spoke would
    // stop meaning anything.
    [Fact(DisplayName = "A parameter straight from the base table leaves the column empty.")]
    public void AParameterFromTheBaseTableLeavesTheColumnEmpty() {
        string rendered = Rendered(Plan([Inferred("value", "string", "Any.String().NonEmpty()")]));

        Check.That(rendered).Contains("  value  string  Any.String().NonEmpty()\n");
    }

    private static ScaffoldPlan WorkedExample() {
        return new ScaffoldPlan(new TargetType("Order", "Shop.Domain", NamespaceStyle.FileScoped),
                                "AnyOrder",
                                ["System", "System.Collections.Generic", "JustDummies"],
                                [
                                    Inferred("reference", "OrderReference",
                                             "Any.String().NonEmpty().As(OrderReference.Create)",
                                             Provenance.Factory | Provenance.Guard),
                                    ScaffoldedParameter.Unresolved("customer", "Customer"),
                                    Inferred("quantity", "int", "Any.Int32().Positive()", Provenance.Guard),
                                    Inferred("status", "OrderStatus", "Any.Enum<OrderStatus>()"),
                                    Inferred("tags", "IReadOnlyList<string>", "Any.ListOf(Any.String().NonEmpty())"),
                                    Inferred("placedAt", "DateTime", "Any.DateTime()")
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
                                "AnySubject",
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
