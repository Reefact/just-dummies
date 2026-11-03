using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>Arbitrary TCP flag sets, valid by construction (the domain refuses SYN+FIN, SYN+RST, FIN+RST and the empty set).</summary>
public static class AnyTcpSegmentFlags {

    /// <summary>The flag sets real segments carry. A pool, so none of them can be a forbidden combination.</summary>
    public static AnyEnum<TcpFlags> Segment() {
        return Any.Enum<TcpFlags>().OneOf(TcpFlags.Syn,
                                          TcpFlags.Syn | TcpFlags.Ack,
                                          TcpFlags.Ack,
                                          TcpFlags.Ack | TcpFlags.Psh,
                                          TcpFlags.Fin | TcpFlags.Ack,
                                          TcpFlags.Rst,
                                          TcpFlags.Rst | TcpFlags.Ack);
    }

    /// <summary>
    ///     Any combination of PSH, URG, ECE and CWR (possibly none): the flags that never clash with SYN, FIN, RST or ACK,
    ///     so adding them to a valid segment leaves it valid.
    /// </summary>
    public static IAny<TcpFlags> Auxiliary() {
        return Any.Byte().As(bits => (TcpFlags)(bits & 0xE8));
    }

}
