#region Usings declarations

using System.Collections.Generic;

using FsCheck;
using FsCheck.Fluent;

using JetBrains.Annotations;

#endregion

namespace JustDummies.PropertyTests;

/// <summary>
///     Property-based tests for <c>AsNullable</c>: the lift widens the type and changes nothing else.
/// </summary>
/// <remarks>
///     Two claims, quantified rather than sampled because each has to hold for <b>every</b> seed and every
///     bound, not for the ones a named case happened to pick. The first is what the member promises a reader —
///     the same values, in the same order. The second is what it promises a distinct collection, and it is the
///     one the general <c>As</c> hop could not keep: a set over the lift is satisfiable exactly when a set over
///     the underlying generator is.
/// </remarks>
[TestSubject(typeof(NullableExtensions))]
public sealed class NullableLiftProperties {

    #region Statics members declarations

    /// <summary>How many values each side draws before the two sequences are compared.</summary>
    private const int Drawn = 25;

    /// <summary>The enums the second property quantifies over, by how many members each declares.</summary>
    private enum One { Only }

    private enum Two { First, Second }

    private enum Three { First, Second, Third }

    private static List<int?> Lifted(int seed, int minimum, int maximum) {
        using IDisposable scope = Any.UseSeed(seed);

        return Drawings(Any.Int32().Between(minimum, maximum).AsNullable());
    }

    private static List<int?> Underlying(int seed, int minimum, int maximum) {
        using IDisposable scope = Any.UseSeed(seed);

        return Drawings(Any.Int32().Between(minimum, maximum), value => (int?)value);
    }

    private static List<int?> Drawings(IAny<int?> generator) {
        List<int?> values = [];

        for (int drawn = 0; drawn < Drawn; drawn++) { values.Add(generator.Generate()); }

        return values;
    }

    private static List<int?> Drawings(IAny<int> generator, Func<int, int?> lift) {
        List<int?> values = [];

        for (int drawn = 0; drawn < Drawn; drawn++) { values.Add(lift(generator.Generate())); }

        return values;
    }

    /// <summary>Whether a set of <paramref name="count" /> values of that enum can be drawn, lifted or not.</summary>
    private static (bool Lifted, bool Underlying) Satisfiable<T>(int count)
        where T : struct, Enum {
        return (Draws(() => Any.SetOf(Any.Enum<T>().AsNullable()).WithCount(count).Generate()),
                Draws(() => Any.SetOf(Any.Enum<T>()).WithCount(count).Generate()));
    }

    private static bool Draws(Action draw) {
        try {
            draw();

            return true;
        } catch (DummyException) {
            return false;
        }
    }

    #endregion

    [Fact(DisplayName = "The lift draws exactly what the underlying generator draws, seed for seed.")]
    public void TheLiftDrawsWhatTheUnderlyingGeneratorDraws() {
        Gen<(int Seed, int Minimum, int Span)> cases =
            from seed in Gen.Choose(1, 1_000_000)
            from minimum in Gen.Choose(-10_000, 10_000)
            from span in Gen.Choose(0, 5_000)
            select (Seed: seed, Minimum: minimum, Span: span);

        Prop.ForAll(cases.ToArbitrary(),
                    // Same seed, same bounds, same sequence — and never a null in it, which is the whole
                    // difference between this member and its OrNull sibling (ADR-0064).
                    triple => {
                        List<int?> lifted     = Lifted(triple.Seed, triple.Minimum, triple.Minimum + triple.Span);
                        List<int?> underlying = Underlying(triple.Seed, triple.Minimum, triple.Minimum + triple.Span);

                        return lifted.SequenceEqual(underlying) && lifted.TrueForAll(value => value.HasValue);
                    })
            .QuickCheckThrowOnFailure();
    }

    [Fact(DisplayName = "A set over the lift is satisfiable exactly when a set over the underlying generator is.")]
    public void ASetOverTheLiftIsSatisfiableExactlyWhenTheUnderlyingOneIs() {
        Gen<(int Members, int Count)> cases =
            from members in Gen.Choose(1, 3)
            from count in Gen.Choose(1, 5)
            select (Members: members, Count: count);

        Prop.ForAll(cases.ToArbitrary(),
                    // The claim the general As hop could not keep: the lift carries the underlying domain's size,
                    // so the two answer alike whichever side of the domain's edge the count falls on.
                    pair => {
                        (bool Lifted, bool Underlying) answers = pair.Members switch {
                            1 => Satisfiable<One>(pair.Count),
                            2 => Satisfiable<Two>(pair.Count),
                            _ => Satisfiable<Three>(pair.Count)
                        };

                        return answers.Lifted == answers.Underlying;
                    })
            .QuickCheckThrowOnFailure();
    }

}
