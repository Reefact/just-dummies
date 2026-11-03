using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="TcpFlagMatch" /> values. Written by hand: the constructor ties
///     <c>expected</c> to <c>mask</c> (a subset of it), forbids the contradictory pairs and one degenerate mask,
///     which no per-parameter recipe can honour.
/// </summary>
public sealed class AnyTcpFlagMatch : IAny<TcpFlagMatch> {

    public TcpFlagMatch Generate() {
        return Any.Combine(Any.ElementOf(Support.FlagsWithoutForbiddenPair),
                           Any.Byte(),
                           (expected, extra) => {
                               TcpFlags mask = expected | (TcpFlags)extra;

                               // mask None would examine nothing; all eight flags with expected None would never match.
                               if (mask == TcpFlags.None) { mask = TcpFlags.Ack; }
                               if (mask == (TcpFlags)0xFF && expected == TcpFlags.None) { mask = (TcpFlags)0xFE; }

                               return new TcpFlagMatch(mask, expected);
                           }).Generate();
    }

}
