namespace Identity.Domain;

/// <summary>CLI probe: distinctness checked directly on the parameter.</summary>
public sealed class ConsentScreen {

    public ConsentScreen(IReadOnlyCollection<Scope> requestedScopes) {
        ArgumentNullException.ThrowIfNull(requestedScopes);
        if (requestedScopes.Distinct().Count() != requestedScopes.Count) {
            throw new ArgumentException("Requested scopes must be distinct.", nameof(requestedScopes));
        }

        RequestedScopes = requestedScopes;
    }

    public IReadOnlyCollection<Scope> RequestedScopes { get; }

}

/// <summary>CLI probe: a Uri guarded inline on its scheme, and a TimeSpan guarded with the ThrowIf helper.</summary>
public sealed class WebhookSubscription {

    public WebhookSubscription(Uri callback, TimeSpan retryAfter) {
        ArgumentNullException.ThrowIfNull(callback);
        if (callback.Scheme != Uri.UriSchemeHttps) {
            throw new ArgumentException("Callbacks must use https.", nameof(callback));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(retryAfter, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(retryAfter, TimeSpan.FromMinutes(10));

        Callback   = callback;
        RetryAfter = retryAfter;
    }

    public Uri Callback { get; }

    public TimeSpan RetryAfter { get; }

}

/// <summary>CLI probe: a Guid guarded through a switch expression.</summary>
public sealed class TenantId {

    public TenantId(Guid value) {
        Value = value switch {
            _ when value == Guid.Empty => throw new ArgumentException("Empty tenant.", nameof(value)),
            _                          => value,
        };
    }

    public Guid Value { get; }

}

/// <summary>CLI probe: distinctness checked through a local copy of the parameter.</summary>
public sealed class RedirectAllowList {

    public RedirectAllowList(IReadOnlyCollection<RedirectUri> uris) {
        ArgumentNullException.ThrowIfNull(uris);
        HashSet<RedirectUri> distinct = new HashSet<RedirectUri>(uris);
        if (distinct.Count != uris.Count) {
            throw new ArgumentException("Redirect URIs must be distinct.", nameof(uris));
        }

        Uris = distinct;
    }

    public IReadOnlyCollection<RedirectUri> Uris { get; }

}

/// <summary>CLI probe: distinctness checked through a field copy of the parameter.</summary>
public sealed class PostLogoutRedirects {

    private readonly HashSet<RedirectUri> _uris;

    public PostLogoutRedirects(IReadOnlyCollection<RedirectUri> uris) {
        _uris = new HashSet<RedirectUri>(uris);
        if (_uris.Count != uris.Count) {
            throw new ArgumentException("Redirect URIs must be distinct.", nameof(uris));
        }
    }

    public IReadOnlyCollection<RedirectUri> Uris => _uris;

}

/// <summary>The roles a subject holds in one tenant; the same role cannot be held twice.</summary>
public sealed class RoleSet {

    private readonly HashSet<Role> _roles;

    public RoleSet(IReadOnlyCollection<Role> roles) {
        ArgumentNullException.ThrowIfNull(roles);
        _roles = new HashSet<Role>(roles);
        if (_roles.Count != roles.Count) {
            throw new ArgumentException("A role cannot be held twice.", nameof(roles));
        }
    }

    public IReadOnlyCollection<Role> Roles => _roles;

    public Permission EffectivePermissions => _roles.Aggregate(Permission.None, (granted, role) => granted | RolePermissions.Of(role));

}
