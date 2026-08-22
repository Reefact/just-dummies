# ADR-0071 | Report a run as data without moving the exit codes

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0071-report-a-run-as-data-without-moving-the-exit-codes.fr.md)

**Status:** Accepted
**Proposed:** 2026-08-12
**Accepted:** 2026-08-22
**Decision Makers:** Reefact

> Section references (§N) point into the [`dum` specification](../specifications/justdummies-tool.md).

## Context

§7 makes a file written with open parameters a **success**: the write succeeded, and the developer's
own build reports the rest, which is the mechanism [ADR-0060](0060-seed-generators-from-constructor-guards.md)
records. Exit `0` therefore reads the same whether every parameter resolved or a third of them did
not.

A single invocation takes several type arguments, processed independently, and exits with the worst
of them (§7). A caller scaffolding forty types in one command has one number for the lot.

The tool's public surface is its command line, and it has published a release, `cli-v1.0.0-beta.1`.
The exit codes of §7 are part of that surface: a script already reads them.

The engine returns its result model and the CLI renders it; the recap of §6 is a rendering, and the
specification states in as many words that provenance is data, not output.

`--dry-run` already spends stdout: the recap goes to stderr and the file to stdout, so one can be
piped while the other is read (§6).

Regeneration and drift detection are dropped, not deferred (§16), so nothing else in the tool reports
on a working tree.

## Decision

A run reports itself as one JSON document on stdout when `--format json` asks for it, carrying the
facts the exit code cannot, and the exit codes of §7 keep the meanings they were published with.

## Rationale

**The missing fact has a shape, and it is not an exit code.** What a scripted bootstrap needs to know
is *how many parameters were left open*, per type and for the run — a number, not a verdict.
Expressing it as an exit code would mean either overloading `0` with a second meaning or minting a
third code, and both rewrite a contract that shipped. Adding a channel costs nothing already
published and answers the question exactly.

**Refusing to move the exit codes is the point, not a caution.** A tool that quietly redefined
success would break the scripts that had been reading it correctly, and it would break them silently
— the worst failure mode a release can have. The report is additive for the same reason the entry
point of [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md) is: the default
behaviour has to keep meaning what it meant.

**One rendering, two audiences, one set of facts.** The engine already returns the model and the
console already renders it, so the report is a second renderer rather than a second source of truth.
The provenance words come from the recap's own table for exactly that reason: two tables would drift,
and a script and a reader would come to disagree about the same run.

**stdout is the machine channel, and it has to be clean.** The recap is suppressed there under
`json`, and everything written for a person keeps going to stderr, so `2>/dev/null` leaves a
parseable pipe. `--dry-run` then has nowhere to put the file it would have printed, so the text
travels inside the document — losing it would make the two flags exclusive for no reason a caller
could act on.

**A total contract beats a shorter one.** A run that stops before its first scaffold produces a
document too, naming the refusal. The alternative — writing nothing — forces every consumer to tell
an empty stdout from a failed parse before it can even look at the run, which is a hole in the
contract disguised as brevity.

## Alternatives Considered

##### A new exit code for "written, but incomplete"

Considered because it needs no parsing at all, and a script already branches on the exit code.

Rejected because it changes what a published contract means. A caller reading `0` today would start
seeing the new code for runs it used to treat as successful, and the tool would have broken it
without saying so. It also carries only one bit where the useful answer is a count and a list.

##### Making an open parameter a failure

Considered because it would make the exit code answer the question directly.

Rejected because it contradicts ADR-0060: the open parameter *is* the mechanism, the file is written
on purpose, and the developer's build is where it is meant to be reported. Turning it into a failure
would make the tool refuse the very case it was designed to hand over.

##### Reporting through a file rather than stdout

Considered because a file survives a pipe and can be read after the fact.

Rejected because it makes the tool write something nobody asked for, in a place it would have to
invent, and leaves it behind on the next run. stdout is already the channel the caller chose by
running the command.

## Consequences

**Positive.** A scripted bootstrap over many types can tell a complete run from an incomplete one,
and say which parameters were left open, without parsing prose. Nothing already published changes
meaning. The recap and the report cannot disagree, because they read one table.

**Negative.** The tool now has two output contracts to keep, and the document's key names are one of
them — renaming a key is a breaking change to a caller even though no type in any assembly moved.
`--dry-run` behaves differently under each format, which is one more thing to know.

**Risks.** A consumer may come to depend on a key the specification does not describe. Mitigated by
the document being small, flat and written out in §6.1 rather than left to be discovered from a
sample.

## Follow-up Actions

* None. The gap this closes was the follow-up action recorded on
  [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).

## References

* §3, §6, §6.1, §7, §10.3, §16 of the specification.
* [ADR-0060](0060-seed-generators-from-constructor-guards.md),
  [ADR-0070](0070-emit-an-entry-point-on-request-as-a-file-of-its-own.md).
