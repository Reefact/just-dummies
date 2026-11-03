using JustDummies;

using NetworkAcl.Domain;
using NetworkAcl.Tests.Expert.Arbitrary;

using Xunit;

using static NetworkAcl.Tests.Expert.Shorthand;

namespace NetworkAcl.Tests.Expert.Tests;

public sealed class PortRangeTests {

    [Fact]
    public void A_range_whose_low_port_exceeds_its_high_port_is_refused() {
        // Arrange
        // Which ports they are does not matter, only that they are reversed.
        (ushort anyLowerPort, ushort anyHigherPort) = Any.OrderedPair(Any.UInt16(), Any.UInt16(), strict: true).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PortRange(anyHigherPort, anyLowerPort));

        // Assert
        Assert.Equal("low", refusal.ParamName);
    }

    [Fact]
    public void A_range_with_the_same_low_and_high_port_is_a_single_port() {
        // Arrange
        ushort anyPort = Any.UInt16().Generate();

        // Act
        PortRange range = new PortRange(anyPort, anyPort);

        // Assert
        Assert.Equal(1, range.Count);
    }

    [Fact]
    public void A_range_exposes_the_ports_it_was_given() {
        // Act
        PortRange range = new PortRange(1000, 1999);

        // Assert
        Assert.Equal((ushort)1000, range.Low);
        Assert.Equal((ushort)1999, range.High);
        Assert.Equal(1000, range.Count);
    }

    [Fact]
    public void Single_is_the_range_of_one_port() {
        Assert.Equal(new PortRange(443, 443), PortRange.Single(443));
    }

    [Fact]
    public void All_spans_every_port_and_counts_them_without_overflowing_a_ushort() {
        Assert.Equal((ushort)0, PortRange.All.Low);
        Assert.Equal(ushort.MaxValue, PortRange.All.High);
        Assert.Equal(65_536, PortRange.All.Count);
        Assert.True(PortRange.All.IsAll);
    }

    [Theory]
    [InlineData(0, 65535, true)]
    [InlineData(1, 65535, false)]
    [InlineData(0, 65534, false)]
    [InlineData(80, 80, false)]
    public void IsAll_is_true_only_for_the_whole_port_space(ushort low, ushort high, bool expected) {
        Assert.Equal(expected, new PortRange(low, high).IsAll);
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(1500, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void Contains_a_port_is_inclusive_at_both_ends(ushort port, bool expected) {
        Assert.Equal(expected, new PortRange(1000, 2000).Contains(port));
    }

    [Fact]
    public void The_whole_port_space_contains_the_extreme_ports() {
        Assert.True(PortRange.All.Contains(0));
        Assert.True(PortRange.All.Contains(ushort.MaxValue));
    }

    [Theory]
    [InlineData(1000, 2000, true)]    // itself
    [InlineData(1200, 1800, true)]    // inside
    [InlineData(1000, 1500, true)]    // sharing the low end
    [InlineData(1500, 2000, true)]    // sharing the high end
    [InlineData(999, 1500, false)]    // sticks out below
    [InlineData(1500, 2001, false)]   // sticks out above
    [InlineData(500, 3000, false)]    // wider
    [InlineData(3000, 4000, false)]   // disjoint
    public void Contains_a_range_only_when_that_range_is_inside_it(ushort low, ushort high, bool expected) {
        Assert.Equal(expected, new PortRange(1000, 2000).Contains(new PortRange(low, high)));
    }

    [Theory]
    [InlineData(2000, 3000, true)]     // sharing exactly one port
    [InlineData(500, 1000, true)]      // idem, from below
    [InlineData(1200, 1800, true)]     // nested
    [InlineData(500, 3000, true)]      // nesting the other way
    [InlineData(2001, 3000, false)]    // adjacent above
    [InlineData(500, 999, false)]      // adjacent below
    [InlineData(5000, 6000, false)]
    public void Overlaps_when_the_ranges_share_at_least_one_port(ushort low, ushort high, bool expected) {
        // Arrange
        PortRange reference = new PortRange(1000, 2000);
        PortRange other     = new PortRange(low, high);

        // Act & Assert: overlapping does not depend on which range is asked
        Assert.Equal(expected, reference.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(reference));
    }

    [Fact]
    public void Contains_and_Overlaps_refuse_a_missing_range() {
        Assert.Throws<ArgumentNullException>(() => PortRange.All.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => PortRange.All.Overlaps(null!));
    }

    [Theory]
    [InlineData(0, 65535, "any")]
    [InlineData(80, 80, "80")]
    [InlineData(1000, 2000, "1000-2000")]
    [InlineData(0, 65534, "0-65534")]
    public void ToString_writes_any_a_single_port_or_low_dash_high(ushort low, ushort high, string expectedText) {
        Assert.Equal(expectedText, new PortRange(low, high).ToString());
    }

    [Fact]
    public void Ranges_with_the_same_bounds_are_equal_and_hash_alike() {
        // Arrange
        (ushort anyLow, ushort anyHigh) = Any.OrderedPair(Any.UInt16(), Any.UInt16()).Generate();
        PortRange first  = new PortRange(anyLow, anyHigh);
        PortRange second = new PortRange(anyLow, anyHigh);

        // Act & Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Ranges_with_different_bounds_are_not_equal() {
        Assert.NotEqual(new PortRange(80, 90), new PortRange(80, 91));
        Assert.NotEqual(new PortRange(80, 90), new PortRange(81, 90));
        Assert.False(new PortRange(80, 90).Equals(null));
    }

}
