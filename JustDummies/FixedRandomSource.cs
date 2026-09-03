namespace JustDummies;

/// <summary>
///     The isolated random context behind <see cref="Dummy.WithSeed" />: one fixed, seeded generator owned by a single
///     <see cref="DummyContext" />. Unlike the ambient source it does not flow with the execution context — it is
///     deterministic by construction and belongs to whoever holds the context.
/// </summary>
internal sealed class FixedRandomSource : RandomSource {

    private readonly SeededRandom _random;

    internal FixedRandomSource(int seed) {
        _random = new SeededRandom(seed);
    }

    internal override SeededRandom Current => _random;

    internal override string ReplayGuidance(int seed) {
        return $"The arbitrary values were drawn from Dummy.WithSeed({seed}), which already replays deterministically.";
    }

    internal override string PartialReplayGuidance(int seed) {
        return $"The seeded draws were made from Dummy.WithSeed({seed}), but some values come from a generator that does not draw from it, so they are not reproducible from this seed alone.";
    }

}
