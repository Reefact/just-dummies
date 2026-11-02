using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Domain.Tests.Discovery;

public sealed class StudentTests {

    [Fact]
    public void A_student_keeps_its_number_and_its_birth_date() {
        // Arrange
        StudentNumber number    = new StudentNumber("STU-2025-00042");
        DateOnly      birthDate = new DateOnly(2004, 5, 17);

        // Act
        Student student = new Student(number, "Ada Lovelace", birthDate);

        // Assert
        Assert.Same(number, student.Number);
        Assert.Equal(birthDate, student.BirthDate);
        Assert.Equal("Ada Lovelace", student.FullName);
    }

    [Fact]
    public void The_stored_full_name_is_trimmed() {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        DateOnly      anyBirthDate = Any.DateOnly().Between(new DateOnly(1950, 1, 1), new DateOnly(2010, 12, 31)).Generate();

        // Act
        Student student = new Student(anyNumber, "  \tAda Lovelace \r\n", anyBirthDate);

        // Assert
        Assert.Equal("Ada Lovelace", student.FullName);
    }

    [Fact]
    public void A_null_number_is_refused_as_null() {
        string anyName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        Assert.Throws<ArgumentNullException>(() => new Student(null!, anyName, new DateOnly(2004, 5, 17)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_missing_or_blank_name_is_refused(string? fullName) {
        StudentNumber anyNumber = Any.StudentNumber().Generate();

        Assert.Throws<ArgumentException>(() => new Student(anyNumber, fullName!, new DateOnly(2004, 5, 17)));
    }

    [Fact]
    public void A_name_of_one_hundred_characters_is_accepted() {
        StudentNumber anyNumber = Any.StudentNumber().Generate();
        string        hundred   = new string('x', 100);

        Student student = new Student(anyNumber, hundred, new DateOnly(2004, 5, 17));

        Assert.Equal(hundred, student.FullName);
    }

    [Fact]
    public void A_name_of_one_hundred_and_one_characters_is_refused() {
        StudentNumber anyNumber = Any.StudentNumber().Generate();

        Assert.Throws<ArgumentException>(() => new Student(anyNumber, new string('x', 101), new DateOnly(2004, 5, 17)));
    }

    [Fact]
    public void The_length_limit_applies_to_the_raw_name_not_the_trimmed_one() {
        // 100 letters plus one padding space: the trimmed name would fit, the raw one does not.
        StudentNumber anyNumber = Any.StudentNumber().Generate();
        string        padded    = new string('x', 100) + " ";

        Assert.Throws<ArgumentException>(() => new Student(anyNumber, padded, new DateOnly(2004, 5, 17)));
    }

    [Fact]
    public void A_student_born_on_the_first_of_january_1900_is_accepted() {
        StudentNumber anyNumber = Any.StudentNumber().Generate();
        string        anyName   = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        Student student = new Student(anyNumber, anyName, new DateOnly(1900, 1, 1));

        Assert.Equal(new DateOnly(1900, 1, 1), student.BirthDate);
    }

    [Fact]
    public void A_student_born_the_day_before_1900_is_refused() {
        StudentNumber anyNumber = Any.StudentNumber().Generate();
        string        anyName   = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        Assert.Throws<ArgumentOutOfRangeException>(() => new Student(anyNumber, anyName, new DateOnly(1899, 12, 31)));
    }

    [Fact]
    public void A_drawn_student_is_always_one_the_constructor_accepts() {
        for (int i = 0; i < 300; i++) {
            Student anyStudent = Any.Student().Generate();

            Assert.NotEmpty(anyStudent.FullName);
        }
    }

}
