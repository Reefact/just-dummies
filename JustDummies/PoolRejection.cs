#region Usings declarations

using System.Collections.ObjectModel;
using System.Diagnostics;

#endregion

namespace JustDummies;

/// <summary>
///     One value a caller supplied to a generator's pool that the declared constraints refuse, together with the
///     constraints that refuse it. It is what tells the two repairs apart when part of a catalogue never draws:
///     the value points at the catalogue, the constraints point at the invariant (ADR-0067).
/// </summary>
/// <remarks>
///     <para>
///         <see cref="RejectedBy" /> holds <b>every</b> declared constraint the value fails, rather than the first
///         one met. A value can miss for more than one reason, and reporting one of them would send a reader at a
///         constraint they could loosen without changing the verdict. The order is stable for a given generator,
///         but it is the specification's own and not the order the constraints were written in.
///     </para>
///     <para>
///         Instances are minted by the library, never by a caller: this is a report about a generator, so a
///         rejection nobody's constraints produced has nothing to describe.
///     </para>
/// </remarks>
/// <typeparam name="T">The type of the pooled value.</typeparam>
[DebuggerDisplay("{ToString()}")]
[ValueObject]
public sealed class PoolRejection<T> : IEquatable<PoolRejection<T>> {

    #region Statics members declarations

    private const int HashMultiplier = 397;

    #endregion

    /// <summary>
    ///     Determines whether two rejections carry the same value, refused by the same constraints in the same
    ///     order.
    /// </summary>
    /// <param name="left">The first rejection to compare.</param>
    /// <param name="right">The second rejection to compare.</param>
    /// <returns><c>true</c> when both describe the same rejection, or both are <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator ==(PoolRejection<T>? left, PoolRejection<T>? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two rejections describe different rejections.
    /// </summary>
    /// <param name="left">The first rejection to compare.</param>
    /// <param name="right">The second rejection to compare.</param>
    /// <returns><c>true</c> when they describe different rejections, or exactly one is <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator !=(PoolRejection<T>? left, PoolRejection<T>? right) {
        return !Equals(left, right);
    }

    // The read-only view is built once here rather than on each read, so RejectedBy stays a property: it hands back
    // a field instead of wrapping a list per call. Wrapping is what the caller's immutability costs -- handing the
    // inner list out directly would let a caller cast it back to List<T> and mutate a report.
    internal PoolRejection(T value, IReadOnlyList<DeclaredConstraint> rejectedBy) {
        // rejectedBy is guarded first on purpose: an unconstrained T reads as nullable, so a caller probing this
        // boundary one parameter at a time leaves the other null, and a value guard placed first would answer for
        // the wrong parameter.
        if (rejectedBy is null) { throw new ArgumentNullException(nameof(rejectedBy)); }
        if (value is null) { throw new ArgumentNullException(nameof(value)); }

        Value      = value;
        RejectedBy = new ReadOnlyCollection<DeclaredConstraint>(rejectedBy.ToArray());
    }

    /// <summary>The declared constraints this value fails, in a stable order.</summary>
    public IReadOnlyList<DeclaredConstraint> RejectedBy { get; }

    /// <summary>The value the caller supplied, which the generator can never draw as constrained.</summary>
    public T Value { get; }

    /// <summary>
    ///     Renders the rejection for a reader: the value, then the constraints that refuse it.
    /// </summary>
    /// <returns>The rejection, such as <c>"de" rejected by WithLength(3)</c>.</returns>
    public override string ToString() {
        return $"{Value} rejected by {string.Join(", ", RejectedBy)}";
    }

    /// <inheritdoc />
    public bool Equals(PoolRejection<T>? other) {
        return other is not null
            && EqualityComparer<T>.Default.Equals(Value, other.Value)
            && RejectedBy.SequenceEqual(other.RejectedBy);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is PoolRejection<T> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        unchecked {
            int hash = EqualityComparer<T>.Default.GetHashCode(Value!);
            foreach (DeclaredConstraint constraint in RejectedBy) {
                hash = (hash * HashMultiplier) ^ constraint.GetHashCode();
            }

            return hash;
        }
    }

}
