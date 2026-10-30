namespace Identity.Domain;

/// <summary>A registered OAuth client: its redirect URIs and the scopes it may ever request.</summary>
public sealed class ClientApplication {

    private readonly HashSet<RedirectUri> _redirectUris;
    private readonly HashSet<Scope>       _allowedScopes;

    public ClientApplication(ClientId id, string displayName, IReadOnlyCollection<RedirectUri> redirectUris, IReadOnlyCollection<Scope> allowedScopes) {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(displayName.Length, 80, nameof(displayName));
        ArgumentNullException.ThrowIfNull(redirectUris);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        if (redirectUris.Count == 0) {
            throw new ArgumentException("A client needs at least one redirect URI.", nameof(redirectUris));
        }

        if (allowedScopes.Count == 0) {
            throw new ArgumentException("A client needs at least one allowed scope.", nameof(allowedScopes));
        }

        _redirectUris  = new HashSet<RedirectUri>(redirectUris);
        _allowedScopes = new HashSet<Scope>(allowedScopes);
        if (_redirectUris.Count != redirectUris.Count) {
            throw new ArgumentException("Redirect URIs must be distinct.", nameof(redirectUris));
        }

        Id          = id;
        DisplayName = displayName;
    }

    public ClientId Id { get; }

    public string DisplayName { get; }

    public IReadOnlyCollection<RedirectUri> RedirectUris => _redirectUris;

    public IReadOnlyCollection<Scope> AllowedScopes => _allowedScopes;

    /// <summary>Exact match only, as RFC 6749 recommends: no prefix or wildcard matching.</summary>
    public bool AcceptsRedirectTo(Uri candidate) => candidate.IsAbsoluteUri && _redirectUris.Any(registered => registered.Value.AbsoluteUri == candidate.AbsoluteUri);

    public bool MayRequest(Scope scope) => _allowedScopes.Contains(scope);

}
