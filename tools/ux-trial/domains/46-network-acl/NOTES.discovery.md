# Discovery notes — 46 network ACL
Agent: the same model and effort as every session of this trial. Library: JustDummies 1.0.0-uxtrial.1 (packed from main).

## What I did
- Pages read, in order: `docs/PROJECT-README.md`, `docs/guides/getting-started.en.md`, `docs/guides/core-concepts.en.md`
  (skimmed, first part only), `docs/packages/justdummies-cli.en.md` (all of it, the longest page), then on need
  `docs/packages/justdummies-xunit.en.md`, `docs/guides/composition.en.md`, `docs/generators/strings.en.md` (Patterns),
  `docs/generators/enums-and-choices.en.md`, and grep-level looks at `collections.en.md` / `numbers.en.md`.
- Read `MODEL.md` and all of `Domain/` (16 files) to know which fields were significant and which were not.
- Test project: `Tests.Discovery/NetworkAcl.Tests.Discovery.csproj`, from the given template, plus a `Usings.cs`
  (global usings for `JustDummies`, `NetworkAcl.Domain`, `Xunit`) and `AssemblyInfo.cs` (`[assembly: Reproducible]`,
  from the xUnit page, so every test is replayable).
- `any` commands run (from `Tests.Discovery`, after a `dotnet restore` the tool asked for):
  `any generate Ipv4Address Ipv6Address AddressPrefix PortRange TcpFlagMatch RuleId Verdict RuleAnomaly Ipv4Packet Ipv6Packet FirewallRule FirewallPolicy --entry-point any`
  (12 types in one call, 24 files with the entry points). I used `--entry-point any` because the CLI page shows the
  `Any.Order()` spelling in its examples and C# 14 is available on net10.0.
- Generators kept as scaffolded, or completed on the tool's request: `AnyIpv4Address`, `AnyIpv6Address`, `AnyPortRange`
  (the tool paired low/high with `Any.OrderedPair` by itself), `AnyFirewallPolicy`, `AnyVerdict` (TODO lines removed),
  `AnyRuleId` (regex guard: retyped `AnyString` to `AnyPattern` in the 3 places the TODO named, pattern
  `[a-z]([a-z0-9]|-[a-z0-9]){0,15}`), `AnyAddressPrefix` (kept the scaffold, deleted the two TODO lines each and masked
  the host bits in `Generate()`).
- Generators I had to write by hand because the constructor ties every parameter to another (the scaffold would have
  needed a full rewrite, so I deleted it): `AnyTcpFlagMatch`, `AnyRuleAnomaly`, `AnyIpv4Packet`, `AnyIpv6Packet`,
  `AnyFirewallRule`; plus `AnyIpv6AddressPrefix` (the scaffold of `AddressPrefix` covers IPv4 only).
- Tests written (11 test files): `Ipv4AddressTests`, `Ipv6AddressTests`, `AddressPrefixTests`,
  `PortRangeTests`, `TcpFlagMatchTests`, `RuleIdTests`, `VerdictAndAnomalyTests`, `IpProtocolsTests`, `PacketTests`,
  `FirewallRuleTests`, `FirewallPolicyTests` (+ `Build.cs`, literal builders for rules and packets). 254 test methods,
  641 cases (Theory rows included). Literals are used wherever the assertion is about the value (addresses, prefixes,
  ports, rule ids in policy tests); dummies are used for what the test does not care about: the extra fields of a
  packet around the one the rule looks at, an IPv6 address given to an IPv4 prefix just for its family, a rule id in a
  verdict, payload bytes, policy name and capacity, the ids and contents of rules filling a policy to its capacity.
- Result: green. First green run after fixing my own mistakes, then 34 more full runs (14 + 20, fresh seeds each time,
  `[assembly: Reproducible]`) all green: 640 passed, 1 skipped. The skipped one is a domain defect (below).

## Frictions and surprises, in order
1. **`dotnet test --project Tests.Discovery` does not work out of the box on the .NET 10 SDK.**
   First `dotnet test --project Tests.Discovery` -> `MSBUILD : error MSB1001: Unknown switch ... --project`;
   `dotnet test Tests.Discovery` -> `error : Testing with VSTest target is no longer supported by Microsoft.Testing.Platform
   on .NET 10 SDK and later. If you use dotnet test, you should opt-in to the new dotnet test experience`.
   Nothing in the documentation I read (getting-started, xUnit page) mentions it; the project README only shows
   `dotnet test --solution` for the repository itself ("pinned in `global.json`"). I created `W/global.json` with
   `{ "test": { "runner": "Microsoft.Testing.Platform" } }` and then `--project` worked. A newcomer on SDK 10 hits
   this before ever seeing a dummy. The xUnit page states the `xunit.v3` 4.0.2 floor but not the runner opt-in.
2. **`any generate` needed a restore first**: `✗ Ipv4Address: this project has not been restored, so its package
   references cannot be read. Restore it, then run the command again: dotnet restore`. Clear message, one extra step; the
   CLI page does not say "restore first" (it says "run it from your test project").
3. **Test project template has no `Xunit` using and ImplicitUsings does not add one**: first build was ~100 errors
   `CS0246: The type or namespace name 'InlineData' could not be found`. Not a library fault, but the getting-started
   samples show `using JustDummies; using JustDummies.Xunit; using Xunit;` per file and nothing about a
   `Usings.cs`. Also, the entry points (`Any.Ipv4Address()`) live in the *domain's* namespace
   (`namespace NetworkAcl.Domain;`), so a test file needs `using NetworkAcl.Domain;` to see `Any.Xxx()`. That was
   discoverable (the generated file says `namespace NetworkAcl.Domain;`, and the recap prints "in namespace ... (--namespace
   to change)") but I only understood it when `Any.PortRange()` did not resolve.
4. **Multiple public constructors: the tool silently scaffolds only one.** `AddressPrefix` has an `(Ipv4Address, int)`
   and an `(Ipv6Address, int)` constructor. The output shows `constructor AddressPrefix(Ipv4Address, int)` and
   `✓ AnyAddressPrefix.cs`, never says another constructor exists, and there is no option to choose one
   (`any generate --help` has none). The entry point is called `Any.AddressPrefix()` although it can only draw IPv4
   prefixes. I found out by reading the recap rows. I wrote `AnyIpv6AddressPrefix` by hand. Expected: a line such as
   "2 constructors, scaffolded the first" or a `--constructor` choice; docs do not state the selection rule.
5. **Types whose parameters are all tied to each other are not scaffoldable in practice.** `TcpFlagMatch`
   (`expected` subset of `mask`, forbidden pairs), `RuleAnomaly` (related rule null iff kind), `Ipv4Packet`/`Ipv6Packet`
   (protocol decides ports, flags and the payload ceiling), `FirewallRule` (family, protocol, ports, flags). The tool
   correctly marks them `to verify` and the build stops, on 6 of 7 packet parameters and 6 of 8 rule parameters, with
   the recap naming the guard: honest and useful, and the CLI page's "when a guard ties two parameters" section is
   exactly this case. But the four suggested forms (start+duration, derive in `Generate()`, check digit, `DifferentFrom`)
   do not help when five fields depend on one choice, and "derive inside `Generate()`" means the `With…` pins stop
   working, so the scaffold's main value (the `With…` surface) disappears. I deleted the five scaffolds and wrote
   generators with my own `With…` pins (`WithProtocol`, `WithTcpFlags`, `WithSource`, ...). It took most of the time
   of the session. No complaint on the correctness of the tool: it never produced a wrong value silently.
6. **`Any.OneOf<byte?>(null, a, b, ...)` compiles with no diagnostic and throws at the first `Generate()`**:
   `System.ArgumentException : The values must not contain a null element; use OrNull() to make the whole generator
   nullable. (Parameter 'values')`. The message is excellent and tells exactly what to do (`.OrNull()`), and it
   surfaced in the first test run (four policy tests). I expected an analyzer to catch a literal `null` in an
   explicit pool (the docs list JD012/JD013/JD025 for pool mistakes); none did. I changed it to `.OrNull()`.
7. **Failure output of xUnit v3 on the Microsoft Testing Platform buries the seed message a little**: the
   `[JustDummies] These arbitrary values were seeded with 1977442023. Reproduce this run with
   [JustDummies.Xunit.Reproducible(Seed = 1977442023)].` line is printed under "Standard output" after the stack, as
   documented. It worked as advertised. Only remark: with `[assembly: Reproducible]` I could not tell from the test
   name whether the failure depended on the draw; for a deterministic failure the seed line is noise (see 8).
8. **A failure that has nothing to do with the draw still prints a seed** (the `"a\n"` RuleId case, a literal
   `InlineData`): expected from "only if the test failed", but it invites a replay that is pointless. A theory row with
   literals never needs the attribute. Minor.
9. **Docs ambiguity about what a dummy is when the test is about a *range*.** For `IsMulticast` I first wrote
   `Any.UInt32().Between(0xE0000000, 0xEFFFFFFF)`; re-reading getting-started ("a constraint states an invariant of the
   domain, never what the test asserts") told me that restates the assertion, so I used boundary literals as theory
   rows. The rule is clear once read; a newcomer will need the "Careful not to misuse a dummy" section to see it. For a
   *domain with algebra* (prefixes, ranges, flags), almost every interesting test is about the value, so the suite
   ended up mostly literals; dummies carried the packet/rule/policy scaffolding. That felt right, not a defect.
10. **`DifferentFrom` on a scaffolded generator works without surprise** (`Any.RuleId().DifferentFrom(anyRule)`) and
    `Any.ListOf(Any.RuleId()).WithCount(5).Distinct()` gave distinct ids (they have value equality); I had assumed I would
    need `DistinctBy`. The docs are clear on this; noting it as a good surprise.
11. **Domain defect found by a literal row, not by a draw**: `new RuleId("a\n")` is accepted although the model says
    letters, digits and single hyphens only. Cause: the `[GeneratedRegex("^[a-z](?:-?[a-z0-9])*$")]` ends with `$`, which
    .NET lets match before a final `\n`. Test kept as `A_trailing_newline_is_refused` with `Skip = "domain defect: ..."`.
    Deterministic, independent of any seed.

## What worked well
- The recap table of `any generate` is the best part: every `to verify` names the exact guard and file/line, and the
  tied parameters say "ties expected to mask". It told me which of the 12 types to hand-write before I opened them.
- `AnyPortRange` came out right with no work: `low <= high` detected and drawn through `Any.OrderedPair`, with `WithLow`
  pins that bound the other side. `AnyFirewallPolicy` (guards from `ThrowIfNullOrWhiteSpace`, `Length`, capacity range with
  the domain's own constants `FirewallPolicy.MaxCapacity`) was usable untouched.
- The regex guard on `RuleId` produced exactly the retype instruction (three places) and `Any.StringMatching` handled
  `[a-z]([a-z0-9]|-[a-z0-9]){0,15}` with groups, alternation and bounded repetition first time.
- `Any.Combine`, `.As`, `Any.OneOf`, `.OrNull()`, `Any.Fixed` and the `ArrayOf(...).WithCountBetween` family were
  enough for every hand-written generator, and the compiler/analyzers never complained (0 warnings, 0 JD diagnostics in
  the final build).
- `[assembly: Reproducible]` in one line, and the seed printed with the exact replay snippet.
- Error messages from the library at the moment of misuse (item 6) were precise and named the fix.

## Fluency: 4 / 5
Getting-started and the CLI page made the dummy-versus-literal decision easy to apply, and the tool's recap pointed me
straight at the hard types. Points lost for: the SDK 10 test-runner opt-in that nothing in the docs mentions (item 1),
the silent single-constructor choice (item 4), and the amount of hand-written generator code for a domain whose
constructors are mostly interdependent (item 5), where the scaffold's `With…` surface was lost as soon as I had to
derive fields inside `Generate()`.
