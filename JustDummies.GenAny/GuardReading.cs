using System;
using System.Collections.Generic;
using System.Linq;

namespace JustDummies.GenAny;

/// <summary>
///     What one constructor's or factory's leading guards said about each of its parameters.
/// </summary>
internal sealed class GuardReading {

    private readonly Dictionary<string, List<GuardConstraint>> constraints = new(StringComparer.Ordinal);

    private readonly HashSet<string> unread = new(StringComparer.Ordinal);

    private GuardReading(bool sourceAvailable) {
        SourceAvailable = sourceAvailable;
    }

    /// <summary>
    ///     Whether a body was there to read at all. A type from a package has none, which is a different fact
    ///     from having no guards, and §6 reports it differently.
    /// </summary>
    internal bool SourceAvailable { get; }

    /// <summary>A method whose body the engine could not see.</summary>
    internal static GuardReading WithoutSource() {
        return new GuardReading(sourceAvailable: false);
    }

    /// <summary>A method whose body was read, whatever it turned out to contain.</summary>
    internal static GuardReading FromSource() {
        return new GuardReading(sourceAvailable: true);
    }

    /// <summary>The constraints read for <paramref name="parameter" />, in the order the guards appear.</summary>
    internal IReadOnlyList<GuardConstraint> For(string parameter) {
        return constraints.TryGetValue(parameter, out List<GuardConstraint>? read) ? read : [];
    }

    /// <summary>
    ///     Whether something refusing values of <paramref name="parameter" /> was seen and not vouched for,
    ///     so the developer is told where to look rather than left to assume there was nothing there (§9).
    /// </summary>
    /// <remarks>
    ///     Two ways to earn it, and the name understates the second. A guard the closed set could not parse
    ///     is the one it was written for. A guard the set parsed perfectly and the engine could not
    ///     <b>place</b> earns it just as much — below a write to its own parameter, under something deciding
    ///     whether it runs, or below a statement that can jump past it — and there the reading understood
    ///     everything and still cannot say the constraint is about the value the generator draws. What
    ///     reaches the recap is the same either way, because the developer's question is the same: is this
    ///     generator right for this parameter?
    /// </remarks>
    internal bool Unread(string parameter) {
        return unread.Contains(parameter);
    }

    internal void Add(string parameter, GuardConstraint constraint) {
        if (!constraints.TryGetValue(parameter, out List<GuardConstraint>? read)) {
            read = [];
            constraints[parameter] = read;
        }

        read.Add(constraint);
    }

    internal void MarkUnread(string parameter) {
        unread.Add(parameter);
    }

    /// <summary>
    ///     The constraints that survive being read together, and whether any had to be dropped (§5.3).
    /// </summary>
    /// <remarks>
    ///     Two guards that bound different things — a floor and a ceiling — are the ordinary bounded-range
    ///     idiom, written as two consecutive guards, and both are kept; discarding it would make guard reading
    ///     useless for the case it most often meets.
    ///     <para>
    ///         Everything else is interval arithmetic over the six members of <see cref="Bound" />, and being
    ///         only that is the point: a fixed table, never propagation. Two guards that bound the <b>same</b>
    ///         side are a conjunction, so the tighter one survives and the looser is dropped in silence — the
    ///         library folds them exactly that way, so writing both would emit a call that provably does
    ///         nothing. Bounds that leave <b>no</b> value are irreconcilable: the library refuses such a chain
    ///         at construction and <c>JD016</c>, <c>JD023</c> and their siblings report it at compile time, so
    ///         the engine must not write it in the first place. Two that say exactly the same thing are not a
    ///         collision — they collapse.
    ///     </para>
    ///     <para>
    ///         The one asymmetry is deliberate. A base-table refinement is the engine's own opinion and a guard
    ///         is the developer's declaration, so where the two cannot both hold, <b>the refinement yields</b>:
    ///         a constructor demanding a blank string is not contradicting itself, it is contradicting the row
    ///         that assumed strings are non-empty. Dropping both would emit a generator that violates a
    ///         perfectly good guard, and report a reconciliation nobody asked for.
    ///     </para>
    /// </remarks>
    internal static IReadOnlyList<GuardConstraint> Combine(IReadOnlyList<GuardConstraint> seeded,
                                                          IReadOnlyList<GuardConstraint> guards,
                                                          out bool dropped) {
        List<GuardConstraint> kept = [.. seeded.Concat(guards).Distinct(GuardConstraint.SameCall)];

        List<GuardConstraint> bounding = [.. kept.Where(Bounds)];

        if (bounding.Count == 0) {
            dropped = false;

            return kept;
        }

        if (!Admits(bounding)) {
            // A base-table refinement is an opinion, a guard is a declaration, and only one of them may be
            // wrong. Dropping the row's own NonEmpty leaves a chain that still honours every guard; dropping
            // the guard leaves one that does not — so the refinement yields, and the recap does not report a
            // reconciliation the developer never asked for.
            List<GuardConstraint> declared = [.. bounding.Where(constraint => !FromTheTableAlone(constraint, seeded, guards))];

            if (declared.Count < bounding.Count && Admits(declared)) {
                bounding = declared;
                kept     = [.. kept.Where(constraint => !Bounds(constraint) || declared.Contains(constraint))];
            } else {
                dropped = true;

                return [.. kept.Where(constraint => !Bounds(constraint))];
            }
        }

        dropped = false;

        GuardConstraint? floor   = Tightest(bounding, Floors, tighter: true);
        GuardConstraint? ceiling = Tightest(bounding, Ceilings, tighter: false);

        // Everything else bounded the same side more loosely, which is not a collision: two guards that both
        // throw are a conjunction, and the conjunction of two floors is the higher one. The library folds them
        // exactly this way and says nothing, so emitting both would write a call that provably does nothing.
        return [.. kept.Where(constraint => !Bounds(constraint) || constraint == floor || constraint == ceiling)];
    }

    /// <summary>Whether a constraint says where the value may lie, rather than what shape it takes.</summary>
    /// <remarks>
    ///     <c>NonEmpty</c> is in, because a length or a count of nothing is a floor of one however it is
    ///     spelled — that is the whole of D6. <c>NonZero</c> is out: it punches a hole, it does not move an
    ///     edge. So is a refinement carrying no value at all, such as the <c>Uri</c> row's <c>Web</c>, which is
    ///     tagged <c>Exact</c> to keep it from colliding rather than to place it on a number line.
    /// </remarks>
    private static bool Bounds(GuardConstraint constraint) {
        return constraint.Bound == Bound.Emptiness
            || (constraint.Value is not null && constraint.Bound is Bound.Lower or Bound.Upper or Bound.Exact);
    }

    private static bool FromTheTableAlone(GuardConstraint constraint, IReadOnlyList<GuardConstraint> seeded, IReadOnlyList<GuardConstraint> guards) {
        return seeded.Contains(constraint, GuardConstraint.SameCall)
            && !guards.Contains(constraint, GuardConstraint.SameCall);
    }

    /// <summary>Whether the floors and ceilings in <paramref name="bounding" /> leave any value at all.</summary>
    private static bool Admits(IReadOnlyList<GuardConstraint> bounding) {
        GuardConstraint? floor   = Tightest(bounding, Floors, tighter: true);
        GuardConstraint? ceiling = Tightest(bounding, Ceilings, tighter: false);

        if (floor is null || ceiling is null) { return true; }

        decimal low  = Edge(floor);
        decimal high = Edge(ceiling);

        return low < high || (low == high && !floor.Exclusive && !ceiling.Exclusive);
    }

    private static IEnumerable<GuardConstraint> Floors(IEnumerable<GuardConstraint> bounding) {
        return bounding.Where(constraint => constraint.Bound is Bound.Lower or Bound.Exact or Bound.Emptiness);
    }

    private static IEnumerable<GuardConstraint> Ceilings(IEnumerable<GuardConstraint> bounding) {
        return bounding.Where(constraint => constraint.Bound is Bound.Upper or Bound.Exact);
    }

    /// <summary>
    ///     The one constraint on that side nothing else beats, or null when that side is open.
    /// </summary>
    /// <remarks>
    ///     An exact size sits on both sides at once, which is what lets a floor above it be caught, and what
    ///     makes <c>WithCount(5).WithMinCount(3)</c> collapse to the exact call it already meant.
    /// </remarks>
    private static GuardConstraint? Tightest(IReadOnlyList<GuardConstraint> bounding,
                                             Func<IEnumerable<GuardConstraint>, IEnumerable<GuardConstraint>> side,
                                             bool tighter) {
        GuardConstraint? found = null;

        foreach (GuardConstraint constraint in side(bounding)) {
            if (found is null) {
                found = constraint;

                continue;
            }

            decimal here = Edge(constraint);
            decimal best = Edge(found);

            // At equal values the exclusive edge is the tighter one: `> 0` admits less than `>= 0`.
            if (here == best) {
                if (constraint.Exclusive && !found.Exclusive) { found = constraint; }

                continue;
            }

            if (tighter ? here > best : here < best) { found = constraint; }
        }

        return found;
    }

    /// <summary>Where a bound sits on the number line. <c>NonEmpty</c> is a length or a count of one.</summary>
    private static decimal Edge(GuardConstraint constraint) {
        return constraint.Bound == Bound.Emptiness ? 1m : constraint.Value!.Value;
    }

}
