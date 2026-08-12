#region Usings declarations

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for the pool inspection (ADR-0067). The example suite pins which constraint a named
///     rejection blames; these quantify over the pool and over the length bound, so the report is checked as a
///     partition of what the caller supplied rather than at one hand-picked coordinate: nothing supplied goes
///     missing, nothing is invented, a survivor really is drawable, and a blamed constraint really does refuse the
///     value it is blamed for.
/// </summary>
[TestSubject(typeof(IPoolInspection<>))]
public sealed class PoolInspectionProperties {

    #region Statics members declarations

    /// <summary>
    ///     Arbitrary non-empty pools of non-null strings of mixed lengths, so a length bound drawn beside them
    ///     splits the pool instead of keeping or refusing all of it.
    /// </summary>
    private static Gen<string[]> MixedLengthPools() {
        return Gen.NonEmptyListOf(Gen.Choose(0, 8).Select(length => new string('x', length)))
                  .Select(values => values.Distinct(StringComparer.Ordinal).Take(12).ToArray());
    }

    /// <summary>
    ///     A pool paired with a maximum length at least one of its values satisfies, so the generator is always
    ///     declarable: a bound no value meets is a conflict at declaration, which is the example suite's subject.
    /// </summary>
    private static Gen<(string[] Pool, int Maximum)> PoolAndSatisfiableMaximum() {
        return from pool in MixedLengthPools()
               from maximum in Gen.Choose(pool.Min(value => value.Length), 8)
               select (pool, maximum);
    }

    /// <summary>
    ///     Arbitrary non-empty pools of instants on distinct days carrying mixed offsets, paired with an offset one
    ///     of them satisfies. The offset is picked out of the pool rather than filtered for, so the generator is
    ///     always declarable. Distinct days keep two spellings of one instant out of the sample: what a pool of
    ///     <see cref="DateTimeOffset" /> considers one value is a separate question this property does not settle.
    /// </summary>
    private static Gen<(DateTimeOffset[] Pool, TimeSpan Offset)> DatePoolAndSatisfiableOffset() {
        return from spellings in Gen.NonEmptyListOf(from day in Gen.Choose(1, 20)
                                                    from hours in Gen.Choose(0, 2)
                                                    select new DateTimeOffset(2026, 1, day, 0, 0, 0, TimeSpan.FromHours(hours)))
               from index in Gen.Choose(0, 63)
               // Named sameDay rather than group: `group` is a query keyword, and this let sits inside a query.
               let pool = spellings.GroupBy(value => value.Day).Select(sameDay => sameDay.First()).Take(8).ToArray()
               select (pool, pool[index % pool.Length].Offset);
    }

    /// <summary>The value as the caller wrote it — the instant AND the offset, which its own equality ignores.</summary>
    private static (long UtcTicks, TimeSpan Offset) Spelling(DateTimeOffset value) {
        return (value.UtcTicks, value.Offset);
    }

    #endregion

    // The string property above cannot see a pool that fails to add up, because Any.String()'s pool is already
    // distinct before the report is built. This one quantifies over the family whose pool is filtered on TWO
    // dimensions -- the instant, inside the engine, and the offset, outside it -- where a value can go missing from
    // both lists or be counted twice, and where both of those actually happened.
    [Fact(DisplayName = "A date pool filtered on its offset still partitions: nothing is lost, nothing is doubled.")]
    public void ADatePoolPartitionsAcrossBothOfItsDimensions() {
        Prop.ForAll(DatePoolAndSatisfiableOffset().ToArbitrary(),
                    testCase => {
                        IPoolInspection<DateTimeOffset> inspection = Any.DateTimeOffset().OneOf(testCase.Pool).WithOffset(testCase.Offset);

                        List<DateTimeOffset> reported = [..inspection.GetSurvivors(), ..inspection.GetRejections().Select(rejection => rejection.Value)];

                        return reported.Select(Spelling).Distinct().Count() == reported.Count
                            && reported.Select(Spelling).OrderBy(spelling => spelling).SequenceEqual(testCase.Pool.Select(Spelling).OrderBy(spelling => spelling))
                            && inspection.GetRejections().All(rejection => rejection.RejectedBy.Count > 0);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "The survivors and the rejected values partition the supplied pool: nothing is lost, nothing is invented.")]
    public void SurvivorsAndRejectionsPartitionTheSuppliedPool() {
        Prop.ForAll(PoolAndSatisfiableMaximum().ToArbitrary(),
                    testCase => {
                        IPoolInspection<string> inspection = Any.String().OneOf(testCase.Pool).WithMaxLength(testCase.Maximum);

                        List<string> reported = [..inspection.GetSurvivors(), ..inspection.GetRejections().Select(rejection => rejection.Value)];

                        return reported.Distinct(StringComparer.Ordinal).Count() == reported.Count
                            && reported.OrderBy(value => value, StringComparer.Ordinal)
                                       .SequenceEqual(testCase.Pool.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Every survivor satisfies the declared constraint, and every rejected value fails it.")]
    public void TheSplitFollowsTheDeclaredConstraint() {
        Prop.ForAll(PoolAndSatisfiableMaximum().ToArbitrary(),
                    testCase => {
                        IPoolInspection<string> inspection = Any.String().OneOf(testCase.Pool).WithMaxLength(testCase.Maximum);

                        return inspection.GetSurvivors().All(value => value.Length <= testCase.Maximum)
                            && inspection.GetRejections().All(rejection => rejection.Value.Length > testCase.Maximum);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Every rejection names at least one constraint, and every constraint it names is one the generator declares.")]
    public void EveryRejectionBlamesADeclaredConstraint() {
        Prop.ForAll(PoolAndSatisfiableMaximum().ToArbitrary(),
                    testCase => {
                        IPoolInspection<string> inspection = Any.String().OneOf(testCase.Pool).WithMaxLength(testCase.Maximum);

                        // Only WithMaxLength can refuse anything here, so it is the only name the report may carry:
                        // a rejection blaming a constraint nobody declared would be a reason a reader cannot act on.
                        return inspection.GetRejections()
                                         .All(rejection => rejection.RejectedBy.Count > 0
                                                        && rejection.RejectedBy.All(constraint => constraint.Name == "WithMaxLength"
                                                                                              && constraint.Arguments == testCase.Maximum.ToString()));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "The survivors are exactly what a draw can yield: every generated value is one of them.")]
    public void EveryDrawLandsInTheReportedSurvivors() {
        Prop.ForAll(PoolAndSatisfiableMaximum().ToArbitrary(),
                    testCase => {
                        AnyString               generator  = Any.String().OneOf(testCase.Pool).WithMaxLength(testCase.Maximum);
                        IPoolInspection<string> inspection = generator;
                        IReadOnlyList<string>   survivors  = inspection.GetSurvivors();

                        return survivors.Count > 0 && Expect.EveryDraw(generator, value => survivors.Contains(value, StringComparer.Ordinal));
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "Inspecting draws nothing: a seeded sequence is the same whether or not it was inspected along the way.")]
    public void InspectingConsumesNoRandomness() {
        Prop.ForAll((from testCase in PoolAndSatisfiableMaximum()
                     from seed in Generators.Seed()
                     select (testCase, seed)).ToArbitrary(),
                    input => {
                        List<string> uninspected = Draw(input.testCase, input.seed, inspect: false);
                        List<string> inspected   = Draw(input.testCase, input.seed, inspect: true);

                        return uninspected.SequenceEqual(inspected, StringComparer.Ordinal);
                    })
            .QuickCheckThrowOnFailure();
    }

    #region Draw helper

    private static List<string> Draw((string[] Pool, int Maximum) testCase, int seed, bool inspect) {
        AnyContext context   = Any.WithSeed(seed);
        AnyString  generator = context.String().OneOf(testCase.Pool).WithMaxLength(testCase.Maximum);

        List<string> drawn = [];
        for (int index = 0; index < 4; index++) {
            drawn.Add(generator.Generate());
            if (!inspect) { continue; }

            IPoolInspection<string> inspection = generator;
            inspection.GetSurvivors();
            inspection.GetRejections();
        }

        return drawn;
    }

    #endregion

}
