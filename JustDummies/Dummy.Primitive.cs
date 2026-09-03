namespace JustDummies;

public static partial class Dummy {

    /// <summary>
    ///     Starts an arbitrary <see cref="string" /> generator drawing from the ambient random context. Unconstrained,
    ///     it yields 0 to 1024 characters drawn from the whole of ASCII, control characters included (ADR-0075,
    ///     ADR-0076); chain constraints to express what the surrounding code requires (<c>NonEmpty()</c>,
    ///     <c>WithMaxLength(...)</c>, <c>Printable()</c>, <c>StartingWith(...)</c>, ...).
    /// </summary>
    /// <returns>A string generator to constrain fluently.</returns>
    public static DummyString String() {
        return new DummyString(AmbientRandomSource.Instance, StringSpec.Unconstrained);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="int" /> generator drawing from the ambient random context. Unconstrained, it
    ///     draws from the full <see cref="int" /> range; chain constraints to express what the surrounding code
    ///     requires (<c>Positive()</c>, <c>Between(...)</c>, ...).
    /// </summary>
    /// <returns>An integer generator to constrain fluently.</returns>
    public static DummyInt32 Int32() {
        return DummyInt32.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="sbyte" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummySByte SByte() {
        return DummySByte.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="byte" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyByte Byte() {
        return DummyByte.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="short" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyInt16 Int16() {
        return DummyInt16.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ushort" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyUInt16 UInt16() {
        return DummyUInt16.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="uint" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyUInt32 UInt32() {
        return DummyUInt32.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="long" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyInt64 Int64() {
        return DummyInt64.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="ulong" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less <c>Positive()</c>
    ///     and <c>Negative()</c>, which an unsigned type cannot express.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyUInt64 UInt64() {
        return DummyUInt64.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeSpan" /> generator drawing from the ambient random context:
    ///     full range unless constrained, negative durations included. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c> and plus <c>WithGranularity(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyTimeSpan TimeSpan() {
        return DummyTimeSpan.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTime" /> generator drawing from the ambient random context:
    ///     any representable instant unless constrained; generated values carry Utc kind. Same constraint algebra as
    ///     <see cref="DummyInt32" /> with the bounds renamed <c>After(...)</c>/<c>Before(...)</c>: no sign or zero
    ///     constraint, no <c>MultipleOf(...)</c>, plus <c>WithGranularity(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyDateTime DateTime() {
        return DummyDateTime.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateTimeOffset" /> generator drawing from the ambient random context:
    ///     any representable instant unless constrained; generated values carry a zero (UTC) offset. Same constraint
    ///     algebra as <see cref="DummyInt32" /> with the bounds renamed <c>After(...)</c>/<c>Before(...)</c>: no sign or
    ///     zero constraint, no <c>MultipleOf(...)</c>, plus <c>WithGranularity(...)</c> and <c>WithOffset(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyDateTimeOffset DateTimeOffset() {
        return DummyDateTimeOffset.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="double" /> generator drawing from the ambient random context:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyDouble Double() {
        return DummyDouble.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="float" /> generator drawing from the ambient random context:
    ///     finite values only — NaN and infinities are never generated. Same constraint algebra as <see cref="DummyInt32"
    ///     />, less <c>MultipleOf(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummySingle Single() {
        return DummySingle.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="decimal" /> generator drawing from the ambient random context:
    ///     full range unless constrained. Same constraint algebra as <see cref="DummyInt32" />, less
    ///     <c>MultipleOf(...)</c> and plus <c>WithScale(...)</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyDecimal Decimal() {
        return DummyDecimal.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="bool" /> generator drawing from the ambient random context — an even coin
    ///     flip unless pinned with <c>True()</c> or <c>False()</c>.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyBoolean Boolean() {
        return DummyBoolean.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Guid" /> generator drawing from the ambient random context — unlike
    ///     <see cref="System.Guid.NewGuid" />, reproducible inside an <c>Dummy.Reproducibly(...)</c> run, and for every
    ///     practical purpose never empty.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyGuid Guid() {
        return DummyGuid.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <typeparamref name="TEnum" /> generator drawing from the ambient random context —
    ///     uniformly across the enum's declared members, never an undeclared numeric value.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to draw values from.</typeparam>
    /// <returns>A generator to constrain fluently.</returns>
    /// <exception cref="DummyGenerationException">Thrown when <typeparamref name="TEnum" /> declares no members.</exception>
    public static DummyEnum<TEnum> Enum<TEnum>()
        where TEnum : struct, Enum {
        return DummyEnum<TEnum>.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="char" /> generator drawing from the ambient random context — the whole of
    ///     ASCII, control characters included, unless constrained (ADR-0075), mirroring <see cref="DummyString" />'s
    ///     character families.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyChar Char() {
        return DummyChar.Create(AmbientRandomSource.Instance);
    }

#if NET8_0_OR_GREATER
    /// <summary>
    ///     Starts an arbitrary <see cref="System.DateOnly" /> generator drawing from the ambient random context — any
    ///     representable date unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyDateOnly DateOnly() {
        return DummyDateOnly.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.TimeOnly" /> generator drawing from the ambient random context — any
    ///     time of day unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyTimeOnly TimeOnly() {
        return DummyTimeOnly.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Int128" /> generator drawing from the ambient random context — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyInt128 Int128() {
        return DummyInt128.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.UInt128" /> generator drawing from the ambient random context — full
    ///     range unless constrained. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyUInt128 UInt128() {
        return DummyUInt128.Create(AmbientRandomSource.Instance);
    }

    /// <summary>
    ///     Starts an arbitrary <see cref="System.Half" /> generator drawing from the ambient random context — finite
    ///     values only. Net8.0 target only, like the type itself.
    /// </summary>
    /// <returns>A generator to constrain fluently.</returns>
    public static DummyHalf Half() {
        return DummyHalf.Create(AmbientRandomSource.Instance);
    }
#endif

}
