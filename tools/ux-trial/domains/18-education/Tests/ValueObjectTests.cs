using Education.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Education.Tests;

[Reproducible]
public sealed class ValueObjectTests {

    [Theory]
    [InlineData(12.25)]
    [InlineData(19.9)]
    public void A_grade_off_the_half_point_grid_is_refused(double value) {
        // Act
        Action grading = () => new Grade((decimal)value);

        // Assert
        Assert.Throws<ArgumentException>(grading);
    }

    [Fact]
    public void A_grade_above_twenty_is_refused() {
        // Arrange
        decimal anyAboveTwenty = Any.Decimal().GreaterThan(Grade.Maximum).WithScale(1).Generate();

        // Act
        Action grading = () => new Grade(anyAboveTwenty);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(grading);
    }

    [Fact]
    public void A_student_number_carries_its_intake_year() {
        // Arrange
        StudentNumber anyNumberOf2023 = new AnyStudentNumber().WithValue(pattern => Any.StringMatching(@"STU-2023-\d{5}")).Generate();

        // Act
        int intakeYear = anyNumberOf2023.IntakeYear;

        // Assert
        Assert.Equal(2023, intakeYear);
    }

    [Fact]
    public void A_semester_contains_its_last_day() {
        // Arrange
        Semester anySemester = Arbitrary.Semester().Generate();

        // Act
        bool contains = anySemester.Contains(anySemester.End);

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void A_student_full_name_is_trimmed() {
        // Arrange
        Student anyStudent = new AnyStudent().WithFullName("  Ada Lovelace ").Generate();

        // Act
        string fullName = anyStudent.FullName;

        // Assert
        Assert.Equal("Ada Lovelace", fullName);
    }
}
