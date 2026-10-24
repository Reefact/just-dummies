#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Model-based properties for <see cref="AnyString" />: random chains of its calls are run against the generator
///     and against an independent <see cref="StringModel" /> of the documented semantics, and the two must agree on
///     where the chain is refused and on what it may draw.
/// </summary>
/// <remarks>
///     <para>
///         The example suite and the other string properties each exercise one interaction someone thought to write.
///         The defects that reached this generator were combinations nobody had — a length floor beside a value set,
///         a subtraction beside a pool, an anchor beside a casing — each call correct on its own (issue #231). A
///         generated chain of the DSL's own calls, judged against a model rather than against a hand-picked
///         expectation, finds that class rather than one instance of it.
///     </para>
///     <para>
///         The comparison is made at the call, not only at the end: the run time refuses a chain at the first call
///         that leaves it unsatisfiable, so the model must say "here" for the same call. A chain both accept is then
///         drawn, and every value must be one the model accepts.
///     </para>
/// </remarks>
[TestSubject(typeof(AnyString))]
public sealed class StringModelProperties {

    /// <summary>
    ///     Many more cases than the suite's default hundred: the space is every combination of up to eight calls, and
    ///     a disagreement lives in a few of them. A chain is cheap — short lengths, eight draws.
    /// </summary>
    private const int Cases = 2000;

    [Fact(DisplayName = "An Any.String() chain is refused exactly where the model says it contradicts itself, and draws only what the model accepts.")]
    public void AStringChainAgreesWithTheModel() {
        Prop.ForAll(StringModelChains.Arbitrary(), Generators.Seed().ToArbitrary(),
                    (chain, seed) => chain.Disagreement(seed) is not string disagreement
                                         ? true.ToProperty()
                                         : false.Label($"{chain} under seed {seed}: {disagreement}."))
            .Check(Config.QuickThrowOnFailure.WithMaxTest(Cases));
    }

    [Fact(DisplayName = "The model exempts a literal from the alphabet only where the layout places it, once.")]
    public void TheModelExemptsALiteralOnlyWhereTheLayoutPlacesIt() {
        // The oracle is only as strong as its acceptance test: one that exempted every occurrence of a contained value
        // would accept "aa" here, where one of the two must come from a numeric filler, and would let through exactly
        // the regression on character constraints it exists to catch.
        StringModel model = new();
        Assert.True(model.WithLength(2) && model.Containing("a") && model.Family("Numeric"));

        Assert.False(model.Accepts("aa"));
        Assert.True(model.Accepts("a1"));
        Assert.True(model.Accepts("1a"));
    }

}
