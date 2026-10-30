using JustDummies;
using JustDummies.Xunit;
using Serialisation.Domain;
using Xunit;

namespace Serialisation.Tests;

[Reproducible]
public sealed class ShelfLifeAndRecallTests {

    [Fact]
    public void Expiry_falls_on_the_last_day_of_the_month_the_shelf_life_ends_in() {
        // Arrange
        ShelfLife twoYears = new ShelfLife(24);

        // Act
        DateOnly expiry = twoYears.ExpiryFor(new DateOnly(2025, 2, 10));

        // Assert
        Assert.Equal(new DateOnly(2027, 2, 28), expiry);
    }

    [Fact]
    public void Expiry_is_always_a_month_end_whatever_the_manufacture_day() {
        // Arrange: the manufacture day within the month is what the test does not care about.
        ShelfLife anyShelfLife      = Arbitrary.ShelfLife().Generate();
        DateOnly  anyDayOfMarch2025 = Any.DateOnly().Between(new DateOnly(2025, 3, 1), new DateOnly(2025, 3, 31)).Generate();

        // Act
        DateOnly expiry = anyShelfLife.ExpiryFor(anyDayOfMarch2025);

        // Assert
        Assert.Equal(1, expiry.AddDays(1).Day);
    }

    [Fact]
    public void Only_a_class_I_recall_reaches_patients() {
        // Arrange
        RecallNotice anyClassIRecall     = Arbitrary.RecallNotice().WithRecallClass(RecallClass.ClassI).Generate();
        RecallNotice anyLowerClassRecall = Arbitrary.RecallNotice().WithRecallClass(c => c.DifferentFrom(RecallClass.ClassI)).Generate();

        // Assert
        Assert.True(anyClassIRecall.ReachesPatients);
        Assert.False(anyLowerClassRecall.ReachesPatients);
    }

    [Fact]
    public void The_last_day_of_a_february_expiry_follows_the_leap_year() {
        // Act
        DateOnly lastDay = ExpiryMonth.Of(2028, 2).LastDay;

        // Assert
        Assert.Equal(new DateOnly(2028, 2, 29), lastDay);
    }

}
