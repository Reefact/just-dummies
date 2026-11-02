using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Expert;

public sealed class StudentTests {

    [Fact]
    public void A_student_keeps_its_number_name_and_birth_date() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");

        // Act
        Student student = new Student(number, "Ada Lovelace", new DateOnly(2004, 12, 10));

        // Assert
        Assert.Same(number, student.Number);
        Assert.Equal("Ada Lovelace", student.FullName);
        Assert.Equal(new DateOnly(2004, 12, 10), student.BirthDate);
    }

    [Fact]
    public void A_student_stores_its_full_name_trimmed() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act
        Student student = new Student(anyStudent.Number, "  Ada Lovelace \t", anyStudent.BirthDate);

        // Assert
        Assert.Equal("Ada Lovelace", student.FullName);
    }

    [Fact]
    public void A_missing_number_is_refused_as_a_missing_argument() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Student(null!, anyStudent.FullName, anyStudent.BirthDate));

        // Assert
        Assert.Equal("number", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" \t\r\n")]
    public void A_missing_or_blank_full_name_is_refused(string? blank) {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act (the exact type: a missing name is refused like a blank one)
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Student(anyStudent.Number, blank!, anyStudent.BirthDate));

        // Assert
        Assert.Equal("fullName", refusal.ParamName);
    }

    [Fact]
    public void A_full_name_of_100_characters_is_accepted() {
        // Arrange
        Student anyStudent = Any.Student().Generate();
        string  name       = new string('a', 100);

        // Act
        Student student = new Student(anyStudent.Number, name, anyStudent.BirthDate);

        // Assert
        Assert.Equal(name, student.FullName);
    }

    [Fact]
    public void A_full_name_of_101_characters_is_refused() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Student(anyStudent.Number, new string('a', 101), anyStudent.BirthDate));

        // Assert
        Assert.Equal("fullName", refusal.ParamName);
    }

    [Fact]
    public void The_length_limit_of_the_full_name_applies_before_it_is_trimmed() {
        // Arrange
        Student anyStudent = Any.Student().Generate();
        string  paddedName = new string('a', 98) + "   ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Student(anyStudent.Number, paddedName, anyStudent.BirthDate));
    }

    [Fact]
    public void A_student_born_on_the_first_of_january_1900_is_accepted() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act
        Student student = new Student(anyStudent.Number, anyStudent.FullName, new DateOnly(1900, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(1900, 1, 1), student.BirthDate);
    }

    [Fact]
    public void A_student_born_before_1900_is_refused_as_out_of_range() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new Student(anyStudent.Number, anyStudent.FullName, new DateOnly(1899, 12, 31)));

        // Assert
        Assert.Equal("birthDate", refusal.ParamName);
        Assert.Equal(new DateOnly(1899, 12, 31), refusal.ActualValue);
    }

    [Fact]
    public void A_missing_number_is_reported_before_an_invalid_name_and_birth_date() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Student(null!, " ", new DateOnly(1800, 1, 1)));
    }

    [Fact]
    public void An_invalid_name_is_reported_before_an_invalid_birth_date() {
        // Arrange
        Student anyStudent = Any.Student().Generate();

        // Act & Assert (the exact type: the birth date would be an out-of-range refusal)
        Assert.Throws<ArgumentException>(() => new Student(anyStudent.Number, " ", new DateOnly(1800, 1, 1)));
    }

}
