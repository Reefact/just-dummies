namespace NetworkAcl.Domain;

/// <summary>IANA protocol numbers the policy treats specially, and what each implies for a packet.</summary>
public static class IpProtocols
{
    public const byte Icmp = 1;
    public const byte Tcp = 6;
    public const byte Udp = 17;
    public const byte Icmpv6 = 58;
    public const byte Sctp = 132;

    /// <summary>255 is reserved by IANA and never appears on the wire.</summary>
    public const byte Reserved = 255;

    /// <summary>TCP, UDP and SCTP carry source and destination ports.</summary>
    public static bool CarriesPorts(byte protocol) => protocol is Tcp or Udp or Sctp;

    /// <summary>The fixed transport header length counted against the datagram size (0 for protocols not modelled).</summary>
    public static int TransportHeaderBytes(byte protocol) => protocol switch
    {
        Tcp => 20,
        Udp => 8,
        Icmp or Icmpv6 => 8,
        Sctp => 12,
        _ => 0,
    };

    /// <summary>ICMP belongs to IPv4 and ICMPv6 to IPv6; every other protocol runs over both.</summary>
    public static bool IsAllowedOn(byte protocol, IpVersion version) => protocol switch
    {
        Icmp => version == IpVersion.V4,
        Icmpv6 => version == IpVersion.V6,
        _ => true,
    };
}
