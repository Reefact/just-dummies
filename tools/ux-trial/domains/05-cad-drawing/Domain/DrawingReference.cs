using System.Text.RegularExpressions;

namespace Drafting.Domain;

/// <summary>A drawing number, its revision letter and sheet, e.g. MEC-04217 rev C sheet 2.</summary>
public sealed partial class DrawingReference : IEquatable<DrawingReference> {

    public DrawingReference(string number, char revision, int sheet) {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        if (!NumberFormat().IsMatch(number)) {
            throw new FormatException($"'{number}' is not a drawing number (expected e.g. MEC-04217).");
        }
        // ASME Y14.35: I, O, Q, S, X and Z are never used as revision letters.
        if (revision is < 'A' or > 'Y' or 'I' or 'O' or 'Q' or 'S' or 'X') {
            throw new ArgumentOutOfRangeException(nameof(revision), revision, "Not an allowed revision letter.");
        }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sheet);

        Number   = number;
        Revision = revision;
        Sheet    = sheet;
    }

    public string Number   { get; }
    public char   Revision { get; }
    public int    Sheet    { get; }

    [GeneratedRegex(@"^(MEC|ELE|HYD)-\d{5}$")]
    private static partial Regex NumberFormat();

    public DrawingReference NextRevision() {
        char next = (char)(Revision + 1);
        while (next is 'I' or 'O' or 'Q' or 'S' or 'X') {
            next++;
        }
        if (next > 'Y') {
            throw new InvalidOperationException($"{this} has no revision letter left.");
        }

        return new DrawingReference(Number, next, Sheet);
    }

    public bool Equals(DrawingReference? other) {
        return other is not null && other.Number == Number && other.Revision == Revision && other.Sheet == Sheet;
    }

    public override bool Equals(object? obj) {
        return Equals(obj as DrawingReference);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Number, Revision, Sheet);
    }

    public override string ToString() {
        return $"{Number} rev {Revision} sheet {Sheet}";
    }

}
