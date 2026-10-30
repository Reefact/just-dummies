namespace Identity.Domain;

public sealed class ScopeNotAllowedException : Exception {

    public ScopeNotAllowedException(Scope scope) : base($"Scope '{scope}' is not allowed for this client.") {
        Scope = scope;
    }

    public Scope Scope { get; }

}

/// <summary>Issues access tokens for a client, within a policy.</summary>
public sealed class TokenIssuer {

    private readonly TokenPolicy _policy;

    public TokenIssuer(TokenPolicy policy) {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public AccessToken Issue(ClientApplication client, Guid subjectId, IEnumerable<Scope> requested, DateTimeOffset now, TimeSpan requestedLifetime) {
        HashSet<Scope> scopes = new HashSet<Scope>(requested);
        foreach (Scope scope in scopes) {
            if (!client.MayRequest(scope)) {
                throw new ScopeNotAllowedException(scope);
            }
        }

        if (scopes.Count > _policy.MaximumScopes) {
            throw new InvalidOperationException($"At most {_policy.MaximumScopes} scopes may be granted at once.");
        }

        TimeSpan lifetime = requestedLifetime > _policy.MaximumLifetime ? _policy.MaximumLifetime : requestedLifetime;

        return new AccessToken(Guid.NewGuid(), client.Id, subjectId, scopes, now, now + lifetime);
    }

}
