#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

[TestSubject(typeof(DummyExtensions))]
public sealed class CompositionTests {

    #region Statics members declarations

    private static T Materialize<T>(IDummy<T> generator) {
        return generator.Generate();
    }

    #endregion

    [Fact(DisplayName = "As bridges a constrained string to a value object through its own factory.")]
    public void AsBuildsAStringValueObject() {
        IDummy<OrderReference> generator = Dummy.String()
                                            .StartingWith("ORD-")
                                            .WithLength(12)
                                            .As(OrderReference.Create);

        OrderReference reference = generator.Generate();

        Check.That(reference.Value).StartsWith("ORD-");
        Check.That(reference.Value.Length).IsEqualTo(12);
    }

    [Fact(DisplayName = "As bridges a constrained integer to a value object through its own factory.")]
    public void AsBuildsANumericValueObject() {
        IDummy<Percentage> generator = Dummy.Int32().Between(0, 100).As(Percentage.Create);

        Percentage percentage = generator.Generate();

        Check.That(percentage.Value).IsGreaterOrEqualThan(0);
        Check.That(percentage.Value).IsLessOrEqualThan(100);
    }

    [Fact(DisplayName = "A factory rejecting the generated value surfaces as DummyGenerationException naming the value and the seed.")]
    public void AsWrapsFactoryFailures() {
        IDummy<Percentage> tooWeaklyConstrained = Dummy.Int32().Between(200, 300).As(Percentage.Create);

        DummyGenerationException? caught = null;
        Check.ThatCode(() => Dummy.Reproducibly(9876, () => {
                try {
                    tooWeaklyConstrained.Generate();
                } catch (DummyGenerationException exception) {
                    caught = exception;

                    throw;
                }
            }, _ => { }))
             .Throws<DummyGenerationException>();

        Check.That(caught).IsNotNull();
        Check.That(caught!.Seed).IsEqualTo(9876);
        Check.That(caught.Message).Contains("As(...)");
        Check.That(caught.Message).Contains("9876");
        Check.That(caught.InnerException).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Combine assembles two constrained parts through a constructor lambda.")]
    public void CombineAssemblesTwoParts() {
        IDummy<Customer> generator = Dummy.Combine(
            Dummy.String().NonEmpty().WithMaxLength(50),
            Dummy.String().StartingWith("ORD-").WithLength(12),
            (name, reference) => new Customer(name, OrderReference.Create(reference)));

        Customer customer = generator.Generate();

        Check.That(customer.Name).IsNotEmpty();
        Check.That(customer.LastOrder.Value).StartsWith("ORD-");
    }

    [Fact(DisplayName = "Combine assembles three parts through a constructor lambda.")]
    public void CombineAssemblesThreeParts() {
        IDummy<string> generator = Dummy.Combine(
            Dummy.String().WithLength(2).InUpperCase(),
            Dummy.Int32().Between(10, 99),
            Dummy.String().WithLength(2).InLowerCase(),
            (head, middle, tail) => $"{head}{middle}{tail}");

        string value = generator.Generate();

        Check.That(value.Length).IsEqualTo(6);
    }

    [Fact(DisplayName = "A composer failure surfaces as DummyGenerationException naming the generated values.")]
    public void CombineWrapsComposerFailures() {
        IDummy<string> generator = Dummy.Combine<int, int, string>(
            Dummy.Int32().Between(1, 3),
            Dummy.Int32().Between(4, 6),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        DummyGenerationException caught = Assert.Throws<DummyGenerationException>(() => generator.Generate());

        Check.That(caught.Message).Contains("Combine(...)");
        Check.That(caught.InnerException).IsInstanceOf<InvalidOperationException>();
    }

    [Fact(DisplayName = "A composer failure over ambient generators reports the Dummy.Reproducibly replay hint.")]
    public void CombineOverAmbientGeneratorsReportsReproduciblyHint() {
        IDummy<string> generator = Dummy.Combine<int, int, string>(
            Dummy.Int32().Between(1, 3),
            Dummy.Int32().Between(4, 6),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        Check.ThatCode(() => Dummy.Reproducibly(31415, () => generator.Generate(), _ => { }))
             .Throws<DummyGenerationException>()
             .WithProperty(caught => caught.Seed, 31415)
             .And.WhichMember(caught => caught.Message)
             .Contains("Dummy.Reproducibly(31415")
             .And.Not.Contains("Dummy.WithSeed(");
    }

    [Fact(DisplayName = "A composer failure over an Dummy.WithSeed(...) context reports the WithSeed replay hint, not the inapplicable Dummy.Reproducibly instruction.")]
    public void CombineOverFixedContextReportsWithSeedHint() {
        DummyContext seeded = Dummy.WithSeed(4242);

        IDummy<string> generator = Dummy.Combine<int, int, string>(
            seeded.Int32().Between(1, 3),
            seeded.Int32().Between(4, 6),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        Check.ThatCode(() => generator.Generate())
             .Throws<DummyGenerationException>()
             .WithProperty(caught => caught.Seed, 4242)
             .And.WhichMember(caught => caught.Message)
             .Contains("Combine(...)")
             .And.Contains("Dummy.WithSeed(4242)")
             .And.Not.Contains("Dummy.Reproducibly(");
    }

    [Fact(DisplayName = "A composer failure over a Combine mixing a foreign operand qualifies the replay hint, though a library operand supplies a nameable source.")]
    public void CombineOverMixedForeignAndLibraryQualifiesTheHint() {
        // The foreign operand has no source, but Dummy.Int32()'s ambient source survives the ?? collapse, so a naive
        // "non-null source means faithful" rule would over-promise. The composed value depends on the foreign draw, so
        // the hint must be qualified even though a seed can still be named.
        IDummy<string> generator = Dummy.Combine<int, int, string>(
            new ForeignInt(),
            Dummy.Int32().Between(1, 3),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        Check.ThatCode(() => Dummy.Reproducibly(31415, () => generator.Generate(), _ => { }))
             .Throws<DummyGenerationException>()
             .WithProperty(caught => caught.Seed, 31415)
             .And.WhichMember(caught => caught.Message)
             .Contains("Combine(...)")
             .And.Contains("not reproducible from this seed alone")
             .And.Not.Contains("The arbitrary values were seeded with");
    }

    [Fact(DisplayName = "A composer failure over a Combine mixing two different seeded sources does not promise a full replay from one seed.")]
    public void CombineOverMixedSeededSourcesQualifiesTheHint() {
        // The first operand draws from Dummy.WithSeed(4242); the second from the ambient source. The composed value
        // depends on BOTH, so replaying WithSeed(4242) alone reproduces only the first — the hint must not promise a
        // deterministic full replay from that one seed (issue #319).
        IDummy<string> generator = Dummy.Combine<int, int, string>(
            Dummy.WithSeed(4242).Int32().Between(1, 3),
            Dummy.Int32().Between(4, 6),
            (first, second) => throw new InvalidOperationException($"rejected {first}/{second}"));

        DummyGenerationException caught = Assert.Throws<DummyGenerationException>(() => generator.Generate());

        Check.That(caught.Message).Contains("Combine(...)");
        Check.WithCustomMessage($"The hint over-promised a full replay. Message: {caught.Message}")
             .That(caught.Message).Not.Contains("already replays deterministically");
        Check.WithCustomMessage($"The hint did not qualify the replay promise. Message: {caught.Message}")
             .That(caught.Message).Contains("not reproducible from this seed alone");
    }

    [Fact(DisplayName = "Combine composes four through eight parts, passing every constrained part to the lambda.")]
    public void CombineSupportsHigherArities() {
        IDummy<int> part = Dummy.Int32().Between(1, 9);

        for (int i = 0; i < 50; i++) {
            int[] four  = Dummy.Combine(part, part, part, part, (a, b, c, d) => new[] { a, b, c, d }).Generate();
            int[] five  = Dummy.Combine(part, part, part, part, part, (a, b, c, d, e) => new[] { a, b, c, d, e }).Generate();
            int[] six   = Dummy.Combine(part, part, part, part, part, part, (a, b, c, d, e, f) => new[] { a, b, c, d, e, f }).Generate();
            int[] seven = Dummy.Combine(part, part, part, part, part, part, part, (a, b, c, d, e, f, g) => new[] { a, b, c, d, e, f, g }).Generate();
            int[] eight = Dummy.Combine(part, part, part, part, part, part, part, part, (a, b, c, d, e, f, g, h) => new[] { a, b, c, d, e, f, g, h }).Generate();

            Check.That(four.Length).IsEqualTo(4);
            Check.That(five.Length).IsEqualTo(5);
            Check.That(six.Length).IsEqualTo(6);
            Check.That(seven.Length).IsEqualTo(7);
            Check.That(eight.Length).IsEqualTo(8);
            foreach (int[] parts in new[] { four, five, six, seven, eight }) {
                Check.That(parts).ContainsOnlyElementsThatMatch(value => value is >= 1 and <= 9);
            }
        }
    }

    [Fact(DisplayName = "Combine draws every operand before composing, including one the composer never reads.")]
    [SuppressMessage(JustDummiesRule.JD027.Category, JustDummiesRule.JD027.Id, Justification = SuppressionJustification.JD027.IgnoredOperandIsTheSubject)]
    public void CombineDrawsAnOperandTheComposerIgnores() {
        int drawnFirst  = 0;
        int drawnSecond = 0;

        IDummy<int> first = Dummy.Int32().As(value => {
            drawnFirst++;

            return value;
        });
        IDummy<int> second = Dummy.Int32().As(value => {
            drawnSecond++;

            return value;
        });

        _ = Dummy.Combine(first, second, (a, b) => a).Generate();

        Check.That(drawnFirst).IsEqualTo(1);
        Check.That(drawnSecond).IsEqualTo(1);
    }

    [Fact(DisplayName = "A higher-arity Combine validates its arguments and wraps composer failures.")]
    public void HigherArityCombineValidatesAndWraps() {
        Check.ThatCode(() => Dummy.Combine(Dummy.Int32(), Dummy.Int32(), Dummy.Int32(), Dummy.Int32(), (Func<int, int, int, int, int>)null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.Combine<int, int, int, int, int>(Dummy.Int32(), Dummy.Int32(), Dummy.Int32(), null!, (a, b, c, d) => a)).Throws<ArgumentNullException>();

        IDummy<string> failing = Dummy.Combine<int, int, int, int, int, int, int, int, string>(
            Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2),
            Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2), Dummy.Int32().Between(1, 2),
            (a, b, c, d, e, f, g, h) => throw new InvalidOperationException("rejected"));

        DummyGenerationException caught = Assert.Throws<DummyGenerationException>(() => failing.Generate());
        Check.That(caught.Message).Contains("Combine(...)");
        Check.That(caught.InnerException).IsInstanceOf<InvalidOperationException>();
    }

    [Fact(DisplayName = "A generated value whose ToString() throws does not break a succeeding As or Combine.")]
    public void AThrowingToStringDoesNotBreakASucceedingDerivation() {
        // Regression: the failure sentence handed to the derivation plumbing was an interpolated string, so rendering
        // the generated value ran on EVERY draw — successful ones included. A domain object whose ToString() throws
        // (state a fixture never set, most often) therefore killed a derivation that had nothing wrong with it: the
        // factory below is never even reached. The sentence is a thunk now, so nothing renders unless it fails.
        IDummy<string> derived = Dummy.ElementOf(new[] { new Unrenderable() }).As(_ => "built");

        Check.That(derived.Generate()).IsEqualTo("built");

        IDummy<string> combined = Dummy.Combine(Dummy.ElementOf(new[] { new Unrenderable() }),
                                            Dummy.Int32().Between(1, 3),
                                            (_, number) => "built " + number);

        Check.That(combined.Generate()).StartsWith("built ");
    }

    [Fact(DisplayName = "A factory failure over a value whose ToString() throws still reports the wrapped diagnostic.")]
    public void AThrowingToStringStillYieldsTheWrappedDiagnostic() {
        // The other half: once the factory does fail, rendering the value is attempted — and must not replace the
        // diagnostic being built with the ToString() failure. The message degrades to the type name; the caller still
        // gets an DummyGenerationException naming As(...) and carrying the real cause.
        IDummy<string> failing = Dummy.ElementOf(new[] { new Unrenderable() })
                                  .As<Unrenderable, string>(_ => throw new InvalidOperationException("rejected"));

        DummyGenerationException caught = Assert.Throws<DummyGenerationException>(() => failing.Generate());

        Check.That(caught.Message).Contains("As(...)");
        Check.That(caught.Message).Contains(nameof(Unrenderable));       // the fallback rendering
        Check.That(caught.Message).Not.Contains("ToString() exploded");  // never the renderer's own failure
        Check.That(caught.InnerException).IsInstanceOf<InvalidOperationException>();
    }

    [Fact(DisplayName = "A collection constraint over a value whose ToString() throws reports the conflict, not the rendering.")]
    public void AThrowingToStringDoesNotMaskACollectionConflict() {
        // Display also renders values into constraint-conflict messages, built eagerly by design at declaration time.
        // The same guard has to hold there: the caller must read the conflict, not the renderer's accident.
        Unrenderable value = new();

        Check.ThatCode(() => Dummy.ListOf(Dummy.ElementOf(new[] { value })).Distinct().Containing(value).Containing(value).Generate())
             .Throws<ConflictingDummyConstraintException>()
             .WhichMember(caught => caught.Message)
             .Contains(nameof(Unrenderable))
             .And.Not.Contains("ToString() exploded");
    }

    [Fact(DisplayName = "Generic inference flows through IDummy<T> without relying on implicit conversions.")]
    public void GenericInferenceFlowsThroughIAny() {
        string text  = Materialize(Dummy.String().NonEmpty().WithMaxLength(50));
        int    value = Materialize(Dummy.Int32().Positive());

        Check.That(text).IsNotEmpty();
        Check.That(value).IsStrictlyGreaterThan(0);
    }

    [Fact(DisplayName = "As and Combine validate their arguments.")]
    public void CompositionValidatesArguments() {
        Check.ThatCode(() => Dummy.String().As<string, OrderReference>(null!)).Throws<ArgumentNullException>();
        Check.ThatCode(() => DummyExtensions.As(null!, (string value) => value)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.Combine(null!, Dummy.Int32(), (int a, int b) => a + b)).Throws<ArgumentNullException>();
        Check.ThatCode(() => Dummy.Combine(Dummy.Int32(), Dummy.Int32(), (Func<int, int, int>)null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A derived generator draws fresh values on every generation.")]
    public void DerivedGeneratorsDrawFreshValues() {
        IDummy<Percentage> generator = Dummy.Int32().Between(0, 100).As(Percentage.Create);

        HashSet<int> seen = [];
        for (int i = 0; i < 100; i++) {
            seen.Add(generator.Generate().Value);
        }

        Check.That(seen.Count).IsStrictlyGreaterThan(1);
    }

    #region Nested types

    [SuppressMessage(SonarRule.S3877.Category, SonarRule.S3877.Id, Justification = SuppressionJustification.S3877.ThrowingToStringIsTheFixture)]
    private sealed class Unrenderable {

        // A domain object whose ToString() throws: the ordinary shape of it is a renderer reaching for state the
        // fixture never set. Diagnostics must survive it, and a successful draw must never trigger it at all.
        public override string ToString() {
            throw new InvalidOperationException("ToString() exploded");
        }

    }

    private sealed class ForeignInt : IDummy<int> {

        // Foreign on purpose: implements IDummy<int> but NOT IHasRandomSource, so it draws from no reported source and a
        // Combine that includes it is not fully reproducible even when another operand carries one.
        public int Generate() {
            return 0;
        }

    }

    #endregion

}
