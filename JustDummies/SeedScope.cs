namespace JustDummies;

/// <summary>
///     The ambient seed scope <see cref="Any.UseSeed(int)" /> opens: disposing it restores whatever was pinned before,
///     and <see cref="HasDrawn" /> tells whoever opened it whether its seed served any draw — so that a failure names
///     a seed to replay only when replaying it would reproduce something.
/// </summary>
/// <remarks>
///     <para>
///         A test-framework adapter is the reader this exists for. It opens the scope before a test and reports the
///         seed when the test fails; a test that failed without drawing — on a literal, or on constraints refused
///         when they were declared — would otherwise be told a seed no value came from, and sent looking for a drawn
///         value that does not exist.
///     </para>
///     <para>
///         Only draws made from this scope's own seed count. Those of a scope nested inside it are made from the inner
///         seed, and those of an explicit <see cref="Any.WithSeed" /> context from that context's: replaying this
///         scope's seed reproduces neither, so neither makes <see cref="HasDrawn" /> true.
///     </para>
/// </remarks>
public sealed class SeedScope : IDisposable {

    #region Fields declarations

    private readonly SeededRandom _random;
    private readonly IDisposable  _release;

    #endregion

    internal SeedScope(SeededRandom random, IDisposable release) {
        if (random is null) { throw new ArgumentNullException(nameof(random)); }
        if (release is null) { throw new ArgumentNullException(nameof(release)); }

        _random  = random;
        _release = release;
    }

    /// <summary>
    ///     Whether at least one value was drawn from this scope's seed so far. It stays readable once the scope is
    ///     disposed, and it says nothing about which values or how many: only whether replaying the seed would
    ///     reproduce anything.
    /// </summary>
    public bool HasDrawn => _random.Draws > 0;

    /// <summary>
    ///     Restores the ambient context that was pinned before this scope opened. Scopes may be disposed in any order,
    ///     and disposing twice is harmless.
    /// </summary>
    public void Dispose() {
        _release.Dispose();
    }

}
