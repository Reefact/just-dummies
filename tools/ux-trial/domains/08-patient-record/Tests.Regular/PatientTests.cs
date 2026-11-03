using JustDummies;

using Xunit;

namespace Clinical.Domain;

public sealed class PatientTests {

    [Fact]
    public void The_names_are_stored_as_given() {
        // Arrange
        Nir anyNir = Any.Nir().BornIn(1987, 3).Generate();

        // Act
        Patient patient = new Patient(anyNir, " de la Fontaine ", "Anne-Claire", new DateOnly(1987, 3, 14));

        // Assert
        Assert.Equal(" de la Fontaine ", patient.LastName);
        Assert.Equal("Anne-Claire", patient.FirstName);
        Assert.Equal(new DateOnly(1987, 3, 14), patient.BirthDate);
        Assert.Same(anyNir, patient.Nir);
    }

    [Fact]
    public void A_generated_patient_is_always_accepted() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();

        // Assert
        Assert.NotNull(anyPatient.Nir);
    }

    [Fact]
    public void A_null_NIR_is_a_null_argument_exception() {
        // Arrange
        string anyLastName  = Any.String().NotBlank().Generate();
        string anyFirstName = Any.String().NotBlank().Generate();

        // Act
        Action construct = () => new Patient(null!, anyLastName, anyFirstName, new DateOnly(1987, 3, 14));

        // Assert
        Assert.Throws<ArgumentNullException>(construct);
    }

    [Fact]
    public void A_null_last_name_is_a_null_argument_exception() {
        Nir anyNir = Any.Nir().BornIn(1987, 3).Generate();

        Assert.Throws<ArgumentNullException>(() => new Patient(anyNir, null!, "Anne", new DateOnly(1987, 3, 14)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_last_name_is_an_argument_exception(string blank) {
        Nir anyNir = Any.Nir().BornIn(1987, 3).Generate();

        Assert.Throws<ArgumentException>(() => new Patient(anyNir, blank, "Anne", new DateOnly(1987, 3, 14)));
    }

    [Fact]
    public void A_null_first_name_is_a_null_argument_exception() {
        Nir anyNir = Any.Nir().BornIn(1987, 3).Generate();

        Assert.Throws<ArgumentNullException>(() => new Patient(anyNir, "Martin", null!, new DateOnly(1987, 3, 14)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_first_name_is_an_argument_exception(string blank) {
        Nir anyNir = Any.Nir().BornIn(1987, 3).Generate();

        Assert.Throws<ArgumentException>(() => new Patient(anyNir, "Martin", blank, new DateOnly(1987, 3, 14)));
    }

    [Fact]
    public void A_birth_date_on_the_first_of_january_1900_is_accepted() {
        // Arrange
        Nir anyNir = Any.Nir().BornIn(1900, 1).Generate();

        // Act
        Patient patient = new Patient(anyNir, "Martin", "Anne", new DateOnly(1900, 1, 1));

        // Assert
        Assert.Equal(Patient.EarliestBirthDate, patient.BirthDate);
    }

    [Fact]
    public void A_birth_date_before_1900_is_out_of_range_whatever_the_NIR_says() {
        // Arrange: the NIR matches the year and month, so only the 1900 floor can refuse it
        Nir matchingNir = Any.Nir().BornIn(1899, 12).Generate();

        // Act
        Action construct = () => new Patient(matchingNir, "Martin", "Anne", new DateOnly(1899, 12, 31));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void The_floor_is_checked_before_the_NIR_match() {
        // Arrange: an arbitrary NIR, very unlikely to carry December 99
        Nir anyNir = Any.Nir().Generate();

        // Act
        Action construct = () => new Patient(anyNir, "Martin", "Anne", new DateOnly(1850, 6, 1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_birth_year_not_matching_the_NIR_is_an_argument_exception() {
        // Arrange
        Nir bornIn1987 = Any.Nir().BornIn(1987, 3).Generate();

        // Act
        Action construct = () => new Patient(bornIn1987, "Martin", "Anne", new DateOnly(1988, 3, 14));

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_birth_month_not_matching_the_NIR_is_an_argument_exception() {
        // Arrange
        Nir bornInMarch = Any.Nir().BornIn(1987, 3).Generate();

        // Act
        Action construct = () => new Patient(bornInMarch, "Martin", "Anne", new DateOnly(1987, 4, 14));

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Theory]
    [InlineData(1905)]
    [InlineData(2005)]
    public void Only_the_last_two_digits_of_the_year_are_compared_with_the_NIR(int birthYear) {
        // Arrange
        Nir carrying05 = Any.Nir().BornIn(2005, 7).Generate();

        // Act
        Patient patient = new Patient(carrying05, "Martin", "Anne", new DateOnly(birthYear, 7, 20));

        // Assert
        Assert.Equal(birthYear, patient.BirthDate.Year);
    }

    [Fact]
    public void The_day_of_birth_is_not_compared_with_anything() {
        // Arrange
        Nir anyNirBornInMarch87 = Any.Nir().BornIn(1987, 3).Generate();
        int anyDay              = Any.Int32().Between(1, 31).Generate();

        // Act
        Patient patient = new Patient(anyNirBornInMarch87, "Martin", "Anne", new DateOnly(1987, 3, anyDay));

        // Assert
        Assert.Equal(1987, patient.BirthDate.Year);
    }

    [Fact]
    public void A_patient_is_not_yet_a_year_old_the_day_before_the_first_birthday() {
        // Arrange
        Patient bornOn14March2020 = Any.Patient().WithBirthDate(new DateOnly(2020, 3, 14)).Generate();

        // Assert
        Assert.Equal(0, bornOn14March2020.AgeOn(new DateOnly(2021, 3, 13)));
        Assert.Equal(1, bornOn14March2020.AgeOn(new DateOnly(2021, 3, 14)));
    }

    [Fact]
    public void The_age_on_the_birth_date_is_zero() {
        // Arrange
        Patient bornOn14March2020 = Any.Patient().WithBirthDate(new DateOnly(2020, 3, 14)).Generate();

        // Assert
        Assert.Equal(0, bornOn14March2020.AgeOn(new DateOnly(2020, 3, 14)));
    }

    [Fact]
    public void The_age_counts_whole_years_between_birthdays() {
        // Arrange
        Patient bornIn1950 = Any.Patient().WithBirthDate(new DateOnly(1950, 11, 30)).Generate();

        // Assert
        Assert.Equal(74, bornIn1950.AgeOn(new DateOnly(2025, 11, 29)));
        Assert.Equal(75, bornIn1950.AgeOn(new DateOnly(2025, 11, 30)));
        Assert.Equal(75, bornIn1950.AgeOn(new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void A_date_before_the_birth_date_gives_a_negative_age() {
        // Arrange
        Patient bornOn17May2000 = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();

        // Act
        int age = bornOn17May2000.AgeOn(new DateOnly(1990, 1, 1));

        // Assert
        Assert.Equal(-11, age);
    }

    [Fact]
    public void A_patient_is_a_minor_until_the_day_before_the_eighteenth_birthday() {
        // Arrange
        Patient bornOn17May2000 = Any.Patient().WithBirthDate(new DateOnly(2000, 5, 17)).Generate();

        // Assert
        Assert.True(bornOn17May2000.IsMinorOn(new DateOnly(2018, 5, 16)));
        Assert.False(bornOn17May2000.IsMinorOn(new DateOnly(2018, 5, 17)));
    }

    [Fact]
    public void A_newborn_is_a_minor() {
        // Arrange
        Patient anyPatient = Any.Patient().Generate();

        // Assert
        Assert.True(anyPatient.IsMinorOn(anyPatient.BirthDate));
    }

    [Fact]
    public void A_patient_born_on_a_leap_day_turns_eighteen_on_the_28th_of_february_of_a_common_year() {
        // Arrange
        Patient bornOnLeapDay = Any.Patient().WithBirthDate(new DateOnly(2004, 2, 29)).Generate();

        // Assert
        Assert.True(bornOnLeapDay.IsMinorOn(new DateOnly(2022, 2, 27)));
        Assert.False(bornOnLeapDay.IsMinorOn(new DateOnly(2022, 2, 28)));
    }

}
