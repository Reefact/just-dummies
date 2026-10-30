using Granularity.Domain;
using JustDummies;
using Xunit;

namespace Granularity.Tests;

public sealed class GranularityTests {
    [Fact]
    public void Day_granularity_with_plus_two_hours_lands_on_local_midnight() {
        DateTimeOffset value = Any.DateTimeOffset()
                                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero))
                                  .WithGranularity(TimeSpan.FromDays(1))
                                  .WithOffset(TimeSpan.FromHours(2))
                                  .Generate();
        TestContext.Current.SendDiagnosticMessage("day/+02:00 -> " + value.ToString("O"));
        Assert.Equal(TimeSpan.Zero, value.TimeOfDay);
    }

    [Fact]
    public void Hour_granularity_with_plus_five_thirty_lands_on_a_local_whole_hour() {
        DateTimeOffset value = Any.DateTimeOffset()
                                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero))
                                  .WithOffset(new TimeSpan(5, 30, 0))
                                  .WithGranularity(TimeSpan.FromHours(1))
                                  .Generate();
        TestContext.Current.SendDiagnosticMessage("hour/+05:30 -> " + value.ToString("O"));
        Assert.Equal(0, value.Minute);
    }

    [Fact]
    public void Day_granularity_with_offset_range_constructs_the_domain_type() {
        DateTimeOffset value = Any.DateTimeOffset()
                                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero))
                                  .WithGranularity(TimeSpan.FromDays(1))
                                  .WithOffsetBetween(TimeSpan.FromHours(1), TimeSpan.FromHours(3))
                                  .Generate();
        TestContext.Current.SendDiagnosticMessage("day/[+01,+03] -> " + value.ToString("O"));
        Slot slot = new Slot(value);
        Assert.Equal(value, slot.Start);
    }
}
