using System.Text.RegularExpressions;

namespace Clinical.Domain;

/// <summary>An international non-proprietary name (INN), e.g. "paracetamol".</summary>
public sealed class Molecule {

    private static readonly Regex InnShape = new Regex("^[a-z][a-z -]*$", RegexOptions.Compiled);

    public Molecule(string name) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > 80) {
            throw new ArgumentOutOfRangeException(nameof(name), "An INN is at most 80 characters.");
        }
        if (!InnShape.IsMatch(name)) {
            throw new ArgumentException("An INN is lower-case letters, spaces and hyphens.", nameof(name));
        }

        Name = name;
    }

    public string Name { get; }

    public override bool Equals(object? obj) {
        return obj is Molecule other && other.Name == Name;
    }

    public override int GetHashCode() {
        return Name.GetHashCode();
    }

    public override string ToString() {
        return Name;
    }

}
