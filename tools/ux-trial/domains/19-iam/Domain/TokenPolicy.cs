namespace Identity.Domain;

/// <summary>How long a token may live and how many scopes it may carry.</summary>
public sealed class TokenPolicy {

    public static readonly TimeSpan HardMaximumLifetime = TimeSpan.FromHours(24);

    public TokenPolicy(TimeSpan maximumLifetime, int maximumScopes) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maximumLifetime, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumLifetime, HardMaximumLifetime);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumScopes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maximumScopes, 20);

        MaximumLifetime = maximumLifetime;
        MaximumScopes   = maximumScopes;
    }

    public TimeSpan MaximumLifetime { get; }

    public int MaximumScopes { get; }

}
