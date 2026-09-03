#region Usings declarations

using JetBrains.Annotations;

using NFluent;

#endregion

namespace JustDummies.UnitTests;

/// <summary>
///     The scope form of reproducibility: a handle a caller opens and disposes itself, for a test-framework adapter
///     that observes a test through before/after hooks and therefore has no delegate to wrap (ADR-0014). The seed
///     behaviour must match <c>Dummy.Reproducibly</c>; what the scope adds is the replay snippet a
///     generation-failure diagnostic names, so a run pinned from outside the test body never advertises a call the
///     test does not contain.
/// </summary>
[TestSubject(typeof(Dummy))]
public sealed class AmbientSeedScopeTests {

    #region Statics members declarations

    private static (int, string) Batch() {
        return (Dummy.Int32().Generate(), Dummy.String().NonEmpty().Generate());
    }

    #endregion

    [Fact(DisplayName = "UseSeed pins the ambient context, so the same seed yields the same values.")]
    public void UseSeedPinsTheAmbientContext() {
        (int, string) first;
        (int, string) second;

        using (Dummy.UseSeed(1234)) { first = Batch(); }
        using (Dummy.UseSeed(1234)) { second = Batch(); }

        Check.That(second).IsEqualTo(first);
    }

    [Fact(DisplayName = "UseSeed with different seeds produces different sequences.")]
    public void DifferentSeedsDiffer() {
        (int, string) fromOne;
        (int, string) fromTwo;

        using (Dummy.UseSeed(1)) { fromOne = Batch(); }
        using (Dummy.UseSeed(2)) { fromTwo = Batch(); }

        Check.That(fromTwo).IsNotEqualTo(fromOne);
    }

    [Fact(DisplayName = "UseSeed pins the same sequence as Reproducibly for the same seed.")]
    public void UseSeedMatchesReproducibly() {
        (int, string) fromScope;
        (int, string) fromRunner = default;

        using (Dummy.UseSeed(4242)) { fromScope = Batch(); }
        Dummy.Reproducibly(4242, () => { fromRunner = Batch(); });

        Check.That(fromScope).IsEqualTo(fromRunner);
    }

    [Fact(DisplayName = "Disposing the scope restores the previous context with its draw sequence intact.")]
    public void DisposingRestoresThePreviousContext() {
        (int, string) first;
        (int, string) second;
        (int, string) restoredFirst;
        (int, string) restoredSecond;

        using (Dummy.UseSeed(7)) {
            first  = Batch();
            second = Batch();
        }

        using (Dummy.UseSeed(7)) {
            restoredFirst = Batch();
            // A nested scope draws from its own generator; the outer one must neither be consumed nor reset by
            // it, so the outer sequence resumes exactly where it was interrupted.
            using (Dummy.UseSeed(99)) { Batch(); }
            restoredSecond = Batch();
        }

        Check.That(restoredFirst).IsEqualTo(first);
        Check.That(restoredSecond).IsEqualTo(second);
    }

    [Fact(DisplayName = "Nested scopes pin the inner seed while they are open.")]
    public void NestedScopesPinTheInnerSeed() {
        (int, string) standalone;
        (int, string) nested;

        using (Dummy.UseSeed(555)) { standalone = Batch(); }

        using (Dummy.UseSeed(111)) {
            using (Dummy.UseSeed(555)) { nested = Batch(); }
        }

        Check.That(nested).IsEqualTo(standalone);
    }

    [Fact(DisplayName = "Disposing the scope twice is harmless.")]
    public void DisposingTwiceIsHarmless() {
        IDisposable scope = Dummy.UseSeed(31);

        scope.Dispose();

        Check.ThatCode(() => scope.Dispose()).DoesNotThrow();
    }

    [Fact(DisplayName = "Disposing an outer scope out of order leaves the still-open inner scope's seed pinned.")]
    public void OutOfOrderDisposalKeepsTheInnerScopePinned() {
        // Reference: what seed 2 yields as the sole, top-of-stack scope.
        (int, string) reference;
        using (Dummy.UseSeed(2)) { reference = Batch(); }

        // Open two scopes, then dispose the OUTER first — the inner (seed 2) is still open, so the ambient
        // context must still be pinned to seed 2. A blind restore-the-previous unpins it instead, leaving the
        // still-open inner scope drawing from a fresh unseeded generator.
        IDisposable outer = Dummy.UseSeed(1);
        IDisposable inner = Dummy.UseSeed(2);
        outer.Dispose();
        (int, string) whileInnerStillOpen = Batch();
        inner.Dispose();

        Check.That(whileInnerStillOpen).IsEqualTo(reference);
    }

    [Fact(DisplayName = "Out-of-order disposal does not leak a pinned seed to what runs next.")]
    public void OutOfOrderDisposalDoesNotLeakASeed() {
        // Reference: seed 1's sequence, to prove it is NOT what a later, unseeded draw replays.
        (int, string) seedOneSequence;
        using (Dummy.UseSeed(1)) { seedOneSequence = Batch(); }

        // Dispose the outer first, then the inner: a blind restore-the-previous now reinstates seed 1's frame,
        // stranding it as the ambient context for whatever runs next.
        IDisposable outer = Dummy.UseSeed(1);
        IDisposable inner = Dummy.UseSeed(2);
        outer.Dispose();
        inner.Dispose();

        // Both scopes are closed, so the ambient context must be unseeded again — not pinned to seed 1.
        (int, string) afterAllDisposed = Batch();

        Check.That(afterAllDisposed).IsNotEqualTo(seedOneSequence);
    }

    [Fact(DisplayName = "Disposing a middle scope out of order leaves the top scope and its live ancestors intact.")]
    public void OutOfOrderMiddleDisposalPreservesTheStack() {
        (int, string) seedThree;
        using (Dummy.UseSeed(3)) { seedThree = Batch(); }
        (int, string) seedOne;
        using (Dummy.UseSeed(1)) { seedOne = Batch(); }

        IDisposable bottom = Dummy.UseSeed(1);
        IDisposable middle = Dummy.UseSeed(2);
        IDisposable top    = Dummy.UseSeed(3);

        // Dispose the middle scope early: the top (seed 3) is untouched and stays pinned.
        middle.Dispose();
        (int, string) topStillPinned = Batch();

        // Now the top goes: it must skip the already-disposed middle and land on the bottom (seed 1).
        top.Dispose();
        (int, string) afterTopDisposed = Batch();

        bottom.Dispose();

        Check.That(topStillPinned).IsEqualTo(seedThree);
        Check.That(afterTopDisposed).IsEqualTo(seedOne);
    }

    [Fact(DisplayName = "The scope does not leak across parallel execution contexts.")]
    public async Task TheScopeDoesNotLeakAcrossExecutionContexts() {
        (int, string) inside;
        (int, string) outside = default;

        using (Dummy.UseSeed(2026)) {
            inside = Batch();

            // A task started inside the scope inherits it, so run the probe on a context that never saw it.
            await Task.Run(() => { outside = Batch(); }, TestContext.Current.CancellationToken);
        }

        // The probe drew from a context whose scope was never entered, so it cannot have replayed the pinned
        // sequence. (An unseeded draw could coincide, but not across both components of the batch.)
        Check.That(outside).IsNotEqualTo(inside);
    }

    [Fact(DisplayName = "Without a replay snippet, a generation failure names Dummy.Reproducibly.")]
    public void WithoutAnInstructionTheFailureNamesTheDelegateRunner() {
        DummyGenerationException caught;

        using (Dummy.UseSeed(1234)) {
            caught = Assert.Throws<DummyGenerationException>(
                () => Dummy.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Seed).IsEqualTo(1234);
        Check.That(caught.Message).Contains("The arbitrary values were seeded with 1234");
        Check.That(caught.Message).Contains("Dummy.Reproducibly(1234, ...)");
    }

    [Fact(DisplayName = "With a replay snippet, a generation failure names it instead of Dummy.Reproducibly.")]
    public void WithAnInstructionTheFailureNamesIt() {
        DummyGenerationException caught;

        using (Dummy.UseSeed(1234, "[Reproducible(Seed = 1234)]")) {
            caught = Assert.Throws<DummyGenerationException>(
                () => Dummy.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("The arbitrary values were seeded with 1234");
        Check.That(caught.Message).Contains("[Reproducible(Seed = 1234)]");
        // The whole point: the reader is never pointed at a call their test does not contain.
        Check.That(caught.Message).Not.Contains("Dummy.Reproducibly");
    }

    [Fact(DisplayName = "The replay snippet also reaches the partial-replay guidance.")]
    public void TheInstructionReachesThePartialReplayGuidance() {
        DummyGenerationException caught;

        using (Dummy.UseSeed(777, "[Reproducible(Seed = 777)]")) {
            IDummy<int> foreign = new ForeignDummy();
            caught = Assert.Throws<DummyGenerationException>(
                () => Dummy.Combine<int, int, int>(Dummy.Int32(), foreign, (_, _) => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("not reproducible from this seed alone");
        Check.That(caught.Message).Contains("[Reproducible(Seed = 777)]");
        Check.That(caught.Message).Not.Contains("Dummy.Reproducibly");
    }

    [Fact(DisplayName = "The replay snippet is scoped: it does not outlive the scope that supplied it.")]
    public void TheInstructionDoesNotOutliveItsScope() {
        using (Dummy.UseSeed(1, "[Reproducible(Seed = 1)]")) { }

        DummyGenerationException caught;
        using (Dummy.UseSeed(2)) {
            caught = Assert.Throws<DummyGenerationException>(
                () => Dummy.Int32().As<int, int>(_ => throw new InvalidOperationException("rejected")).Generate());
        }

        Check.That(caught.Message).Contains("Dummy.Reproducibly(2, ...)");
        Check.That(caught.Message).Not.Contains("[Reproducible");
    }

    [Fact(DisplayName = "UseSeed rejects a null replay snippet.")]
    public void UseSeedRejectsANullInstruction() {
        Check.ThatCode(() => Dummy.UseSeed(1, null!)).Throws<ArgumentNullException>();
    }

    [Theory(DisplayName = "UseSeed rejects a blank replay snippet.")]
    [InlineData("")]
    [InlineData("   ")]
    public void UseSeedRejectsABlankInstruction(string instruction) {
        Check.ThatCode(() => Dummy.UseSeed(1, instruction)).Throws<ArgumentException>();
    }

    #region Nested types

    /// <summary>A generator carrying no random source, so a derivation over it cannot promise a full replay.</summary>
    private sealed class ForeignDummy : IDummy<int> {

        public int Generate() {
            return 42;
        }

    }

    #endregion

}
