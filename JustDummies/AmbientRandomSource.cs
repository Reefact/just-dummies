namespace JustDummies;

/// <summary>
///     The default random context behind the static <see cref="Dummy" /> entry points. The state is stored in an
///     <see cref="AsyncLocal{T}" />, so it flows with the current execution context and never leaks across tests
///     running in parallel. Outside an <see cref="UseSeed(int)" /> scope it lazily seeds itself with a fresh seed — every
///     run differs, which surfaces a test that secretly depends on a value — and that seed is remembered, so a
///     generation failure can still report it. Inside a scope (how <c>Dummy.Reproducibly(...)</c> pins a run) it is
///     deterministic.
/// </summary>
internal sealed class AmbientRandomSource : RandomSource {

    #region Statics members declarations

    internal static readonly AmbientRandomSource Instance = new();

    private static readonly AsyncLocal<AmbientState?> State = new();

    internal static int NewSeed() {
        return Guid.NewGuid().GetHashCode();
    }

    internal static IDisposable UseSeed(int seed) {
        return UseSeed(seed, null);
    }

    internal static IDisposable UseSeed(int seed, string? replaySnippet) {
        AmbientState frame = new(new SeededRandom(seed), replaySnippet, State.Value);
        State.Value = frame;

        return new SeedScope(frame);
    }

    #endregion

    private AmbientRandomSource() { }

    internal override SeededRandom Current {
        get {
            AmbientState? current = State.Value;
            if (current is null) {
                current     = new AmbientState(new SeededRandom(NewSeed()), null, null);
                State.Value = current;
            }

            return current.Random;
        }
    }

    internal override string ReplayGuidance(int seed) {
        return $"The arbitrary values were seeded with {seed}; reproduce this run with {ReplaySnippet(seed)}.";
    }

    internal override string PartialReplayGuidance(int seed) {
        return $"The seeded draws were made with {seed} ({ReplaySnippet(seed)}), but some values come from a generator that does not draw from this source, so they are not reproducible from this seed alone.";
    }

    /// <summary>
    ///     The code the reader copies to replay the current run — the fragment the guidance sentence embeds, never the
    ///     sentence itself: the snippet the opener of the scope supplied, or the delegate runner when none was. Read
    ///     from the scope rather than fixed on the source, because the ambient source is pinned by several mechanisms
    ///     and each is replayed differently.
    /// </summary>
    private static string ReplaySnippet(int seed) {
        return State.Value?.Snippet ?? $"Dummy.Reproducibly({seed}, ...)";
    }

    #region Nested types

    /// <summary>
    ///     One frame of the ambient seed stack a scope installs: the seeded generator, how to replay the run that uses
    ///     it, and the frame it was pushed on top of. The frames form a linked stack (each points at its
    ///     <see cref="Parent" />) so a scope disposed out of order can be removed without stranding the ones still open
    ///     — see <see cref="SeedScope" />. <see cref="Disposed" /> tombstones a frame whose scope has closed but which is
    ///     not yet the top of the stack, so the top's later disposal can skip past it.
    /// </summary>
    private sealed class AmbientState {

        internal AmbientState(SeededRandom random, string? replaySnippet, AmbientState? parent) {
            if (random is null) { throw new ArgumentNullException(nameof(random)); }

            Random  = random;
            Snippet = replaySnippet;
            Parent  = parent;
        }

        internal SeededRandom  Random   { get; }

        /// <summary>
        ///     The replay snippet the opener of this scope supplied, if any — the fragment, never the whole guidance
        ///     sentence. Named <c>Snippet</c> rather than <c>ReplaySnippet</c> so it does not shadow the enclosing
        ///     <see cref="AmbientRandomSource.ReplaySnippet(int)" />, which reads it.
        /// </summary>
        internal string?       Snippet  { get; }
        internal AmbientState? Parent   { get; }
        internal bool          Disposed { get; set; }

    }

    /// <summary>
    ///     The handle returned by <see cref="UseSeed(int, string?)" />. Disposal is <b>order-independent</b>: it
    ///     tombstones its own frame, and only the frame that is currently the top of the stack rewrites the ambient
    ///     slot — walking past any tombstoned ancestors to the nearest frame whose scope is still open (or to
    ///     <c>null</c> when none is). So the documented "scopes nest, disposing restores whatever was pinned before"
    ///     holds even when scopes are disposed out of order: an outer scope closed early strands nothing, and no order
    ///     leaves a dead seed pinned for whatever runs next. Disposing twice is a no-op.
    /// </summary>
    private sealed class SeedScope : IDisposable {

        private readonly AmbientState _frame;
        private          bool         _disposed;

        internal SeedScope(AmbientState frame) {
            if (frame is null) { throw new ArgumentNullException(nameof(frame)); }

            _frame = frame;
        }

        public void Dispose() {
            if (_disposed) { return; }

            _disposed       = true;
            _frame.Disposed = true;

            // Only the current top owns the ambient slot; an out-of-order dispose of an inner frame just tombstones
            // it and lets the top's own dispose skip it later.
            if (ReferenceEquals(State.Value, _frame)) {
                AmbientState? restored = _frame.Parent;
                while (restored is { Disposed: true }) { restored = restored.Parent; }
                State.Value = restored;
            }
        }

    }

    #endregion

}
