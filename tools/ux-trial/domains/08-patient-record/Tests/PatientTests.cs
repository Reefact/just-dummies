using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

[Reproducible]
public sealed class PatientTests {

    [Fact]
    public void A_patient_is_17_on_the_day_before_their_18th_birthday() {
        // Arrange
        DateOnly birthDate          = new DateOnly(2008, 5, 20);
        DateOnly dayBefore18thBirth = new DateOnly(2026, 5, 19);
        Patient  anyPatient         = Any.Patient().WithBirthDate(birthDate).Generate();

        // Act
        int age = anyPatient.AgeOn(dayBefore18thBirth);

        // Assert
        Assert.Equal(17, age);
    }

    [Fact]
    public void A_patient_is_no_longer_a_minor_on_their_18th_birthday() {
        // Arrange
        Patient anyPatient = Any.Patient().WithBirthDate(new DateOnly(2008, 5, 20)).Generate();

        // Act
        bool isMinor = anyPatient.IsMinorOn(new DateOnly(2026, 5, 20));

        // Assert
        Assert.False(isMinor);
    }

    [Fact]
    public void A_birth_date_in_another_month_than_the_NIR_says_is_refused() {
        // Arrange
        Nir    anyNirBornInMarch1985 = Any.Nir().BornIn(1985, 3).Generate();
        string anyLastName           = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        string anyFirstName          = Any.String().Alpha().WithLengthBetween(1, 40).Generate();
        DateOnly anyDayInApril1985   = Any.DateOnly().Between(new DateOnly(1985, 4, 1), new DateOnly(1985, 4, 30)).Generate();

        // Act
        Action creating = () => new Patient(anyNirBornInMarch1985, anyLastName, anyFirstName, anyDayInApril1985);

        // Assert
        Assert.Throws<ArgumentException>("birthDate", creating);
    }

}
