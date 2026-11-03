using System;
using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     A generator of arbitrary <see cref="BigInteger" /> values within a closed range. JustDummies has no
///     <c>BigInteger</c> generator, so this one is written by hand: four 64-bit draws make a 256-bit word,
///     folded into the range.
/// </summary>
public sealed class AnyBigInteger : IAny<BigInteger> {

    private static readonly BigInteger DefaultBound = BigInteger.One << 200;

    private readonly BigInteger _min;
    private readonly BigInteger _max;

    public AnyBigInteger()
        : this(-DefaultBound, DefaultBound) { }

    private AnyBigInteger(BigInteger min, BigInteger max) {
        if (min > max) { throw new ArgumentException($"Empty range: {min} is above {max}."); }

        _min = min;
        _max = max;
    }

    public AnyBigInteger Between(BigInteger min, BigInteger max) {
        return new AnyBigInteger(BigInteger.Max(_min, min), BigInteger.Min(_max, max));
    }

    public AnyBigInteger AtLeast(BigInteger min) {
        return Between(min, _max);
    }

    public AnyBigInteger AtMost(BigInteger max) {
        return Between(_min, max);
    }

    public BigInteger Generate() {
        BigInteger span = _max - _min + 1;
        BigInteger word = BigInteger.Zero;
        for (int i = 0; i < 6; i++) {
            word = (word << 64) | Any.UInt64().Generate();
        }

        return _min + (word % span);
    }

}
