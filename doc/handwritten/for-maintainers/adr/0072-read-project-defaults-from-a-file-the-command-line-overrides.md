# ADR-0072 | Read project defaults from a file the command line overrides

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0072-read-project-defaults-from-a-file-the-command-line-overrides.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§3 stated that there is no config file, and it was written when every option was a per-invocation
decision: which project, which type, where this one file goes.

Two options added since are not that. `--entry-point` and `--entry-point-namespace`
([ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md)) describe how a project's
generators are reached, and the answer is the same for every type in it — a root gathered from
several namespaces is only a root if every scaffold contributes to the same one. `--output` is the
same kind of fact once a team has decided where its generators live.

An option that has to be repeated on every invocation is an option that will eventually be typed
differently on one of them, and the tool scaffolds once per type (ADR-0056) over a graph of
aggregates, so a project meets these options as many times as it has types.

§16 already reserved the file: an optional `dum.json` at the project root, with a `naming` key, for
the v1.1 naming options. Those options — `--name`, `--pattern` — are not implemented.

The engine performs no IO and knows nothing of MSBuild or the disk ([ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md));
the CLI locates the project and holds its path (§11.1).

The tool distinguishes a scaffolding failure (exit `1`) from a command line it could not read
(exit `2`), and §7 keeps the two apart.

## Decision

An optional `dum.json` beside the project file supplies defaults for the options that describe the
project rather than the invocation, the command line overrides any of them, and a key the file does
not read is refused.

## Rationale

**The file is warranted by what the options became, not by convenience.** §3's sentence was right
about the options it was written for and stopped being right when options describing the project
arrived. A team that wants one root, gathered in one namespace, is stating a property of a
repository; making them restate it per invocation is how the eleventh scaffold ends up in a different
namespace from the first ten.

**Precedence needs no table, and that is the design.** A value the developer typed is already in the
settings, and nothing the file supplies overwrites one — the whole rule is that the file fills blanks.
A config file whose interaction with the flags needs explaining is one nobody trusts enough to use.

**Refusing an unknown key is the point of having the file at all.** A silently ignored key is a
default the developer believes is in force and is not, which is a worse state than having no file: it
produces the wrong layout and gives no reason. This is the same bargain the rest of the tool makes —
refuse loudly at the edge rather than carry on plausibly — and it is why §16's own `naming` key is
refused here until the options it configures exist.

**Rooting a relative path at the project is what makes it a default.** A path typed on the command
line is relative to where it was typed, which is correct. A path committed beside the `.csproj` has
to mean the same thing from every working directory, or a developer running the tool from the
repository root and one running it from the test project get different layouts from the same
committed intent.

**One validation, not two.** The merged state goes back through the rules the command line answers
to, so a value from the file is refused for the same reasons and in the same words as a typed one.
A second rule set would drift, and the file would come to accept what the flag rejects.

**The shell reads it, so the engine stays what it is.** The file is on disk, next to a project the
CLI located; the engine is handed options, exactly as before, and keeps knowing nothing of either
(ADR-0065).

## Alternatives Considered

##### Leaving it out and keeping §3's sentence intact

Considered because the sentence is load-bearing: it is what a structural test holds the command line
to, and every option not added is a surface not defended.

Rejected because the sentence defends against *options*, and this adds none — the five keys are the
options that already exist. What it removes is the requirement to retype them, which is not surface
at all. The test that guards §3 still guards it: adding a sixth option would still have to be argued
for.

##### Ignoring an unrecognised key, as most config formats do

Considered because it is forgiving, and because a file that refuses an unknown key cannot be read by
an older tool once a newer one adds a key.

Rejected because forgiving is the wrong virtue here. The failure it forgives is a typo in the one
file whose whole job is to be believed, and the symptom — files landing in the wrong namespace — is
far from the cause. The forward-compatibility cost is real and small: the tool and the file are
committed to the same repository.

##### Putting the defaults in the `.csproj` as MSBuild properties

Considered because the project file is already the place a project's build settings live, and no new
file appears.

Rejected because it would put the tool's configuration behind MSBuild evaluation, which the engine
must not need (ADR-0065) and which the CLI would then have to interpret. A flat JSON file beside the
project is read by the shell in a dozen lines and by a developer at a glance.

##### Searching upwards from the current directory

Considered because it would let one file at the repository root serve several test projects.

Rejected because it makes which file applies depend on where the tool was run from, which is the
property this decision exists to remove. Beside the project is unambiguous, and a repository wanting
one shared set of defaults can copy four lines.

## Consequences

**Positive.** The options that describe a project are stated once, committed, and reviewed like any
other project setting. A typo is named rather than absorbed. Nothing about an invocation without a
file changes.

**Negative.** §3's "there is no config file" is no longer true and had to be rewritten; the sentence
was a useful thing to be able to say. The tool now has two places an option can come from, so
answering "why did it land there?" means looking at one more thing — mitigated by the refusals
naming the file. And a repository with several test projects needs the file in each of them.

**Risks.** The file will attract keys that are not options — a list of types to scaffold, a `--force`
default — each of which would take it further from "defaults for existing options" and toward the
`--all` and the regeneration that §16 dropped. The five-key list and its refusal are what hold that
line.

## Follow-up Actions

* When `--name` and `--pattern` land (§16), `naming` joins the read keys and stops being refused.

## References

* §3, §3.3, §7, §16 of the specification.
* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md),
  [ADR-0065](0065-keep-the-scaffolding-engine-loadable-by-a-roslyn-host.md),
  [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).
