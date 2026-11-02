namespace NetworkAcl.Domain;

/// <summary>An IPv4 datagram (20-byte header, no options).</summary>
public sealed class Ipv4Packet : Packet
{
    public const int HeaderBytes = 20;

    public Ipv4Packet(
        Ipv4Address source,
        Ipv4Address destination,
        byte protocol,
        ushort sourcePort,
        ushort destinationPort,
        TcpFlags tcpFlags,
        byte[] payload)
        : base(IpVersion.V4, protocol, sourcePort, destinationPort, tcpFlags, payload)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (source.IsMulticast || source.IsLimitedBroadcast)
        {
            throw new ArgumentException($"{source} is a multicast or broadcast address and cannot be a source.", nameof(source));
        }

        Source = source;
        Destination = destination;
    }

    public Ipv4Address Source { get; }

    public Ipv4Address Destination { get; }

    internal override UInt128 SourceBits => Source.Value;

    internal override UInt128 DestinationBits => Destination.Value;

    public override string ToString() => $"{Source}:{SourcePort} -> {Destination}:{DestinationPort} proto {Protocol}";
}
