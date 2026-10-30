using System.Text.RegularExpressions;

namespace Education.Domain;

// A course of the catalogue, e.g. MAT101 "Linear algebra", worth 6 ECTS credits.
public sealed class Course {

    public Course(string code, string title, int credits) {
        if (!Regex.IsMatch(code ?? string.Empty, "^[A-Z]{3}[0-9]{3}$")) {
            throw new ArgumentException("A course code is three capitals and three digits.", nameof(code));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(credits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(credits, 30);

        Code    = code!;
        Title   = title;
        Credits = credits;
    }

    public string Code { get; }

    public string Title { get; }

    public int Credits { get; }

    public override bool Equals(object? obj) => obj is Course other && other.Code == Code;

    public override int GetHashCode() => Code.GetHashCode();
}
