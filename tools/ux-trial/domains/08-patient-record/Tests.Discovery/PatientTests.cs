using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Discovery;

public sealed class PatientTests {

    [Fact]
    public void A_patient_keeps_what_it_was_given() {
        // Arrange
        DateOnly birthDate = new DateOnly(1985, 5, 17);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();

        // Act
        Patient patient = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Assert
        Assert.Same(anyNir, patient.Nir);
        Assert.Equal("Dupont", patient.LastName);
        Assert.Equal("Marie", patient.FirstName);
        Assert.Equal(birthDate, patient.BirthDate);
    }

    [Fact]
    public void Names_are_stored_as_given_without_trimming() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();
        Nir      anyNir       = Any.Nir().BornOn(anyBirthDate).Generate();

        // Act
        Patient patient = new Patient(anyNir, "  de la Fontaine ", " Jean-Pierre  ", anyBirthDate);

        // Assert
        Assert.Equal("  de la Fontaine ", patient.LastName);
        Assert.Equal(" Jean-Pierre  ", patient.FirstName);
    }

    [Fact]
    public void A_null_nir_is_refused() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Patient(null!, "Dupont", "Marie", anyBirthDate));

        // Assert
        Assert.Equal("nir", refusal.ParamName);
    }

    [Fact]
    public void A_null_last_name_is_refused_as_a_null_argument() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();
        Nir      anyNir       = Any.Nir().BornOn(anyBirthDate).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Patient(anyNir, null!, "Marie", anyBirthDate));
    }

    [Fact]
    public void A_blank_last_name_is_refused() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();
        Nir      anyNir       = Any.Nir().BornOn(anyBirthDate).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyNir, "  ", "Marie", anyBirthDate));

        // Assert
        Assert.Equal("lastName", refusal.ParamName);
    }

    [Fact]
    public void A_null_first_name_is_refused_as_a_null_argument() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();
        Nir      anyNir       = Any.Nir().BornOn(anyBirthDate).Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Patient(anyNir, "Dupont", null!, anyBirthDate));
    }

    [Fact]
    public void A_blank_first_name_is_refused() {
        // Arrange
        DateOnly anyBirthDate = Any.DateOnly().Between(Patient.EarliestBirthDate, new DateOnly(2025, 12, 31)).Generate();
        Nir      anyNir       = Any.Nir().BornOn(anyBirthDate).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyNir, "Dupont", "\t", anyBirthDate));

        // Assert
        Assert.Equal("firstName", refusal.ParamName);
    }

    [Fact]
    public void The_earliest_birth_date_is_the_first_of_january_1900() {
        // Arrange
        DateOnly earliest = new DateOnly(1900, 1, 1);
        Nir      anyNir   = Any.Nir().BornOn(earliest).Generate();

        // Act
        Patient patient = new Patient(anyNir, "Dupont", "Marie", earliest);

        // Assert
        Assert.Equal(Patient.EarliestBirthDate, patient.BirthDate);
        Assert.Equal(new DateOnly(1900, 1, 1), Patient.EarliestBirthDate);
    }

    [Fact]
    public void A_birth_date_before_1900_is_out_of_range() {
        // Arrange
        DateOnly dayBefore = new DateOnly(1899, 12, 31);
        Nir      anyNir    = Any.Nir().BornOn(dayBefore).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Patient(anyNir, "Dupont", "Marie", dayBefore));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Fact]
    public void A_birth_month_that_differs_from_the_nirs_is_refused() {
        // Arrange: the NIR says May 1985.
        Nir anyNir = Any.Nir().BornOn(new DateOnly(1985, 5, 1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyNir, "Dupont", "Marie", new DateOnly(1985, 6, 17)));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Fact]
    public void A_birth_year_that_differs_from_the_nirs_is_refused() {
        // Arrange: the NIR says May 1985.
        Nir anyNir = Any.Nir().BornOn(new DateOnly(1985, 5, 1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyNir, "Dupont", "Marie", new DateOnly(1986, 5, 17)));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Theory]
    [InlineData(1925)]
    [InlineData(2025)]
    public void The_nir_carries_two_digits_of_the_year_so_either_century_matches(int year) {
        // Arrange
        DateOnly birthDate = new DateOnly(year, 5, 17);
        Nir      anyNir    = Any.Nir().BornOn(new DateOnly(2025, 5, 1)).Generate();

        // Act
        Patient patient = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Assert
        Assert.Equal(birthDate, patient.BirthDate);
    }

    [Theory]
    [InlineData("185057800608491")]   // a man
    [InlineData("285057800608441")]   // a woman
    public void A_patient_is_accepted_whatever_the_sex_the_nir_carries(string nirValue) {
        // Act
        Patient patient = new Patient(new Nir(nirValue), "Dupont", "Camille", new DateOnly(1985, 5, 17));

        // Assert
        Assert.Equal(nirValue, patient.Nir.Value);
    }

    [Theory]
    [InlineData("2000-06-15", "2020-06-14", 19)]
    [InlineData("2000-06-15", "2020-06-15", 20)]
    [InlineData("2000-06-15", "2020-06-16", 20)]
    [InlineData("2000-06-15", "2000-06-15", 0)]
    [InlineData("2000-06-15", "2001-06-14", 0)]
    [InlineData("2000-06-15", "2001-06-15", 1)]
    [InlineData("2000-12-31", "2026-01-01", 25)]
    public void Age_counts_the_birthdays_reached(string birth, string on, int expectedAge) {
        // Arrange
        DateOnly birthDate = DateOnly.Parse(birth, System.Globalization.CultureInfo.InvariantCulture);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();
        Patient  patient   = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Act
        int age = patient.AgeOn(DateOnly.Parse(on, System.Globalization.CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public void A_leap_day_baby_has_its_birthday_on_the_28th_of_february_in_a_common_year() {
        // Arrange
        DateOnly birthDate = new DateOnly(2004, 2, 29);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();
        Patient  patient   = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Act / Assert
        Assert.Equal(18, patient.AgeOn(new DateOnly(2023, 2, 27)));
        Assert.Equal(19, patient.AgeOn(new DateOnly(2023, 2, 28)));
    }

    [Fact]
    public void The_day_before_the_birth_date_gives_a_negative_age() {
        // Arrange: nothing in the model guards a date before the birth.
        DateOnly birthDate = new DateOnly(2000, 6, 15);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();
        Patient  patient   = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Act
        int age = patient.AgeOn(new DateOnly(2000, 6, 14));

        // Assert
        Assert.Equal(-1, age);
    }

    [Fact]
    public void A_patient_is_a_minor_until_the_eve_of_the_18th_birthday() {
        // Arrange
        DateOnly birthDate = new DateOnly(2008, 3, 10);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();
        Patient  patient   = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Act / Assert
        Assert.True(patient.IsMinorOn(new DateOnly(2026, 3, 9)));
        Assert.False(patient.IsMinorOn(new DateOnly(2026, 3, 10)));
    }

    [Fact]
    public void A_newborn_is_a_minor() {
        // Arrange
        DateOnly birthDate = new DateOnly(2024, 9, 1);
        Nir      anyNir    = Any.Nir().BornOn(birthDate).Generate();
        Patient  patient   = new Patient(anyNir, "Dupont", "Marie", birthDate);

        // Act / Assert
        Assert.True(patient.IsMinorOn(birthDate));
    }

}
