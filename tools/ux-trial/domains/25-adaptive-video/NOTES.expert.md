# Adaptive video: expert crafter notes

## What I did

- Created `Tests.Expert`, restored it, and scaffolded all nine constructible types in one call:
  `any generate Bitrate EncodingVariant Segment Latency BandwidthEstimate AdaptationPolicy PlaybackBuffer BitrateLadder PlaybackSession --entry-point any --output ./Arbitrary`.
- Resolved every `TODO_verify_…` line by writing the domain ranges with the domain's own constants
  (`Bitrate.MinKbps`, `Latency.MaxMilliseconds`, `Segment.MinDurationSeconds`, ...).
- For guards that tie two parameters, I used disjoint bands so the rule holds by construction:
  panic 0..8 s and comfort 10..30 s; buffer level 0..30 s and capacity 60..300 s. That also makes every
  drawn `PlaybackSession` valid (comfort <= capacity, segment <= capacity) and ready to decide.
- Rewrote two scaffolds by hand. In `AnyEncodingVariant`, the id is derived as `<height>p<fps>` and the
  width is drawn from the height up. In `AnyBitrateLadder`, two distinct sets (bitrates and heights) are
  sorted and paired, with `WithBitrates(400, 1000, 2500, 5000)`: the test pins the bitrates it is about
  and every other trait of a rung stays arbitrary.
- Put the reusable scenario recipes in `Player.cs`: the reference ladder, the policy, a trusted estimate
  that follows its last sample, `PlayingSecondRung(estimate, buffer)`, `Download` and `DrainTo`.
- Wrote 34 test methods (39 cases) covering startup, warm-up, panic, switching down several rungs at once,
  climbing one rung, holding, refusals, stalls, the EWMA estimate, ladder invariants, decision consistency,
  and an end-to-end "climb, then the network collapses, then a stall, then panic" scenario.
- `[assembly: Reproducible]` is set once. I checked the failure path with a throw-away failing test: the
  seed was reported as `[Reproducible(Seed = -1498147116)]`. 25 consecutive runs were all green.

## Frictions and surprises, in the order I met them

1. **The scaffolder read almost none of the range guards.** 13 of the 15 scalar parameters guarded without a regex came out as
   `to verify, unread guards`. Only `index < 0` and `sampleCount < 1` were read. The shapes it left
   unread are the most common ones in any domain:
   - `if (width < 128 || width > 7680)`: plain literals
   - `if (kbps < MinKbps || kbps > MaxKbps)`: constants
   - `if (double.IsNaN(panicBufferSeconds) || panicBufferSeconds < 0)`: a NaN check before one bound
   The output was honest (`width: unread guard at EncodingVariant.cs, line 19: if (width < 128 || width > 7680)`)
   and the build stopped at the right line. Still, nearly every factory needed a hand-written `Between`.
   The user documentation never says which guard shapes are read: its only example is `quantity <= 0`.
2. **Guards that tie parameters needed hand work, as documented.** That covers the id and the height,
   width >= height, and comfort > panic. The ladder is the hardest case: its invariants hold across
   elements (unique ids, distinct bitrates, resolution rising with bitrate), and
   `Any.SequenceOf(new AnyEncodingVariant())` cannot express them. Only the first guard
   (`list.Count == 0`) was named. The "start + duration" advice in the CLI page carried over well
   (derive, don't filter).
3. **Reusing a configured composite generator in a `With…` slot** means writing `WithLadder(_ => Player.Ladder)`.
   The documented idiom is `Func<AnyX, AnyX>`, which reads badly when the same recipe is also used
   on its own: you end up writing `Recipe(Any.BitrateLadder()).Generate()`.
4. **No shuffle or permutation generator.** "Whatever order the rungs are listed in" is a real dummy.
   I drew a permutation as `Any.ListOf(Any.Int32().Between(0, 2)).WithCount(3).Distinct()`, which works
   but reads as a trick.
5. **Making a foreign variant needed `Except`.** An arbitrary variant can, rarely, share an id with a
   rung (about 0.04 %). `WithHeight(h => h.Except(usedHeights))` removed that flakiness cleanly.
6. **The analyzers stayed silent** on the final code (SARIF: no JD result). I never hit a misuse to
   correct.

## What worked well

- One command scaffolds nine types, with the `Any.X()` entry points. The recap names the exact guard
  and line, and the build-breaking `TODO_verify` line is the best possible to-do list.
- Nested refinement reads like the domain:
  `WithBandwidth(b => b.WithSampleCount(c => c.GreaterThanOrEqualTo(3)).WithSmoothingFactor(1))`.
- I own the generated files: adding `WithBitrates(...)` and a derived id was ordinary code, and
  `Any.SetOf(...).WithCount(n)` made the distinct rungs trivial.
- With assembly-level `[Reproducible]`, each theory case gets its own seed, with zero ceremony per test.
- Tests read as intended: the significant numbers (bitrates, thresholds, budgets) are literals, and
  everything else is `Any…` or a recipe.

## What I would design differently

- Read the canonical `x < a || x > b` range guard (with literals or constants, plus a leading
  `double.IsNaN(x) ||`). That would remove most of this domain's manual work.
- Add `Any.PermutationOf(list)` (or `ShuffledOf`).
- Let a scaffolded parameter be marked "derived" from others, so a re-scaffold does not drop
  hand-reshaped generators. Today both rewritten files carry a "do not --force" warning.

## Fluency: 4/5

The library and the xUnit adapter were frictionless, and the analyzers never got in the way. One point
is lost because the scaffolder missed the domain's range guards: nearly every factory became manual work.
