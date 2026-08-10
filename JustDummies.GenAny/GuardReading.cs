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
    ///     Whether a throwing guard on <paramref name="parameter" /> was seen and not understood, so the
    ///     developer is told where to look rather than left to assume there was nothing there (§9).
    /// </summary>
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
    ///         Two that set the <b>same</b> bound to different values are irreconcilable, and so is a floor
    ///         above a ceiling: the library refuses such a chain at construction and <c>JD023</c> reports it at
    ///         compile time, so the engine must not write it. Two that say exactly the same thing are not a
    ///         collision — they collapse.
    ///     </para>
    /// </remarks>
    internal static IReadOnlyList<GuardConstraint> Combine(IReadOnlyList<GuardConstraint> read, out bool dropped) {
        List<GuardConstraint> kept = [.. read.Distinct(GuardConstraint.SameCall)];

        List<GuardConstraint> collided = kept.GroupBy(constraint => constraint.Bound)
                                             .Where(group => group.Count() > 1)
                                             .SelectMany(group => group)
                                             .ToList();

        if (Contradicts(kept)) {
            collided.AddRange(kept.Where(constraint => constraint.Bound is Bound.Lower or Bound.Upper));
        }

        dropped = collided.Count > 0;

        return [.. kept.Where(constraint => !collided.Contains(constraint))];
    }

    /// <summary>A floor above a ceiling admits nothing, so neither is emitted.</summary>
    private static bool Contradicts(IReadOnlyList<GuardConstraint> kept) {
        decimal? floor   = kept.FirstOrDefault(constraint => constraint.Bound == Bound.Lower)?.Value;
        decimal? ceiling = kept.FirstOrDefault(constraint => constraint.Bound == Bound.Upper)?.Value;

        return floor is not null && ceiling is not null && floor > ceiling;
    }

}
