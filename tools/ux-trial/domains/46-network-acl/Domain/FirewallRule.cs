namespace NetworkAcl.Domain;

/// <summary>
/// One line of the policy: what it matches (family, source and destination prefixes, protocol, ports, TCP flags)
/// and what it does. A null protocol matches every protocol; a null flags test matches every flag set.
/// </summary>
public sealed class FirewallRule
{
    public FirewallRule(
        RuleId id,
        RuleAction action,
        AddressPrefix source,
        AddressPrefix destination,
        byte? protocol,
        PortRange sourcePorts,
        PortRange destinationPorts,
        TcpFlagMatch? tcpFlags)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(sourcePorts);
        ArgumentNullException.ThrowIfNull(destinationPorts);
        if (!Enum.IsDefined(action))
        {
            throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown rule action.");
        }

        if (source.Version != destination.Version)
        {
            throw new ArgumentException($"A rule cannot mix families: source {source} is IP{source.Version}, destination {destination} is IP{destination.Version}.", nameof(destination));
        }

        if (protocol == IpProtocols.Reserved)
        {
            throw new ArgumentOutOfRangeException(nameof(protocol), protocol, "Protocol 255 is reserved.");
        }

        if (protocol is byte p && !IpProtocols.IsAllowedOn(p, source.Version))
        {
            throw new ArgumentException($"Protocol {p} does not run over IP{source.Version}.", nameof(protocol));
        }

        bool portBearing = protocol is byte q && IpProtocols.CarriesPorts(q);
        if (!portBearing && (!sourcePorts.IsAll || !destinationPorts.IsAll))
        {
            throw new ArgumentException("Port ranges can only be narrowed for TCP, UDP or SCTP.", nameof(protocol));
        }

        if (tcpFlags is not null && protocol != IpProtocols.Tcp)
        {
            throw new ArgumentException("A TCP flags test needs protocol TCP (6).", nameof(tcpFlags));
        }

        Id = id;
        Action = action;
        Source = source;
        Destination = destination;
        Protocol = protocol;
        SourcePorts = sourcePorts;
        DestinationPorts = destinationPorts;
        TcpFlags = tcpFlags;
    }

    public RuleId Id { get; }

    public RuleAction Action { get; }

    public IpVersion Version => Source.Version;

    public AddressPrefix Source { get; }

    public AddressPrefix Destination { get; }

    public byte? Protocol { get; }

    public PortRange SourcePorts { get; }

    public PortRange DestinationPorts { get; }

    public TcpFlagMatch? TcpFlags { get; }

    public bool Matches(Packet packet)
    {
        ArgumentNullException.ThrowIfNull(packet);
        return packet.Version == Version
            && Source.ContainsBits(packet.SourceBits)
            && Destination.ContainsBits(packet.DestinationBits)
            && (Protocol is null || Protocol == packet.Protocol)
            && SourcePorts.Contains(packet.SourcePort)
            && DestinationPorts.Contains(packet.DestinationPort)
            && (TcpFlags is null || TcpFlags.Matches(packet.TcpFlags));
    }

    /// <summary>True when every packet <paramref name="other"/> matches is matched by this rule too, field by field.</summary>
    public bool Covers(FirewallRule other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Version == Version
            && Source.Covers(other.Source)
            && Destination.Covers(other.Destination)
            && (Protocol is null || Protocol == other.Protocol)
            && SourcePorts.Contains(other.SourcePorts)
            && DestinationPorts.Contains(other.DestinationPorts)
            && (TcpFlags is null || (other.TcpFlags is not null && TcpFlags.Covers(other.TcpFlags)));
    }

    /// <summary>True when some packet could match both rules. Errs towards true on TCP flags.</summary>
    public bool Overlaps(FirewallRule other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return other.Version == Version
            && Source.Overlaps(other.Source)
            && Destination.Overlaps(other.Destination)
            && (Protocol is null || other.Protocol is null || Protocol == other.Protocol)
            && SourcePorts.Overlaps(other.SourcePorts)
            && DestinationPorts.Overlaps(other.DestinationPorts)
            && (TcpFlags is null || other.TcpFlags is null || TcpFlags.Overlaps(other.TcpFlags));
    }

    public override string ToString() =>
        $"{Id}: {Action} {Source} -> {Destination} proto {(Protocol is null ? "any" : Protocol.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))} sport {SourcePorts} dport {DestinationPorts}{(TcpFlags is null ? string.Empty : $" flags {TcpFlags}")}";
}
