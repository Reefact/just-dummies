using System.Collections.Generic;
using System.Globalization;

namespace JustDummies.GenAny;

/// <summary>
///     One constraint read from one guard clause: the member to call, its argument, and the bound it sets.
/// </summary>
/// <remarks>
///     The bound is what makes composition decidable. Two guards that bound different things — a floor and a
///     ceiling — are the ordinary bounded-range idiom and both are kept; two that set the same bound to
///     different values are irreconcilable, and the engine drops them rather than pick one (§5.3).
/// </remarks>
internal sealed class GuardConstraint {

    internal GuardConstraint(string member, string? argument, Bound bound, decimal? value = null, bool exclusive = false) {
        Member    = member;
        Argument  = argument;
        Bound     = bound;
        Value     = value;
        Exclusive = exclusive;
    }

    /// <summary>The constraint member — <c>NonEmpty</c>, <c>WithMaxLength</c>, <c>Positive</c>.</summary>
    internal string Member { get; }

    /// <summary>Its argument as the emitted code spells it, or null for a member that takes none.</summary>
    internal string? Argument { get; }

    /// <summary>Which bound this sets, so a second guard on the same one can be recognised as a collision.</summary>
    internal Bound Bound { get; }

    /// <summary>The bound's value where it has one, so a floor above a ceiling can be caught.</summary>
    internal decimal? Value { get; }

    /// <summary>
    ///     Whether the bound excludes its own value.
    /// </summary>
    /// <remarks>
    ///     <c>Positive</c> is a floor at zero that zero itself does not satisfy, and saying so is what keeps it
    ///     honest on both sides of the type divide: on an integral type it means one, on a <c>decimal</c> it
    ///     means anything above zero, and a single value would have to be wrong for one of them.
    ///     <c>Positive().LessThanOrEqualTo(0.5m)</c> draws perfectly well, and a floor written as <c>1</c> would
    ///     have declared it empty.
    /// </remarks>
    internal bool Exclusive { get; }

    /// <summary>How many arguments the member takes, which is what ADR-0059 is checked against.</summary>
    internal int Arity => Argument is null ? 0 : 1;

    /// <summary>The call, as it appears in the emitted chain.</summary>
    internal string Render() {
        return Argument is null ? $".{Member}()" : $".{Member}({Argument})";
    }

    /// <summary>
    ///     Two readings that say exactly the same thing, so they collapse rather than collide.
    /// </summary>
    /// <remarks>
    ///     Which is what keeps a row's own refinement compatible with a guard repeating it: a <c>string</c> row
    ///     is already <c>.NonEmpty()</c>, and a constructor guarding on <c>IsNullOrWhiteSpace</c> says the same
    ///     thing rather than a second, contradictory thing.
    /// </remarks>
    internal static IEqualityComparer<GuardConstraint> SameCall { get; } = new ByCall();

    private sealed class ByCall : IEqualityComparer<GuardConstraint> {

        public bool Equals(GuardConstraint? left, GuardConstraint? right) {
            return left?.Member == right?.Member && left?.Argument == right?.Argument;
        }

        public int GetHashCode(GuardConstraint constraint) {
            return (constraint.Member + "(" + constraint.Argument + ")").GetHashCode();
        }

    }

    /// <inheritdoc />
    public override string ToString() {
        return Render() + " [" + Bound.ToString().ToLowerInvariant() + "]"
             + (Value is null ? string.Empty : " = " + Value.Value.ToString(CultureInfo.InvariantCulture));
    }

}
