# Discovery notes — 25 adaptive video

## What I did
- Read `guides/getting-started`, the `any` CLI page, `generators/numbers` and `collections`, `guides/composition`
  and the "what arbitrary does not promise" part of `core-concepts`. Nothing else.
- Created `Tests.Discovery`, restored, and scaffolded all nine types in one call:
  `any generate Bitrate EncodingVariant Segment Latency BandwidthEstimate AdaptationPolicy PlaybackBuffer BitrateLadder PlaybackSession --entry-point any --output Arbitrary`.
- Resolved every `to verify`, then wrote 38 tests (value objects, ladder, decisions, and the
  decide/download/play loop of `PlaybackSession`). All 38 pass; I re-ran the suite 8 times, all green.

## Frictions and surprises, in the order I met them
1. **Most guards came back `to verify`.** 15 of 19 parameters did. Only `index < 0` and `sampleCount < 1`
   were read. Every guard written the usual .NET way, `if (x < Min || x > Max) throw`, was reported as
   `unread guard`, e.g. `kbps: unread guard at Bitrate.cs, line 11: if (kbps < MinKbps || kbps > MaxKbps)`.
   I checked with a throwaway type and `--dry-run`: the same range split into two `if`s gives
   `Any.Int32().Between(128, 7680)  guard`, but `if (b <= 0 || b > 1)` gives `unread guards`.
   The CLI page lists what the tool cannot read and never names a disjunction, so I only learned this by
   experimenting. The message did tell me what to do (the line, and "verify or replace, then delete the line").
2. `double.IsNaN(x) || ...` is also unread, though NaN is never drawn anyway, so the tool could ignore that part.
3. The `TODO(any)` blocks are long (6 lines each, 15 of them). Removing them by hand is tedious. I used a
   small script, which a newcomer would not have.
4. **Rules between parameters** (the id `720p30` must match `height`, `height <= width`, comfort > panic,
   level <= capacity, comfort <= buffer capacity): the CLI page's "When a guard ties two parameters" was
   exactly the help I needed. I used disjoint bases (panic 0..4, comfort (4..15]) and derived the id in
   `Generate()`. For the id this meant restructuring the generated class: `WithId(...)` became
   `WithFramesPerSecond(int)`, and the `_id` field now holds only the fps suffix. It works, but the
   generated shape fights you a little when one parameter is computed from another.
5. `BitrateLadder` needs distinct bitrates **and** heights that never drop as the bitrate rises. A
   per-element `AnyEncodingVariant` cannot give that, so I replaced the base with
   `Any.Combine(ListOf(...).Distinct(), ListOf(...).Distinct(), ..., composer)`. As the doc warns, the
   factory type changes to `IAny<IEnumerable<EncodingVariant>>`, so I deleted the refining overload.
   The doc said this in advance; no surprise, just work.
6. I hesitated over naming: is `Any.Segment().WithDurationSeconds(4.0).Generate()` a dummy? The core-concepts
   section "Arbitrary does not mean dummy" settled it: `anyFourSecondSegment`, drawn index and chosen duration.
7. Many session tests are mostly literals (ladder rungs, buffer level, throughput, elapsed time) because the
   assertion depends on them. The dummies are mostly latency, frame rates, widths, smoothing and sample count.
   That is correct, but with an aggregate like this the generator for `PlaybackSession` helps only the
   "refuses when ..." tests.
8. No analyzer diagnostic at all on the final build (I expected JD030 or JD031 somewhere). Not a problem.

## What worked well
- The recap table and its last column: I knew immediately what was inferred and what was guessed.
- The cross-parameter guard in `PlaybackSession` (`policy.ComfortBufferSeconds > buffer.CapacitySeconds`)
  was flagged on **both** `buffer` and `policy`, with the line. That prevented a flaky default.
- The "does not compile until you resolve it" design: I could not forget a parameter.
- `--entry-point any` + `With…(lambda)` read very naturally:
  `Any.PlaybackSession().WithBuffer(b => b.WithLevelSeconds(3).WithCapacitySeconds(30))`.
- Refining a nested base (`WithCapacitySeconds(c => c.GreaterThanOrEqualTo(15))` on top of `Between(4, 300)`)
  just worked.
- `[Fact, Reproducible]` needed nothing else. All tests passed on the first run.

## Fluency: 4 / 5
The library itself was smooth: the vocabulary is guessable, the docs answered every real question (cross-parameter
rules, changing a factory's type) before I asked it. I lose one point to `any` on this domain: three quarters of
the parameters needed manual work because `||` range guards are not read, and the docs do not say so.
