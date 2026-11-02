namespace NetworkAcl.Domain;

/// <summary>An IPv6 packet (fixed header, no extension headers).</summary>
public sealed class Ipv6Packet : Packet
{
    public Ipv6Packet(
        Ipv6Address source,
        Ipv6Address destination,
        byte protocol,
        ushort sourcePort,
        ushort destinationPort,
        TcpFlags tcpFlags,
        byte[] payload)
        : base(IpVersion.V6, protocol, sourcePort, destinationPort, tcpFlags, payload)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        if (source.IsMulticast)
        {
            throw new ArgumentException($"{source} is a multicast address and cannot be a source.", nameof(source));
        }

        Source = source;
        Destination = destination;
    }

    public Ipv6Address Source { get; }

    public Ipv6Address Destination { get; }

    internal override UInt128 SourceBits => Source.Value;

    internal override UInt128 DestinationBits => Destination.Value;

    public override string ToString() => $"[{Source}]:{SourcePort} -> [{Destination}]:{DestinationPort} proto {Protocol}";
}
