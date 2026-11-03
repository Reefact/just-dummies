using System.Numerics;

using JustDummies;

namespace TokenLedger.Domain;

/// <summary>
///     JustDummies has no generator for <see cref="BigInteger" />, and the scaffolder names one anyway
///     (<c>new AnyBigInteger()</c>): this is the one written by hand. It draws uniformly over a range
///     declared with <see cref="Between" />, 256 bits wide at most.
/// </summary>
public sealed class AnyBigInteger : IAny<BigInteger> {

    private static readonly BigInteger DefaultBound = BigInteger.One << 128;

    private readonly BigInteger _min;
    private readonly BigInteger _max;

    public AnyBigInteger()
        : this(-DefaultBound, DefaultBound) { }

    private AnyBigInteger(BigInteger min, BigInteger max) {
        _min = min;
        _max = max;
    }

    public AnyBigInteger Between(BigInteger min, BigInteger max) {
        if (min > max) { throw new ArgumentException($"{min} is above {max}."); }

        return new AnyBigInteger(min, max);
    }

    public AnyBigInteger GreaterThanOrEqualTo(BigInteger min) {
        return Between(min, _max < min ? min : _max);
    }

    public AnyBigInteger LessThanOrEqualTo(BigInteger max) {
        return Between(_min > max ? max : _min, max);
    }

    public BigInteger Generate() {
        BigInteger span  = _max - _min + 1;
        int        words = (int)((span.GetBitLength() + 64) / 64) + 1;

        BigInteger raw = BigInteger.Zero;
        for (int i = 0; i < words; i++) {
            raw = (raw << 64) | Any.UInt64().Generate();
        }

        return _min + raw % span;
    }

}
