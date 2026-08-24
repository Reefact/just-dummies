using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     How a type the base table has no row for is drawn anyway (§5.4).
/// </summary>
public sealed class CompositionTests {

    /// <summary>
    ///     A scaffolded generator wins, and that is how aggregates compose in cascade: scaffold
    ///     <c>Customer</c>, re-run <c>--force</c> on <c>Order</c>, and the open parameter closes.
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

    [Theory(DisplayName = "A one-parameter static factory is recognised by name.")]
    [InlineData("Create")]
    [InlineData("From")]
    [InlineData("Of")]
    [InlineData("Parse")]
    public void AOneParameterStaticFactoryIsRecognisedByName(string factory) {
        ScaffoldedParameter parameter = Composed($$"""
                                                  public sealed class Email {
                                                      public static Email {{factory}}(string value) { return new Email(); }
                                                  }
                                                  """,
                                                  "Email");

        Check.That(parameter.Expression).IsEqualTo($"Any.String().NonEmpty().As(Email.{factory})");
        Check.That(parameter.Provenance.HasFlag(Provenance.Factory)).IsTrue();
    }

    /// <summary>
    ///     Guard reading is what makes factory composition correct rather than nominally present.
    /// </summary>
    /// <remarks>
    ///     <c>OrderReference.Create</c> guards on <c>IsNullOrWhiteSpace</c>, so the emitted chain is
    ///     <c>Any.String().NonEmpty().As(OrderReference.Create)</c>. Without the guard it would be
    ///     <c>Any.String().As(OrderReference.Create)</c> — measured throwing <c>AnyGenerationException</c> 594
    ///     times in 10 000 draws, about one in seventeen, which is what an unconstrained draw over the
    ///     seventeen lengths 0 to 16 predicts.
    /// </remarks>
    [Fact(DisplayName = "A factory's own guards tighten the generator for its parameter.")]
    public void AFactorysOwnGuardsTightenItsParameter() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class OrderReference {

                                                     public static OrderReference Create(string value) {
                                                         if (string.IsNullOrWhiteSpace(value)) {
                                                             throw new ArgumentException(nameof(value));
                                                         }

                                                         if (value.Length != 12) { throw new ArgumentException(nameof(value)); }

                                                         return new OrderReference();
                                                     }

                                                 }
                                                 """,
                                                 "OrderReference");

        Check.That(parameter.Expression).IsEqualTo("Any.String().WithLength(12).As(OrderReference.Create)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Factory)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
    }

    /// <summary>
    ///     A factory that constructs through a guarded private constructor — <c>return new
    ///     Coupon(number);</c> — hands the factory's own parameter to that constructor unchanged, so the
    ///     constructor's guard tightens the composed parameter exactly as the factory's own guard would.
    /// </summary>
    [Fact(DisplayName = "A factory composed over a guarded private constructor folds its guard too.")]
    public void AFactoryComposedOverAGuardedPrivateConstructorFoldsItsGuardToo() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Coupon {

                                                     private readonly int number;

                                                     private Coupon(int number) {
                                                         if (number <= 0) { throw new ArgumentOutOfRangeException(nameof(number)); }

                                                         this.number = number;
                                                     }

                                                     public static Coupon Create(int number) { return new Coupon(number); }

                                                 }
                                                 """,
                                                 "Coupon");

        Check.That(parameter.Expression).IsEqualTo("Any.Int32().Positive().As(Coupon.Create)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Factory)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     The reported case, pinned: a bounded reference, spelled as the interval it is and nothing more.
    /// </summary>
    /// <remarks>
    ///     Three guards, and the chain deduced from them used to carry two faults the tool's own package
    ///     reports. <c>WithMinLength(8).WithMaxLength(20)</c> is the pair <c>JD031</c> names, so the scaffolded
    ///     file was marked on its first run, before its author had touched it. And the <c>NonEmpty</c> read
    ///     from <c>IsNullOrWhiteSpace</c> narrowed nothing beside a floor of eight, which absorbs it — one
    ///     invariant stated twice.
    ///     <para>
    ///         Both come from the same absence, which is why one change answers both: the engine wrote whatever
    ///         survived combination without ever asking what the finished chain said.
    ///     </para>
    /// </remarks>
    [Fact(DisplayName = "A bounded factory parameter is emitted as the range it is, once.")]
    public void ABoundedFactoryParameterIsEmittedAsTheRange() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class OrderReference {

                                                     public static OrderReference Create(string value) {
                                                         if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }
                                                         if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                                                         if (value.Length > 20) { throw new ArgumentException(nameof(value)); }

                                                         return new OrderReference();
                                                     }

                                                 }
                                                 """,
                                                 "OrderReference");

        Check.That(parameter.Expression).IsEqualTo("Any.String().WithLengthBetween(8, 20).As(OrderReference.Create)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsFalse();
    }

    /// <summary>
    ///     §6 words the column <c>tightened</c>, so <c>guard</c> is computed from the constraints applied,
    ///     never from those read — on the factory path exactly as on the constructor's.
    /// </summary>
    /// <remarks>
    ///     Two factory guards that admit no value are read correctly and tighten nothing: the bounding
    ///     constraints are all dropped and the recap says so. Reporting <c>guard</c> beside
    ///     <c>guards not combined</c> claimed a tightening the chain does not carry — the flag came from the
    ///     reading, where every other path computes it from the writing.
    /// </remarks>
    [Fact(DisplayName = "A factory whose guards admit no value reports the drop, not a tightening.")]
    public void AFactoryWhoseGuardsAdmitNoValueReportsTheDropNotATightening() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class OrderReference {

                                                     public static OrderReference Create(string value) {
                                                         if (value.Length < 8) { throw new ArgumentException(nameof(value)); }
                                                         if (value.Length > 5) { throw new ArgumentException(nameof(value)); }

                                                         return new OrderReference();
                                                     }

                                                 }
                                                 """,
                                                 "OrderReference");

        Check.That(parameter.Expression).IsEqualTo("Any.String().As(OrderReference.Create)");
        Check.That(parameter.Provenance.HasFlag(Provenance.Factory)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.GuardsNotCombined)).IsTrue();
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsFalse();
    }

    [Fact(DisplayName = "Create wins where several factories qualify.")]
    public void CreateWinsWhereSeveralQualify() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Of(string value) { return new Email(); }
                                                     public static Email Create(string value) { return new Email(); }
                                                 }
                                                 """,
                                                 "Email");

        Check.That(parameter.Expression).IsEqualTo("Any.String().NonEmpty().As(Email.Create)");
    }

    // Where several remain the parameter is left open rather than guessed at: which one the developer meant is
    // theirs to say.
    [Fact(DisplayName = "Several qualifying factories and no Create leaves the parameter open.")]
    public void SeveralQualifyingFactoriesLeaveTheParameterOpen() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Of(string value) { return new Email(); }
                                                     public static Email From(string value) { return new Email(); }
                                                 }
                                                 """,
                                                 "Email");

        Check.That(parameter.IsUnresolved).IsTrue();
    }

    [Theory(DisplayName = "A method that is not a one-parameter conversion does not qualify.")]
    [InlineData("public static Email Create(string value, bool checked_) { return new Email(); }")]
    [InlineData("public static string Create(string value) { return value; }")]
    [InlineData("public Email Create(string value) { return new Email(); }")]
    [InlineData("internal static Email Create(string value) { return new Email(); }")]
    [InlineData("public static Email Build(string value) { return new Email(); }")]
    public void AMethodThatIsNotAOneParameterConversionDoesNotQualify(string method) {
        ScaffoldedParameter parameter = Composed($$"""
                                                  public sealed class Email {
                                                      {{method}}
                                                  }
                                                  """,
                                                  "Email");

        Check.That(parameter.IsUnresolved).IsTrue();
    }

    // The guard §5.2 asks for, now that composition is what can make a type reach itself.
    [Fact(DisplayName = "A factory taking its own type does not send the engine round in circles.")]
    public void AFactoryTakingItsOwnTypeIsNotFollowedForever() {
        ScaffoldedParameter parameter = Composed("""
                                                 public sealed class Email {
                                                     public static Email Create(Email value) { return value; }
                                                 }
                                                 """,
                                                 "Email");

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

        Check.That(parameter.Expression).IsEqualTo("Any.ListOf(Any.String().NonEmpty().As(Email.Create))");
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
