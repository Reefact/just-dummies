# Expert notes — 46 Network ACL
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
**Pages read, in order:** `PROJECT-README.md`, `guides/getting-started.en.md`, `guides/core-concepts.en.md`,
`packages/justdummies-cli.en.md` (the whole page, it is the long one), `guides/composition.en.md`,
`packages/justdummies-xunit.en.md`, `generators/README.md`, `generators/numbers.en.md`, `generators/strings.en.md`,
`generators/collections.en.md`, `generators/enums-and-choices.en.md`, then `analyzers/README.md` (to raise severities).
Domain: `MODEL.md` and every `.cs` file of `Domain/` before writing anything.

**Setup:** the test project from the template (`NetworkAcl.Tests.Expert.csproj`), plus `W/global.json` selecting the
Microsoft.Testing.Platform runner (see friction 1) and `Tests.Expert/.editorconfig` raising every JustDummies rule to warning.

**`any` commands run** (all from `Tests.Expert/`, `ANY` = the tool's full path):
- `any generate Ipv4Address Ipv6Address AddressPrefix PortRange TcpFlagMatch RuleId Verdict RuleAnomaly --output ./Arbitrary --entry-point any`
  (first try, before `dotnet restore`: `✗ Ipv4Address: this project has not been restored ...`; after restore: 8 files + 8 entry points)
- `any generate Ipv4Packet Ipv6Packet FirewallRule FirewallPolicy Packet --output ./Arbitrary --entry-point any`
  (`Packet` refused as abstract, exit 1, the four others written)
- the same twelve types again with `--namespace NetworkAcl.Tests.Expert.Arbitrary --entry-point any --force`, so that test
  generators do not live in the production namespace `NetworkAcl.Domain`. Final scaffold: 12 types, 24 files.

**What I did with the 12 scaffolds**
- kept as scaffolded: `AnyIpv4Address`, `AnyIpv6Address`, `AnyPortRange` (the ordered pair low/high was inferred), `AnyFirewallPolicy`;
- sentinel removed, nothing else: `AnyVerdict` (see friction 6);
- regex guard completed as the TODO comment instructs (retype `AnyString` to `AnyPattern` on four lines, base `Any.StringMatching("[a-z](-?[a-z0-9]){0,15}")`): `AnyRuleId`;
- rewritten by hand because their parameters are tied to one another (see friction 4): `AnyAddressPrefix`, `AnyTcpFlagMatch`,
  `AnyIpv4Packet`, `AnyIpv6Packet`, `AnyFirewallRule`, `AnyRuleAnomaly`. Where a tied part is not pinned it follows what is decided
  (ports only for TCP/UDP/SCTP, flags only for TCP, same family for source and destination, related rule derived from the kind);
  a pinned part is handed to the constructor as it is, so a contradictory pin is refused by the domain rather than dropped.
- plus two small hand-written helpers: `AnyTcpSegmentFlags` (valid segment flag sets, auxiliary flags) and `Shorthand` (literal
  `V4("..")`, `Prefix("..")`, `Rule(id:, source:, ...)` whose defaults match everything IPv4, `Policy(defaultAction, strict, rules)`).

**Tests written** (`Tests/`, 13 files): 282 test methods (194 `[Fact]`, 88 `[Theory]`), 649 cases, assembly-wide `[Reproducible]`.
- `Ipv4AddressTests`, `Ipv6AddressTests`: parse (valid forms, dozens of refusals incl. leading zero, `::` rules, zone, dotted tail), RFC 5952 canonical form
  (run choice, tie, trailing/leading run, hex tail), multicast/broadcast boundaries, ordering, equality.
- `AddressPrefixTests`: both families, host bits, length range, check order, Parse (family by colon, error types), Any/Host, Contains, Covers, Overlaps, equality.
- `PortRangeTests`, `TcpFlagMatchTests` (forbidden pairs, mask rules, Matches/Covers/Overlaps, factories), `RuleIdTests`, `IpProtocolsTests`,
  `VerdictAndAnomalyTests`.
- `PacketTests`: IPv4 and IPv6 packets: every refusal and its order, payload copy, lengths, payload limits at the limit and one byte over (10 cases), multicast sources.
- `FirewallRuleTests`: construction refusals and their order, Matches dimension by dimension, Covers, Overlaps.
- `FirewallPolicyConstructionTests`, `FirewallPolicyEditingTests` (Insert/Append, strict mode, Move, Remove), `FirewallPolicyEvaluationTests`
  (first match wins, counters, reset) and `FirewallPolicyAnalysisTests` (four anomaly kinds, priorities, blockers, the pairwise limit, IsReachable).

**Result:** green: 648 passed, 1 skipped (`RuleIdTests.An_id_with_a_trailing_line_feed_is_refused`, 3 cases, `Skip = "domain defect: ..."`).
After the first green run I ran the suite 12 more times with fresh seeds (`dotnet test --project Tests.Expert --no-build`): 12 of 12 identical
(`failed: 0 succeeded: 648 skipped: 1`). With every JustDummies rule at warning severity: 0 diagnostics on the final code.
A differential run against `System.Net.IPAddress` (scratch program in `W/tmp/diff`, 300 000 random addresses) found only the BCL's own
`::a.b.c.d` rendering for values below 2^32, which the model specifies differently (hex tail): not a domain defect.

**Dummies in the suite (what I drew, and why it is a dummy):** packet payload and every packet field the rule under test does not look at;
the other-family address/prefix when the test is about the family; the other three octets when one octet carries a leading zero; the 28 / 120 low
bits of a multicast address; policy name, capacity and strictness when the test is about evaluation or ordering (strictness never mattered:
the draw would have shown otherwise); the characters of a 32/33-character id and a 64/65-character name; the two ports of a *reversed* range
(drawn as a strict ordered pair, then swapped); rule bodies when only the id and the order are read back. Every other value is a literal.

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Expert` fails out of the box.** Exact output: `MSBUILD : error MSB1001: Unknown switch. ... Switch: --project`
   (SDK 10.0.114). `--project` belongs to the Microsoft.Testing.Platform runner, which the SDK only uses when a `global.json` says
   `{"test": {"runner": "Microsoft.Testing.Platform"}}`. Nothing in the user docs (xunit adapter page, getting started) says how the tests are
   run or that xUnit v3 + MTP needs this. I wrote the file in W and moved on. *Docs, low.*
2. **`any generate AddressPrefix` scaffolds one constructor and says nothing about the other.** `AddressPrefix` has an IPv4 and an IPv6 constructor;
   the recap shows only `constructor AddressPrefix(Ipv4Address, int)` and the resulting generator can never draw an IPv6 prefix. No option selects
   the constructor, no line says another exists. I found out by reading `Domain/`. *Friction, medium.*
3. **First `any generate` before `dotnet restore`** printed `✗ Ipv4Address: this project has not been restored, so its package references cannot be
   read. Restore it, then run the command again: dotnet restore`. Clear and actionable (listed under what worked), but the whole 8-type batch was
   aborted by the first type's error instead of reporting once. *Friction, low.*
4. **Tied parameters: the scaffold names every tie precisely and gives me nothing to express it with.** `Ipv4Packet`: 6 of 7 parameters `to verify`;
   `FirewallRule`: 6 of 8; `TcpFlagMatch`, `RuleAnomaly`, `AddressPrefix`: all parameters. The messages are accurate
   (`sourcePort: unread guard at Packet.cs, line 26: if (!IpProtocols.CarriesPorts(protocol) && (sourcePort != 0 || destinationPort != 0)) ... — ties protocol, sourcePort and destinationPort`),
   but the docs' only exits are "derive one draw from another inside `Generate()`" (which, as the page itself says, makes `WithEnd(...)`-style pins
   dead or silently inconsistent) or `Any.Combine`. For 6 of the 12 types I deleted the scaffold and wrote a ~40-line generator with nullable
   pins by hand, so the 24-file scaffold bought me the leaf types and a few lines of plumbing. For a domain whose entities are all invariants
   across fields this is the common case, not the exception. *Friction, medium; idea: emit a `Generate()` skeleton that already draws the
   untied parameters first and leaves the tied ones as one named `TODO` per tie group.*
5. **`[Flags]` enum scaffolded as a plain `Any.Enum<TcpFlags>()`.** Recap: `tcpFlags  TcpFlags  Any.Enum<TcpFlags>()`. That draws one declared member
   at a time (never `Syn | Ack`), as `enums-and-choices.en.md` explains under "Flags enumerations" (`AllowingCombinations()` is the opt-in), but neither
   the recap nor the file mentions it for a `[Flags]` type, and the page is not linked from the CLI page. A reviewer reading `AnyTcpFlagMatch` would
   believe flag combinations are covered. *Docs/friction, low.*
6. **`Verdict(RuleAction action, ...)` marked `to verify` for a guard that `Any.Enum` cannot violate.** Guard: `if (action != RuleAction.Allow && action != RuleAction.Deny)`;
   the build stopped on `CS0103: TODO_verify_the_generator_for_action` until I deleted the sentinel. The same guard written `!Enum.IsDefined(action)`
   is a documented "nothing to add" row; the explicit two-comparison form (a `&&`, which the table says is never read) earns a blocking mark.
   I verified by reading and deleted the two lines. *Friction, low.*
7. **Regex guard completed as instructed**, but the pattern the tool points at (`^[a-z](?:-?[a-z0-9])*$`) is unbounded, while the domain also caps the id at 32 characters in a
   separate guard. The comment tells me how to retype the four lines (it worked first time) and does not say that the length guard has to be folded into
   the pattern by hand: I derived `{0,15}` (1 + 15 * 2 = 31 <= 32) myself. *Friction, low.*
8. **`Any.OneOf<byte?>(null, IpProtocols.Tcp, ...)` compiled and failed at the first `Generate()`:** `ArgumentException : The values must not contain a null element; use OrNull() to make the whole generator nullable. (Parameter 'values')`.
   The message is exactly right; there is no build-time diagnostic for a literal `null` in a pool, although JD-rules exist for the neighbouring mistakes (JD012, JD013, JD025).
   Replaced by `Any.Byte().OneOf(...).OrNull()`. *Idea, low.*
9. **The seed line appears for a test that drew nothing.** The skipped-then-run `RuleId` theory (literal `InlineData`, no `Any.*` call)
   failed three times and each time printed `[JustDummies] These arbitrary values were seeded with 698961879. Reproduce this run with [JustDummies.Xunit.Reproducible(Seed = 698961879)].`
   With an assembly-wide `[Reproducible]` that is every failing test; here it sent me looking for a drawn value that did not exist. The seed is harmless,
   the sentence "These arbitrary values" is not true when none were drawn. *Defect (message), low.*
10. **Tests the library's own rule made me drop.** Three tests I would have written without a second thought at work are properties with a sample of one:
    "an address printed then parsed is equal to itself", "a strict policy holds only reachable rules after ten arbitrary rules were offered", and the BCL
    differential. In this domain nearly every value object is "every `uint`/`UInt128`/`ushort` is valid", so the subject of almost every test is the value itself and stays a literal;
    dummies only appear around it. I think that is the right outcome, but the docs offer no home for the invariant/round-trip test a team still wants (the FAQ points to a property library).
    I ran the differential once as a scratch program instead. *Idea, low.*
11. **Mixing entry styles.** `any --entry-point any` gave me `Any.Ipv4Packet()` for the types it scaffolded, while my hand-written generators have no `With…Func` overloads,
    so `Any.FirewallRule().WithSource(Prefix(".."))` takes a value and never a lambda like the scaffolded ones. Nothing wrong, but a reader of the tests cannot tell which generators
    are refinable. I kept the entry-point files (they construct `new AnyXxx()`, which my classes satisfy) so the call sites stay uniform. *Friction, low.*

## What worked well
- **`AnyPortRange`**: the scaffold recognised `if (low > high)` and wrote an `Any.OrderedPair`-backed generator with working `WithLow`/`WithHigh` pins, no TODO. `Any.OrderedPair(Any.UInt16(), Any.UInt16(), strict: true)` then
  gave me a reversed range for the refusal test with both ports as true dummies.
- **The `AnyValid…` base plus lambda refinement** is exactly what test data builders want: `Any.FirewallPolicy().WithDefaultAction(RuleAction.Deny).WithCapacity(c => c.GreaterThanOrEqualTo(20))`
  states the one fact the tests need (room for the rules) on top of the domain's own bounds, and `WithStrict(...)` pins only when the test is about strictness.
- **The sentinel mechanism**: a `CS0103` at the exact line, with the guard quoted, meant I never shipped a generator that could be invalid by omission. For the regex row the TODO told me which four lines to retype and it compiled first time.
- **`Any.Combine` with a composer that calls the real constructor** (`AnyTcpFlagMatch`): the tie "expected inside mask, no forbidden pair, not all-flags-none" holds by construction (one of SYN/FIN/RST, free auxiliary bits, a mask of 1..254 extra bits), with no redraw and no way to produce a value the domain refuses. JD027 stayed quiet because every operand is read.
- **Generation scale**: `Any.ArrayOf(Any.Byte()).WithCount(65_495)` in ten limit tests costs nothing measurable (whole suite: about 2 s).
- **Analyzers at full volume** (every JustDummies category at `warning`, JD019 and JD011 included): zero findings on 4.8k lines, and a probe file proved the settings were live (`JD030`, `JD031` fired). I did not have to argue with a rule once.
- **`[Reproducible]` assembly-wide**: nothing to add per test, and the three failures I saw carried a usable seed line.
- **Refusal messages** (e.g. the `.As`/`OneOf(null)` one, the "bounded redraw" family) name the cause and the remedy.
- **The draw as a prober**: policy strictness, capacity and name were drawn in ~40 tests and never changed an outcome, which is the evidence the tests wanted that those parameters are inert for evaluation, ordering and analysis.

## Fluency: 4 / 5
The docs' central idea (what is arbitrary becomes visibly arbitrary, what matters stays literal) fit this domain once I accepted that most values here are the subject: the suite ended with a short, honest
list of real dummies and a lot of literals, and the `Rule(...)`/`Prefix(...)` shorthands read better than any generator would have. The leaf scaffolds and the `OrderedPair`/`AnyValid…`/lambda design are
excellent, the analyzers cost nothing, and I never fought the library at run time. I take a point off because the tool's value collapses precisely where a domain gets interesting (cross-field invariants):
half of my scaffolds were thrown away, a second constructor was invisible, and a few small things (global.json for `dotnet test --project`, the `[Flags]` default, the seed line on draw-free tests) cost
me time that a sentence in the docs or the recap would have saved.
