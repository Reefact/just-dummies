using System;

using JustDummies;

namespace NetworkAcl.Domain;

/// <summary>
///     A generator of arbitrary <see cref="FirewallRule" /> values. Written by hand: the family, the protocol, the ports
///     and the flags test constrain each other, so what a test pins reshapes the rest of the draw. Both prefixes are
///     of one family; ports are narrowed only for a port-bearing protocol; a flags test appears only with TCP.
/// </summary>
public sealed class AnyFirewallRule : IAny<FirewallRule> {

    private readonly RuleId?        _id;
    private readonly RuleAction?    _action;
    private readonly IpVersion?     _version;
    private readonly AddressPrefix? _source;
    private readonly AddressPrefix? _destination;
    private readonly bool           _protocolPinned;
    private readonly byte?          _protocol;
    private readonly PortRange?     _sourcePorts;
    private readonly PortRange?     _destinationPorts;
    private readonly bool           _flagsPinned;
    private readonly TcpFlagMatch?  _flags;

    public AnyFirewallRule() { }

    private AnyFirewallRule(AnyFirewallRule from,
                            RuleId?        id               = null,
                            RuleAction?    action           = null,
                            IpVersion?     version          = null,
                            AddressPrefix? source           = null,
                            AddressPrefix? destination      = null,
                            bool?          protocolPinned   = null,
                            byte?          protocol         = null,
                            PortRange?     sourcePorts      = null,
                            PortRange?     destinationPorts = null,
                            bool?          flagsPinned      = null,
                            TcpFlagMatch?  flags            = null) {
        _id               = id ?? from._id;
        _action           = action ?? from._action;
        _version          = version ?? from._version;
        _source           = source ?? from._source;
        _destination      = destination ?? from._destination;
        _protocolPinned   = protocolPinned ?? from._protocolPinned;
        _protocol         = protocolPinned == true ? protocol : from._protocol;
        _sourcePorts      = sourcePorts ?? from._sourcePorts;
        _destinationPorts = destinationPorts ?? from._destinationPorts;
        _flagsPinned      = flagsPinned ?? from._flagsPinned;
        _flags            = flagsPinned == true ? flags : from._flags;
    }

    public AnyFirewallRule WithId(RuleId id) { return new AnyFirewallRule(this, id: id); }

    public AnyFirewallRule WithAction(RuleAction action) { return new AnyFirewallRule(this, action: action); }

    public AnyFirewallRule OfVersion(IpVersion version) { return new AnyFirewallRule(this, version: version); }

    public AnyFirewallRule WithSource(AddressPrefix source) { return new AnyFirewallRule(this, source: source); }

    public AnyFirewallRule WithDestination(AddressPrefix destination) { return new AnyFirewallRule(this, destination: destination); }

    /// <summary>Pins the protocol; <c>null</c> pins "any protocol".</summary>
    public AnyFirewallRule WithProtocol(byte? protocol) { return new AnyFirewallRule(this, protocolPinned: true, protocol: protocol); }

    public AnyFirewallRule WithSourcePorts(PortRange ports) { return new AnyFirewallRule(this, sourcePorts: ports); }

    public AnyFirewallRule WithDestinationPorts(PortRange ports) { return new AnyFirewallRule(this, destinationPorts: ports); }

    /// <summary>Pins the flags test; <c>null</c> pins "no flags test".</summary>
    public AnyFirewallRule WithTcpFlags(TcpFlagMatch? flags) { return new AnyFirewallRule(this, flagsPinned: true, flags: flags); }

    public FirewallRule Generate() {
        IpVersion version = _source?.Version ?? _destination?.Version ?? _version ?? Any.Enum<IpVersion>().Generate();

        AddressPrefix source      = _source ?? new AnyAddressPrefix().OfVersion(version).Generate();
        AddressPrefix destination = _destination ?? new AnyAddressPrefix().OfVersion(version).Generate();

        byte? protocol = _protocolPinned ? _protocol : DrawProtocol(version);

        PortRange sourcePorts      = PortsFor(protocol, _sourcePorts);
        PortRange destinationPorts = PortsFor(protocol, _destinationPorts);

        TcpFlagMatch? flags = protocol != IpProtocols.Tcp ? null : _flagsPinned ? _flags : new AnyTcpFlagMatch().OrNull().Generate();

        return new FirewallRule(_id ?? new AnyRuleId().Generate(),
                                _action ?? Any.Enum<RuleAction>().Generate(),
                                source,
                                destination,
                                protocol,
                                sourcePorts,
                                destinationPorts,
                                flags);
    }

    private static byte? DrawProtocol(IpVersion version) {
        // One in five rules matches any protocol.
        return Any.Int32().Between(1, 5).Generate() == 1 ? null : Support.PacketProtocols(version).Generate();
    }

    private static PortRange PortsFor(byte? protocol, PortRange? pinned) {
        if (protocol is not { } p || !IpProtocols.CarriesPorts(p)) { return pinned ?? PortRange.All; }

        return pinned ?? new AnyPortRange().Generate();
    }

}
