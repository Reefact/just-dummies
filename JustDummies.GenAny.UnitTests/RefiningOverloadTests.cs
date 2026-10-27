using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The refining overload of ADR-0109: a test adjusts a parameter's valid base instead of replacing it.
/// </summary>
/// <remarks>
///     The golden files pin the shape and prove it compiles; what they cannot show is what the overload
///     <b>does</b>. So the files here are compiled with a harness that calls it and actually draws — the only
///     way to tell a lambda that starts from the base from one that starts from scratch, since both compile.
/// </remarks>
public sealed class RefiningOverloadTests {

    /// <summary>The seed every harness draws under, so a red run is a red run again.</summary>
    private const int Seed = 20261027;

    /// <summary>
    ///     The type the factory returns and the lambda receives, row by row, and the hop that follows them.
    /// </summary>
    /// <remarks>
    ///     Read off the chain's last call rather than its first, which is what the <c>Uri</c> row exists to
    ///     pin: <c>Web()</c> returns an <c>AnyWebUri</c>, and a factory declared to return the <c>AnyUri</c> the
    ///     chain starts from does not compile. The collection rows pin the arguments a factory infers from its
    ///     operand — an element in a covariant position keeps its concrete collection, one in an invariant
    ///     position is converted to the declared interface first.
    /// </remarks>
    [Theory(DisplayName = "A parameter's factory returns the generator its chain ends on, and the file compiles.")]
    [InlineData("int", "AnyInt32", "")]
    [InlineData("string", "AnyString", "")]
    [InlineData("string?", "AnyString", "")]
    [InlineData("Uri", "AnyWebUri", "")]
    [InlineData("OrderStatus", "AnyEnum<OrderStatus>", "")]
    [InlineData("int?", "AnyInt32", ".AsNullable()")]
    [InlineData("int[]", "AnyArray<int>", "")]
    [InlineData("IReadOnlyList<string>", "AnyList<string>", "")]
    [InlineData("IReadOnlyList<IReadOnlyList<int>>", "AnyList<List<int>>", "")]
    [InlineData("List<IReadOnlyList<int>>", "AnyList<IReadOnlyList<int>>", "")]
    [InlineData("IEnumerable<int>", "AnySequence<int>", "")]
    [InlineData("ISet<int>", "AnySet<int>", "")]
    [InlineData("Dictionary<string, int>", "AnyDictionary<string, int>", "")]
    [InlineData("Customer", "AnyCustomer", "")]
    public void AParametersFactoryReturnsTheGeneratorItsChainEndsOn(string parameterType, string generatorType, string conversion) {
        string domain = $$"""
                          using System;
                          using System.Collections.Generic;

                          namespace Shop.Domain;

                          public enum OrderStatus { Draft, Placed }

                          public sealed class Customer { }

                          public sealed class AnyCustomer : JustDummies.IAny<Customer> {
                              public Customer Generate() { return new Customer(); }
                          }

                          public sealed class Subject {
                              public Subject({{parameterType}} value) { }
                          }
                          """;

        ScaffoldOutcome     outcome   = Subject.ScaffoldByName("Subject", domain);
        ScaffoldedParameter parameter = outcome.Plan!.Parameters.Single();

        Check.That(parameter.GeneratorType).IsEqualTo(generatorType);
        Check.That(parameter.Conversion).IsEqualTo(conversion);
        Check.That(outcome.File!.SourceText).Contains($"public AnySubject WithValue(Func<{generatorType}, {generatorType}> configure) {{");
        Check.That(EmittedCodeCompiler.ErrorsIn(EmittedCodeCompiler.CompileWith(outcome.File.SourceText, domain))).IsEmpty();
    }

    /// <summary>
    ///     No overload replaces the base: a generator passed where a value is expected no longer compiles,
    ///     which is the point — that call was how a test lost a rule without a word.
    /// </summary>
    [Fact(DisplayName = "No overload takes a replacement generator.")]
    public void NoOverloadTakesAReplacementGenerator() {
        string emitted = GeneratorEmitter.Emit(Shapes.Order()).SourceText;

        Check.That(emitted).Not.Contains(" generator)");
        Check.That(emitted).Not.Contains("WithQuantity(IAny<int>");
    }

    /// <summary>
    ///     A parameter the engine could not resolve has no base to refine, so it keeps the value overload alone
    ///     (§5.5).
    /// </summary>
    [Fact(DisplayName = "An unresolved parameter keeps the value overload alone.")]
    public void AnUnresolvedParameterKeepsTheValueOverloadAlone() {
        string emitted = GeneratorEmitter.Emit(Shapes.OrderWithTodo()).SourceText;

        Check.That(emitted).Contains("public AnyOrder WithCustomer(Customer value) {");
        Check.That(emitted).Not.Contains("WithCustomer(Func<");
    }

    /// <summary>
    ///     The overload starts from the base the guard put there: refined to at most three, every draw is
    ///     still positive, so the domain's own constructor accepts every one of them.
    /// </summary>
    /// <remarks>
    ///     <c>LessThanOrEqualTo(3)</c> alone would draw zero and negatives, and the constructor below rejects
    ///     both — so a lambda handed a fresh <c>Any.Int32()</c> fails this test on the first such draw.
    /// </remarks>
    [Fact(DisplayName = "A refinement starts from the valid base, and keeps what it guarantees.")]
    public void ARefinementStartsFromTheValidBase() {
        const string domain = """
                              using System;

                              namespace Shop.Domain;

                              public sealed class Subject {
                                  public Subject(int quantity) {
                                      if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }

                                      Quantity = quantity;
                                  }

                                  public int Quantity { get; }
                              }
                              """;

        string harness = $$"""
                           using System.Linq;

                           using JustDummies;

                           namespace Shop.Domain;

                           public static class Harness {
                               public static int[] Run() {
                                   using (Any.UseSeed({{Seed}})) {
                                       AnySubject refined = new AnySubject().WithQuantity(q => q.LessThanOrEqualTo(3));

                                       return Enumerable.Range(0, 500).Select(_ => refined.Generate().Quantity).ToArray();
                                   }
                               }
                           }
                           """;

        int[] drawn = (int[])Run(Scaffolded(domain), domain, harness);

        Check.That(drawn).ContainsOnlyElementsThatMatch(quantity => quantity is >= 1 and <= 3);
    }

    /// <summary>
    ///     A refinement that contradicts the base is refused where it is written, naming both sides — the
    ///     library's own conflict, since the lambda adds to the base's constraints rather than replacing them.
    /// </summary>
    [Fact(DisplayName = "A refinement that contradicts the base fails at once, naming both sides.")]
    public void ARefinementThatContradictsTheBaseFailsAtOnce() {
        const string domain = """
                              using System;

                              namespace Shop.Domain;

                              public sealed class Subject {
                                  public Subject(int quantity) {
                                      if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
                                  }
                              }
                              """;

        const string harness = """
                               using System;

                               namespace Shop.Domain;

                               public static class Harness {
                                   public static string Run() {
                                       try {
                                           new AnySubject().WithQuantity(q => q.Negative());

                                           return "nothing was thrown";
                                       } catch (Exception refused) {
                                           return refused.GetType().Name + ": " + refused.Message;
                                       }
                                   }
                               }
                               """;

        string refusal = (string)Run(Scaffolded(domain), domain, harness);

        Check.That(refusal).StartsWith("ConflictingAnyConstraintException");
        Check.That(refusal).Contains("Positive()", "Negative()");
    }

    /// <summary>
    ///     The conversion of a nullable value type follows the lambda: the lambda refines the <c>decimal</c>
    ///     generator the constraints were written on, and the hop to <c>decimal?</c> comes after it.
    /// </summary>
    [Fact(DisplayName = "A nullable parameter is refined before its conversion, and never drawn null.")]
    public void ANullableParameterIsRefinedBeforeItsConversion() {
        const string domain = """
                              namespace Shop.Domain;

                              public sealed class Subject {
                                  public Subject(decimal? discount) {
                                      Discount = discount;
                                  }

                                  public decimal? Discount { get; }
                              }
                              """;

        string harness = $$"""
                           using System.Linq;

                           using JustDummies;

                           namespace Shop.Domain;

                           public static class Harness {
                               public static decimal?[] Run() {
                                   using (Any.UseSeed({{Seed}})) {
                                       AnySubject refined = new AnySubject().WithDiscount(d => d.Between(1m, 50m));

                                       return Enumerable.Range(0, 200).Select(_ => refined.Generate().Discount).ToArray();
                                   }
                               }
                           }
                           """;

        decimal?[] drawn = (decimal?[])Run(Scaffolded(domain), domain, harness);

        Check.That(drawn).ContainsOnlyElementsThatMatch(discount => discount is >= 1m and <= 50m);
    }

    /// <summary>
    ///     The case the overload exists for. <c>Shipment</c> only accepts a domestic route, a rule that belongs
    ///     to it and not to <c>Route</c>; the developer writes it once, in <c>AnyValidRoute()</c>, after the
    ///     engine marked the parameter (§5.6). A test that cares about the distance alone then states the
    ///     distance alone, and every shipment it draws is still a valid one.
    /// </summary>
    /// <remarks>
    ///     Two scaffolds, as in a developer's project: <c>AnyRoute</c> for the value object, <c>AnyShipment</c>
    ///     composing it. Only the second is edited, exactly as §5.6 asks — the sentinel deleted, the base
    ///     written where it stood.
    /// </remarks>
    [Fact(DisplayName = "A composed refinement starts from the base the developer wrote.")]
    public void AComposedRefinementStartsFromTheBaseTheDeveloperWrote() {
        const string domain = """
                              using System;

                              namespace Shop.Domain;

                              public sealed class Route {
                                  public Route(int distance, bool domestic) {
                                      if (distance <= 0) { throw new ArgumentOutOfRangeException(nameof(distance)); }

                                      Distance = distance;
                                      Domestic = domestic;
                                  }

                                  public int Distance { get; }

                                  public bool Domestic { get; }
                              }

                              public sealed class Shipment {
                                  public Shipment(Route route) {
                                      if (!route.Domestic) { throw new ArgumentException("A shipment route must be domestic.", nameof(route)); }

                                      Route = route;
                                  }

                                  public Route Route { get; }
                              }
                              """;

        string harness = $$"""
                           using System.Linq;

                           using JustDummies;

                           namespace Shop.Domain;

                           public static class Harness {
                               public static int[] Run() {
                                   using (Any.UseSeed({{Seed}})) {
                                       AnyShipment refined = new AnyShipment().WithRoute(r => r.WithDistance(240));

                                       return Enumerable.Range(0, 200).Select(_ => refined.Generate().Route.Distance).ToArray();
                                   }
                               }
                           }
                           """;

        string route    = Subject.ScaffoldByName("Route", domain).File!.SourceText;
        string shipment = Subject.ScaffoldByName("Shipment", domain).File!.SourceText;

        Check.That(shipment).Contains("return new AnyRoute();");

        string written = VerifySentinel.StrippedFrom(shipment).Replace("return new AnyRoute();", "return new AnyRoute().WithDomestic(true);");

        int[] drawn = (int[])Run([route, written, harness], domain);

        Check.That(drawn).ContainsOnlyElementsThatMatch(distance => distance == 240);
    }

    private static string Scaffolded(string domain) {
        return Subject.ScaffoldByName("Subject", domain).File!.SourceText;
    }

    private static object Run(string emitted, string domain, string harness) {
        return Run([emitted, harness], domain);
    }

    /// <summary>Compiles the files with the domain, loads the assembly, and returns what the harness returns.</summary>
    private static object Run(string[] files, string domain) {
        CSharpCompilation compilation = EmittedCodeCompiler.CompileAllWith(files, domain);

        Check.That(EmittedCodeCompiler.ErrorsIn(compilation)).IsEmpty();

        using MemoryStream image   = new();
        EmitResult         emitted = compilation.Emit(image);

        Check.That(emitted.Success).IsTrue();

        MethodInfo run = Assembly.Load(image.ToArray()).GetType("Shop.Domain.Harness")!.GetMethod("Run")!;

        return run.Invoke(obj: null, parameters: null)!;
    }

}
