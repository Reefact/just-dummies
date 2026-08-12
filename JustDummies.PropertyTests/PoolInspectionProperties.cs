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

    /// <summary>
    ///     Arbitrary pools drawn from FEW days and several offsets, so two spellings of one instant occur often —
    ///     the shape that made the two declaration orders disagree. The offset is picked out of the pool, so the
    ///     generator is declarable whichever order it is written in.
    /// </summary>
    private static Gen<(DateTimeOffset[] Pool, TimeSpan Offset)> PoolWithRepeatedInstants() {
        return from spellings in Gen.NonEmptyListOf(from day in Gen.Choose(1, 4)
                                                    from hours in Gen.Choose(0, 2)
                                                    select new DateTimeOffset(2026, 1, day, 0, 0, 0, TimeSpan.Zero).ToOffset(TimeSpan.FromHours(hours)))
               from index in Gen.Choose(0, 63)
               let pool = spellings.Take(8).ToArray()
               select (pool, pool[index % pool.Length].Offset);
    }

    private static AnyDateTimeOffset Build(DateTimeOffset[] pool, TimeSpan offset, bool poolFirst) {
        return poolFirst
                   ? Any.WithSeed(1).DateTimeOffset().OneOf(pool).WithOffset(offset)
                   : Any.WithSeed(1).DateTimeOffset().WithOffset(offset).OneOf(pool);
    }

    /// <summary>
    ///     What one spelling of the chain reports, rendered so two of them can be compared: the conflict if it
    ///     refuses, otherwise the survivors and the rejections with their reasons. Sorted, because the supplied
    ///     order is the reported order and reordering the pool legitimately reorders both lists — it is the
    ///     <i>content</i> that must not move.
    /// </summary>
    private static string Report(DateTimeOffset[] pool, TimeSpan offset, bool poolFirst) {
        AnyDateTimeOffset generator;
        try { generator = Build(pool, offset, poolFirst); } catch (ConflictingAnyConstraintException caught) { return $"CONFLICT {caught.Message}"; }

        IPoolInspection<DateTimeOffset> inspection = generator;

        return string.Join(";", inspection.GetSurvivors().Select(Spelling).OrderBy(spelling => spelling))
             + "|" + string.Join(";", inspection.GetRejections().Select(rejection => $"{Spelling(rejection.Value)}<-{string.Join(",", rejection.RejectedBy)}").OrderBy(text => text, StringComparer.Ordinal));
    }

    /// <summary>
    ///     The same report reduced to the <b>instant</b> of each value. That is the granularity a pool of
    ///     <see cref="DateTimeOffset" /> has an identity at — <c>OneOf</c> publishes "duplicates (same instant) are
    ///     ignored" — so it is what must survive REORDERING the supplied array. The spelling must not: both lists
    ///     are published as being "in the order they were supplied", and for an instant the caller wrote twice the
    ///     representative is the first of those spellings, which reordering legitimately changes.
    /// </summary>
    private static string ReportByInstant(DateTimeOffset[] pool, TimeSpan offset, bool poolFirst) {
        AnyDateTimeOffset generator;
        try { generator = Build(pool, offset, poolFirst); } catch (ConflictingAnyConstraintException caught) { return $"CONFLICT {caught.Message}"; }

        IPoolInspection<DateTimeOffset> inspection = generator;

        return string.Join(";", inspection.GetSurvivors().Select(value => value.UtcTicks).OrderBy(ticks => ticks))
             + "|" + string.Join(";", inspection.GetRejections().Select(rejection => $"{rejection.Value.UtcTicks}<-{string.Join(",", rejection.RejectedBy)}").OrderBy(text => text, StringComparer.Ordinal));
    }

    /// <summary>
    ///     What one spelling of the chain draws from a fixed seed. Compared only between the two DECLARATION orders
    ///     of one supplied array: reordering the array reorders the pool, and a pool is drawn from by index, so a
    ///     seeded sequence is expected to differ there.
    /// </summary>
    private static string Draws(DateTimeOffset[] pool, TimeSpan offset, bool poolFirst) {
        AnyDateTimeOffset generator;
        try { generator = Build(pool, offset, poolFirst); } catch (ConflictingAnyConstraintException caught) { return $"CONFLICT {caught.Message}"; }

        return string.Join(";", Enumerable.Range(0, 8).Select(_ => Spelling(generator.Generate())));
    }

    #endregion

    // The defect this closes: the pool was collapsed to one spelling per instant AT DECLARATION, so whichever of
    // the offset filter and the instant dedup ran first decided which spelling the other one got to judge. Writing
    // the same specification in a different order -- or merely re-sorting the supplied array -- changed the
    // survivors, the rejection count, and whether the declaration was refused at all. ADR-0030 records the
    // opposite as a consequence of the offset filter, so this quantifies it rather than pinning one example.
    [Fact(DisplayName = "The verdict does not depend on the order the pool or the offset was written in.")]
    public void TheVerdictDoesNotDependOnTheOrderItWasWrittenIn() {
        Prop.ForAll(PoolWithRepeatedInstants().ToArbitrary(),
                    testCase => {
                        // Enumerable.Reverse called by name, not through the extension syntax: on the .NET
                        // Framework floor an array binds `.Reverse()` to MemoryExtensions.Reverse(this Span<T>),
                        // which reverses IN PLACE and returns void, so the expression does not compile there while
                        // it does on net10 -- a break only the 4.7.2 job can see.
                        DateTimeOffset[] reversed = Enumerable.Reverse(testCase.Pool).ToArray();

                        // The claim ADR-0030 records: the two DECLARATION orders of one chain reach one verdict.
                        // Whole report, spelling for spelling, and the seeded draw with it.
                        bool declarationOrderAgrees =
                            Report(testCase.Pool, testCase.Offset, poolFirst: true) == Report(testCase.Pool, testCase.Offset, poolFirst: false)
                         && Report(reversed, testCase.Offset, poolFirst: true) == Report(reversed, testCase.Offset, poolFirst: false)
                         && Draws(testCase.Pool, testCase.Offset, poolFirst: true) == Draws(testCase.Pool, testCase.Offset, poolFirst: false)
                         && Draws(reversed, testCase.Offset, poolFirst: true) == Draws(reversed, testCase.Offset, poolFirst: false);

                        // And re-sorting the supplied array cannot change WHICH instants draw, which are refused,
                        // or why — nor whether the declaration is refused at all. That last one is what used to
                        // break: sorting a catalogue the other way turned a satisfiable pool into a conflict.
                        string byInstant = ReportByInstant(testCase.Pool, testCase.Offset, poolFirst: true);

                        return declarationOrderAgrees
                            && ReportByInstant(reversed, testCase.Offset, poolFirst: true) == byInstant
                            && ReportByInstant(reversed, testCase.Offset, poolFirst: false) == byInstant;
                    })
            .QuickCheckThrowOnFailure();
    }

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
