namespace InternationalCatalogue.Domain;

/// <summary>A person named on a record, with the part they played in the work.</summary>
public sealed class Contributor : IEquatable<Contributor>
{
    public Contributor(PersonName name, ContributorRole role)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown contributor role.");
        }

        Name = name;
        Role = role;
    }

    public PersonName Name { get; }

    public ContributorRole Role { get; }

    public bool Equals(Contributor? other) => other is not null && other.Role == Role && other.Name.Equals(Name);

    public override bool Equals(object? obj) => Equals(obj as Contributor);

    public override int GetHashCode() => HashCode.Combine(Name, Role);

    public override string ToString() => $"{Name} ({Role})";
}
