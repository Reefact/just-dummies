using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     JustDummies has no generator for <see cref="BigInteger" />; this one composes the
///     library's own byte draws, so it follows the ambient seed like any other.
///     Immutable, in the shape of the library's builders: each constraint returns a new generator.
/// </summary>
public sealed class AnyBigInteger : IAny<BigInteger> {

    // A side left open is not unbounded in practice: it is drawn within 10^40 of the other side (or of zero).
    private static readonly BigInteger OrdinaryMagnitude = BigInteger.Pow(10, 40);

    private readonly BigInteger? _min;
    private readonly BigInteger? _max;

    public AnyBigInteger()
        : this(null, null) { }

    private AnyBigInteger(BigInteger? min, BigInteger? max) {
        _min = min;
        _max = max;
    }

    public AnyBigInteger Between(BigInteger min, BigInteger max) {
        return GreaterThanOrEqualTo(min).LessThanOrEqualTo(max);
    }

    public AnyBigInteger GreaterThanOrEqualTo(BigInteger min) {
        return new AnyBigInteger(_min is { } current ? BigInteger.Max(current, min) : min, _max).Checked();
    }

    public AnyBigInteger LessThanOrEqualTo(BigInteger max) {
        return new AnyBigInteger(_min, _max is { } current ? BigInteger.Min(current, max) : max).Checked();
    }

    public AnyBigInteger GreaterThan(BigInteger bound) {
        return GreaterThanOrEqualTo(bound + 1);
    }

    public AnyBigInteger LessThan(BigInteger bound) {
        return LessThanOrEqualTo(bound - 1);
    }

    public BigInteger Generate() {
        BigInteger min = _min ?? (_max is { } max1 ? BigInteger.Min(max1, BigInteger.Zero) - OrdinaryMagnitude : -OrdinaryMagnitude);
        BigInteger max = _max ?? (_min is { } min1 ? BigInteger.Max(min1, BigInteger.Zero) + OrdinaryMagnitude : OrdinaryMagnitude);

        BigInteger count = max - min + 1;
        // Eight spare bytes keep the modulo bias negligible.
        int    byteCount = (int)((count.GetBitLength() + 7) / 8) + 8;
        byte[] drawn     = Any.ArrayOf(Any.Byte()).WithCount(byteCount).Generate();

        return new BigInteger(drawn, isUnsigned: true) % count + min;
    }

    private AnyBigInteger Checked() {
        if (_min is { } min && _max is { } max && min > max) {
            throw new InvalidOperationException($"No integer lies between {min} and {max}: the constraints contradict each other.");
        }

        return this;
    }

}
