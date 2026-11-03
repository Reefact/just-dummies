using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary valid <see cref="FirewallRule" /> values. Written by hand rather than completed from the
///     scaffold: family, protocol, ports and flags are tied to each other, so they are drawn in that order.
/// </summary>
public sealed class AnyFirewallRule : IAny<FirewallRule> {

    private sealed class Pins {

        public IpVersion?     Version;
        public RuleId?        Id;
        public RuleAction?    Action;
        public AddressPrefix? Source;
        public AddressPrefix? Destination;
        public bool           ProtocolPinned;
        public byte?          Protocol;
        public PortRange?     SourcePorts;
        public PortRange?     DestinationPorts;
        public bool           FlagsPinned;
        public TcpFlagMatch?  Flags;

    }

    private readonly Pins _pins;

    public AnyFirewallRule() : this(new Pins()) { }

    private AnyFirewallRule(Pins pins) {
        _pins = pins;
    }

    public AnyFirewallRule OnIpv4()                             => With(p => p.Version = IpVersion.V4);
    public AnyFirewallRule OnIpv6()                             => With(p => p.Version = IpVersion.V6);
    public AnyFirewallRule WithId(RuleId value)                 => With(p => p.Id = value);
    public AnyFirewallRule WithAction(RuleAction value)         => With(p => p.Action = value);
    public AnyFirewallRule WithSource(AddressPrefix value)      => With(p => { p.Source = value; p.Version = value.Version; });
    public AnyFirewallRule WithDestination(AddressPrefix value) => With(p => { p.Destination = value; p.Version = value.Version; });
    public AnyFirewallRule WithProtocol(byte? value)            => With(p => { p.ProtocolPinned = true; p.Protocol = value; });
    public AnyFirewallRule WithSourcePorts(PortRange value)     => With(p => p.SourcePorts = value);
    public AnyFirewallRule WithDestinationPorts(PortRange value) => With(p => p.DestinationPorts = value);
    public AnyFirewallRule WithTcpFlags(TcpFlagMatch? value)    => With(p => { p.FlagsPinned = true; p.Flags = value; });

    public FirewallRule Generate() {
        IpVersion version = _pins.Version ?? Any.Enum<IpVersion>().Generate();
        IAny<AddressPrefix> prefixes = version == IpVersion.V4 ? new AnyAddressPrefix() : new AnyIpv6AddressPrefix();

        byte familyIcmp = version == IpVersion.V4 ? IpProtocols.Icmp : IpProtocols.Icmpv6;
        byte? protocol  = _pins.ProtocolPinned
            ? _pins.Protocol
            : Any.OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, familyIcmp, (byte)47).OrNull().Generate();

        bool carriesPorts = protocol is byte p && IpProtocols.CarriesPorts(p);

        TcpFlagMatch? flags = _pins.FlagsPinned
            ? _pins.Flags
            : (protocol == IpProtocols.Tcp ? new AnyTcpFlagMatch().OrNull().Generate() : null);

        return new FirewallRule(
            _pins.Id          ?? new AnyRuleId().Generate(),
            _pins.Action      ?? Any.Enum<RuleAction>().Generate(),
            _pins.Source      ?? prefixes.Generate(),
            _pins.Destination ?? prefixes.Generate(),
            protocol,
            _pins.SourcePorts      ?? (carriesPorts ? new AnyPortRange().Generate() : PortRange.All),
            _pins.DestinationPorts ?? (carriesPorts ? new AnyPortRange().Generate() : PortRange.All),
            flags);
    }

    private AnyFirewallRule With(Action<Pins> change) {
        Pins copy = new() {
            Version = _pins.Version, Id = _pins.Id, Action = _pins.Action, Source = _pins.Source, Destination = _pins.Destination,
            ProtocolPinned = _pins.ProtocolPinned, Protocol = _pins.Protocol, SourcePorts = _pins.SourcePorts,
            DestinationPorts = _pins.DestinationPorts, FlagsPinned = _pins.FlagsPinned, Flags = _pins.Flags
        };
        change(copy);

        return new AnyFirewallRule(copy);
    }

}
