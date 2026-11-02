# Network ACL — domain model

Namespace `NetworkAcl.Domain` (`Domain/`). A stateless, NAT-free firewall policy: ordered rules where the first match
decides, evaluated against IPv4 and IPv6 packets, with an analysis of rules that can never matter. Addresses are bits:
IPv4 as `uint`, IPv6 as `UInt128`, both in network order.

## Value objects
- `Ipv4Address(uint value)` — every `uint` is an address. `Parse` takes dotted-quad: exactly four decimal octets 0-255,
  no empty octet, **no leading zero** (`010` is refused, `0` is fine); `FormatException` otherwise. `IsMulticast`
  (224.0.0.0/4), `IsLimitedBroadcast` (255.255.255.255); ordered by value.
- `Ipv6Address(UInt128 value)` — every `UInt128` is an address. `Parse` takes the RFC 4291 text forms: groups of 1-4
  hex digits (either case) separated by `:`; at most one `::`, standing for **one or more** zero groups (so at most 7
  explicit groups with it, exactly 8 without); an optional dotted-quad IPv4 tail counting as two groups, only as the
  very last part (`::ffff:192.0.2.1`, never before the `::`); a zone index (`%eth0`) is refused. `FormatException` for
  every syntax error. `ToString` is RFC 5952 canonical: lower case, no leading zeros, the longest run of ≥ 2 zero
  groups compressed (the first on a tie), the IPv4 tail rendered as hex. `IsMulticast` (ff00::/8); ordered by value.
- `AddressPrefix(Ipv4Address network, int prefixLength)` / `AddressPrefix(Ipv6Address network, int prefixLength)` —
  a CIDR prefix of either family. Length **0-32** (IPv4) or **0-128** (IPv6), `ArgumentOutOfRangeException` outside;
  **the host bits past the length must be zero** (`10.1.0.0/8` is refused with `ArgumentException`).
  `Parse("addr/len")` (one `/`, length 1-3 digits; a `:` in the address means IPv6) throws `FormatException` for
  syntax and lets the constructor's `ArgumentException`/`ArgumentOutOfRangeException` through. `Any(version)` (/0),
  `Host(address)` (/32, /128), `Version`, `PrefixLength`, `HostBits`, `Ipv4Network`/`Ipv6Network` (the other is null),
  `Contains(address)` (false for the other family), `Covers(prefix)` (same family, shorter or equal length, nested),
  `Overlaps(prefix)` (nested either way).
- `PortRange(ushort low, ushort high)` — inclusive, **low ≤ high** (`ArgumentException` otherwise; `low == high` is a
  single port). `All` (0-65535), `Single(port)`, `Count`, `IsAll`, `Contains(port)`, `Contains(range)`, `Overlaps(range)`.
- `TcpFlagMatch(TcpFlags mask, TcpFlags expected)` — the `--tcp-flags mask expected` test: flags under the mask must
  equal `expected`. Mask not `None`; **`expected` ⊆ `mask`**; `expected` holds none of the forbidden pairs SYN+FIN,
  SYN+RST, FIN+RST; mask = all eight flags with `expected = None` is refused (only a flagless segment, which is
  invalid, would match). All `ArgumentException`. `NewConnection` (mask SYN|ACK, expected SYN), `Established`
  (ACK/ACK). `IsValidCombination(flags)` (static): not `None` and no forbidden pair. `Matches`, `Covers` (examines no
  more flags and agrees on them), `Overlaps` (agree on every flag both examine).
- `RuleId(string)` — 1-32 characters, `^[a-z](?:-?[a-z0-9])*$`: starts with a lower-case letter, letters, digits and
  single hyphens, no trailing hyphen (e.g. `allow-dns-out`).
- `Verdict(RuleAction action, RuleId? matchedRule)` — defined action; `IsDefault` when no rule matched, `IsAllowed`.
- `RuleAnomaly(RuleId rule, AnomalyKind kind, RuleId? relatedRule)` — defined kind; **the related rule is null exactly
  when the kind is `RedundantToDefault`** (`ArgumentException` if given, `ArgumentNullException` if missing
  otherwise); **related ≠ rule**. `IsUnreachable` for `Shadowed` and `RedundantToEarlier`.
- `IpProtocols` (static) — `Icmp` 1, `Tcp` 6, `Udp` 17, `Icmpv6` 58, `Sctp` 132, `Reserved` 255. `CarriesPorts` (TCP, UDP,
  SCTP); `TransportHeaderBytes` (TCP 20, UDP 8, ICMP/ICMPv6 8, SCTP 12, others 0); `IsAllowedOn(protocol, version)`
  (ICMP only on IPv4, ICMPv6 only on IPv6, everything else on both).
- Enums: `IpVersion` (V4 = 4, V6 = 6), `RuleAction` (Allow, Deny), `TcpFlags` (`[Flags]`, byte: Fin 0x01, Syn 0x02,
  Rst 0x04, Psh 0x08, Ack 0x10, Urg 0x20, Ece 0x40, Cwr 0x80 — every byte value is a defined combination),
  `PolicyRefusalReason`, `AnomalyKind` (Shadowed, RedundantToEarlier, RedundantToLater, RedundantToDefault).

## Packets
- `Packet` — abstract (constructible only through its two subclasses). Common rules, checked in this order: payload not
  null; protocol ≠ 255 (`ArgumentOutOfRangeException`); protocol allowed on the family (no ICMP in IPv6, no ICMPv6 in
  IPv4); **a protocol without ports (anything but TCP, UDP, SCTP) has both ports 0**; **TCP flags must be a valid
  combination for TCP, and `None` for every other protocol**; **payload ≤ 65 535 − headers**, where headers are
  the IPv4 20-byte header (IPv6: none, its payload-length field excludes the fixed header) plus the transport header:
  IPv4/TCP 65 495, IPv4/UDP 65 507, IPv6/TCP 65 515, IPv6/UDP 65 527, IPv4/other 65 515
  (`ArgumentOutOfRangeException`). The payload is copied. `Length`, `MaxPayloadBytes(version, protocol)` (static).
- `Ipv4Packet(Ipv4Address source, Ipv4Address destination, byte protocol, ushort sourcePort, ushort destinationPort,
  TcpFlags tcpFlags, byte[] payload)` — source not multicast and not 255.255.255.255.
- `Ipv6Packet(Ipv6Address source, Ipv6Address destination, ...)` — same parameters; source not multicast.

## Entity
- `FirewallRule(RuleId id, RuleAction action, AddressPrefix source, AddressPrefix destination, byte? protocol,
  PortRange sourcePorts, PortRange destinationPorts, TcpFlagMatch? tcpFlags)` — a null protocol matches any protocol,
  a null flags test any flags. Checked in order: nulls; defined action; **source and destination of the same family**
  (a rule cannot mix IPv4 and IPv6); protocol ≠ 255; protocol allowed on the family; **port ranges other than `All`
  only with TCP, UDP or SCTP** (so a null protocol has both ranges `All`); **a flags test only with protocol TCP**.
  `Version`, `Matches(packet)` (family, prefixes, protocol, ports, flags), `Covers(rule)` (field by field, every packet
  of the other matches this one; a null field covers anything, a set flags test never covers a null one),
  `Overlaps(rule)` (some packet could match both; errs towards true on TCP flags).

## Aggregate: `FirewallPolicy(string name, RuleAction defaultAction, int capacity, bool strict)`
- Name not blank, ≤ 64 characters; defined default action; capacity 1-10 000. `Rules` in evaluation order, `Count`,
  `IndexOf(id)` (−1 when absent), `Find(id)`, `Revision` (+1 per accepted change), `DefaultHits`, `HitsOf(id)`.
- Every refusal is a `PolicyRefusedException` (an `InvalidOperationException`) carrying a `PolicyRefusalReason`.
- `Insert(position, rule)` (0 = first, `Count` = last), checked in order: `DuplicateRuleId`; `PositionOutOfRange`
  (outside 0..Count); `CapacityReached`; in strict mode `RuleWouldBeUnreachable` (an earlier rule covers it) then
  `RuleWouldShadowAnother` (it covers a rule after it). `Append(rule)` = insert at `Count`.
- `Move(id, position)` — `RuleNotFound`; `PositionOutOfRange` (outside 0..Count − 1); same position is a no-op (no
  revision); strict-mode checks as for `Insert`, against the other rules with the moved one at its new place.
- `Remove(id)` — `RuleNotFound`; returns the rule, drops its counter. Never refused in strict mode.
- `Evaluate(packet)` → `Verdict` — **first match wins**; no match gives the default action with no rule. Counts the hit
  on the rule or on `DefaultHits`. `ResetCounters()`.
- `FindAnomalies()` — in rule order, at most one per rule: an earlier rule covers it → `Shadowed` (opposite action) or
  `RedundantToEarlier` (same action), naming the first such rule; else a later rule of the same action covers it with
  no overlapping rule of the other action in between → `RedundantToLater`; else its action is the default and no later
  rule of the other action overlaps it → `RedundantToDefault`. Coverage is pairwise: a rule hidden only by several
  earlier rules together is not reported.
- `IsReachable(id)` — no earlier rule covers it (`RuleNotFound` if absent).
- In strict mode the policy holds the invariant "no rule is covered by an earlier one" at all times.
