# Developer-UX trial

A hands-on trial of JustDummies and the `any` scaffolder, as a developer who has never seen them
would meet them: twenty invented business domains, each modelled with ordinary constructor guards,
scaffolded with `any`, and tested with xUnit using only the user documentation as a guide. The point
is twofold — to find out whether writing tests with the library is fluent, and to catch bugs on the
way.

It ran twice:

* **Domains 01–20**: one participant per domain, who also probed the library's limits on purpose.
  The findings, verified and deduplicated, are in [`REPORT.md`](REPORT.md) and
  [`REPORT.fr.md`](REPORT.fr.md).
* **Domains 21–40**: richer models (graphs, state machines, probability distributions, 3D geometry,
  rational durations, double-entry ledgers…), each modelled once, then tested by three developer
  profiles — one discovering the library, one using it for months, one expert crafter — on real
  cases only, with no boundary hunting. The findings, verified and deduplicated, are in
  [`REPORT-2.md`](REPORT-2.md) and [`REPORT-2.fr.md`](REPORT-2.fr.md).

## Layout

| | |
|---|---|
| `domains/NN-name/Domain` | the domain model: value objects and aggregates with their guards |
| `domains/NN-name/Tests` | the scaffolded `AnyX` generators and the tests; `Probes.cs` holds the deliberate edge cases |
| `domains/NN-name/NOTES.md` | the participant's log: CLI recaps, hand fixes, findings, a fluency score |
| `domains/NN-name/MODEL.md` | domains 21–40: the model's types, invariants and behaviour |
| `domains/NN-name/Tests.Discovery`, `Tests.Regular`, `Tests.Expert` | domains 21–40: each profile's own generators and tests, scaffolded independently |
| `domains/NN-name/NOTES.discovery.md`, `.regular.md`, `.expert.md` | domains 21–40: each profile's log and fluency score |
| `repros/` | the minimal reproductions left by the verification pass |
| `smoke/` | a small domain whose generators are **not** committed: `run.sh` scaffolds them with the packed `any` into a disposable `.smoke/` copy, then builds and tests it |

Every test passes. During the trial a test named `Bug_…` demonstrated a defect and was left failing;
those defects are fixed, and the trial is now run regularly, so it stays green. A defect found
later is an issue, and the test that shows it lands with its fix. A probe that pins the exact text of a
message is updated when the message is improved on purpose.

## Running it

Like `tools/justdummies-check`, this is deliberately **outside** `JustDummies.sln`: it consumes the
packed packages, not the projects.

```
tools/ux-trial/run.sh                                            # the consumer contract, as the release runs it
tools/ux-trial/run.sh --all                                      # every domain, as the ux-trial workflow does
tools/ux-trial/prepare.sh                                        # or prepare once,
dotnet test --project tools/ux-trial/domains/01-payments/Tests  # then run one domain
```

The two uses are kept apart. The **consumer contract** blocks a release: the `release` workflow runs
`run.sh` before it packs and publishes a train. It checks that the packed `any` scaffolds the smoke
domain, that the result builds with the bundled analyzers loaded (the build must report JD014 on
`smoke/Tests/AnalyzerProbe.cs`) and passes its tests, and that one full domain passes. The **whole
corpus** is for exploration and a wide regression net: the `ux-trial` workflow runs `run.sh --all` on
demand and every week, and blocks nothing. `prepare.sh` packs the three packages into `local-feed/` and installs `any` into `.tools/`; run
`.tools/any generate <Type>` from a domain's `Tests` folder. The shared `Directory.Build.props`,
`Directory.Packages.props` and `nuget.config` here shadow the repository's own, so each trial
project builds as a consumer's would.
