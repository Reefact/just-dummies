using System.Linq;

using NFluent;

namespace JustDummies.GenAny.UnitTests;

/// <summary>
///     Which constructor <c>Generate()</c> calls (§5.1).
/// </summary>
public sealed class ConstructorChoiceTests {

    // The widest constructor states the type's whole shape; a narrower overload usually defaults something the
    // developer would rather see varied.
    [Fact(DisplayName = "The widest public constructor wins.")]
    public void TheWidestPublicConstructorWins() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int one) { }
                                                       public Subject(int one, string two) { }
                                                       public Subject(int one, string two, bool three) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one", "two", "three");
    }

    [Fact(DisplayName = "Two constructors of the same width are settled by source order.")]
    public void TwoConstructorsOfTheSameWidthAreSettledBySourceOrder() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int first) { }
                                                       public Subject(string second) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("first");
    }

    [Fact(DisplayName = "A non-public constructor never competes.")]
    public void ANonPublicConstructorNeverCompetes() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       internal Subject(int one, string two, bool three) { }
                                                       public Subject(int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    /// <summary>
    ///     A <c>ref</c> or <c>out</c> parameter makes a constructor ineligible rather than merely awkward:
    ///     <c>Generate()</c> passes value arguments, and such a call site does not compile (<c>CS1620</c>).
    /// </summary>
    [Fact(DisplayName = "A constructor taking ref or out is skipped for the next candidate.")]
    public void AConstructorTakingRefOrOutIsSkipped() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(int one, out string two) { two = ""; }
                                                       public Subject(int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    // `in` is fine: a value argument binds to it.
    [Fact(DisplayName = "A constructor taking in is eligible.")]
    public void AConstructorTakingInIsEligible() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(in int one) { }
                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
    }

    [Theory(DisplayName = "A type nothing can construct is refused, and says which.")]
    [InlineData("public sealed class Subject { private Subject(int one) { } }")]
    [InlineData("public sealed class Subject { public Subject(ref int one) { } }")]
    [InlineData("public static class Subject { }")]
    public void ATypeNothingCanConstructIsRefused(string declaration) {
        Check.That(Subject.Scaffold(declaration).Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
    }

    /// <summary>
    ///     Finding a constructor is not the same question as being able to call it.
    /// </summary>
    /// <remarks>
    ///     Each of these three declares a public constructor, so §5.1 chose one and a file was written that
    ///     said <c>1 of 1 parameters inferred</c> — then failed the developer's own build with <c>CS0144</c>,
    ///     <c>CS0246</c> and <c>CS9035</c> respectively. A refusal is the outcome ADR-0046 asks for, and for
    ///     the required-member row it is also what §16 deferring the feature has to mean.
    /// </remarks>
    [Theory(DisplayName = "A type the emitted file could not construct is refused before anything is written.")]
    [InlineData("public abstract class Subject { public Subject(int one) { } }",
                "Shop.Domain.Subject",
                ScaffoldStatus.TypeIsAbstract)]
    [InlineData("public sealed class Subject<TPayload> { public Subject(int one) { } }",
                "Shop.Domain.Subject`1",
                ScaffoldStatus.TypeIsGeneric)]
    [InlineData("public sealed class Subject { public required string Name { get; init; } public Subject(int one) { } }",
                "Shop.Domain.Subject",
                ScaffoldStatus.RequiredMembersUnset)]
    public void ATypeTheEmittedFileCouldNotConstructIsRefused(string declaration, string metadataName, ScaffoldStatus expected) {
        ScaffoldOutcome outcome = Subject.Scaffold(declaration, metadataName: metadataName);

        Check.That(outcome.Status).IsEqualTo(expected);
        Check.That(outcome.File).IsNull();
    }

    /// <summary>A required member the constructor does set is not a refusal — the bar is the call site.</summary>
    [Fact(DisplayName = "A constructor marked SetsRequiredMembers scaffolds despite the required member.")]
    public void AConstructorMarkedSetsRequiredMembersScaffolds() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using System.Diagnostics.CodeAnalysis;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       public required string Name { get; init; }

                                                       [SetsRequiredMembers]
                                                       public Subject(string name) {
                                                           Name = name;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(Names(outcome)).ContainsExactly("name");
    }

    /// <summary>A nested type cannot be named without its container's type argument either.</summary>
    [Fact(DisplayName = "A type nested in a generic one is refused as generic.")]
    public void ATypeNestedInAGenericOneIsRefusedAsGeneric() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Outer<TPayload> {
                                                       public sealed class Subject {
                                                           public Subject(int one) { }
                                                       }
                                                   }
                                                   """,
                                                   metadataName: "Shop.Domain.Outer`1+Subject");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.TypeIsGeneric);
    }

    // A record needs no special handling: its primary constructor is an ordinary public one, and the copy
    // constructor the compiler adds is protected, so it never competes.
    [Fact(DisplayName = "A positional record scaffolds from its primary constructor.")]
    public void APositionalRecordScaffoldsFromItsPrimaryConstructor() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed record Subject(string Street, string City);");

        Check.That(Names(outcome)).ContainsExactly("Street", "City");
    }

    /// <summary>
    ///     A parameterless constructor is not a failure: the generator it yields is still an
    ///     <c>IAny&lt;T&gt;</c>, so it composes into <c>Any.ListOf(…)</c> and <c>Any.Combine(…)</c>, which
    ///     <c>new Subject()</c> does not (§4.2).
    /// </summary>
    [Fact(DisplayName = "A parameterless constructor yields the degenerate generator.")]
    public void AParameterlessConstructorYieldsTheDegenerateGenerator() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { public Subject() { } }");

        Check.That(outcome.Succeeded).IsTrue();
        Check.That(outcome.Plan!.IsDegenerate).IsTrue();
        Check.That(outcome.File!.SourceText).Contains("return new Subject();");
    }

    /// <summary>
    ///     §5.1's second rule: no accessible constructor, one recognised factory — <c>Generate()</c> calls it.
    /// </summary>
    /// <remarks>
    ///     The canonical validating value object, and the factory's own guards are read like a constructor's:
    ///     the parameter comes back tightened, not merely present.
    /// </remarks>
    [Fact(DisplayName = "A type with no accessible constructor scaffolds through its factory.")]
    public void ATypeWithNoAccessibleConstructorScaffoldsThroughItsFactory() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private Subject(string value) { }

                                                       public static Subject Create(string value) {
                                                           if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException(nameof(value)); }

                                                           return new Subject(value);
                                                       }

                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("value");

        ScaffoldPlan plan = outcome.Plan!;

        Check.That(plan.Factory).IsEqualTo("Subject.Create");
        Check.That(plan.Parameters.Single().Expression).IsEqualTo("Any.String().NonEmpty()");
        Check.That(outcome.File!.SourceText).Contains("return Subject.Create(");
    }

    /// <summary>
    ///     The private constructor a factory delegates to can carry a guard the factory's own body does
    ///     not restate — here, a floor the factory's <c>IsNullOrWhiteSpace</c> check does not cover — and it
    ///     folds onto the factory's own parameter exactly as a second guard in the same body already would.
    /// </summary>
    [Fact(DisplayName = "The guards of the constructor a factory delegates to fold onto its own parameter.")]
    public void TheGuardsOfTheConstructorAFactoryDelegatesToFoldOntoItsOwnParameter() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly string value;

                                                       private Subject(string value) {
                                                           if (value.Length < 8) { throw new ArgumentException("too short", nameof(value)); }

                                                           this.value = value;
                                                       }

                                                       public static Subject Create(string value) {
                                                           if (string.IsNullOrWhiteSpace(value)) { throw new ArgumentException("blank", nameof(value)); }

                                                           return new Subject(value);
                                                       }

                                                   }
                                                   """);

        ScaffoldPlan plan = outcome.Plan!;

        Check.That(plan.Factory).IsEqualTo("Subject.Create");
        Check.That(plan.Parameters.Single().Expression).IsEqualTo("Any.String().WithMinLength(8)");
        Check.That(plan.Parameters.Single().Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A computed argument hands the delegated constructor a value the factory's own parameter never
    ///     draws, so its guard is never folded — the same subject-identity discipline every row of §5.3
    ///     already keeps, applied one hop into the constructor a factory delegates to.
    /// </summary>
    [Fact(DisplayName = "A computed argument to the delegated constructor does not fold its guard.")]
    public void AComputedArgumentToTheDelegatedConstructorDoesNotFoldItsGuard() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int value;

                                                       private Subject(int value) {
                                                           if (value <= 0) { throw new ArgumentOutOfRangeException(nameof(value)); }

                                                           this.value = value;
                                                       }

                                                       public static Subject Create(int value) {
                                                           return new Subject(value + 1);
                                                       }

                                                   }
                                                   """);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters.Single();

        Check.That(parameter.Expression).IsEqualTo("Any.Int32()");
        Check.That(parameter.Provenance.HasFlag(Provenance.Guard)).IsFalse();
    }

    /// <summary>
    ///     <c>CS0144</c> is about <c>new</c>, which a factory call site never writes — so the abstract refusal
    ///     of §5.1.6 does not reach a type built through its own factory.
    /// </summary>
    [Fact(DisplayName = "An abstract type with a factory scaffolds rather than being refused.")]
    public void AnAbstractTypeWithAFactoryScaffolds() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public abstract class Subject {

                                                       private sealed class Concrete : Subject {
                                                           public Concrete(int one) { }
                                                       }

                                                       public static Subject Create(int one) {
                                                           return new Concrete(one);
                                                       }

                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("one");
        Check.That(outcome.Plan!.Factory).IsEqualTo("Subject.Create");
    }

    // The bar is the call site (§5.1.4), and a factory call site asks for no required member: setting them is
    // the factory's own business, checked in the factory's own body by the developer's compiler.
    [Fact(DisplayName = "A required member does not refuse a type built through its factory.")]
    public void ARequiredMemberDoesNotRefuseAFactoryBuiltType() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   using System.Diagnostics.CodeAnalysis;

                                                   namespace Shop.Domain;

                                                   public sealed class Subject {

                                                       public required string Name { get; init; }

                                                       [SetsRequiredMembers]
                                                       private Subject(string name) {
                                                           Name = name;
                                                       }

                                                       public static Subject Create(string name) {
                                                           return new Subject(name);
                                                       }

                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("name");
        Check.That(outcome.Plan!.Factory).IsEqualTo("Subject.Create");
    }

    /// <summary>
    ///     §5.4's tie rule, applied to the target itself: <c>Create</c> wins, and where several still remain
    ///     nothing is picked on the developer's behalf.
    /// </summary>
    [Fact(DisplayName = "Two factories neither named Create refuse rather than guess.")]
    public void TwoFactoriesNeitherNamedCreateRefuse() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       private Subject(string value) { }
                                                       public static Subject From(string value) { return new Subject(value); }
                                                       public static Subject Parse(string value) { return new Subject(value); }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
    }

    /// <summary>
    ///     §5.1.2 gates on <b>no accessible constructor</b>, and §5.1.5 says how an ineligible public one
    ///     ends: unresolved — not routed around the surface the type itself declares.
    /// </summary>
    [Fact(DisplayName = "A public but ineligible constructor is not routed around through a factory.")]
    public void APublicButIneligibleConstructorIsNotRoutedAroundThroughAFactory() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       public Subject(ref int one) { }
                                                       public static Subject Create(int one) { return new Subject(ref one); }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
    }

    /// <summary>
    ///     §5.1's second rule applies to a <c>struct</c> exactly as it does to a <c>class</c> — a private
    ///     constructor behind a public <c>Create</c> is constructible through the factory either way.
    /// </summary>
    /// <remarks>
    ///     A <c>struct</c> always carries a compiler-synthesized public parameterless constructor, which
    ///     <see cref="Scaffolder.ChosenConstructor" /> must not mistake for one the developer wrote: that
    ///     constructor bypasses the private constructor's own guard and the factory's alike, zero-initializing
    ///     every field with nothing in the recap to say so.
    /// </remarks>
    [Fact(DisplayName = "A readonly struct with no accessible constructor scaffolds through its factory too.")]
    public void AReadonlyStructWithNoAccessibleConstructorScaffoldsThroughItsFactoryToo() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public readonly struct Subject {

                                                       private readonly decimal amount;

                                                       private Subject(decimal amount) { this.amount = amount; }

                                                       public static Subject Create(decimal amount) {
                                                           if (amount <= 0) { throw new ArgumentOutOfRangeException(nameof(amount)); }

                                                           return new Subject(amount);
                                                       }

                                                   }
                                                   """);

        Check.That(Names(outcome)).ContainsExactly("amount");

        ScaffoldPlan plan = outcome.Plan!;

        Check.That(plan.Factory).IsEqualTo("Subject.Create");
        Check.That(plan.Parameters.Single().Expression).IsEqualTo("Any.Decimal().Positive()");
        Check.That(outcome.File!.SourceText).Contains("return Subject.Create(");
    }

    private static string[] Names(ScaffoldOutcome outcome) {
        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);

        return outcome.Plan!.Parameters.Select(parameter => parameter.Name).ToArray();
    }

}
