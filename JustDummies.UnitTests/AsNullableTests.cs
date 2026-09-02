#region Usings declarations

using System.Collections.Generic;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     <c>AsNullable</c> widens the type and leaves everything else alone — including what a distinct collection
///     is allowed to know about the domain.
/// </summary>
/// <remarks>
///     The sibling of <c>OrNull</c> and its opposite, so the two are worth reading together: <c>OrNull</c> draws
///     <c>null</c> about half the time, this one never does. It exists because the general
///     <c>As(value =&gt; (T?)value)</c> hop it replaces produced a derived generator, and a derived generator
///     advertises no cardinality — so a set of two <c>bool?</c> was refused on a domain that plainly holds two.
///     Every case below is the sequence a scaffolded generator runs into, not a synthetic one.
/// </remarks>
public sealed class AsNullableTests {

    private const int SampleCount = 200;

    private enum Slot { None, Morning, Evening }

    [Fact(DisplayName = "AsNullable never yields null, and every value honours the inner constraints.")]
    public void AsNullableNeverYieldsNull() {
        IAny<int?> generator = Any.Int32().Between(1, 100).AsNullable();

        for (int drawn = 0; drawn < SampleCount; drawn++) {
            int? value = generator.Generate();

            Check.That(value).IsNotNull();
            Check.That(value!.Value is >= 1 and <= 100).IsTrue();
        }
    }

    /// <summary>
    ///     The defect this member was added for, as the sequence that produced it.
    /// </summary>
    /// <remarks>
    ///     A set of nullable enums with a floor of one used to refuse: the set had no ceiling to draw a size
    ///     under, picked one the three-member domain could not fill, and exhausted its bounded redraw. The
    ///     assertion is not that some size comes back but that a draw comes back at all.
    /// </remarks>
    [Fact(DisplayName = "A distinct collection over a lifted generator draws, where one over a derived generator refused.")]
    public void ADistinctCollectionOverALiftedGeneratorDraws() {
        IAny<ISet<Slot?>> generator = Any.SetOf(Any.Enum<Slot>().AsNullable()).NonEmpty();

        for (int drawn = 0; drawn < SampleCount; drawn++) {
            ISet<Slot?> slots = generator.Generate();

            Check.That(slots).Not.IsEmpty();
            Check.That(slots.Count <= 3).IsTrue();
            Check.That(slots).Not.Contains((Slot?)null);
        }
    }

    /// <summary>
    ///     The lift is transparent: a set over it answers exactly as a set over the underlying generator does.
    /// </summary>
    /// <remarks>
    ///     Both halves matter and the second is the one the defect was. Three distinct values exist and a set of
    ///     three comes back; a fourth does not exist and both forms refuse it the same way, at the same point —
    ///     which is the claim "the lift changes the type and nothing else" reduced to something checkable.
    /// </remarks>
    [Fact(DisplayName = "A set over the lift answers exactly as a set over the underlying generator.")]
    public void TheLiftedDomainIsCountedExactly() {
        Check.That(Any.SetOf(Any.Enum<Slot>().AsNullable()).WithCount(3).Generate()).HasSize(3);
        Check.That(Any.SetOf(Any.Enum<Slot>()).WithCount(3).Generate()).HasSize(3);

        Check.ThatCode(() => Any.SetOf(Any.Enum<Slot>().AsNullable()).WithCount(4).Generate())
             .Throws<ConflictingAnyConstraintException>();
        Check.ThatCode(() => Any.SetOf(Any.Enum<Slot>()).WithCount(4).Generate())
             .Throws<ConflictingAnyConstraintException>();
    }

    /// <summary>
    ///     A pinned <c>null</c> extends the domain rather than sitting inside it.
    /// </summary>
    /// <remarks>
    ///     Which is the honest answer and not a conservative one: this generator never draws <c>null</c>, so a
    ///     collection pinning it is supplying a value the generator could not have produced. Counting it as
    ///     inside would refuse a size the collection can build.
    /// </remarks>
    [Fact(DisplayName = "A null pinned into a distinct collection extends the lifted domain.")]
    public void APinnedNullExtendsTheDomain() {
        ISet<Slot?> slots = Any.SetOf(Any.Enum<Slot>().AsNullable()).Containing(null).WithCount(4).Generate();

        Check.That(slots).HasSize(4);
        Check.That(slots).Contains((Slot?)null);
    }

    [Fact(DisplayName = "AsNullable refuses a null generator.")]
    public void AsNullableRefusesANullGenerator() {
        Check.ThatCode(() => ((IAny<int>)null!).AsNullable()).Throws<System.ArgumentNullException>();
    }

    /// <summary>The values replay from the seed, exactly as the wrapped generator's do.</summary>
    [Fact(DisplayName = "A lifted generator replays from its seed.")]
    public void ALiftedGeneratorReplaysFromItsSeed() {
        const int seed = 20260902;

        List<int?> first  = Drawn(seed);
        List<int?> second = Drawn(seed);

        Check.That(second).ContainsExactly(first);
    }

    private static List<int?> Drawn(int seed) {
        using IDisposable scope = Any.UseSeed(seed);

        IAny<int?>  generator = Any.Int32().Between(1, 1000).AsNullable();
        List<int?>  values    = [];

        for (int drawn = 0; drawn < 20; drawn++) { values.Add(generator.Generate()); }

        return values;
    }

}
