using JustDummies;

using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Regular;

/// <summary>Builders the policy tests share. Everything a test does not care about is drawn; everything it is about is spelled.</summary>
internal static class PolicyFixtures {

    public static AddressPrefix P(string text) {
        return AddressPrefix.Parse(text);
    }

    /// <summary>A policy roomy enough for any test here to add its rules; the capacity is not what those tests are about.</summary>
    public static FirewallPolicy PolicyOf(RuleAction defaultAction, bool strict) {
        return Any.FirewallPolicy()
                  .WithDefaultAction(defaultAction)
                  .WithStrict(strict)
                  .WithCapacity(capacity => capacity.GreaterThanOrEqualTo(50))
                  .Generate();
    }

    public static List<RuleId> DistinctIds(int count) {
        return Any.ListOf(Any.RuleId()).WithCount(count).Distinct().Generate();
    }

    public static FirewallRule Rule(RuleId id, RuleAction action, string source, string destination, byte? protocol = null, PortRange? destinationPorts = null, TcpFlagMatch? flags = null) {
        return new FirewallRule(id, action, P(source), P(destination), protocol, PortRange.All, destinationPorts ?? PortRange.All, flags);
    }

    public static Ipv4Packet Packet4(string source, string destination, byte protocol = IpProtocols.Udp, ushort destinationPort = 0) {
        return Any.Ipv4Packet()
                  .WithSource(Ipv4Address.Parse(source))
                  .WithDestination(Ipv4Address.Parse(destination))
                  .WithProtocol(protocol)
                  .WithDestinationPort(IpProtocols.CarriesPorts(protocol) ? destinationPort : (ushort)0)
                  .Generate();
    }

}
