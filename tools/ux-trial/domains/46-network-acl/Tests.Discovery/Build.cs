using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

/// <summary>Small literal builders: the significant fields of a rule or packet are written out, the rest is left open.</summary>
internal static class Build {

    public static PortRange Ports(string text) {
        if (text == "any") { return PortRange.All; }

        string[] parts = text.Split('-');

        return parts.Length == 1
            ? PortRange.Single(ushort.Parse(parts[0]))
            : new PortRange(ushort.Parse(parts[0]), ushort.Parse(parts[1]));
    }

    public static FirewallRule Rule(string id,
                                    RuleAction action,
                                    string source = "0.0.0.0/0",
                                    string destination = "0.0.0.0/0",
                                    byte? protocol = null,
                                    string sourcePorts = "any",
                                    string destinationPorts = "any",
                                    TcpFlagMatch? flags = null) {
        return new FirewallRule(new RuleId(id), action, AddressPrefix.Parse(source), AddressPrefix.Parse(destination), protocol,
                                Ports(sourcePorts), Ports(destinationPorts), flags);
    }

    public static Ipv4Packet Packet4(string source, string destination, byte protocol = IpProtocols.Tcp, ushort sourcePort = 40000,
                                     ushort destinationPort = 443, TcpFlags flags = TcpFlags.Ack) {
        TcpFlags effective = protocol == IpProtocols.Tcp ? flags : TcpFlags.None;
        bool     ports     = IpProtocols.CarriesPorts(protocol);

        return new Ipv4Packet(Ipv4Address.Parse(source), Ipv4Address.Parse(destination), protocol, ports ? sourcePort : (ushort)0,
                              ports ? destinationPort : (ushort)0, effective, []);
    }

    public static void Refused(PolicyRefusalReason reason, Action action) {
        PolicyRefusedException exception = Assert.Throws<PolicyRefusedException>(action);

        Assert.Equal(reason, exception.Reason);
    }

    public static UInt128 Hex(string hex) {
        return UInt128.Parse(hex, System.Globalization.NumberStyles.AllowHexSpecifier);
    }

}
