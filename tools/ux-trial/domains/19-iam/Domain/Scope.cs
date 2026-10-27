using System.Text.RegularExpressions;

namespace Identity.Domain;

/// <summary>An OAuth scope such as <c>orders:read</c>: a lower-case resource, a colon, a lower-case action.</summary>
public sealed partial class Scope : IEquatable<Scope> {

    public Scope(string value) {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (!ScopeFormat().IsMatch(value)) {
            throw new FormatException($"'{value}' is not a scope of the form resource:action.");
        }

        Value = value;
    }

    public string Value { get; }

    public string Resource => Value.Substring(0, Value.IndexOf(':'));

    public string Action => Value.Substring(Value.IndexOf(':') + 1);

    public bool Equals(Scope? other) => other is not null && string.Equals(other.Value, Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as Scope);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z][a-z0-9_]{1,31}:(read|write|admin)$")]
    private static partial Regex ScopeFormat();

}
