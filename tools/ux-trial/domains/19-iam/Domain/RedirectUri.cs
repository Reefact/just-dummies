namespace Identity.Domain;

/// <summary>An OAuth redirect URI: absolute, https (plain http only on a loopback host), no fragment.</summary>
public sealed class RedirectUri : IEquatable<RedirectUri> {

    public RedirectUri(Uri value) {
        Validate(value);
        Value = value;
    }

    public Uri Value { get; }

    public bool Equals(RedirectUri? other) => other is not null && Uri.Compare(other.Value, Value, UriComponents.AbsoluteUri, UriFormat.UriEscaped, StringComparison.Ordinal) == 0;

    public override bool Equals(object? obj) => Equals(obj as RedirectUri);

    public override int GetHashCode() => Value.AbsoluteUri.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value.AbsoluteUri;

    private static void Validate(Uri value) {
        ArgumentNullException.ThrowIfNull(value);

        if (!value.IsAbsoluteUri) {
            throw new ArgumentException("A redirect URI must be absolute.", nameof(value));
        }

        bool secure = value.Scheme == Uri.UriSchemeHttps;
        bool loopbackHttp = value.Scheme == Uri.UriSchemeHttp && value.IsLoopback;
        if (!secure && !loopbackHttp) {
            throw new ArgumentException($"Scheme '{value.Scheme}' is not allowed for a redirect URI.", nameof(value));
        }

        if (value.Fragment.Length > 0) {
            throw new ArgumentException("A redirect URI must not carry a fragment.", nameof(value));
        }
    }

}
