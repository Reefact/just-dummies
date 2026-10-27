namespace Insurance.Domain;

public sealed class Driver {

    public Driver(string fullName, DateOnly birthDate, int yearsLicensed) {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentOutOfRangeException.ThrowIfNegative(yearsLicensed);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(yearsLicensed, 80);

        FullName      = fullName.Trim();
        BirthDate     = birthDate;
        YearsLicensed = yearsLicensed;
    }

    public string   FullName      { get; }
    public DateOnly BirthDate     { get; }
    public int      YearsLicensed { get; }

    public bool IsYoungDriver => YearsLicensed < 3;

}
