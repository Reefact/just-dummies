using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="TcpFlagMatch" /> values. Written by hand: the scaffold drew mask and expected
///     independently, but the constructor ties them (expected within mask, no forbidden pair, not "all flags, none set").
///     Here the tie holds by construction: at most one of SYN/FIN/RST, the rest free, and a mask made of the expected
///     bits plus 1 to 254 other bits (so never 0x00, and never 0xFF with an empty expectation).
/// </summary>
public sealed class AnyTcpFlagMatch : IAny<TcpFlagMatch> {

    private static readonly IAny<TcpFlagMatch> Recipe = Any.Combine(
        Any.OneOf(TcpFlags.None, TcpFlags.Syn, TcpFlags.Fin, TcpFlags.Rst),
        Any.Byte(),
        Any.Byte().Between(1, 254),
        (exclusive, others, extra) => {
            TcpFlags expected = exclusive | (TcpFlags)(others & 0xF8);
            TcpFlags mask     = expected | (TcpFlags)extra;

            return new TcpFlagMatch(mask, expected);
        });

    public TcpFlagMatch Generate() {
        return Recipe.Generate();
    }

}
