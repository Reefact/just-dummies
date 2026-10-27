namespace Identity.Domain;

public sealed class ClientId : IEquatable<ClientId> {

    public ClientId(Guid value) {
        if (value == Guid.Empty) {
            throw new ArgumentException("A client id cannot be the empty GUID.", nameof(value));
        }

        Value = value;
    }

    public Guid Value { get; }

    public bool Equals(ClientId? other) => other is not null && other.Value == Value;

    public override bool Equals(object? obj) => Equals(obj as ClientId);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

}
