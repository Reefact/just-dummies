namespace Clinical.Domain;

/// <summary>A patient as the prescription sees them.</summary>
public sealed class Patient {

    public static readonly DateOnly EarliestBirthDate = new DateOnly(1900, 1, 1);

    public Patient(Nir nir, string lastName, string firstName, DateOnly birthDate) {
        ArgumentNullException.ThrowIfNull(nir);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        if (birthDate < EarliestBirthDate) {
            throw new ArgumentOutOfRangeException(nameof(birthDate), "A birth date is on or after 1900-01-01.");
        }
        if (birthDate.Year % 100 != nir.BirthYearTwoDigits || birthDate.Month != nir.BirthMonth) {
            throw new ArgumentException("The birth date does not match the year and month carried by the NIR.", nameof(birthDate));
        }

        Nir       = nir;
        LastName  = lastName;
        FirstName = firstName;
        BirthDate = birthDate;
    }

    public Nir Nir { get; }

    public string LastName { get; }

    public string FirstName { get; }

    public DateOnly BirthDate { get; }

    public int AgeOn(DateOnly date) {
        int age = date.Year - BirthDate.Year;
        if (date < BirthDate.AddYears(age)) {
            age--;
        }

        return age;
    }

    public bool IsMinorOn(DateOnly date) {
        return AgeOn(date) < 18;
    }

}
