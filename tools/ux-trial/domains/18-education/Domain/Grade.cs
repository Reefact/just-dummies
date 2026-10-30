namespace Education.Domain;

// A French-style grade out of 20, recorded in half-point steps: 0, 0.5, 1, ..., 19.5, 20.
public sealed class Grade {

    public const decimal Maximum = 20m;

    public Grade(decimal value) {
        if (value < 0m || value > Maximum) {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A grade lies between 0 and 20.");
        }
        if (value % 0.5m != 0m) {
            throw new ArgumentException($"A grade is recorded in half points; {value} is not.", nameof(value));
        }

        Value = value;
    }

    public decimal Value { get; }

    public bool IsEliminatory => Value < 5m;

    public override bool Equals(object? obj) => obj is Grade other && other.Value == Value;

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"{Value}/20";
}
