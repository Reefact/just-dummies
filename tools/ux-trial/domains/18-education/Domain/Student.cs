namespace Education.Domain;

public sealed class Student {

    public Student(StudentNumber number, string fullName, DateOnly birthDate) {
        Validate(number, fullName, birthDate);

        Number    = number;
        FullName  = fullName.Trim();
        BirthDate = birthDate;
    }

    public StudentNumber Number { get; }

    public string FullName { get; }

    public DateOnly BirthDate { get; }

    private static void Validate(StudentNumber number, string fullName, DateOnly birthDate) {
        ArgumentNullException.ThrowIfNull(number);
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100) {
            throw new ArgumentException("A full name is between 1 and 100 characters.", nameof(fullName));
        }
        if (birthDate < new DateOnly(1900, 1, 1)) {
            throw new ArgumentOutOfRangeException(nameof(birthDate), birthDate, "Born before 1900.");
        }
    }
}
