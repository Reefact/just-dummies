namespace JustDummies;

/// <summary>
///     A generator of arbitrary <b>relative</b> URI references — a path with an optional query and fragment, and no
///     scheme or authority (e.g. <c>orders/42?page=2</c> or <c>/a/b/c#top</c>). A relative reference is well-formed on
///     its own; only its <i>resolution</i> against a base needs a base, not its validity. <see cref="Generate" />
///     returns a <see cref="Uri" /> with <see cref="Uri.IsAbsoluteUri" /> <c>false</c>.
/// </summary>
public sealed class DummyRelativeUri : IDummy<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal DummyRelativeUri(RandomSource source, UriSpec spec) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (spec is null) { throw new ArgumentNullException(nameof(spec)); }
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Starts the path with <c>/</c> (an absolute-path reference).</summary>
    public DummyRelativeUri Rooted() {
        return new DummyRelativeUri(_source, _spec.Rooted());
    }

    /// <summary>Fixes the path to exactly <paramref name="count" /> segments. Declared once per generator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when a path constraint is already declared.</exception>
    public DummyRelativeUri WithPathSegments(int count) {
        return new DummyRelativeUri(_source, _spec.WithPath(UriPathMode.Exact, UriSpec.RequireSegmentCount(count, nameof(count)), UriSpec.Label(nameof(WithPathSegments), count)));
    }

    /// <summary>Includes an arbitrary query string.</summary>
    public DummyRelativeUri WithQuery() {
        return new DummyRelativeUri(_source, _spec.WithQuery());
    }

    /// <summary>Includes an arbitrary fragment.</summary>
    public DummyRelativeUri WithFragment() {
        return new DummyRelativeUri(_source, _spec.WithFragment());
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
