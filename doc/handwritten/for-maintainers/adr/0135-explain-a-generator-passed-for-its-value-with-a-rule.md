# ADR-0135 | Explain a generator passed for its value with a rule

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](0135-explain-a-generator-passed-for-its-value-with-a-rule.fr.md)

**Status:** Accepted
**Proposed:** 2026-11-06
**Accepted:** 2026-11-06
**Decision Makers:** Reefact

## Context

A scaffolded `With…` takes a value to pin or a lambda to refine the parameter's valid base, and no
generator: a prepared generator would replace that base with every rule it kept
([ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md)). Passing one
therefore does not compile, and the compiler says only `CS1503`, cannot convert. The third developer-UX
trial met it in four sessions (CLI-21); two of them had read the page paragraph that explains it.

The same error meets a generator passed anywhere its value is expected — a constructor, a helper —
where the generator was simply not drawn from.

Two places can carry the explanation: a rule shipped with the library, or an overload the scaffolder
emits to fail with a message of its own.

## Decision

A warning in the usage family, JD039, reports an argument whose type is a generator of `T` passed to a
call that fails overload resolution while a candidate parameter takes a `T`, and names the way out: the
lambda overload when the method has one, `Generate()` otherwise.

## Rationale

**The types already say it, so no marker is needed.** Overload resolution keeps the candidates it
rejected; the argument's type says it is a generator of `T`, and a candidate parameter says it takes a
`T`. Unlike a rule on a whole scaffolded chain, nothing about the scaffolded file has to be recognised.

**It explains where the error is.** The diagnostic sits on the argument, beside `CS1503`, in every IDE
and build, where a page paragraph had been read and missed.

**It covers the same mistake outside the scaffolder.** A generator passed to a constructor or a helper
taking its value is the same slip, and gets the same explanation.

**It adds no gate.** The call already fails to compile; the rule only says why, so it cannot block code
that would otherwise build.

## Alternatives Considered

### An `[Obsolete(error: true)]` overload emitted by `any`

Considered because the compiler would then print the library's own message, from the CLI train alone.
Rejected: every scaffolded file would carry one unusable overload per parameter, offered by completion,
in a file the developer owns
([ADR-0056](0056-scaffold-the-generator-once-and-hand-the-file-to-the-developer.md)), and it would help
only scaffolded generators.

### Keep the documented paragraph alone

Considered because the error is intended and documented. Rejected: two of the four sessions had read it
and still hit the error with nothing pointing back to it.

## Consequences

### Positive

* `CS1503` on a generator argument comes with its reason and its remedy, scaffolded or not.

### Negative

* One more rule to keep in step with its pages, its catalogue constant and its count.

### Risks

* The rule reads candidates of a call that does not bind; a compiler that stops reporting them would
  silence it. The named cases in `JustDummies.Analyzers.UnitTests` compile the failing call and would show it.

## Follow-up Actions

* None beyond the implementation and its documentation.

## References

* [ADR-0109](0109-let-a-test-refine-a-parameters-valid-base-rather-than-replace-it.md) — why a scaffolded
  `With…` takes no generator.
* [UX trial 3, CLI-21](../../../../tools/ux-trial/REPORT-3.md#cli-21).
* Issue [#486](https://github.com/Reefact/just-dummies/issues/486).
