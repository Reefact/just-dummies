# `gendummy-sweep` workflow

🌍 🇬🇧 English (this file) · 🇫🇷 [Français](gendummy-sweep.fr.md)

> Maintainer documentation — part of the [workflow reference](README.md).
> Not part of the user documentation under `doc/`.

**Workflow file:** [`.github/workflows/gendummy-sweep.yml`](../../../../.github/workflows/gendummy-sweep.yml)
**Suite:** [`JustDummies.GenDummy.UnitTests/GenerativeSweepTests.cs`](../../../../JustDummies.GenDummy.UnitTests/GenerativeSweepTests.cs)
**Bench:** [`JustDummies.GenDummy.UnitTests/Sweep/`](../../../../JustDummies.GenDummy.UnitTests/Sweep/)

## What it is for

It takes the product of a declared set of axes — collection type × element type × size guard ×
family — and puts every domain that comes out through the whole scaffolder: scaffold it, compile
what came out, run the library's own analyzers over it, and draw from it. About 3600 shapes.

**It is the instrument that finds things,** and its first complete run found two defects nobody had
seen. That claim is measured rather than assumed. Over the
guard-reading campaign of August 2026, mutation testing and the named corpus of
[`GuardCorpus.cs`](../../../../JustDummies.GenDummy.UnitTests/GuardCorpus.cs) between them produced
**no engine defect** across twenty-six hand-written shapes; an ad-hoc generative survey produced
**twenty**. The two look for different things, and until this workflow existed the one that finds
defects **was not in the repository**: that survey was scratch code. Nothing could replay it against
`main`, and nobody could say whether its findings were closed.

The three benches divide like this, and none of them replaces another:

| Bench | Asks |
|---|---|
| `GuardCorpus` + `GuardedScaffoldsHoldTests` | *does the engine get **this** domain right?* — a person chose each one, and each one is a question |
| The mutation legs of [`justdummies-mutation`](justdummies-mutation.en.md) | *is there code nothing asserts?* — cells no test has visited |
| This sweep | *does anything in a wide, uniform product come out wrong?* — nobody chose any of them |

## The seven rules

The sweep predicts nothing. A bench that computed the expected verdict from the axes would encode
today's behaviour and become a change detector wearing a defect detector's clothes; one that
classified by the wording of a compiler message would be reading prose. What it holds shapes to are
claims that stay true whatever the engine does — and the first is a claim about the bench itself.

| # | Rule | A violation is |
|---|---|---|
| 0 | the generated domain compiles **on its own**, before anything is asked of the engine | **a sweep bug** — never a finding |
| 1 | the engine scaffolds the target, and every generator the target's own file names | a finding |
| 2 | what does not compile, does not compile **on a sentinel line** | a finding |
| 3 | with the `TODO_verify_*` line deleted, as §5.6 tells the developer to, it compiles | a finding |
| 4 | no rule of the library's own above `Info`, and no `Info` outside `JD030` | a finding |
| 5 | a draw produces a value, **or** is refused with `DummyGenerationException` | a finding |
| 6 | a distinctness refusal happens exactly when the source says it must — **both directions** | a finding |

### Rule 0 is the one that was learned the hard way

The August survey printed 4394 rows. Sorting them afterwards showed 208 whose emitted file failed on
`CS0019: Operator '<' cannot be applied to operands of type 'method group' and 'int'` — the survey
had guarded arrays with `.Count` instead of `.Length`, so **the domains it generated did not compile**,
and it read its own broken C# as engine defects. Nothing in it could tell the two apart.

So the sweep compiles the domain alone first, and a failure there is reported in words that cannot be
mistaken for a finding. It is also why `SweepAxes.Collections` carries the count member per collection
rather than assuming `Count`: the axis knows what an array answers to.

### Rule 5 is the sharpest one, and it is free

Every one of the 352 draw failures in the August survey was a `DummyGenerationException` — the library
declining a domain it cannot honour (ADR-0046). Not one was the domain's own constructor rejecting a
value the engine produced. That gives an exact line: a refusal in the first class is an outcome the
library is entitled to, and **anything else is a value that should never have been drawn**.

### Rule 6 is the one August could not state

`SweepAxes.Elements` declares a cardinality per element — how many distinct values the type holds —
and only where the generated source itself settles it, which in practice means the enums. Then a set
demanding five distinct `Slot` where `Slot` declares three members **must** be refused, and a set
demanding two `Wide` where `Wide` declares thirty-two **must not** be. Between those the answer
depends on how the library draws, the library bounds its redraws and fails rather than looping
(ADR-0004, ADR-0012, ADR-0027), and the sweep says nothing rather than guessing.

## The verdicts

Pass and fail would be wrong here: three of these are outcomes the engine is **entitled** to, and
folding them into "failed" would report the library's own honesty as a defect — the mirror of the
mistake [ADR-0093](../adr/0093-publish-mutation-statuses-not-a-score.md) records on the mutation
instrument, where a timeout was folded into "killed".

| Verdict | What happened |
|---|---|
| `Held` | compiled, raised nothing, drew values its own domain accepts |
| `RefusedByDesign` | the draw met a first-class refusal (ADR-0046) |
| `BlockedForVerification` | a `TODO_verify_*` sentinel blocks compilation over a base that is real (§5.6, ADR-0083) |
| `Unresolved` | a `TODO_supply_a_generator_for_*` sentinel: an open parameter (§5.5) |
| `KnownResidue` | the generator drew a value the domain rejects, **and §9 says it would** |
| `KnownDefect` | a rule broken, and an entry in `SweepDefects` already says so |
| `Finding` | a rule the engine must hold, broken |
| `SweepBug` | a generated domain does not compile on its own — ours, not the engine's |

`KnownResidue` is the one worth reading twice. §9 declares, as a non-goal, that a guard reached
through a level of indirection the tool does not follow — *a local copy of the parameter* above all —
is one the tool cannot tell from no guard at all: it marks nothing, blocks nothing, and draws freely.
The sixteen `delegate-computed-*` shapes sit there on purpose, each carrying the sentence that excuses
it. **This is the only instrument in the repository that puts a number on how wide that residue is**,
and a shape that stops landing there moves the committed counts — so the residue shrinking announces
itself too.

## What the first run found

The whole product runs in **two minutes** on four cores. On 2026-09-02 it came back with 3627 shapes
judged, no sweep bug, and **103 findings in two classes** — both open, both recorded in
[`SweepDefects.cs`](../../../../JustDummies.GenDummy.UnitTests/Sweep/SweepDefects.cs), neither fixed in
the change that installed the bench.

**`cardinality-hint-lost-through-as` (55 shapes).** `Dummy.SetOf(Dummy.Boolean())` gates the set at two
elements, because `DummyBoolean` carries `ICardinalityHint<bool>` and `DummySet` reads it (ADR-0004).
`Dummy.SetOf(Dummy.Boolean().As(value => (bool?)value))` does not: `DummyExtensions.As` returns a
`DerivedDummy<TResult>` carrying the random source and the reproducibility of what it wraps, and nothing
else. The set then has no ceiling, picks a size the element pool cannot fill, and dies on the bounded
redraw — on a domain that asks for **one** element. Forwarding the hint through a projection is sound
in general: a projection can collapse distinct values, never create them. It reaches every scaffolded
set or dictionary keyed by a **nullable** enum or bool, since that cast is exactly what the engine
writes for a nullable element.

**`nested-collection-loses-its-declared-interface` (48 shapes).** `Dummy.SetOf(…)` is typed
`IDummy<HashSet<T>>` and `Dummy.ListOf(…)` is `IDummy<List<T>>`, so a collection *of* one of those carries
the concrete type where the parameter declares the interface. Covariant outer types still bind —
which is why `nested-rolist-*` and `nested-array-*` compile — and invariant ones cannot:
`List<HashSet<Slot>>` is not a `List<ISet<Slot>>`. The emitted file then fails on a plain `CS0029`
with no sentinel over it, which is the one thing ADR-0083 says must not happen.
`List<IReadOnlyList<string>>` is an ordinary domain.

Two other numbers in that run are worth reading. The `element` family came back **78 blocked, 0 held**:
a distinctness guard over the elements and a null check inside a `foreach` are both outside the closed
set of §5.3, so every one of them meets a sentinel — which is the designed answer, working. And the
sixteen `delegate-computed-*` shapes are the whole of `KnownResidue`: the §9 residue, measured.

## When it runs

* **Weekly**, Mondays at 07:07 UTC.
* **On demand**, through `workflow_dispatch`.

Never on a pull request, for the reason [ADR-0028](../adr/0028-drop-the-justdummies-generator-from-the-per-pull-request-mutation-matrix.md)
gave the generator's mutation leg: the cost follows the size of the product, not the size of the diff.

**What runs on every build instead** is the covering slice — the smallest prefix-greedy subset that
still touches every axis value, about ninety shapes — as an ordinary theory inside
`JustDummies.GenDummy.UnitTests`. It cannot find what the product finds. It exists so the apparatus
cannot quietly stop working between two Mondays, which is precisely how the other benches in this
repository broke.

## How it runs

The workflow adds exactly one thing to an ordinary test run: the `JUSTDUMMIES_SWEEP` variable.
Without it the full sweep skips and the slice still runs, so `dotnet test JustDummies.sln` stays fast
and the generator's mutation leg pays nothing for the sweep existing.

```
dotnet test JustDummies.sln                                     # the slice only
JUSTDUMMIES_SWEEP=1 dotnet test JustDummies.GenDummy.UnitTests    # the whole product
```

Shapes run **sequentially**, and not for want of cores: the draw runs under an ambient seed
(ADR-0061) that two shapes drawing at once would share. A bench whose values depend on how many ran
beside it is the exact defect ADR-0093 records on the other instrument.

## What it publishes

Counts by verdict, per family — never a score, for the reason ADR-0093 gives. A run where every shape
came back `Unresolved` would score perfectly against any ratio anyone cared to define, and would mean
the engine had stopped resolving anything.

* `artifacts/sweep/generative-sweep.tsv` — one row per shape, in the seven columns the August survey
  printed (`name`, `family`, `status`, `provenance`, `compiles`, `rules`, `draw`) plus the verdict and
  its reason, so the two surveys can be joined line by line.
* `artifacts/sweep/summary.md` — the counts, which the job appends to the run summary.
* `JustDummies.GenDummy.UnitTests/Sweep/sweep-baseline.tsv` — **committed**, and checked by the full
  sweep.

## Handle with care

* **The baseline is a golden file, and it moves deliberately.** It carries one line per family and
  verdict — coarse on purpose. A line per shape would be accepted rather than reviewed; a table this
  size shows a coverage regression as a number that moved, and three hundred shapes sliding from a
  read guard to a sentinel breaks no rule of the oracle and would otherwise pass in silence. On a
  mismatch the run writes `sweep-baseline.received.tsv` beside it. Move the second over the first
  **only** once you can say why it moved.
* **Do not fold a verdict into another to make a run green.** Each of the six answers a different
  question, and the whole design rests on their staying apart.
* **A `SweepDefects` entry comes off with the fix, not with the test.** An entry no shape reproduces any
  more fails the run: a defect nothing reproduces is a defect that was fixed, and its entry is then the
  only thing left saying otherwise.
* **A `KnownResidue` needs its sentence.** The `residue:` argument is a claim about the
  **specification**, not a prediction about the engine: it says a reader can find the sentence that
  excuses this shape. Adding one without that sentence turns the bench into a change detector.
* **Adding an axis value multiplies.** The element axis is spent wide on the distinct collections,
  where cardinality decides the answer, and narrow on the rest, where a count guard interacts with the
  element through nothing at all. Keep that asymmetry when you extend it.
* **The sweep claims no row of the closed idiom tables** (§5.3). That is the corpus's work, and
  `RecognisedIdiomCoverageTests` is its judge. The two benches do not overlap and must not start to.

## Related

* [`justdummies-mutation`](justdummies-mutation.en.md) — the other instrument, and what it measures
  instead.
* [ADR-0083](../adr/0083-block-compilation-on-a-guard-the-engine-cannot-vouch-for.md) — why an
  unverifiable guard blocks compilation rather than shipping quietly.
* [ADR-0085](../adr/0085-change-the-guard-reader-only-against-a-field-report.md) — the
  named corpus, and the draw oracle both benches share.
* [ADR-0093](../adr/0093-publish-mutation-statuses-not-a-score.md) — statuses rather than a score, and
  why a status that means "no verdict" must never be folded into one that means "caught".
* [`justdummies-tool.md`](../specifications/justdummies-tool.md) — §5.3 the closed idiom set, §5.5 and
  §5.6 the two sentinels, §9 the residue.
