namespace JustDummies;

/// <summary>
///     A generator of arbitrary <c>ws</c>/<c>wss</c> URIs. Per RFC 6455 a WebSocket URI carries <b>no user-info and
///     no fragment</b>, so — unlike <see cref="DummyWebUri" /> — this builder does not expose them. Pin the TLS variant
///     with <see cref="UsingWs" />/<see cref="UsingWss" />; unpinned, the scheme is drawn from both.
/// </summary>
public sealed class DummyWebSocketUri : IDummy<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal DummyWebSocketUri(RandomSource source, UriSpec spec) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (spec is null) { throw new ArgumentNullException(nameof(spec)); }
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the scheme to <c>ws</c>. Declared once per generator.</summary>
    public DummyWebSocketUri UsingWs() {
        return new DummyWebSocketUri(_source, _spec.WithScheme("ws", ConstraintCall.Of(nameof(UsingWs))));
    }

    /// <summary>Pins the scheme to <c>wss</c>. Declared once per generator.</summary>
    public DummyWebSocketUri UsingWss() {
        return new DummyWebSocketUri(_source, _spec.WithScheme("wss", ConstraintCall.Of(nameof(UsingWss))));
    }

    /// <summary>Pins the host. Must be an ASCII host name (pass the punycode form for internationalized hosts).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host" /> is empty, non-ASCII or not a valid host name.</exception>
    public DummyWebSocketUri WithHost(string host) {
        return new DummyWebSocketUri(_source, _spec.WithHost(UriSpec.RequireHost(host, nameof(host)), UriSpec.Label(nameof(WithHost), host)));
    }

    /// <summary>Includes an arbitrary non-default port.</summary>
    public DummyWebSocketUri WithPort() {
        return new DummyWebSocketUri(_source, _spec.WithPort(null, UriSpec.Label(nameof(WithPort))));
    }

    /// <summary>Includes the given <paramref name="port" />.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="port" /> is outside 1..65535.</exception>
    public DummyWebSocketUri WithPort(int port) {
        return new DummyWebSocketUri(_source, _spec.WithPort(UriSpec.RequirePort(port, nameof(port)), UriSpec.Label(nameof(WithPort), port)));
    }

    /// <summary>Fixes the path to exactly <paramref name="count" /> segments. Declared once per generator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when a path constraint is already declared.</exception>
    public DummyWebSocketUri WithPathSegments(int count) {
        return new DummyWebSocketUri(_source, _spec.WithPath(UriPathMode.Exact, UriSpec.RequireSegmentCount(count, nameof(count)), UriSpec.Label(nameof(WithPathSegments), count)));
    }

    /// <summary>Renders the root path (<c>/</c>) with no segments. Declared once per generator.</summary>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when a path constraint is already declared.</exception>
    public DummyWebSocketUri WithoutPath() {
        return new DummyWebSocketUri(_source, _spec.WithPath(UriPathMode.Root, 0, ConstraintCall.Of(nameof(WithoutPath))));
    }

    /// <summary>Includes an arbitrary query string.</summary>
    public DummyWebSocketUri WithQuery() {
        return new DummyWebSocketUri(_source, _spec.WithQuery());
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
