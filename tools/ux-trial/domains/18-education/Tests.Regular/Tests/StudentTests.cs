using Education.Domain;

using JustDummies;

using Xunit;

namespace Education.Tests.Regular;

public sealed class StudentTests {

    [Fact]
    public void A_valid_student_exposes_what_it_was_built_with() {
        // Arrange
        StudentNumber number = new StudentNumber("STU-2025-00042");

        // Act
        Student student = new Student(number, "Ada Lovelace", new DateOnly(2004, 12, 10));

        // Assert
        Assert.Same(number, student.Number);
        Assert.Equal("Ada Lovelace", student.FullName);
        Assert.Equal(new DateOnly(2004, 12, 10), student.BirthDate);
    }

    [Theory]
    [InlineData("  Ada Lovelace", "Ada Lovelace")]
    [InlineData("Ada Lovelace  ", "Ada Lovelace")]
    [InlineData("\tAda Lovelace\n", "Ada Lovelace")]
    [InlineData("Ada  Lovelace", "Ada  Lovelace")]   // inner spaces are kept
    public void The_full_name_is_stored_trimmed(string given, string expected) {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        Student student = new Student(anyNumber, given, anyBirthDate);

        // Assert
        Assert.Equal(expected, student.FullName);
    }

    [Fact]
    public void A_null_number_is_refused_with_a_null_argument_exception() {
        // Arrange
        string   anyFullName  = Any.String().Alpha().WithLengthBetween(1, 100).Generate();
        DateOnly anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Student(null!, anyFullName, anyBirthDate));

        // Assert
        Assert.Equal("number", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void A_null_or_blank_full_name_is_refused(string? fullName) {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Student(anyNumber, fullName!, anyBirthDate));

        // Assert
        Assert.Equal("fullName", exception.ParamName);
    }

    [Fact]
    public void A_full_name_of_one_hundred_characters_is_accepted() {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        string        anyFullName  = Any.String().Alpha().WithLength(100).Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        Student student = new Student(anyNumber, anyFullName, anyBirthDate);

        // Assert
        Assert.Equal(100, student.FullName.Length);
    }

    [Fact]
    public void A_full_name_of_a_hundred_and_one_characters_is_refused() {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        string        anyFullName  = Any.String().Alpha().WithLength(101).Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Student(anyNumber, anyFullName, anyBirthDate));

        // Assert
        Assert.Equal("fullName", exception.ParamName);
    }

    [Fact]
    public void The_length_limit_applies_to_the_raw_name_so_padding_counts() {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        string        anyLetters   = Any.String().Alpha().WithLength(99).Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act: 99 letters are within the limit once trimmed, but the raw text holds 101 characters.
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Student(anyNumber, " " + anyLetters + " ", anyBirthDate));

        // Assert
        Assert.Equal("fullName", exception.ParamName);
    }

    [Fact]
    public void A_padded_name_of_exactly_one_hundred_raw_characters_is_accepted_and_trimmed() {
        // Arrange
        StudentNumber anyNumber    = Any.StudentNumber().Generate();
        string        anyLetters   = Any.String().Alpha().WithLength(98).Generate();
        DateOnly      anyBirthDate = Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1)).Generate();

        // Act
        Student student = new Student(anyNumber, " " + anyLetters + " ", anyBirthDate);

        // Assert
        Assert.Equal(anyLetters, student.FullName);
    }

    [Fact]
    public void A_student_born_on_the_first_of_january_1900_is_accepted() {
        // Arrange
        StudentNumber anyNumber   = Any.StudentNumber().Generate();
        string        anyFullName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act
        Student student = new Student(anyNumber, anyFullName, new DateOnly(1900, 1, 1));

        // Assert
        Assert.Equal(new DateOnly(1900, 1, 1), student.BirthDate);
    }

    [Fact]
    public void A_student_born_before_1900_is_refused() {
        // Arrange
        StudentNumber anyNumber   = Any.StudentNumber().Generate();
        string        anyFullName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Student(anyNumber, anyFullName, new DateOnly(1899, 12, 31)));

        // Assert
        Assert.Equal("birthDate", exception.ParamName);
    }

    [Fact]
    public void A_student_born_long_before_1900_is_refused() {
        // Arrange
        StudentNumber anyNumber   = Any.StudentNumber().Generate();
        string        anyFullName = Any.String().Alpha().WithLengthBetween(1, 100).Generate();
        DateOnly      anyAncientDate = Any.DateOnly().Before(new DateOnly(1900, 1, 1)).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Student(anyNumber, anyFullName, anyAncientDate));

        // Assert
        Assert.Equal("birthDate", exception.ParamName);
    }

    [Fact]
    public void The_number_is_checked_before_the_name_and_the_name_before_the_birth_date() {
        // Assert: every argument is wrong, the first rule broken is the one reported.
        Assert.Throws<ArgumentNullException>(() => new Student(null!, "", new DateOnly(1800, 1, 1)));

        StudentNumber anyNumber = Any.StudentNumber().Generate();

        Assert.IsType<ArgumentException>(Record.Exception(() => new Student(anyNumber, "", new DateOnly(1800, 1, 1))));
    }

}
