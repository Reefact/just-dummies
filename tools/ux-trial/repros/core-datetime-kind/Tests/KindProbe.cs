using Xunit;
using JustDummies;

public sealed class KindProbe {
    [Fact]
    public void Between_local_bounds_yields_utc_value_with_same_ticks() {
        DateTime start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        DateTime end = new DateTime(2025, 1, 1, 0, 0, 1, DateTimeKind.Local);
        DateTime value = Any.DateTime().Between(start, end).Generate();
        Console.WriteLine($"TZ={TimeZoneInfo.Local.Id} value={value:O} kind={value.Kind}");
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.True(start <= value && value <= end);                         // ticks: holds
        bool instantInside = start.ToUniversalTime() <= value && value <= end.ToUniversalTime();
        Console.WriteLine($"instantInside={instantInside} startUtc={start.ToUniversalTime():O}");
        Assert.False(instantInside);                                          // instants: outside under Paris
    }

    [Fact]
    public void OneOf_keeps_local_kind() {
        DateTime local = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);
        Assert.Equal(DateTimeKind.Local, Any.DateTime().OneOf(local).Generate().Kind);
    }

    [Fact]
    public void Unconstrained_is_utc() {
        Assert.Equal(DateTimeKind.Utc, Any.DateTime().Generate().Kind);
    }
}
