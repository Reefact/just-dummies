using System.Numerics;

namespace TokenLedger.Domain.Tests;

/// <summary>Short spellings for the amounts a test states on purpose.</summary>
internal static class Amount {

    public static TokenAmount Of(BigInteger baseUnits) {
        return new TokenAmount(baseUnits);
    }

    /// <summary>2^256 - 1, the largest amount a 256-bit word holds.</summary>
    public static TokenAmount Max => TokenAmount.Max;

}
