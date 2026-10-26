# ADR-0108 | Name the scaffolder after what it draws, not after the role a value plays

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0108-name-the-scaffolder-after-what-it-draws.fr.md)

**Status:** Accepted
**Proposed:** 2026-10-26
**Accepted:** 2026-10-26
**Decision Makers:** Reefact

> Section references (§N) point into the [`any` specification](../specifications/justdummies-tool.md).

## Context

The library defines a dummy by the test that uses it: a value the test needs and does not care
about, whose value never reaches the assertion. **Data that takes part in what the test is trying to
verify is not a dummy**, whatever produced it.

The scaffolder was named `dum` and described itself as writing "the dummy generator" for a type. What
it writes is an `AnyX`: a `partial class` implementing `IAny<X>`, with a `With…` method per
constructor parameter (§4). A test can pin any of those parameters to a value it has chosen, and
nothing stops it pinning the one its assertion is about. Even with nothing pinned, the drawn object is
a dummy only if the test does not look at it.

Everything else the library exposes is already named after what it draws, not after the role the
value plays: the entry point is `Any`, the generators are `AnyInt32`, `AnyString`, `IAny<T>`, the
scaffolding engine is `JustDummies.GenAny`, the emitted types are `AnyX`, and the tool's own entry
point option is `--entry-point any`. The scaffolder was the only surface named after the role.

No `cli` release has left beta: the train stands at `cli-v1.1.0-beta.6`, and its command name and
the name of its project defaults file (ADR-0072) are not yet a stable contract.

Issue #272 reported the contradiction from a reader's side: the tool's description promises a dummy
generator, and the library's own definition says the object it returns, once a value is pinned, is
not a dummy.

## Decision

The scaffolder's command is `any`, and every name it owns — its project defaults file, its emitted
comments, its run report — follows the command.

## Rationale

**A tool is named after what it produces, and what it produces is a generator of arbitrary values.**
Whether a drawn value is a dummy is decided by the test that draws it, after the tool has finished.
Naming the tool after that role makes a promise the tool cannot keep, and the promise is broken as soon
as a test uses what it writes as intended — pinning, through a `With…` method, the value its assertion
is about.

**The library keeps its name, because the name is a scope, not a description of each value.**
*JustDummies* says what the library is *for*: producing values for tests to use as dummies, and
refusing to grow into a property-testing or object-filling tool. That stays true. The tool's name has
a narrower job: to say what the command does when it runs, and there `any` is exact.

**One vocabulary is cheaper than a vocabulary and an exception.** With the tool renamed, a reader
meets `Any`, `AnyX`, `IAny<T>`, `GenAny` and `any` — one word for one idea. Keeping `dum` would have
left the only name built on *dummy* on the one surface where the definition does not hold.

**This is the moment it is cheap.** A command name becomes a contract the day a stable release
publishes it, and scripts and `dum.json` files then have to be migrated. Before that, the cost is one
change in this repository and a line in the changelog.

**The defaults file follows the command, with no fallback.** A file named after a command that no
longer exists is a riddle for the next reader. Reading the old name as well would keep two spellings
alive for a tool that has only ever shipped betas, and a silently honoured legacy file is the kind of
default ADR-0072 refuses to leave implicit.

## Alternatives Considered

### Keep `dum` and explain the nuance in the documentation

It was the smallest change, and the issue asked for no more than an explanation.

Rejected: an explanation has to be found and read, while a command name is seen on every invocation.
Documenting that the "dummy generator" does not always produce dummies is documenting a defect.

### Rename the library too

Considered for full consistency: if the tool should not say *dummy*, why should the package?

Rejected: the library's name states its scope, which is still correct, and the
[design principles](../../for-users/guides/design-principles.en.md) are built on it. The package name
is also far more widely published than a beta tool's command.

### A longer, unambiguous command such as `justdummies` or `anygen`

Considered to avoid a short, generic command name on a developer's `PATH`.

Rejected: no command named `any` is known to collide on the platforms the tool supports, and the
tool is installed deliberately, as a global .NET tool. A longer name would break the one-word
vocabulary the rename exists to establish.

## Consequences

### Positive

* The tool's description and the library's definition of a dummy no longer contradict each other.
* One word, `any`, names everything the library and its tool draw.

### Negative

* Anyone who installed a beta must type `any` instead of `dum` and rename their `dum.json` to
  `any.json`. The changelog says so.
* Accepted records and dated audits keep saying `dum`: they record what was true when they were
  written, and are not edited.

### Risks

* A future tool named `any` on the same machine would shadow, or be shadowed by, this one. The risk
  is the same for any global tool name, and is not specific to this one.

## Follow-up Actions

* Explain in the user documentation where a JustDummies dummy sits among the vocabulary of
  *xUnit Test Patterns*, which readers will bring with them.

## References

* [ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md) — scaffold the generator once.
* [ADR-0057](0057-make-the-emitted-generator-a-first-class-iany.md) — the emitted generator is a first-class `IAny<T>`.
* [ADR-0072](0072-read-project-defaults-from-a-file-the-command-line-overrides.md) — the project defaults file.
* Issue [#272](https://github.com/Reefact/just-dummies/issues/272).
