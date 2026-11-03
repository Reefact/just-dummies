using NetworkAcl.Domain;

namespace NetworkAcl.Tests.Discovery;

public sealed class PortRangeTests {

    [Fact]
    public void A_reversed_range_is_refused() {
        Assert.Throws<ArgumentException>(() => new PortRange(443, 80));
        Assert.Throws<ArgumentException>(() => new PortRange(1, 0));
    }

    [Fact]
    public void A_range_whose_ends_are_equal_is_a_single_port() {
        PortRange range = new PortRange(443, 443);

        Assert.Equal(1, range.Count);
        Assert.True(range.Contains((ushort)443));
        Assert.False(range.Contains((ushort)442));
        Assert.False(range.Contains((ushort)444));
    }

    [Fact]
    public void Single_is_the_same_range_as_equal_ends() {
        Assert.Equal(new PortRange(53, 53), PortRange.Single(53));
    }

    [Fact]
    public void All_runs_from_zero_to_65535() {
        Assert.Equal(0, PortRange.All.Low);
        Assert.Equal(65535, PortRange.All.High);
        Assert.Equal(65536, PortRange.All.Count);
        Assert.True(PortRange.All.IsAll);
    }

    [Theory]
    [InlineData(0, 65535, true)]
    [InlineData(1, 65535, false)]
    [InlineData(0, 65534, false)]
    [InlineData(80, 80, false)]
    public void IsAll_is_true_only_for_the_whole_range(ushort low, ushort high, bool expected) {
        Assert.Equal(expected, new PortRange(low, high).IsAll);
    }

    [Theory]
    [InlineData(80, 89, 10)]
    [InlineData(0, 0, 1)]
    [InlineData(65535, 65535, 1)]
    [InlineData(1024, 65535, 64512)]
    public void Count_is_the_number_of_ports_both_ends_included(ushort low, ushort high, int expected) {
        Assert.Equal(expected, new PortRange(low, high).Count);
    }

    [Theory]
    [InlineData(1024, true)]
    [InlineData(65535, true)]
    [InlineData(40000, true)]
    [InlineData(1023, false)]
    [InlineData(0, false)]
    public void Contains_a_port_includes_both_ends(ushort port, bool expected) {
        Assert.Equal(expected, new PortRange(1024, 65535).Contains(port));
    }

    [Theory]
    [InlineData(1000, 2000, 1000, 2000, true)]
    [InlineData(1000, 2000, 1500, 1600, true)]
    [InlineData(1000, 2000, 999, 1500, false)]
    [InlineData(1000, 2000, 1500, 2001, false)]
    [InlineData(1000, 2000, 0, 65535, false)]
    [InlineData(0, 65535, 1000, 2000, true)]
    public void Contains_a_range_means_every_port_of_it_is_inside(ushort low, ushort high, ushort otherLow, ushort otherHigh, bool expected) {
        Assert.Equal(expected, new PortRange(low, high).Contains(new PortRange(otherLow, otherHigh)));
    }

    [Theory]
    [InlineData(1000, 2000, 2000, 3000, true)]
    [InlineData(1000, 2000, 2001, 3000, false)]
    [InlineData(1000, 2000, 0, 1000, true)]
    [InlineData(1000, 2000, 0, 999, false)]
    [InlineData(1000, 2000, 1200, 1300, true)]
    [InlineData(1200, 1300, 1000, 2000, true)]
    [InlineData(80, 80, 80, 80, true)]
    [InlineData(80, 80, 81, 81, false)]
    public void Overlaps_means_a_port_is_in_both(ushort low, ushort high, ushort otherLow, ushort otherHigh, bool expected) {
        Assert.Equal(expected, new PortRange(low, high).Overlaps(new PortRange(otherLow, otherHigh)));
    }

    [Fact]
    public void Range_operations_refuse_null() {
        Assert.Throws<ArgumentNullException>(() => PortRange.All.Contains((PortRange)null!));
        Assert.Throws<ArgumentNullException>(() => PortRange.All.Overlaps(null!));
    }

    [Theory]
    [InlineData(0, 65535, "any")]
    [InlineData(80, 80, "80")]
    [InlineData(1024, 65535, "1024-65535")]
    public void ToString_is_any_a_port_or_a_range(ushort low, ushort high, string expected) {
        Assert.Equal(expected, new PortRange(low, high).ToString());
    }

    [Fact]
    public void Ranges_are_equal_when_both_ends_are() {
        Assert.Equal(new PortRange(10, 20), new PortRange(10, 20));
        Assert.Equal(new PortRange(10, 20).GetHashCode(), new PortRange(10, 20).GetHashCode());
        Assert.NotEqual(new PortRange(10, 20), new PortRange(10, 21));
        Assert.NotEqual(new PortRange(10, 20), new PortRange(11, 20));
    }


    [Fact]
    public void A_range_with_a_pinned_low_end_keeps_it() {
        PortRange pinned = Any.PortRange().WithLow(8080).Generate();

        Assert.Equal(8080, pinned.Low);
        Assert.True(pinned.High >= 8080);
    }

}
