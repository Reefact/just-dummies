#region Usings declarations

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

public sealed class AnyCollectionTests {

    #region Statics members declarations

    private const int SampleCount = 200;

    private enum Suit {

        Clubs,
        Diamonds,
        Hearts,
        Spades

    }

    #endregion

    [Fact(DisplayName = "ListOf: unconstrained draws vary in size, stay within 0..8, and hold elements from the item generator.")]
    public void ListOfUnconstrained() {
        HashSet<int> sizes = [];
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).Generate();
            sizes.Add(list.Count);
            Check.That(list.Count).IsGreaterOrEqualThan(0);
            Check.That(list.Count).IsLessOrEqualThan(8);
            Check.That(list).ContainsOnlyElementsThatMatch(value => value is >= 1 and <= 9);
        }
        Check.That(sizes.Count).IsStrictlyGreaterThan(1);
    }

    [Fact(DisplayName = "ListOf: the count family fixes, floors, caps and ranges the size.")]
    public void ListOfCountFamily() {
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.ListOf(Any.Int32()).WithCount(5).Generate().Count).IsEqualTo(5);
            Check.That(Any.ListOf(Any.Int32()).Empty().Generate().Count).IsEqualTo(0);
            Check.That(Any.ListOf(Any.Int32()).NonEmpty().Generate().Count).IsStrictlyGreaterThan(0);
            Check.That(Any.ListOf(Any.Int32()).WithMinCount(3).Generate().Count).IsGreaterOrEqualThan(3);
            Check.That(Any.ListOf(Any.Int32()).WithMaxCount(2).Generate().Count).IsLessOrEqualThan(2);

            int ranged = Any.ListOf(Any.Int32()).WithCountBetween(4, 6).Generate().Count;
            Check.That(ranged is >= 4 and <= 6).IsTrue();
        }
    }

    [Fact(DisplayName = "ListOf: contradictory count constraints fail eagerly naming both sides.")]
    public void ListOfCountConflicts() {
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(3).WithMinCount(5))
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("WithCount(3)");

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(5).WithMaxCount(3)).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(2).WithCount(3)).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "ListOf: count constraints validate their arguments.")]
    public void ListOfCountValidation() {
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(-1)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCountBetween(6, 4)).Throws<ArgumentException>();
        Check.ThatCode(() => Any.ListOf<int>(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "A produced count is refused above the ceiling; the bound just below it is accepted.")]
    public void ProducedCountsAreCeilinged() {
        // ADR-0029, at the two coordinates a mis-written comparison would pass: the ceiling and the first value past it.
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMinCount(1_000_001)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.SetOf(Any.Int32()).WithCountBetween(1_000_001, 2_000_000)).Throws<ArgumentOutOfRangeException>();

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(1_000_000)).DoesNotThrow();
    }

    [Fact(DisplayName = "An enormous count names the caller's parameter instead of exhausting memory.")]
    public void AnEnormousCountNamesTheCallersParameter() {
        // Regression: WithCount(int.MaxValue) used to fail on the allocation itself, and WithMaxCount(int.MaxValue)
        // to grind for minutes filling a collection sized after the cap. Both are now decided at declaration.
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => Any.ListOf(Any.Int32()).WithCount(int.MaxValue));

        Check.That(error.ParamName).IsEqualTo("count");
    }

    [Fact(DisplayName = "A maximum steers the count and is ceilinged like every other size.")]
    public void AMaximumSteersTheCount() {
        // The policy is the string's (ADR-0076); only the spread differs, a thousand elements costing what their
        // element generator costs rather than one character.
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.ListOf(Any.Int32()).WithMaxCount(50).Generate().Count).IsLessOrEqualThan(50);
        }

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithMaxCount(int.MaxValue)).Throws<ArgumentOutOfRangeException>();
        Check.ThatCode(() => Any.ArrayOf(Any.Int32()).WithMaxCount(4_000_000)).Throws<ArgumentOutOfRangeException>();
    }

    [Fact(DisplayName = "Distinct: a wide-domain distinct list holds only distinct elements.")]
    public void DistinctOverAWideDomain() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 1000)).WithCount(20).Distinct().Generate();
            Check.That(list.Count).IsEqualTo(20);
            Check.That(new HashSet<int>(list).Count).IsEqualTo(20);
        }
    }

    [Fact(DisplayName = "Distinct: a count beyond the element cardinality conflicts before any element is drawn, naming the shortfall.")]
    public void DistinctCardinalityConflictsBeforeDrawing() {
        Check.ThatCode(() => Any.SetOf(Any.Boolean()).WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(fromBool => fromBool.Message).Contains("2 distinct value");

        Check.ThatCode(() => Any.SetOf(Any.Enum<Suit>()).WithMinCount(5).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Int32().Between(1, 3)).WithCount(5).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32().Between(1, 3)).WithCount(5).Distinct().Generate()).Throws<ConflictingAnyConstraintException>();
        // Order-independent: turning distinct on after the count is set conflicts just the same.
        Check.ThatCode(() => Any.ListOf(Any.Boolean()).WithCount(3).Distinct().Generate()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Distinct: an unknowable small domain cannot be detected early, so a shortfall surfaces at generation.")]
    public void DistinctFallbackThrowsAtGeneration() {
        // '.As' erases the cardinality hint, so the conflict cannot be seen at declaration — the bounded dedup-draw
        // fallback catches it while generating instead.
        IAny<int> opaque = Any.Int32().Between(1, 3).As(value => value);

        Check.ThatCode(() => Any.SetOf(opaque).WithCount(5).Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "Distinct: over an element type without value equality the requirement is inert, and the collection holds repeats.")]
    [SuppressMessage(JustDummiesRule.JD028.Category, JustDummiesRule.JD028.Id, Justification = SuppressionJustification.JD028.InertDistinctnessIsTheSubject)]
    public void DistinctOverReferenceEqualityIsInert() {
        // Percentage has no value equality, and '.As' builds a NEW instance per draw, so the default comparer can
        // never call two of them equal. Six 'distinct' elements over a two-value domain therefore succeed — and hold
        // repeats. The count is not statistical: six draws from a domain of two cannot show more than two values.
        List<Percentage> percentages = Any.ListOf(Any.Int32().Between(1, 2).As(Percentage.Create)).Distinct().WithCount(6).Generate();

        Check.That(percentages.Count).IsEqualTo(6);
        Check.That(percentages.Select(percentage => percentage.Value).Distinct().Count()).IsStrictlyLessThan(3);
    }

    [Fact(DisplayName = "SetOf: elements are always distinct and drawn from the item generator.")]
    public void SetOfIsDistinct() {
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().Between(1, 500)).WithCount(10).Generate();
            Check.That(set.Count).IsEqualTo(10);
            Check.That(set).ContainsOnlyElementsThatMatch(value => value is >= 1 and <= 500);
        }
    }

    [Fact(DisplayName = "SetOf: a comparer merges values, so cardinality is only an upper bound and the fallback still guards.")]
    public void SetOfHonoursAComparer() {
        IEqualityComparer<int> modTen = new ModuloComparer(10);

        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().Between(0, 999), modTen).WithCount(5).Generate();
            Check.That(set.Count).IsEqualTo(5);
            List<int> classes = set.Select(value => value % 10).ToList();
            Check.That(classes.Count).IsEqualTo(new HashSet<int>(classes).Count);
        }

        // Only ten residue classes exist, so twenty distinct-under-the-comparer elements are impossible; the raw
        // cardinality (1000) hides that, so it can only be caught while drawing.
        Check.ThatCode(() => Any.SetOf(Any.Int32().Between(0, 999), modTen).WithCount(20).Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "Containing: a required value is present, and a distinct duplicate requirement conflicts.")]
    public void ContainingPlacesValues() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).WithCount(5).Containing(777).Generate();
            Check.That(list).Contains(777);
            Check.That(list.Count).IsEqualTo(5);
        }

        Check.ThatCode(() => Any.ListOf(Any.Int32()).WithCount(1).Containing(1).Containing(2)).Throws<ConflictingAnyConstraintException>();

        Check.ThatCode(() => Any.SetOf(Any.Int32()).Containing(7).Containing(7).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(duplicate => duplicate.Message).Contains("more than once");
    }

    [Fact(DisplayName = "Containing: a value drawn from a generator is forced into the collection.")]
    public void ContainingFromAGenerator() {
        for (int i = 0; i < SampleCount; i++) {
            List<int> list = Any.ListOf(Any.Int32().Between(1, 9)).NonEmpty().ContainingAny(Any.Int32().OneOf(4242)).Generate();
            Check.That(list).Contains(4242);
        }
    }

    [Fact(DisplayName = "Containing: a fixed value outside the element domain extends the effective cardinality (issue #188).")]
    public void ContainingOutsideDomainExtendsCardinality() {
        // The motivating case: {1, 2, 3} is satisfiable — 3 is supplied directly and lies outside the {1, 2} the
        // generator can produce, so only two elements must be drawn from it.
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().OneOf(1, 2)).Containing(3).WithCount(3).Generate();
            Check.That(set).Contains(1, 2, 3);
            Check.That(set.Count).IsEqualTo(3);
        }

        // The same reasoning holds for a distinct list and for several out-of-domain values at once. Dictionary keys
        // run through the very same CollectionState path, so the correction reaches them too, now exercised directly
        // through AnyDictionary.ContainingKey (see DictionaryContainingKeyOutsideDomainExtendsCardinality).
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> list = [.. Any.ListOf(Any.Int32().OneOf(1, 2)).Containing(3).WithCount(3).Distinct().Generate()];
            Check.That(list).Contains(1, 2, 3);

            HashSet<int> quad = Any.SetOf(Any.Int32().OneOf(1, 2)).Containing(3).Containing(4).WithCount(4).Generate();
            Check.That(quad).Contains(1, 2, 3, 4);
            Check.That(quad.Count).IsEqualTo(4);
        }
    }

    [Fact(DisplayName = "Containing: a value already inside the element domain does not inflate the cardinality, so an impossible count still conflicts.")]
    public void ContainingInsideDomainDoesNotInflate() {
        // 1 is already producible by the generator, so it adds no capacity: three distinct values over {1, 2} remain
        // impossible and must still be refused before any element is drawn, naming the shortfall.
        Check.ThatCode(() => Any.SetOf(Any.Int32().OneOf(1, 2)).Containing(1).WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("2 distinct value");

        // Mixed: 1 is inside the domain, 5 is outside — effective capacity is 2 + 1 = 3. Four is over the top; three
        // is exactly reachable as {1, 2, 5}.
        Check.ThatCode(() => Any.SetOf(Any.Int32().OneOf(1, 2)).Containing(1).Containing(5).WithCount(4).Generate()).Throws<ConflictingAnyConstraintException>();
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().OneOf(1, 2)).Containing(1).Containing(5).WithCount(3).Generate();
            Check.That(set).Contains(1, 2, 5);
        }
    }

    [Fact(DisplayName = "Containing: the effective cardinality is order-independent across Distinct, Containing and the count.")]
    public void EffectiveCardinalityIsOrderIndependent() {
        // Every ordering of the same three constraints reaches the same verdict — accepted, because 3 is outside the
        // domain — since Distinct() re-runs the whole validation on the accumulated state.
        for (int i = 0; i < SampleCount; i++) {
            Check.That(new HashSet<int>(Any.ListOf(Any.Int32().OneOf(1, 2)).WithCount(3).Containing(3).Distinct().Generate())).Contains(1, 2, 3);
            Check.That(new HashSet<int>(Any.ListOf(Any.Int32().OneOf(1, 2)).Distinct().Containing(3).WithCount(3).Generate())).Contains(1, 2, 3);
            Check.That(new HashSet<int>(Any.ListOf(Any.Int32().OneOf(1, 2)).Containing(3).WithCount(3).Distinct().Generate())).Contains(1, 2, 3);
        }

        // And rejected whatever the order, because the contained value is inside the domain.
        Check.ThatCode(() => Any.ListOf(Any.Int32().OneOf(1, 2)).WithCount(3).Containing(1).Distinct().Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32().OneOf(1, 2)).Distinct().Containing(1).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.ListOf(Any.Int32().OneOf(1, 2)).Containing(1).WithCount(3).Distinct().Generate()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Every widening constraint is honoured wherever in the chain it was declared.")]
    public void AWideningConstraintIsHonouredWhereverItWasDeclared() {
        // The shapes a sweep of the collection surface found order-sensitive. Three calls widen what a distinct
        // collection can reach -- Containing with a value the element generator cannot produce, ContainingAny, and
        // Distinct(comparer) with an equality finer than the default -- and each of them used to arrive too late
        // when it was written after the count. The same constraint set was refused in one order and honoured in
        // another; all of these draw.
        for (int i = 0; i < SampleCount; i++) {
            Check.That(Any.SetOf(Any.Int32().Between(1, 3)).WithCount(4).Containing(99).Generate()).Contains(99);
            Check.That(Any.SetOf(Any.Int32().Between(1, 3)).WithCount(4).ContainingAny(Any.Int32().Between(50, 60)).Generate().Count).IsEqualTo(4);
            Check.That(Any.ListOf(Any.Int32().Between(1, 2)).Distinct().WithCount(3).Containing(99).Generate()).Contains(99);
            Check.That(Any.ListOf(Any.Int32().Between(1, 2)).WithCount(3).Distinct().Containing(99).Generate()).Contains(99);
            Check.That(Any.ListOf(Any.Int32().Between(1, 2)).Distinct().WithCount(3).ContainingAny(Any.Int32().Between(50, 60)).Generate().Count).IsEqualTo(3);
        }
    }

    [Fact(DisplayName = "A comparer finer than the default is honoured when it is declared last.")]
    public void AFinerComparerIsHonouredWhenDeclaredLast() {
        // Distinct(comparer) widens twice over: it raises the cardinality the count is measured against, and it
        // tells apart two pinned values the default equality merges. Declared after the count and after the pins it
        // used to arrive too late for both, so the chain was refused for a shortfall the comparer removes.
        DateTimeOffset noon    = new(2020, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset elsewhere = new(2020, 1, 1, 13, 0, 0, TimeSpan.FromHours(1));

        Check.That(noon.EqualsExact(elsewhere)).IsFalse();    // two spellings...
        Check.That(noon).IsEqualTo(elsewhere);                // ...of one instant

        List<DateTimeOffset> pinned = Any.ListOf(Any.DateTimeOffset())
                                         .Distinct()
                                         .Containing(noon)
                                         .Containing(elsewhere)
                                         .Distinct(new BySpellingComparer())
                                         .Generate();

        Check.That(pinned).Contains(noon, elsewhere);

        // And the same for the cardinality it raises: one instant across a ten-hour offset range is one value by
        // default and many under the finer equality, so six of them fit.
        DateTimeOffset start = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        AnyDateTimeOffset ranged = Any.DateTimeOffset()
                                      .Between(start, start.AddSeconds(2))
                                      .WithGranularity(TimeSpan.FromSeconds(1))
                                      .WithOffsetBetween(TimeSpan.Zero, TimeSpan.FromHours(10));

        Check.That(Any.ListOf(ranged).Distinct().WithCount(6).Distinct(new BySpellingComparer()).Generate().Count).IsEqualTo(6);
    }

    [Fact(DisplayName = "Containing: a comparer stricter than the default one does not turn a satisfiable spec into a conflict.")]
    public void AComparerStricterThanTheDefaultDoesNotCauseAFalseConflict() {
        // Regression: FixedOutsideCount asked the element generator's cardinality hint whether a pinned value was
        // already inside its domain, and that hint answers under the DEFAULT comparer. Under reference equality the
        // two Tag(1) below are two distinct values, so { pooled, pinned } is a legal two-element distinct list — but
        // the hint reported the pinned one as already-inside, the effective domain stayed at one, and the declaration
        // was refused for a specification the collection can satisfy. The comment defending it claimed a custom
        // comparer "can only merge values, never create new ones"; a stricter one splits them instead.
        Tag pooled = new(1);
        Tag pinned = new(1);

        Check.That(pooled).IsEqualTo(pinned);                     // equal by value
        Check.That(ReferenceEquals(pooled, pinned)).IsFalse();    // distinct by reference

        List<Tag> list = Any.ListOf(Any.OneOf(pooled))
                            .Distinct(new ReferenceComparer())
                            .Containing(pinned)
                            .WithCount(2)
                            .Generate();

        Check.That(list.Count).IsEqualTo(2);
        Check.That(list.Count(element => ReferenceEquals(element, pinned))).IsEqualTo(1);
        Check.That(list.Count(element => ReferenceEquals(element, pooled))).IsEqualTo(1);
    }

    [Fact(DisplayName = "Containing: the default comparer still refuses a pinned value the element generator already covers.")]
    public void TheDefaultComparerStillRefusesAnInDomainPinnedValue() {
        // The other side of the same guard: relaxing the eager check under a CUSTOM comparer must not relax it when
        // there is none. Without a comparer the hint answers under the very equality the collection will use, so a
        // pinned value inside the domain does not extend it and the conflict is still caught at declaration.
        Tag pooled = new(1);
        Tag pinned = new(1);

        Check.ThatCode(() => Any.ListOf(Any.OneOf(pooled)).Distinct().Containing(pinned).WithCount(2).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Distinct: a comparer finer than the default does not refuse a satisfiable count over an offset range.")]
    public void AComparerFinerThanTheDefaultDoesNotRefuseAnOffsetRange() {
        // The bound's turn, after the membership two tests up: the SAME faulty reasoning, on the other member of the
        // same interface. DistinctCardinality was held to survive any comparer because a comparer "can only merge
        // values, never split them" -- true while the default comparer is the finest equality the type admits, and
        // false for DateTimeOffset, whose Equals compares the instant and ignores the offset. One instant across a
        // five-hour offset range is one value by default and 241 under EqualsExact; the bound said one and the
        // declaration below -- satisfiable, and satisfied here -- was refused as exceeding it.
        DateTimeOffset   instant = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        AnyDateTimeOffset ranged = Any.DateTimeOffset().Between(instant, instant).WithOffsetBetween(TimeSpan.FromHours(-2), TimeSpan.FromHours(2));

        List<DateTimeOffset> list = Any.ListOf(ranged).Distinct(new BySpellingComparer()).WithCount(3).Generate();

        Check.That(list.Count).IsEqualTo(3);
        Check.That(list.Select(value => value.Offset).Distinct().Count()).IsEqualTo(3);
        // All three are the same instant: it is the spelling the comparer keeps apart, nothing else.
        Check.That(list.Select(value => value.UtcTicks).Distinct().Count()).IsEqualTo(1);
    }

    [Fact(DisplayName = "Distinct: the default comparer still refuses three values drawn from one instant.")]
    public void TheDefaultComparerStillRefusesThreeSpellingsOfOneInstant() {
        // The other side of the same guard, as above: relaxing the bound under a CUSTOM comparer must not relax it
        // when there is none. DateTimeOffset equality is by instant, so under the default comparer the three
        // spellings really are one value and the refusal is correct.
        DateTimeOffset   instant = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        AnyDateTimeOffset ranged = Any.DateTimeOffset().Between(instant, instant).WithOffsetBetween(TimeSpan.FromHours(-2), TimeSpan.FromHours(2));

        Check.ThatCode(() => Any.ListOf(ranged).Distinct().WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Distinct: a pool keeps its bound under a finer comparer, because it draws one spelling per instant.")]
    public void APoolKeepsItsBoundUnderAFinerComparer() {
        // Not a blanket refusal to count: the offset range only splits an instant when the draw picks a minute from
        // it, and a pool short-circuits before that, returning one supplied spelling per instant. Two instants are
        // two values under any comparer, so three is still refused eagerly -- the eager check is given up exactly
        // where it was wrong, and nowhere else.
        DateTimeOffset first  = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset second = new(2026, 1, 2, 12, 0, 0, TimeSpan.Zero);

        AnyDateTimeOffset pooled = Any.DateTimeOffset().OneOf(first, second).WithOffsetBetween(TimeSpan.FromHours(-2), TimeSpan.FromHours(2));

        Check.ThatCode(() => Any.ListOf(pooled).Distinct(new BySpellingComparer()).WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "Containing: a near-maximum element cardinality plus an out-of-domain value does not overflow into a false conflict.")]
    public void ContainingNearMaximumCardinalityDoesNotOverflow() {
        // Between(0, long.MaxValue - 1) advertises long.MaxValue distinct values; the additive form base + extras
        // would overflow to a negative and reject spuriously. The subtractive check stays correct.
        for (int i = 0; i < SampleCount; i++) {
            HashSet<long> set = Any.SetOf(Any.Int64().Between(0, long.MaxValue - 1)).Containing(-1L).WithCount(3).Generate();
            Check.That(set).Contains(-1L);
            Check.That(set.Count).IsEqualTo(3);
        }
    }

    [Fact(DisplayName = "ContainingAny stays conservative: no eager false conflict, and an opaque shortfall surfaces at generation.")]
    public void ContainingAnyDefersToGeneration() {
        // The generator drawn from can yield a value outside the element domain, so the request cannot be proven
        // impossible at declaration — a wide ContainingAny makes it genuinely satisfiable.
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().OneOf(1, 2)).ContainingAny(Any.Int32().GreaterThan(100)).WithCount(3).Generate();
            Check.That(set.Count).IsEqualTo(3);
            Check.That(set).Contains(1, 2);
        }

        // When every source draws from the same two-value domain, three distinct values are impossible — but the
        // overlap is opaque, so it is caught while drawing (a replayable AnyGenerationException) rather than as a
        // false eager conflict.
        Check.ThatCode(() => Any.SetOf(Any.Boolean()).ContainingAny(Any.Boolean()).ContainingAny(Any.Boolean()).ContainingAny(Any.Boolean()).Generate())
             .Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "Containing under a merging comparer: an out-of-domain value is credited, and a comparer that merges it back is caught at generation.")]
    public void ContainingUnderAMergingComparer() {
        IEqualityComparer<int> modTen = new ModuloComparer(10);

        // 15 is outside {1, 2, 3} and its residue class (5) is fresh too, so {1, 2, 3, 15} has four classes and
        // generation succeeds.
        for (int i = 0; i < SampleCount; i++) {
            HashSet<int> set = Any.SetOf(Any.Int32().OneOf(1, 2, 3), modTen).Containing(15).WithCount(4).Generate();
            Check.That(set.Count).IsEqualTo(4);
        }

        // 12 is outside {1, 2} by value, so it is still credited and the request is accepted eagerly — but 12 ≡ 2
        // (mod 10) collapses it back into the domain, so three distinct-under-the-comparer values are impossible and
        // the shortfall surfaces while drawing, never as a false eager conflict.
        Check.ThatCode(() => Any.SetOf(Any.Int32().OneOf(1, 2), modTen).Containing(12).WithCount(3).Generate()).Throws<AnyGenerationException>();
    }

    [Fact(DisplayName = "The eager perimeter reaches every finite generator: decimal, floating-point and 128-bit allow-lists gate distinct collections too.")]
    public void FiniteScalarGeneratorsGateEagerly() {
        // A finite allow-list or a narrow range over decimal, double, single or Int128 now advertises its cardinality,
        // so a count beyond it conflicts at declaration — the same promise integers and enums already kept, held
        // across the whole knowable perimeter rather than only part of it.
        Check.ThatCode(() => Any.SetOf(Any.Decimal().OneOf(1m, 2m)).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Double().OneOf(1d, 2d)).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Single().OneOf(1f, 2f)).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
#if NET8_0_OR_GREATER
        Check.ThatCode(() => Any.SetOf(Any.Int128().Between(1, 3)).WithCount(5).Generate()).Throws<ConflictingAnyConstraintException>();
#endif

        // Membership travels with cardinality: an out-of-domain contained value extends the effective domain...
        for (int i = 0; i < SampleCount; i++) {
            HashSet<decimal> set = Any.SetOf(Any.Decimal().OneOf(1m, 2m)).Containing(3m).WithCount(3).Generate();
            Check.That(set).Contains(1m, 2m, 3m);
        }

        // ...while a contained value already inside it does not, so an impossible count still conflicts eagerly.
        Check.ThatCode(() => Any.SetOf(Any.Decimal().OneOf(1m, 2m)).Containing(1m).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Double().OneOf(1d, 2d)).Containing(2d).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "A validated pin is a singleton domain: a distinct collection asking for more than one conflicts eagerly.")]
    public void SingletonScalarDomainsGateEagerly() {
        // Zero()/Between(x, x) pins the domain to a single value; asking a distinct collection for two is a fully
        // knowable contradiction, so it must fail at declaration, not only while drawing.
        Check.ThatCode(() => Any.SetOf(Any.Decimal().Zero()).WithCount(2).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Double().Between(1d, 1d)).WithCount(2).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Single().Zero()).WithCount(2).Generate()).Throws<ConflictingAnyConstraintException>();

        // The singleton still generates at count one, and an out-of-domain contained value extends it as usual.
        for (int i = 0; i < SampleCount; i++) {
            HashSet<decimal> one = Any.SetOf(Any.Decimal().Zero()).WithCount(1).Generate();
            Check.That(one).ContainsExactly(0m);

            HashSet<decimal> two = Any.SetOf(Any.Decimal().Zero()).Containing(5m).WithCount(2).Generate();
            Check.That(two).Contains(0m, 5m);
        }
    }

    [Fact(DisplayName = "ArrayOf: produces an array of the requested size, distinct when asked.")]
    public void ArrayOfProducesArrays() {
        for (int i = 0; i < SampleCount; i++) {
            int[] array = Any.ArrayOf(Any.Int32().Between(1, 100)).WithCount(6).Distinct().Generate();
            Check.That(array.Length).IsEqualTo(6);
            Check.That(new HashSet<int>(array).Count).IsEqualTo(6);
        }
    }

    [Fact(DisplayName = "SequenceOf: is fully materialized — enumerating twice yields the same elements without re-drawing.")]
    public void SequenceOfIsMaterialized() {
        IEnumerable<int> sequence = Any.SequenceOf(Any.Int32()).WithCount(5).Generate();

        List<int> first  = sequence.ToList();
        List<int> second = sequence.ToList();

        Check.That(first).ContainsExactly(second);
    }

    [Fact(DisplayName = "DictionaryOf: builds unique-keyed dictionaries and gates the count by the key domain.")]
    public void DictionaryOfBehaves() {
        for (int i = 0; i < SampleCount; i++) {
            Dictionary<int, string> dictionary = Any.DictionaryOf(Any.Int32().Between(1, 1000), Any.String().NonEmpty()).WithCount(8).Generate();
            Check.That(dictionary.Count).IsEqualTo(8);
            Check.That(dictionary.Values).ContainsOnlyElementsThatMatch(value => value.Length > 0);
        }

        Check.ThatCode(() => Any.DictionaryOf(Any.Boolean(), Any.Int32()).WithCount(3).Generate()).Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.DictionaryOf<int, int>(null!, Any.Int32())).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ContainingKey: a key outside the key domain extends the effective cardinality (issue #225).")]
    public void DictionaryContainingKeyOutsideDomainExtendsCardinality() {
        // Mirrors ContainingOutsideDomainExtendsCardinality on the dictionary surface: {1, 2} is all the key
        // generator can produce, and 3 is supplied directly from outside that domain, so a three-entry dictionary is
        // satisfiable — the out-of-domain cardinality-credit path AnyDictionary could not exercise before.
        for (int i = 0; i < SampleCount; i++) {
            Dictionary<int, string> dictionary =
                Any.DictionaryOf(Any.Int32().OneOf(1, 2), Any.String().NonEmpty()).ContainingKey(3).WithCount(3).Generate();
            Check.That(dictionary.Keys).Contains(1, 2, 3);
            Check.That(dictionary.Count).IsEqualTo(3);
            Check.That(dictionary.Values).ContainsOnlyElementsThatMatch(value => value.Length > 0);
        }
    }

    [Fact(DisplayName = "ContainingKey: a within-domain key is present, an out-of-capacity one is still refused before any entry is drawn.")]
    public void DictionaryContainingKeyInsideDomainDoesNotInflate() {
        // A within-domain fixed key is present and adds no capacity of its own.
        for (int i = 0; i < SampleCount; i++) {
            Dictionary<int, string> dictionary =
                Any.DictionaryOf(Any.Int32().Between(1, 9), Any.String().NonEmpty()).WithCount(5).ContainingKey(7).Generate();
            Check.That(dictionary.ContainsKey(7)).IsTrue();
            Check.That(dictionary.Count).IsEqualTo(5);
        }

        // 1 is already producible, so three distinct keys over {1, 2} remain impossible and are still refused before
        // any entry is drawn, naming the shortfall — exactly as ContainingInsideDomainDoesNotInflate asserts for a set.
        Check.ThatCode(() => Any.DictionaryOf(Any.Int32().OneOf(1, 2), Any.String().NonEmpty()).ContainingKey(1).WithCount(3).Generate())
             .Throws<ConflictingAnyConstraintException>()
             .WhichMember(conflict => conflict.Message).Contains("2 distinct value");
    }

    [Fact(DisplayName = "ContainingAnyKey: a key drawn from a generator is forced into the dictionary; null is rejected (issue #287).")]
    public void DictionaryContainingAnyKeyForcesADrawnKey() {
        // The drawn key (4242) lies outside the key generator's own {1..9} domain, so it is supplied directly and
        // extends the effective cardinality — the ContainingAny path, now reaching dictionary keys.
        for (int i = 0; i < SampleCount; i++) {
            Dictionary<int, string> dictionary =
                Any.DictionaryOf(Any.Int32().Between(1, 9), Any.String().NonEmpty()).NonEmpty().ContainingAnyKey(Any.Int32().OneOf(4242)).Generate();
            Check.That(dictionary.ContainsKey(4242)).IsTrue();
        }

        Check.ThatCode(() => Any.DictionaryOf(Any.Int32(), Any.Int32()).ContainingAnyKey(null!)).Throws<ArgumentNullException>();
    }

    [Fact(DisplayName = "ContainingEntry: pins the value for a required key, extending cardinality out of domain (issue #288).")]
    public void DictionaryContainingEntryPinsTheValue() {
        for (int i = 0; i < SampleCount; i++) {
            // Key 3 is outside the key domain {1, 2} (supplied directly, extends cardinality); value 99 is outside
            // the value domain {1..9}, proving it is the pinned value rather than a generated one.
            Dictionary<int, int> dictionary =
                Any.DictionaryOf(Any.Int32().OneOf(1, 2), Any.Int32().Between(1, 9))
                   .ContainingEntry(3, 99)
                   .WithCount(3)
                   .Generate();
            Check.That(dictionary.Keys).Contains(1, 2, 3);
            Check.That(dictionary[3]).IsEqualTo(99);
            Check.That(dictionary.Count).IsEqualTo(3);
        }
    }

    [Fact(DisplayName = "ContainingEntry: pinning the same key twice — or an entry and a ContainingKey — conflicts.")]
    public void DictionaryContainingEntryDuplicateKeyConflicts() {
        Check.ThatCode(() => Any.DictionaryOf(Any.Int32(), Any.Int32()).ContainingEntry(1, 10).ContainingEntry(1, 20).Generate())
             .Throws<ConflictingAnyConstraintException>();

        Check.ThatCode(() => Any.DictionaryOf(Any.Int32(), Any.Int32()).ContainingKey(1).ContainingEntry(1, 20).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    [Fact(DisplayName = "PairOf and TripleOf assemble value tuples from constrained parts.")]
    public void PairAndTriple() {
        for (int i = 0; i < SampleCount; i++) {
            (int first, string second) pair = Any.PairOf(Any.Int32().Positive(), Any.String().NonEmpty()).Generate();
            Check.That(pair.first).IsStrictlyGreaterThan(0);
            Check.That(pair.second).IsNotEmpty();

            (int a, int b, int c) triple = Any.TripleOf(Any.Int32().Between(1, 2), Any.Int32().Between(3, 4), Any.Int32().Between(5, 6)).Generate();
            Check.That(triple.a is 1 or 2).IsTrue();
            Check.That(triple.b is 3 or 4).IsTrue();
            Check.That(triple.c is 5 or 6).IsTrue();
        }
    }

    [Fact(DisplayName = "Collections are reproducible when their element generator draws from a seeded context.")]
    public void CollectionsAreReproducible() {
        HashSet<int> first  = Any.SetOf(Any.WithSeed(4242).Int32()).WithCount(6).Generate();
        HashSet<int> second = Any.SetOf(Any.WithSeed(4242).Int32()).WithCount(6).Generate();

        Check.That(second.OrderBy(value => value)).ContainsExactly(first.OrderBy(value => value));

        List<int> listOne = Any.ListOf(Any.WithSeed(7).Int32().Between(0, 99)).WithCount(5).Generate();
        List<int> listTwo = Any.ListOf(Any.WithSeed(7).Int32().Between(0, 99)).WithCount(5).Generate();
        Check.That(listTwo).ContainsExactly(listOne);
    }

    [Fact(DisplayName = "Collections compose into value objects and aggregates through As and Combine.")]
    public void CollectionsComposeThroughAsAndCombine() {
        IAny<List<OrderReference>> references = Any.ListOf(Any.String().StartingWith("ORD-").WithLength(12).As(OrderReference.Create)).WithCount(3);

        List<OrderReference> list = references.Generate();
        Check.That(list.Count).IsEqualTo(3);
        Check.That(list).ContainsOnlyElementsThatMatch(reference => reference.Value.StartsWith("ORD-"));
    }

    [Fact(DisplayName = "Exhaustion over a foreign element generator qualifies the replay hint instead of promising a full replay of the elements.")]
    public void ExhaustionOverAForeignElementGeneratorQualifiesTheHint() {
        // A foreign IAny carries no IHasRandomSource, so the collection falls back to the ambient source for its count
        // and layout while the foreign generator's own draws ignore that seed. The reported seed therefore cannot
        // replay the elements, and the message must not claim it can.
        Check.ThatCode(() => Any.Reproducibly(2026, () => Any.SetOf(new ForeignPair()).WithCount(5).Generate(), _ => { }))
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 2026)
             .And.WhichMember(caught => caught.Message)
             .Contains("the element generator")
             .And.Contains("not reproducible from this seed alone")
             .And.Contains("Any.Reproducibly(2026")
             // The faithful full-replay sentence must be gone — it is the false promise this fix removes.
             .And.Not.Contains("The arbitrary values were seeded with");
    }

    [Fact(DisplayName = "A derivation built over a foreign generator is qualified too: the discriminator is a null source, not the IHasRandomSource type.")]
    public void ExhaustionOverAnAsDerivedForeignGeneratorQualifiesTheHint() {
        // DerivedAny (from As) implements IHasRandomSource but propagates a null source when its operand is foreign,
        // so its elements are as unreproducible as the foreign generator's. Keying on the type rather than the null
        // source would misclassify this as faithful and keep over-promising.
        IAny<int> derivedOverForeign = new ForeignPair().As(value => value);

        Check.ThatCode(() => Any.SetOf(derivedOverForeign).WithCount(5).Generate())
             .Throws<AnyGenerationException>()
             .WhichMember(caught => caught.Message)
             .Contains("not reproducible from this seed alone")
             .And.Not.Contains("The arbitrary values were seeded with");
    }

    [Fact(DisplayName = "A foreign ContainingAny generator is qualified at its own site, and a fixed source is named as Any.WithSeed rather than Any.Reproducibly.")]
    public void ExhaustionOverAForeignContainingAnyQualifiesAndNamesTheFixedSource() {
        // The collection's own elements come from a fixed Any.WithSeed(...) context (faithful), but the ContainingAny
        // draw is foreign. The twin exhaustion site must qualify the hint for that specific generator — and, because
        // the collection's source is fixed, name Any.WithSeed, never the inapplicable Any.Reproducibly.
        AnyContext seeded = Any.WithSeed(4242);

        Check.ThatCode(() => Any.SetOf(seeded.Int32()).Containing(0).Containing(1).ContainingAny(new ForeignPair()).Generate())
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 4242)
             .And.WhichMember(caught => caught.Message)
             .Contains("a ContainingAny(...) generator")
             .And.Contains("Any.WithSeed(4242)")
             .And.Contains("not reproducible from this seed alone")
             .And.Not.Contains("Any.Reproducibly(");
    }

    [Fact(DisplayName = "Exhaustion over a library element generator keeps the faithful full-replay hint unchanged.")]
    public void ExhaustionOverALibraryElementGeneratorKeepsTheFaithfulHint() {
        // A comparer collapses the effective domain below the requested count, so a library generator — whose draws do
        // follow the reported seed — exhausts the bounded draw. Its message must stay the faithful one: the fix only
        // touches the genuinely-foreign case.
        IEqualityComparer<int> modTen = new ModuloComparer(10);

        Check.ThatCode(() => Any.Reproducibly(1234, () => Any.SetOf(Any.Int32().Between(0, 999), modTen).WithCount(20).Generate(), _ => { }))
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 1234)
             .And.WhichMember(caught => caught.Message)
             .Contains("The arbitrary values were seeded with 1234")
             .And.Contains("Any.Reproducibly(1234")
             .And.Not.Contains("not reproducible from this seed alone");
    }

    [Fact(DisplayName = "Exhaustion over a Combine that mixes a foreign operand is qualified, even though a library operand supplies a non-null source.")]
    public void ExhaustionOverACombineMixingAForeignOperandQualifiesTheHint() {
        // Any.Combine keeps the library operand's non-null source (SourceOf(first) ?? SourceOf(second)), but the
        // composed value follows the foreign draw, so the elements are not reproducible from the reported seed. The
        // discriminator is full reproducibility, not merely a non-null source.
        IAny<int> mixed = Any.Combine(new ForeignPair(), Any.Int32(), (foreign, _) => foreign);

        Check.ThatCode(() => Any.Reproducibly(777, () => Any.SetOf(mixed).WithCount(5).Generate(), _ => { }))
             .Throws<AnyGenerationException>()
             .WithProperty(caught => caught.Seed, 777)
             .And.WhichMember(caught => caught.Message)
             .Contains("not reproducible from this seed alone")
             .And.Not.Contains("The arbitrary values were seeded with");
    }

    #region Nested types

    // A value-equal reference type: two Tag(1) are equal under the default comparer and distinct under reference
    // equality. That is the whole point — it is the ordinary shape of a domain value object, and the pair it forms
    // with ReferenceComparer is what makes a comparer STRICTER than the default one observable.
    private sealed class Tag {

        public Tag(int value) {
            Value = value;
        }

        private int Value { get; }

        public override bool Equals(object? obj) {
            return obj is Tag tag && tag.Value == Value;
        }

        public override int GetHashCode() {
            return Value;
        }

        public override string ToString() {
            return $"Tag({Value.ToString(CultureInfo.InvariantCulture)})";
        }

    }

    private sealed class ReferenceComparer : IEqualityComparer<Tag> {

        // Stricter than EqualityComparer<Tag>.Default: it splits value-equal instances rather than merging them.
        public bool Equals(Tag? x, Tag? y) {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(Tag obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }

    }

    private sealed class BySpellingComparer : IEqualityComparer<DateTimeOffset> {

        // Finer than EqualityComparer<DateTimeOffset>.Default, which compares the instant and ignores the offset:
        // EqualsExact is the BCL's own way of asking whether two values are the same SPELLING of that instant.
        public bool Equals(DateTimeOffset x, DateTimeOffset y) {
            return x.EqualsExact(y);
        }

        public int GetHashCode(DateTimeOffset obj) {
            return obj.Offset.GetHashCode() ^ obj.DateTime.GetHashCode();
        }

    }

    private sealed class ModuloComparer : IEqualityComparer<int> {

        private readonly int _modulus;

        public ModuloComparer(int modulus) {
            _modulus = modulus;
        }

        public bool Equals(int x, int y) {
            return x % _modulus == y % _modulus;
        }

        public int GetHashCode(int obj) {
            return obj % _modulus;
        }

    }

    private sealed class ForeignPair : IAny<int> {

        private int _n;

        // Foreign on purpose: implements IAny<int> but NOT IHasRandomSource, so it does not draw from the collection's
        // reported source. It yields only two distinct values (0 and 1), driving a distinct collection past its budget.
        public int Generate() {
            return _n++ % 2;
        }

    }

    #endregion

}
