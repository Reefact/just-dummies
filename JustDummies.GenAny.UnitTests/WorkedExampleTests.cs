using System.Linq;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     The worked example of §4.1, produced end to end.
/// </summary>
/// <remarks>
///     The specification writes that example out in full — the source under analysis, and the file
///     <c>dum generate Order</c> emits from it — and says it is not a sketch: it was compiled and run against
///     the real library. This runs the engine over that same source and compares the result against the
///     approved file, byte for byte.
///     <para>
///         It is the one test that can fail for a reason none of the others would catch: every part being right
///         and the whole being wrong. The base table, the guards, the composition, the naming, the usings and
///         the emitter each have their own suite; only this says they agree.
///     </para>
/// </remarks>
public sealed class WorkedExampleTests {

    /// <summary>
    ///     §4.1's source, minus the two types the fixture domain already declares — <c>Customer</c> and
    ///     <c>OrderStatus</c> — which are exactly the ones it declares.
    /// </summary>
    private const string Source = """
                                  namespace Shop.Domain;

                                  using System;
                                  using System.Collections.Generic;

                                  using JustDummies;

                                  public sealed class Order {

                                      public Order(OrderReference reference, Customer customer, int quantity,
                                                   OrderStatus status, IReadOnlyList<string> tags, DateTime placedAt) {
                                          if (reference is null) { throw new ArgumentNullException(nameof(reference)); }
                                          if (quantity <= 0) { throw new ArgumentOutOfRangeException(nameof(quantity)); }
                                      }

                                  }

                                  public sealed class OrderReference {

                                      public static OrderReference Create(string value) {
                                          if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }

                                          return new OrderReference();
                                      }

                                  }

                                  // Already scaffolded, which is why the example composes it rather than leaving it open.
                                  public sealed class AnyCustomer : IAny<Customer> {
                                      public Customer Generate() { return new Customer("name"); }
                                  }
                                  """;

    [Fact(DisplayName = "The worked example resolves exactly as §4.1 writes it.")]
    public void TheWorkedExampleResolvesAsWritten() {
        ScaffoldPlan plan = Scaffolded();

        Check.That(plan.Parameters.Select(parameter => parameter.Expression))
             .ContainsExactly("Any.String().NonEmpty().As(OrderReference.Create)",
                              "new AnyCustomer()",
                              "Any.Int32().Positive()",
                              "Any.Enum<OrderStatus>()",
                              "Any.ListOf(Any.String().NonEmpty())",
                              "Any.DateTime()");
    }

    // §6's own worked recap: `reference` is `factory, guard` — composed through OrderReference.Create, and
    // tightened by the guard inside that factory's body. `quantity` is `guard`; `status` is neither.
    [Fact(DisplayName = "The worked example reports where each expression came from.")]
    public void TheWorkedExampleReportsItsProvenance() {
        ScaffoldPlan plan = Scaffolded();

        Check.That(plan.Parameters[0].Provenance).IsEqualTo(Provenance.Factory | Provenance.Guard);
        Check.That(plan.Parameters[1].Provenance).IsEqualTo(Provenance.Scaffolded);
        Check.That(plan.Parameters[2].Provenance).IsEqualTo(Provenance.Guard);
        Check.That(plan.Parameters[3].Provenance).IsEqualTo(Provenance.None);
    }

    /// <summary>
    ///     And the file it produces is the approved one, to the byte.
    /// </summary>
    /// <remarks>
    ///     That approved file was written from a plan built by hand, when the emitter was all there was. It is
    ///     the same file now that a real compilation produces it, which is the strongest thing this suite can
    ///     say: the halves were specified separately and they meet.
    /// </remarks>
    [Fact(DisplayName = "The file it emits is the approved AnyOrder, byte for byte.")]
    public void TheFileItEmitsIsTheApprovedOne() {
        ScaffoldOutcome outcome = Scaffold();

        Check.That(outcome.File!.SourceText).IsEqualTo(GoldenFile.ApprovedTextOf("AnyOrder"));
    }

    private static ScaffoldPlan Scaffolded() {
        return Scaffold().Plan!;
    }

    private static ScaffoldOutcome Scaffold() {
        ScaffoldOutcome outcome = Subject.Scaffold(Source, metadataName: "Shop.Domain.Order");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome;
    }

}
