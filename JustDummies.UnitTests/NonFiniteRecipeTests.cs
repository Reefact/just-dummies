#region Usings declarations

using System.Diagnostics.CodeAnalysis;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Locks in the "NaN and the infinities" recipe of <c>JustDummies/README.nuget.md</c>. That page tells a user
///     three things: the floating-point builders refuse a non-finite value as a bound or a <c>OneOf</c> value, they
///     accept one in <c>Except</c> and <c>DifferentFrom</c> as a no-op, and the way to get one anyway is the generic
///     pool. All three are behaviour, so all three are pinned here, for each of the three builders — a documented
///     exit that silently stopped working would be worse than no documentation at all, since the reader would have
///     no reason to doubt it.
/// </summary>
public sealed class NonFiniteRecipeTests {

    private const int SampleCount = 200;

    /// <summary>
    ///     The guarded declarations, each under the call it makes. The delegate itself cannot travel as theory data
    ///     — a <c>Func&lt;object&gt;</c> is not serializable, so the runner shows rows it cannot tell apart and
    ///     cannot run one of them on its own. The key travels instead, and it is what names the row: a failure then
    ///     says WHICH entry point stopped refusing rather than that one of them did.
    /// </summary>
    private static readonly Dictionary<string, Func<object>> Declarations = new() {
        ["Any.Double().GreaterThan(double.NegativeInfinity)"] = () => Any.Double().GreaterThan(double.NegativeInfinity),
        ["Any.Double().LessThan(double.NaN)"]                 = () => Any.Double().LessThan(double.NaN),
        ["Any.Double().OneOf(1.0, double.NaN)"]               = () => Any.Double().OneOf(1.0, double.NaN),
        ["Any.Single().GreaterThan(float.NegativeInfinity)"]  = () => Any.Single().GreaterThan(float.NegativeInfinity),
        ["Any.Single().OneOf(1.0f, float.PositiveInfinity)"]  = () => Any.Single().OneOf(1.0f, float.PositiveInfinity),
        ["Any.Half().GreaterThan(Half.NegativeInfinity)"]     = () => Any.Half().GreaterThan(Half.NegativeInfinity),
        ["Any.Half().OneOf((Half)1, Half.NaN)"]               = () => Any.Half().OneOf((Half)1, Half.NaN),
    };

    /// <summary>
    ///     Except and DifferentFrom used to be guarded the same way (issue #180's own genesis) but no longer are: a
    ///     non-finite value is already guaranteed never to be drawn, so excluding one is accepted as a no-op instead
    ///     — see <see cref="ExcludingANonFiniteValueIsANoOp" />.
    /// </summary>
    private static readonly Dictionary<string, Func<object>> AcceptedExclusions = new() {
        ["Any.Double().Except(double.NaN)"]                      = () => Any.Double().Except(double.NaN),
        ["Any.Double().DifferentFrom(double.PositiveInfinity)"]  = () => Any.Double().DifferentFrom(double.PositiveInfinity),
        ["Any.Single().Except(float.NaN)"]                       = () => Any.Single().Except(float.NaN),
        ["Any.Single().DifferentFrom(float.NegativeInfinity)"]   = () => Any.Single().DifferentFrom(float.NegativeInfinity),
        ["Any.Half().Except(Half.NaN)"]                          = () => Any.Half().Except(Half.NaN),
        ["Any.Half().DifferentFrom(Half.PositiveInfinity)"]      = () => Any.Half().DifferentFrom(Half.PositiveInfinity),
    };

    public static TheoryData<string> GuardedEntryPoints => [.. Declarations.Keys];
    public static TheoryData<string> AcceptedExclusionEntryPoints => [.. AcceptedExclusions.Keys];

    [Theory(DisplayName = "The guarded entry points reject a non-finite ARGUMENT, not only a non-finite draw.")]
    [MemberData(nameof(GuardedEntryPoints))]
    public void GuardedEntryPointsRejectNonFiniteArguments(string declaration) {
        Check.ThatCode(() => Declarations[declaration]()).Throws<ArgumentException>();
    }

    [Theory(DisplayName = "Except and DifferentFrom accept a non-finite value instead of refusing it (issue #180).")]
    [MemberData(nameof(AcceptedExclusionEntryPoints))]
    public void ExcludingANonFiniteValueIsANoOp(string declaration) {
        Check.ThatCode(() => AcceptedExclusions[declaration]()).DoesNotThrow();
    }

    [Fact(DisplayName = "Excluding a non-finite value changes nothing about what is drawn, seed for seed, on all three builders (issue #180).")]
    public void ExcludingANonFiniteValueIsGenuinelyInert() {
        for (int seed = 0; seed < SampleCount; seed++) {
            double plainDouble;
            double excludedDouble;
            using (Any.UseSeed(seed)) { plainDouble = Any.Double().Generate(); }
            using (Any.UseSeed(seed)) { excludedDouble = Any.Double().Except(double.NaN).DifferentFrom(double.PositiveInfinity).Generate(); }
            Check.That(excludedDouble).IsEqualTo(plainDouble);

            float plainSingle;
            float excludedSingle;
            using (Any.UseSeed(seed)) { plainSingle = Any.Single().Generate(); }
            using (Any.UseSeed(seed)) { excludedSingle = Any.Single().Except(float.NaN).DifferentFrom(float.NegativeInfinity).Generate(); }
            Check.That(excludedSingle).IsEqualTo(plainSingle);

            Half plainHalf;
            Half excludedHalf;
            using (Any.UseSeed(seed)) { plainHalf = Any.Half().Generate(); }
            using (Any.UseSeed(seed)) { excludedHalf = Any.Half().Except(Half.NaN).DifferentFrom(Half.PositiveInfinity).Generate(); }
            Check.That(excludedHalf).IsEqualTo(plainHalf);
        }
    }

    [Fact(DisplayName = "A bound's refusal names the way out, and says what is refused — a bound, not every argument — on all three builders.")]
    public void TheRefusalNamesTheWayOut() {
        ArgumentException[] refusals = [
            Assert.Throws<ArgumentException>(() => Any.Double().LessThan(double.NaN)),
            Assert.Throws<ArgumentException>(() => Any.Single().LessThan(float.NaN)),
            Assert.Throws<ArgumentException>(() => Any.Half().LessThan(Half.NaN)),
        ];

        foreach (ArgumentException refusal in refusals) {
            Check.That(refusal.Message).Contains("must be finite");
            // Narrowed to what is actually refused: an exclusion is accepted, so "never accepted as arguments" would
            // now be false, and a caller reading the bounds' sentence must not conclude otherwise.
            Check.That(refusal.Message).Contains("not accepted as a bound");
            // The half the recipe is about: a message that states the rule and stops leaves the reader concluding
            // the library is missing a feature it deliberately does not have.
            Check.That(refusal.Message).Contains("Any.OneOf");
        }
    }

    [Fact(DisplayName = "OneOf's refusal names the generic pool, distinct from the builder's own OneOf, so the advice does not read as the call just written — on all three builders (issue #180).")]
    public void OneOfsRefusalNamesTheGenericPool() {
        ArgumentException[] refusals = [
            Assert.Throws<ArgumentException>(() => Any.Double().OneOf(1.0, double.NaN)),
            Assert.Throws<ArgumentException>(() => Any.Single().OneOf(1.0f, float.NaN)),
            Assert.Throws<ArgumentException>(() => Any.Half().OneOf((Half)1, Half.NaN)),
        ];

        foreach (ArgumentException refusal in refusals) {
            Check.That(refusal.Message).Contains("must be finite");
            Check.That(refusal.Message).Contains("not accepted as a value of this builder's OneOf");
            Check.That(refusal.Message).Contains("Any.OneOf<double>");
        }
    }

    [Fact(DisplayName = "An explicit pool is the documented exit, and it really does yield the non-finite values.")]
    public void AnExplicitPoolYieldsNonFiniteValues() {
        HashSet<double> seen = [];
        for (int i = 0; i < SampleCount; i++) {
            seen.Add(Any.OneOf(double.NaN, double.PositiveInfinity, 1.0).Generate());
        }

        // NaN does not compare equal to itself, so membership is asserted through the predicate rather than Contains.
        Check.That(seen.Any(double.IsNaN)).IsTrue();
        Check.That(seen).Contains(double.PositiveInfinity);
        Check.That(seen).Contains(1.0);
    }

    [Fact(DisplayName = "Any.Double() never draws a non-finite value.")]
    public void UnconstrainedDrawsStayFinite() {
        for (int i = 0; i < SampleCount; i++) {
            double value = Any.Double().Generate();
            Check.That(double.IsNaN(value) || double.IsInfinity(value)).IsFalse();
        }
    }

    [Fact(DisplayName = "Decimal has nothing to guard: the type carries no NaN and no infinity to begin with.")]
    public void DecimalIsOutsideTheSubject() {
        // Pinned because the recipe makes a claim about the BCL, not about this library: a reader who went looking for
        // the symmetry with Any.Double() is told the reason it does not exist. If decimal ever gained a non-finite
        // representation, that paragraph would become false and this test is what would say so.
        Check.That(typeof(decimal).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                                  .Select(field => field.Name))
             .Not.Contains("NaN");

        for (int i = 0; i < SampleCount; i++) { Check.ThatCode(() => Any.Decimal().Generate()).DoesNotThrow(); }
    }

    [SuppressMessage(NetAnalyzersRule.CA2242.Category, NetAnalyzersRule.CA2242.Id, Justification = SuppressionJustification.CA2242.ComparisonIsTheAssertion)]
    [SuppressMessage(SonarRule.S2688.Category, SonarRule.S2688.Id, Justification = SuppressionJustification.S2688.ComparisonIsTheAssertion)]
    [Fact(DisplayName = "The Equals/== asymmetry the recipe warns about is real: a pooled NaN deduplicates.")]
    public void PooledNaNDeduplicatesUnderTheDefaultComparer() {
        // The trap, asserted rather than described: user code comparing with == sees two different values where a
        // comparer-based collection sees one. Anyone deliberately pooling a NaN is warned about exactly this.
        Check.That(EqualityComparer<double>.Default.Equals(double.NaN, double.NaN)).IsTrue();
#pragma warning disable CS1718 // Comparison made to same variable — that is the point being asserted.
        Check.That(double.NaN == double.NaN).IsFalse();
#pragma warning restore CS1718

        HashSet<double> deduplicated = [double.NaN, double.NaN];
        Check.That(deduplicated.Count).IsEqualTo(1);
    }

}
