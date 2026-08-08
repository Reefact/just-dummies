#region Usings declarations

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     Locks in the "NaN and the infinities" recipe of <c>JustDummies/README.nuget.md</c>. That page tells a user two
///     things: the floating-point builders refuse a non-finite value as an ARGUMENT as well as a draw, and the way to
///     get one anyway is an explicit pool. Both halves are behaviour, so both are pinned here — a documented exit that
///     silently stopped working would be worse than no documentation at all, since the reader would have no reason to
///     doubt it.
/// </summary>
public sealed class NonFiniteRecipeTests {

    private const int SampleCount = 200;

    public static TheoryData<Func<object>> GuardedEntryPoints => new() {
        () => Any.Double().Except(double.NaN),
        () => Any.Double().DifferentFrom(double.PositiveInfinity),
        () => Any.Double().GreaterThan(double.NegativeInfinity),
        () => Any.Double().LessThan(double.NaN),
        () => Any.Double().OneOf(1.0, double.NaN),
        () => Any.Single().Except(float.NaN),
        () => Any.Single().GreaterThan(float.NegativeInfinity),
        () => Any.Single().OneOf(1.0f, float.PositiveInfinity),
    };

    [Theory(DisplayName = "The guarded entry points reject a non-finite ARGUMENT, not only a non-finite draw.")]
    [MemberData(nameof(GuardedEntryPoints))]
    public void GuardedEntryPointsRejectNonFiniteArguments(Func<object> declaration) {
        Check.ThatCode(() => declaration()).Throws<ArgumentException>();
    }

    [Fact(DisplayName = "The refusal names the way out, so the wall explains its own exit.")]
    public void TheRefusalNamesTheWayOut() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Double().Except(double.NaN));

        Check.That(refusal.Message).Contains("must be finite");
        // The half the recipe is about: a message that states the rule and stops leaves the reader concluding the
        // library is missing a feature it deliberately does not have.
        Check.That(refusal.Message).Contains("Any.OneOf");
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage(SonarRule.S2688.Category, SonarRule.S2688.Id,
                                                     Justification =
                                                         "The rule says to write double.IsNaN(x) rather than compare with ==, which is right everywhere except here: this " +
                                                         "test asserts that the two DISAGREE. Replacing the comparison with IsNaN would delete the assertion and leave a " +
                                                         "test that proves nothing, on the exact trap the README warns a user about.")]
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
