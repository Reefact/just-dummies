#region Usings declarations

using System.Diagnostics;

#endregion

namespace JustDummies;

/// <summary>
///     A constraint as the caller declared it, in the form a reader recognizes from the diagnostics —
///     <c>WithMinLength(2)</c>, <c>Numeric()</c>, <c>Except(...)</c>. It is what a
///     <see cref="PoolRejection{T}" /> names to say why a supplied value never draws, so the reason travels as a
///     value that can be compared and filtered rather than as text that would have to be parsed back (ADR-0042).
/// </summary>
/// <remarks>
///     <para>
///         Instances are minted by the library and never by a caller: this is a report about a generator, so a
///         constraint that was never declared has nothing to describe. Two of them are equal when they read the
///         same, which is what lets a caller group the rejections of one catalogue by the constraint that took
///         them.
///     </para>
///     <para>
///         <see cref="Arguments" /> carries what the declaring generator rendered between the parentheses, already
///         text: a length is its number, a value set is the quoted values. It reads <c>...</c> when the arguments
///         are ones the library must not render — a pool of an opaque element type, whose <c>ToString</c> belongs
///         to the caller and could be anything.
///     </para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
[ValueObject]
public sealed class DeclaredConstraint : IEquatable<DeclaredConstraint> {

    /// <summary>
    ///     Determines whether two declared constraints read the same.
    /// </summary>
    /// <param name="left">The first constraint to compare.</param>
    /// <param name="right">The second constraint to compare.</param>
    /// <returns><c>true</c> when both read the same, or both are <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator ==(DeclaredConstraint? left, DeclaredConstraint? right) {
        return Equals(left, right);
    }

    /// <summary>
    ///     Determines whether two declared constraints read differently.
    /// </summary>
    /// <param name="left">The first constraint to compare.</param>
    /// <param name="right">The second constraint to compare.</param>
    /// <returns><c>true</c> when they read differently, or exactly one is <c>null</c>; otherwise <c>false</c>.</returns>
    public static bool operator !=(DeclaredConstraint? left, DeclaredConstraint? right) {
        return !Equals(left, right);
    }

    #region Fields declarations

    private readonly string _rendered;

    #endregion

    internal DeclaredConstraint(string name, string arguments) {
        if (name is null) { throw new ArgumentNullException(nameof(name)); }
        if (arguments is null) { throw new ArgumentNullException(nameof(arguments)); }

        Name      = name;
        Arguments = arguments;
        _rendered = name + "(" + arguments + ")";
    }

    /// <summary>The arguments the declaring generator rendered, or <c>...</c> when it could not render them.</summary>
    public string Arguments { get; }

    /// <summary>The declaring method's name, such as <c>WithMinLength</c>.</summary>
    public string Name { get; }

    /// <summary>
    ///     Returns the constraint as the caller spelled it. Total by construction: the text was built when the
    ///     constraint was declared, so quoting one cannot fail.
    /// </summary>
    /// <returns>The rendered constraint, such as <c>WithMinLength(2)</c>.</returns>
    public override string ToString() {
        return _rendered;
    }

    /// <inheritdoc />
    public bool Equals(DeclaredConstraint? other) {
        return other is not null && string.Equals(_rendered, other._rendered, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) {
        return obj is DeclaredConstraint other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode() {
        return StringComparer.Ordinal.GetHashCode(_rendered);
    }

}
