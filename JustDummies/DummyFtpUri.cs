namespace JustDummies;

/// <summary>
///     A generator of arbitrary <c>ftp</c> URIs — the classic <c>ftp://user:password@host/path</c> shape of legacy
///     code. An FTP URI carries user-info but <b>no query and no fragment</b>, so this builder does not expose them.
/// </summary>
public sealed class DummyFtpUri : IDummy<Uri>, IHasRandomSource {

    #region Fields declarations

    private readonly RandomSource _source;
    private readonly UriSpec      _spec;

    #endregion

    internal DummyFtpUri(RandomSource source, UriSpec spec) {
        if (source is null) { throw new ArgumentNullException(nameof(source)); }
        if (spec is null) { throw new ArgumentNullException(nameof(spec)); }
        _source = source;
        _spec   = spec;
    }

    RandomSource? IHasRandomSource.Source => _source;

    /// <summary>Pins the host. Must be an ASCII host name (pass the punycode form for internationalized hosts).</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="host" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="host" /> is empty, non-ASCII or not a valid host name.</exception>
    public DummyFtpUri WithHost(string host) {
        return new DummyFtpUri(_source, _spec.WithHost(UriSpec.RequireHost(host, nameof(host)), UriSpec.Label(nameof(WithHost), host)));
    }

    /// <summary>Includes arbitrary <c>user:password</c> user-info.</summary>
    public DummyFtpUri WithUserInfo() {
        return new DummyFtpUri(_source, _spec.WithUserInfo(null, null, UriSpec.Label(nameof(WithUserInfo))));
    }

    /// <summary>Includes user-info with the given <paramref name="user" /> and an arbitrary password.</summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user" /> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="user" /> contains a non-unreserved character.</exception>
    public DummyFtpUri WithUserInfo(string user) {
        return new DummyFtpUri(_source, _spec.WithUserInfo(UriSpec.RequireUserInfoPart(user, nameof(user)), null, UriSpec.Label(nameof(WithUserInfo), user)));
    }

    /// <summary>Includes the given <paramref name="user" /> and <paramref name="password" /> user-info.</summary>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument contains a non-unreserved character.</exception>
    public DummyFtpUri WithUserInfo(string user, string password) {
        return new DummyFtpUri(_source, _spec.WithUserInfo(UriSpec.RequireUserInfoPart(user, nameof(user)), UriSpec.RequireUserInfoPart(password, nameof(password)), UriSpec.Label(nameof(WithUserInfo), user, password)));
    }

    /// <summary>Includes an arbitrary non-default port.</summary>
    public DummyFtpUri WithPort() {
        return new DummyFtpUri(_source, _spec.WithPort(null, UriSpec.Label(nameof(WithPort))));
    }

    /// <summary>Includes the given <paramref name="port" />.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="port" /> is outside 1..65535.</exception>
    public DummyFtpUri WithPort(int port) {
        return new DummyFtpUri(_source, _spec.WithPort(UriSpec.RequirePort(port, nameof(port)), UriSpec.Label(nameof(WithPort), port)));
    }

    /// <summary>Fixes the path to exactly <paramref name="count" /> segments. Declared once per generator.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when a path constraint is already declared.</exception>
    public DummyFtpUri WithPathSegments(int count) {
        return new DummyFtpUri(_source, _spec.WithPath(UriPathMode.Exact, UriSpec.RequireSegmentCount(count, nameof(count)), UriSpec.Label(nameof(WithPathSegments), count)));
    }

    /// <summary>Renders the root path (<c>/</c>) with no segments. Declared once per generator.</summary>
    /// <exception cref="ConflictingDummyConstraintException">Thrown when a path constraint is already declared.</exception>
    public DummyFtpUri WithoutPath() {
        return new DummyFtpUri(_source, _spec.WithPath(UriPathMode.Root, 0, ConstraintCall.Of(nameof(WithoutPath))));
    }

    /// <inheritdoc />
    public Uri Generate() {
        return _spec.Generate(_source);
    }

}
