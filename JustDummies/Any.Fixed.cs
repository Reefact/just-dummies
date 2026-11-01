namespace JustDummies;

public static partial class Any {

    /// <summary>
    ///     A generator that returns <paramref name="value" /> as given, every time — a literal the test supplies, in the
    ///     shape of a generator so it composes wherever an <see cref="IAny{T}" /> is expected: a pinned parameter of a
    ///     scaffolded generator, the branch of a dependent draw, an operand of <see cref="Combine{T1,T2,TResult}" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         It <b>consumes no draw</b>, so placing it in a chain shifts no other value of a replay, and it never
    ///         qualifies the replay hint of a composition it takes part in: it draws from no source at all.
    ///     </para>
    ///     <para>
    ///         It <b>accepts <c>null</c></b>. The value is a literal the test chose, not a draw, which is what lets a
    ///         nullable parameter be pinned to its absent case — where <see cref="OneOf{T}(T[])" /> refuses a
    ///         <c>null</c> element and consumes a draw.
    ///     </para>
    ///     <para>
    ///         It returns <b>the same instance</b> on every call. For a mutable object that instance is shared by every
    ///         value built from the generator: a test that mutates what one draw returned sees the change in the next. Pin
    ///         an immutable value, or build a fresh one through <see cref="AnyExtensions.As{TSource,TResult}" />, when the
    ///         instances must not alias (ADR-0118).
    ///     </para>
    /// </remarks>
    /// <param name="value">The value every generation returns; <c>null</c> is accepted.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>A generator returning <paramref name="value" />.</returns>
    public static IAny<T> Fixed<T>(T value) {
        return new FixedAny<T>(value);
    }

}
