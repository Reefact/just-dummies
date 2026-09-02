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

    /// <summary>
    ///     The same three questions, asked of a type that declares no public constructor at all.
    /// </summary>
    /// <remarks>
    ///     Which is the ordinary shape of an abstract type — its constructors are <c>protected</c> — and one
    ///     of the two commonest shapes of a generic one. The theory above only ever reached
    ///     <c>TypeIsAbstract</c> and <c>TypeIsGeneric</c> through a public constructor, so §5.1.6's refusals
    ///     sat behind a question §5.1.1 answers first, and every such type heard "Generate() needs a public
    ///     instance constructor" instead. The remedy that answer sends the developer to is the wrong one:
    ///     adding a public constructor to an abstract type changes nothing about instantiating it. Measured
    ///     over seven repositories, 12 of 55 <c>NoEligibleConstructor</c> refusals were really one of these
    ///     two (<c>audit/2026-09-02-dum-first-field-measurement.md</c>).
    /// </remarks>
    [Theory(DisplayName = "A type refused for what it is says so, even when it also has no constructor to choose.")]
    [InlineData("public abstract class Subject { protected Subject(int one) { } }",
                "Shop.Domain.Subject",
                ScaffoldStatus.TypeIsAbstract)]
    [InlineData("public abstract class Subject { }",
                "Shop.Domain.Subject",
                ScaffoldStatus.TypeIsAbstract)]
    [InlineData("public sealed class Subject<TPayload> { private Subject(int one) { } }",
                "Shop.Domain.Subject`1",
                ScaffoldStatus.TypeIsGeneric)]
    public void ATypeRefusedForWhatItIsSaysSoWithoutAConstructorToChoose(string declaration,
                                                                         string metadataName,
                                                                         ScaffoldStatus expected) {
        ScaffoldOutcome outcome = Subject.Scaffold(declaration, metadataName: metadataName);

        Check.That(outcome.Status).IsEqualTo(expected);
        Check.That(outcome.File).IsNull();
    }

    /// <summary>
    ///     And an abstract type reached through a factory is still scaffolded, which is why the abstract
    ///     refusal asks about the factory rather than about the constructor.
    /// </summary>
    /// <remarks>
    ///     <c>CS0144</c> is about <c>new</c>, and a factory call site never writes one: <c>T.Create(…)</c>
    ///     compiles and returns whatever concrete type the author decided on. Refusing here would take a
    ///     working generator away from the one design — a private constructor behind a public factory —
    ///     that §5.1.2 exists to serve.
    /// </remarks>
    [Fact(DisplayName = "An abstract type behind a recognised factory scaffolds through it.")]
    public void AnAbstractTypeBehindARecognisedFactoryScaffolds() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public abstract class Subject {

                                                       protected Subject(string name) { }

                                                       public static Subject Create(string name) {
                                                           return new Concrete(name);
                                                       }

                                                   }

                                                   public sealed class Concrete : Subject {
                                                       public Concrete(string name) : base(name) { }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.Scaffolded);
        Check.That(outcome.Plan!.Factory).IsEqualTo("Subject.Create");
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
        Check.That(plan.Parameters.Single().Expression).IsEqualTo("Any.String().NotBlank()");
        Check.That(outcome.File!.SourceText).Contains("return Subject.Create(");
    }

    /// <summary>
    ///     The private constructor a factory delegates to can carry a guard the factory's own body does
    ///     not restate — here, a floor the factory's <c>IsNullOrWhiteSpace</c> check does not cover — and it
    ///     folds onto the factory's own parameter exactly as a second guard in the same body already would.
    /// </summary>
    /// <remarks>
    ///     Both survive rather than one absorbing the other: the floor is the tighter of the two lengths, and
    ///     <c>NotBlank</c> carries a refusal it says nothing about — eight characters may every one be a space.
    /// </remarks>
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
        Check.That(plan.Parameters.Single().Expression).IsEqualTo("Any.String().WithMinLength(8).NotBlank()");
        Check.That(plan.Parameters.Single().Provenance.HasFlag(Provenance.UnreadGuards)).IsFalse();
    }

    /// <summary>
    ///     A guard the delegated constructor could not read is a guard over the outer parameter too, so the
    ///     doubt folds across the hop exactly as the constraints do.
    /// </summary>
    /// <remarks>
    ///     The half the fold's first draft left behind. Read directly this body earns the verification mark;
    ///     read through <c>: this(value, false)</c> it reported <c>guard</c> with nothing to verify, over a
    ///     domain that rejects the draw — the defect class §5.3 exists to prevent, reached by way of the fix
    ///     for a different one.
    /// </remarks>
    [Fact(DisplayName = "A guard the delegated constructor could not read marks the parameter that hands it over.")]
    public void AnUnreadDelegatedGuardMarksTheHandingParameter() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly string value;

                                                       private Subject(string value, bool trusted) {
                                                           if (!value.StartsWith("REF-", StringComparison.Ordinal)) { throw new ArgumentException(nameof(value)); }
                                                           if (value.Length < 8) { throw new ArgumentException(nameof(value)); }

                                                           this.value = value;
                                                       }

                                                       public Subject(string value) : this(value, false) { }

                                                   }
                                                   """);

        ScaffoldedParameter parameter = outcome.Plan!.Parameters.Single();

        Check.That(parameter.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(parameter.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     A <c>params</c> hand-off reads in NORMAL form and is refused in EXPANDED form, and the two are
    ///     told apart by how the compiler bound the call rather than by guesswork.
    /// </summary>
    /// <remarks>
    ///     Pinned as a pair, because the first attempt at this refused both: an expanded call fills one
    ///     ELEMENT of the array, so a guard about the array's length is not about the argument, while a
    ///     normal-form call hands over the array itself and the guard is exactly about it. Measured, refusing
    ///     both cost a guard the engine reads correctly about the value the generator actually draws.
    /// </remarks>
    [Fact(DisplayName = "A params hand-off reads in normal form and is refused in expanded form.")]
    public void AParamsHandoffReadsInNormalFormOnly() {
        ScaffoldedParameter normal = Subject.Scaffold("""
                                                      public sealed class Subject {

                                                          private readonly string[] kept;

                                                          private Subject(params string[] names) {
                                                              if (names.Length < 4) { throw new ArgumentException("few", nameof(names)); }

                                                              kept = names;
                                                          }

                                                          public static Subject Of(string[] names) {
                                                              return new Subject(names);
                                                          }
                                                      }
                                                      """).Plan!.Parameters.Single();

        Check.That(normal.Expression).IsEqualTo("Any.ArrayOf(Any.String().NonEmpty()).WithMinCount(4)");
        Check.That(normal.RequiresVerification).IsFalse();

        ScaffoldedParameter expanded = Subject.Scaffold("""
                                                        public sealed class Subject {

                                                            private readonly string[] kept;

                                                            private Subject(params string[] names) {
                                                                if (names.Length < 4) { throw new ArgumentException("few", nameof(names)); }

                                                                kept = names;
                                                            }

                                                            public static Subject Of(string name) {
                                                                return new Subject(name);
                                                            }
                                                        }
                                                        """).Plan!.Parameters.Single();

        Check.That(expanded.Provenance.HasFlag(Provenance.UnreadGuards)).IsTrue();
        Check.That(expanded.RequiresVerification).IsTrue();
    }

    /// <summary>
    ///     A null-forgiving hand-off carries the same value, so the delegated guard folds through it.
    /// </summary>
    /// <remarks>
    ///     <c>!</c> is a compile-time annotation with no run-time effect, so <c>this(value!, false)</c> hands
    ///     over exactly what <c>value</c> holds. A cast is deliberately not unwrapped the same way: it can
    ///     hand over a different number.
    /// </remarks>
    [Fact(DisplayName = "A null-forgiving hand-off folds the delegated guard rather than losing it.")]
    public void ANullForgivingHandoffFolds() {
        ScaffoldedParameter parameter = Subject.Scaffold("""
                                                         public sealed class Subject {

                                                             private readonly string value;

                                                             private Subject(string value, bool _) {
                                                                 if (value.Length < 8) { throw new ArgumentException(nameof(value)); }

                                                                 this.value = value;
                                                             }

                                                             public Subject(string? value) : this(value!, false) { }

                                                         }
                                                         """).Plan!.Parameters.Single();

        Check.That(parameter.Expression).IsEqualTo("Any.String().WithMinLength(8)");
        Check.That(parameter.RequiresVerification).IsFalse();
    }

    /// <summary>
    ///     An initializer that delegates to its own constructor is read once and abandoned, rather than
    ///     followed forever.
    /// </summary>
    /// <remarks>
    ///     <c>CS0516</c> forbids this, and the fold's first draft leaned on that to skip a cycle guard — but
    ///     the compiler's refusal only covers source that COMPILES, and the engine reads whatever the
    ///     developer currently has open. Measured before the guard: the process died with a stack overflow
    ///     rather than scaffolding anything, which no <c>unread guards</c> mark can soften. The assertion is
    ///     simply that this returns.
    /// </remarks>
    [Fact(DisplayName = "An initializer that delegates to its own constructor terminates.")]
    public void AnInitializerThatDelegatesToItselfTerminates() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {

                                                       private readonly int a;

                                                       public Subject(int a, int b) : this(a, b) {
                                                           this.a = a;
                                                       }

                                                   }
                                                   """);

        Check.That(outcome).IsNotNull();
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
    ///     §5.1.2's tie rule: <c>Create</c> wins, and where several still remain nothing is picked on the
    ///     developer's behalf — but the refusal carries the ones that tied, so it can say what it was
    ///     between.
    /// </summary>
    [Fact(DisplayName = "Two factories neither named Create refuse rather than guess, and name both.")]
    public void TwoFactoriesNeitherNamedCreateRefuse() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       private Subject(string value) { }
                                                       public static Subject From(string value) { return new Subject(value); }
                                                       public static Subject Parse(string value) { return new Subject(value); }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
        Check.That(outcome.Candidates).ContainsExactly("Shop.Domain.Subject.From(string)", "Shop.Domain.Subject.Parse(string)");
    }

    /// <summary>
    ///     A factory the engine would never choose is not a remedy, and is not offered as one.
    /// </summary>
    /// <remarks>
    ///     §5.1.2 gates on <b>no accessible constructor</b>, so a type declaring a public one whose
    ///     parameters are <c>ref</c> keeps the refusal §5.1.5 gives it: unresolved, rather than routed around
    ///     the surface it declared. Naming its factories under that refusal would advertise a door the engine
    ///     holds shut — and with two of them the sentence would say to leave one, which changes nothing at
    ///     all, since the constructor closes the route either way.
    /// </remarks>
    [Theory(DisplayName = "A factory behind a public but ineligible constructor is not offered as a way out.")]
    [InlineData("""
                public sealed class Subject {
                    public Subject(ref int one) { }
                    public static Subject From(string value) { return null!; }
                }
                """)]
    [InlineData("""
                public sealed class Subject {
                    public Subject(ref int one) { }
                    public static Subject From(string value) { return null!; }
                    public static Subject Parse(string value) { return null!; }
                }
                """)]
    public void AFactoryBehindAPublicButIneligibleConstructorIsNotOffered(string declaration) {
        ScaffoldOutcome outcome = Subject.Scaffold(declaration);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
        Check.That(outcome.Candidates).IsEmpty();
    }

    /// <summary>
    ///     Where the tie is what stops an abstract type, the tie is what the refusal names.
    /// </summary>
    /// <remarks>
    ///     Abstractness would be the wrong answer here and a demonstrably wrong one: deleting either factory
    ///     leaves the same abstract type scaffolding through the other. So the refusal a developer can act on
    ///     is the ambiguity, and reporting <c>TypeIsAbstract</c> would send them to write a derived type they
    ///     do not need.
    /// </remarks>
    [Fact(DisplayName = "An abstract type stopped by tied factories is refused for the tie, not for being abstract.")]
    public void AnAbstractTypeStoppedByTiedFactoriesIsRefusedForTheTie() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public abstract class Subject {
                                                       protected Subject(string value) { }
                                                       public static Subject From(string value) { return null!; }
                                                       public static Subject Parse(string value) { return null!; }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
        Check.That(outcome.Candidates).ContainsExactly("Shop.Domain.Subject.From(string)", "Shop.Domain.Subject.Parse(string)");
    }

    /// <summary>
    ///     A name §5.1.2 already ranks below <c>Create</c> is not part of the tie <c>Create</c> could not
    ///     settle.
    /// </summary>
    /// <remarks>
    ///     The preference is a rule, not a hint: with two <c>Create</c> overloads beside a <c>From</c>, the
    ///     question the developer has to answer is which <c>Create</c>, and naming <c>From</c> under a
    ///     sentence about what the tool "does not pick between" would contradict the rule the same paragraph
    ///     states. It stopped being invisible when the set became the refusal's own candidates.
    /// </remarks>
    [Fact(DisplayName = "A tie among Create overloads names only them, not the lower-ranked names beside them.")]
    public void ATieAmongCreateOverloadsNamesOnlyThem() {
        ScaffoldOutcome outcome = Subject.Scaffold("""
                                                   public sealed class Subject {
                                                       private Subject() { }
                                                       public static Subject Create(string value) { return null!; }
                                                       public static Subject Create(int value) { return null!; }
                                                       public static Subject From(string value) { return null!; }
                                                   }
                                                   """);

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
        Check.That(outcome.Candidates).ContainsExactly("Shop.Domain.Subject.Create(int)", "Shop.Domain.Subject.Create(string)");
    }

    /// <summary>A type with nothing to call at all carries no candidate, so the sentence stays the short one.</summary>
    [Fact(DisplayName = "A type with no factory at all is refused with nothing to list.")]
    public void ATypeWithNoFactoryAtAllIsRefusedWithNothingToList() {
        ScaffoldOutcome outcome = Subject.Scaffold("public sealed class Subject { private Subject(int one) { } }");

        Check.That(outcome.Status).IsEqualTo(ScaffoldStatus.NoEligibleConstructor);
        Check.That(outcome.Candidates).IsEmpty();
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
