using Clinical.Domain;
using Clinical.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Clinical.Tests.Expert;

public sealed class PatientTests {

    [Fact]
    public void A_patient_keeps_the_names_exactly_as_they_were_given() {
        // Arrange
        Nir      anyNir       = Any.Nir().BornIn(90, 7).Generate();
        DateOnly birthDate    = Dates.On("1990-07-21");
        string   lastName     = "  de la Fontaine ";
        string   firstName    = "\tJean-Pierre";

        // Act
        Patient patient = new Patient(anyNir, lastName, firstName, birthDate);

        // Assert
        Assert.Same(anyNir, patient.Nir);
        Assert.Equal("  de la Fontaine ", patient.LastName);
        Assert.Equal("\tJean-Pierre", patient.FirstName);
        Assert.Equal(birthDate, patient.BirthDate);
    }

    [Fact]
    public void A_patient_may_be_born_on_the_earliest_birth_date() {
        // Arrange
        Nir anyNir = Any.Nir().BornIn(0, 1).Generate();

        // Act
        Patient patient = Any.Patient().WithNir(anyNir).WithBirthDate(Patient.EarliestBirthDate).Generate();

        // Assert
        Assert.Equal(new DateOnly(1900, 1, 1), patient.BirthDate);
    }

    [Fact]
    public void A_patient_born_before_the_earliest_birth_date_is_refused() {
        // Arrange
        Nir      anyNir    = Any.Nir().BornIn(99, 12).Generate();
        DateOnly birthDate = Dates.On("1899-12-31");

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => AnyPatientWith(anyNir, birthDate));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Fact]
    public void The_earliest_birth_date_is_the_first_of_january_1900() {
        // Assert
        Assert.Equal(Dates.On("1900-01-01"), Patient.EarliestBirthDate);
    }

    [Fact]
    public void A_missing_nir_is_refused_as_a_missing_argument() {
        // Arrange
        string   anyLastName  = Any.String().NotBlank().WithMaxLength(60).Generate();
        string   anyFirstName = Any.String().NotBlank().WithMaxLength(60).Generate();
        DateOnly anyBirthDate = Any.DateOnly().Between(Dates.On("1950-01-01"), Dates.On("2020-12-31")).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Patient(null!, anyLastName, anyFirstName, anyBirthDate));

        // Assert
        Assert.Equal("nir", refusal.ParamName);
    }

    [Fact]
    public void A_missing_last_name_is_refused_as_a_missing_argument() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Patient(anyPatient.Nir, null!, anyPatient.FirstName, anyPatient.BirthDate));

        // Assert
        Assert.Equal("lastName", refusal.ParamName);
    }

    [Fact]
    public void A_blank_last_name_is_refused_as_an_invalid_argument() {
        // Arrange
        Patient anyPatient   = Any.Patient().Generate();
        string  anyBlankName = Any.String().Whitespaces().WithMaxLength(10).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyPatient.Nir, anyBlankName, anyPatient.FirstName, anyPatient.BirthDate));

        // Assert
        Assert.Equal("lastName", refusal.ParamName);
    }

    [Fact]
    public void A_missing_first_name_is_refused_as_a_missing_argument() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Patient(anyPatient.Nir, anyPatient.LastName, null!, anyPatient.BirthDate));

        // Assert
        Assert.Equal("firstName", refusal.ParamName);
    }

    [Fact]
    public void A_blank_first_name_is_refused_as_an_invalid_argument() {
        // Arrange
        Patient anyPatient   = Any.Patient().Generate();
        string  anyBlankName = Any.String().Whitespaces().WithMaxLength(10).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Patient(anyPatient.Nir, anyPatient.LastName, anyBlankName, anyPatient.BirthDate));

        // Assert
        Assert.Equal("firstName", refusal.ParamName);
    }

    [Fact]
    public void A_birth_date_whose_year_differs_from_the_one_in_the_nir_is_refused() {
        // Arrange
        Nir      anyNirBornIn1985 = Any.Nir().BornIn(85, 5).Generate();
        DateOnly bornIn1986       = Dates.On("1986-05-12");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => AnyPatientWith(anyNirBornIn1985, bornIn1986));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Fact]
    public void A_birth_date_whose_month_differs_from_the_one_in_the_nir_is_refused() {
        // Arrange
        Nir      anyNirBornInMay = Any.Nir().BornIn(85, 5).Generate();
        DateOnly bornInJune      = Dates.On("1985-06-12");

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => AnyPatientWith(anyNirBornInMay, bornInJune));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
    }

    [Theory]
    [InlineData("1925-05-12")]
    [InlineData("2025-05-12")]
    public void Only_the_last_two_digits_of_the_year_are_compared_so_the_century_is_not(string birthDate) {
        // Arrange
        Nir anyNirBornIn25OfMay = Any.Nir().BornIn(25, 5).Generate();

        // Act
        Patient patient = AnyPatientWith(anyNirBornIn25OfMay, Dates.On(birthDate));

        // Assert
        Assert.Equal(Dates.On(birthDate), patient.BirthDate);
    }

    [Fact]
    public void The_day_of_birth_is_not_compared_with_the_nir() {
        // Arrange
        Nir anyNirBornInMay = Any.Nir().BornIn(85, 5).Generate();

        // Act
        Patient firstOfMonth = AnyPatientWith(anyNirBornInMay, Dates.On("1985-05-01"));
        Patient lastOfMonth  = AnyPatientWith(anyNirBornInMay, Dates.On("1985-05-31"));

        // Assert
        Assert.Equal(1, firstOfMonth.BirthDate.Day);
        Assert.Equal(31, lastOfMonth.BirthDate.Day);
    }

    [Fact]
    public void The_sex_of_the_nir_is_not_compared_with_anything() {
        // Arrange
        Nir anyFemaleNir = Any.Nir().OfSex(Sex.Female).Generate();

        // Act
        Patient patient = Any.Patient().WithNir(anyFemaleNir).Generate();

        // Assert
        Assert.Equal(Sex.Female, patient.Nir.Sex);
    }

    [Theory]
    [InlineData("2020-06-14", 19)]    // the day before the 20th birthday
    [InlineData("2020-06-15", 20)]    // the birthday itself
    [InlineData("2020-06-16", 20)]    // the day after
    [InlineData("2000-06-15", 0)]     // the day of birth
    [InlineData("2001-06-14", 0)]     // the eve of the first birthday
    [InlineData("2001-06-15", 1)]
    public void The_age_goes_up_on_the_birthday_not_before(string date, int expectedAge) {
        // Arrange
        Patient bornOn15June2000 = Any.Patient().WithBirthDate(Dates.On("2000-06-15")).Generate();

        // Act
        int age = bornOn15June2000.AgeOn(Dates.On(date));

        // Assert
        Assert.Equal(expectedAge, age);
    }

    [Theory]
    [InlineData("2021-02-27", 20)]
    [InlineData("2021-02-28", 21)]    // AddYears clamps February 29th onto the 28th in a common year
    [InlineData("2024-02-28", 23)]
    [InlineData("2024-02-29", 24)]
    public void A_patient_born_on_a_leap_day_has_the_birthday_clamped_to_the_28th_in_a_common_year(string date, int expectedAge) {
        // Arrange
        Patient bornOnLeapDay = Any.Patient().WithBirthDate(Dates.On("2000-02-29")).Generate();

        // Act
        int age = bornOnLeapDay.AgeOn(Dates.On(date));

        // Assert
        Assert.Equal(expectedAge, age);
    }

    [Fact]
    public void A_date_before_the_birth_gives_a_negative_age_instead_of_a_refusal() {
        // Arrange
        Patient bornOn15June2000 = Any.Patient().WithBirthDate(Dates.On("2000-06-15")).Generate();

        // Act
        int ageTheYearBefore = bornOn15June2000.AgeOn(Dates.On("1999-06-16"));
        int ageTwoYearsEarlier = bornOn15June2000.AgeOn(Dates.On("1998-06-15"));

        // Assert
        Assert.Equal(-1, ageTheYearBefore);
        Assert.Equal(-2, ageTwoYearsEarlier);
    }

    [Theory]
    [InlineData("2018-03-14", true)]     // the eve of the 18th birthday
    [InlineData("2018-03-15", false)]    // the 18th birthday
    [InlineData("2000-03-15", true)]     // the day of birth
    [InlineData("2030-01-01", false)]
    public void A_patient_is_a_minor_until_the_day_they_turn_eighteen(string date, bool expectedMinor) {
        // Arrange
        Patient bornOn15March2000 = Any.Patient().WithBirthDate(Dates.On("2000-03-15")).Generate();

        // Act
        bool minor = bornOn15March2000.IsMinorOn(Dates.On(date));

        // Assert
        Assert.Equal(expectedMinor, minor);
    }

    private static Patient AnyPatientWith(Nir nir, DateOnly birthDate) {
        string anyLastName  = Any.String().NotBlank().WithMaxLength(60).Generate();
        string anyFirstName = Any.String().NotBlank().WithMaxLength(60).Generate();

        return new Patient(nir, anyLastName, anyFirstName, birthDate);
    }

}
