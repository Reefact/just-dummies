namespace Identity.Domain;

public sealed class AccessToken {

    public AccessToken(Guid tokenId, ClientId clientId, Guid subjectId, IReadOnlyCollection<Scope> scopes, DateTimeOffset issuedAt, DateTimeOffset expiresAt) {
        if (tokenId == Guid.Empty || subjectId == Guid.Empty) {
            throw new ArgumentException("A token and its subject must be identified.");
        }

        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(scopes);
        if (scopes.Count == 0) {
            throw new ArgumentException("A token carries at least one scope.", nameof(scopes));
        }

        if (expiresAt <= issuedAt) {
            throw new ArgumentException("A token must expire after it is issued.", nameof(expiresAt));
        }

        if (expiresAt - issuedAt > TokenPolicy.HardMaximumLifetime) {
            throw new ArgumentException("A token cannot live longer than 24 hours.", nameof(expiresAt));
        }

        TokenId   = tokenId;
        ClientId  = clientId;
        SubjectId = subjectId;
        Scopes    = new HashSet<Scope>(scopes);
        IssuedAt  = issuedAt;
        ExpiresAt = expiresAt;
    }

    public Guid TokenId { get; }

    public ClientId ClientId { get; }

    public Guid SubjectId { get; }

    public IReadOnlySet<Scope> Scopes { get; }

    public DateTimeOffset IssuedAt { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;

    public TimeSpan RemainingLifetimeAt(DateTimeOffset now) => now >= ExpiresAt ? TimeSpan.Zero : ExpiresAt - now;

    public bool Authorizes(Scope required, DateTimeOffset now) => !IsExpiredAt(now) && Scopes.Contains(required);

}
