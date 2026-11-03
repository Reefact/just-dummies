using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary valid <see cref="TcpFlagMatch" /> values. Written by hand: every rule of the
///     constructor ties <c>mask</c> to <c>expected</c>, so the two are drawn together and fixed up by construction.
/// </summary>
public sealed class AnyTcpFlagMatch : IAny<TcpFlagMatch> {

    private readonly IAny<TcpFlagMatch> _recipe;

    public AnyTcpFlagMatch() {
        // At most one of FIN, SYN, RST (or none) plus any of the five other bits: never a forbidden pair.
        IAny<byte> exclusive = Any.OneOf((byte)TcpFlags.None, (byte)TcpFlags.Fin, (byte)TcpFlags.Syn, (byte)TcpFlags.Rst);
        IAny<byte> others    = Any.Byte().As(b => (byte)(b & 0xF8));
        IAny<byte> extra     = Any.Byte();

        _recipe = Any.Combine(exclusive, others, extra,
                              (single, rest, more) => {
                                  TcpFlags expected = (TcpFlags)(single | rest);
                                  TcpFlags mask     = expected | (TcpFlags)more;

                                  if (mask == TcpFlags.None) { mask = TcpFlags.Ack; }
                                  if (mask == (TcpFlags)0xFF && expected == TcpFlags.None) { expected = TcpFlags.Ack; }

                                  return new TcpFlagMatch(mask, expected);
                              });
    }

    public TcpFlagMatch Generate() {
        return _recipe.Generate();
    }

}
