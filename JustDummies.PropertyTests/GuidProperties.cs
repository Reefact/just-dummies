#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <see cref="DummyGuid" />'s value constraints — <c>OneOf</c>, <c>Except</c> and
///     <c>DifferentFrom</c> — quantified over arbitrary <see cref="Guid" /> pools rather than a handful of
///     hand-picked identifiers, mirroring <see cref="StringShapeProperties" />'s treatment of the same shapes on
///     <see cref="DummyString" />.
/// </summary>
[TestSubject(typeof(DummyGuid))]
public sealed class GuidProperties {

    [Fact(DisplayName = "OneOf draws only the supplied values, whichever pool is supplied.")]
    public void OneOfDrawsOnlyTheSuppliedValues() {
        Gen<Guid[]> pools = Gen.NonEmptyListOf(ArbMap.Default.GeneratorFor<Guid>())
                               .Select(values => values.Distinct().Take(10).ToArray());

        Prop.ForAll(pools.ToArbitrary(),
                    pool => Expect.EveryDraw(Dummy.Guid().OneOf(pool), value => pool.Contains(value)))
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Exclusions accumulate and never yield an excluded value, whatever the excluded set.")]
    public void ExclusionsNeverYieldAnExcludedValue() {
        Prop.ForAll(Gen.Choose(1, 10).ToArbitrary(),
                    // The excluded values are drawn from the very generator they are then excluded from, so the
                    // exclusion is never vacuous — the 128-bit space leaves it amply satisfiable regardless.
                    excludeCount => {
                        DummyGuid shaped   = Dummy.Guid();
                        Guid[]  excluded = Expect.Draws(shaped, excludeCount).Distinct().ToArray();
                        Guid    banned   = shaped.Generate();
                        DummyGuid narrowed = shaped.Except(excluded).DifferentFrom(banned);

                        return Expect.EveryDraw(narrowed, value => !excluded.Contains(value) && value != banned);
                    })
            .QuickCheckThrowOnFailure();
    }

}
