using System;

using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Expert.Arbitrary;

/// <summary>
///     Arbitrary <see cref="FirewallRule" /> values. Written by hand: of the eight parameters the scaffold flagged six, all
///     tied to one another (same family, protocol allowed on that family, ports only for TCP/UDP/SCTP, flags only for TCP).
///     An unpinned part follows what is already decided; a pinned part is handed over as it is.
/// </summary>
public sealed class AnyFirewallRule : IAny<FirewallRule> {

    private const byte Gre = 47;

    private RuleId?        _id;
    private RuleAction?    _action;
    private AddressPrefix? _source;
    private AddressPrefix? _destination;
    private byte?          _protocol;
    private bool           _protocolPinned;
    private PortRange?     _sourcePorts;
    private PortRange?     _destinationPorts;
    private TcpFlagMatch?  _tcpFlags;
    private bool           _tcpFlagsPinned;

    public AnyFirewallRule WithId(RuleId value) => Copy(copy => copy._id = value);

    public AnyFirewallRule WithAction(RuleAction value) => Copy(copy => copy._action = value);

    public AnyFirewallRule WithSource(AddressPrefix value) => Copy(copy => copy._source = value);

    public AnyFirewallRule WithDestination(AddressPrefix value) => Copy(copy => copy._destination = value);

    public AnyFirewallRule WithProtocol(byte value) => Copy(copy => { copy._protocol = value; copy._protocolPinned = true; });

    public AnyFirewallRule WithAnyProtocol() => Copy(copy => { copy._protocol = null; copy._protocolPinned = true; });

    public AnyFirewallRule WithSourcePorts(PortRange value) => Copy(copy => copy._sourcePorts = value);

    public AnyFirewallRule WithDestinationPorts(PortRange value) => Copy(copy => copy._destinationPorts = value);

    public AnyFirewallRule WithTcpFlags(TcpFlagMatch value) => Copy(copy => { copy._tcpFlags = value; copy._tcpFlagsPinned = true; });

    public AnyFirewallRule WithoutTcpFlagsTest() => Copy(copy => { copy._tcpFlags = null; copy._tcpFlagsPinned = true; });

    public FirewallRule Generate() {
        RuleId     id      = _id ?? new AnyRuleId().Generate();
        RuleAction action  = _action ?? Any.Enum<RuleAction>().Generate();
        IpVersion  version = _source?.Version ?? _destination?.Version ?? Any.Enum<IpVersion>().Generate();

        AddressPrefix source      = _source ?? new AnyAddressPrefix().OfFamily(version).Generate();
        AddressPrefix destination = _destination ?? new AnyAddressPrefix().OfFamily(version).Generate();

        byte? protocol = _protocolPinned
            ? _protocol
            : Any.Byte().OneOf(IpProtocols.Tcp, IpProtocols.Udp, IpProtocols.Sctp, version == IpVersion.V4 ? IpProtocols.Icmp : IpProtocols.Icmpv6, Gre).OrNull().Generate();

        bool carriesPorts = protocol is byte p && IpProtocols.CarriesPorts(p);

        PortRange sourcePorts      = _sourcePorts ?? (carriesPorts ? new AnyPortRange().Generate() : PortRange.All);
        PortRange destinationPorts = _destinationPorts ?? (carriesPorts ? new AnyPortRange().Generate() : PortRange.All);
        TcpFlagMatch? tcpFlags     = _tcpFlagsPinned
            ? _tcpFlags
            : protocol == IpProtocols.Tcp ? new AnyTcpFlagMatch().OrNull().Generate() : null;

        return new FirewallRule(id, action, source, destination, protocol, sourcePorts, destinationPorts, tcpFlags);
    }

    private AnyFirewallRule Copy(Action<AnyFirewallRule> change) {
        AnyFirewallRule copy = (AnyFirewallRule)MemberwiseClone();
        change(copy);

        return copy;
    }

}
