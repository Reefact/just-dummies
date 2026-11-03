using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using JustDummies;

namespace NetworkAcl.Tests.Expert;

/// <summary>Literal shorthands: the tests that use them are about the value they spell, so it stays visible.</summary>
internal static class Shorthand {

    public static Ipv4Address V4(string text) => Ipv4Address.Parse(text);

    public static Ipv6Address V6(string text) => Ipv6Address.Parse(text);

    public static AddressPrefix Prefix(string text) => AddressPrefix.Parse(text);

    public static RuleId Id(string text) => new RuleId(text);

    public static UInt128 Bits(ulong high, ulong low) => new UInt128(high, low);

    public static PortRange Ports(ushort low, ushort high) => new PortRange(low, high);

    public static PortRange Port(ushort port) => PortRange.Single(port);

    /// <summary>
    ///     A rule spelled out field by field; whatever is not named matches everything IPv4 (0.0.0.0/0 to 0.0.0.0/0, any protocol,
    ///     all ports, no flags test). Tests vary one field and read the outcome of that field alone.
    /// </summary>
    public static FirewallRule Rule(string id = "rule",
                                    RuleAction action = RuleAction.Allow,
                                    string source = "0.0.0.0/0",
                                    string destination = "0.0.0.0/0",
                                    byte? protocol = null,
                                    PortRange? sourcePorts = null,
                                    PortRange? destinationPorts = null,
                                    TcpFlagMatch? tcpFlags = null) {
        return new FirewallRule(Id(id), action, Prefix(source), Prefix(destination), protocol, sourcePorts ?? PortRange.All, destinationPorts ?? PortRange.All, tcpFlags);
    }

    /// <summary>A policy whose name, capacity and strictness are dummies unless the test says otherwise. Room for at least 20 rules.</summary>
    public static FirewallPolicy Policy(RuleAction defaultAction) => NewPolicy(defaultAction, null);

    public static FirewallPolicy Policy(RuleAction defaultAction, bool strict, params FirewallRule[] rules) {
        FirewallPolicy policy = NewPolicy(defaultAction, strict);

        foreach (FirewallRule rule in rules) { policy.Append(rule); }

        return policy;
    }

    private static FirewallPolicy NewPolicy(RuleAction defaultAction, bool? strict) {
        AnyFirewallPolicy generator = Any.FirewallPolicy()
                                         .WithDefaultAction(defaultAction)
                                         .WithCapacity(capacity => capacity.GreaterThanOrEqualTo(20));

        return (strict is bool pinned ? generator.WithStrict(pinned) : generator).Generate();
    }


}
