# Developer-UX trial

A hands-on trial of JustDummies and the `any` scaffolder, as a developer who has never seen them
would meet them: twenty invented business domains, each modelled with ordinary constructor guards,
scaffolded with `any`, and tested with xUnit using only the user documentation as a guide. The point
is twofold — to find out whether writing tests with the library is fluent, and to catch bugs on the
way.

The findings, verified and deduplicated, are in [`REPORT.md`](REPORT.md).

## Layout

| | |
|---|---|
| `domains/NN-name/Domain` | the domain model: value objects and aggregates with their guards |
| `domains/NN-name/Tests` | the scaffolded `AnyX` generators and the tests; `Probes.cs` holds the deliberate edge cases |
| `domains/NN-name/NOTES.md` | the participant's log: CLI recaps, hand fixes, findings, a fluency score |
| `repros/` | the minimal reproductions left by the verification pass |

A test named `Bug_…` demonstrates a defect reported in `REPORT.md` and is **expected to fail**. Some
turn green once the library is fixed and re-packed; those about the scaffolder replay the recipe `any`
emitted at the time, so they are confirmed by re-scaffolding instead — `REPORT.md` says which is which.

## Running it

Like `tools/justdummies-check`, this is deliberately **outside** `JustDummies.sln`: it consumes the
packed packages, not the projects.

```
tools/ux-trial/prepare.sh
dotnet test --project tools/ux-trial/domains/01-payments/Tests
```

`prepare.sh` packs the three packages into `local-feed/` and installs `any` into `.tools/`; run
`.tools/any generate <Type>` from a domain's `Tests` folder. The shared `Directory.Build.props`,
`Directory.Packages.props` and `nuget.config` here shadow the repository's own, so each trial
project builds as a consumer's would.
