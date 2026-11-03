using JustDummies;

using NetworkAcl.Domain;

using Xunit;

namespace NetworkAcl.Tests.Regular;

public sealed class PortRangeTests {

    [Fact]
    public void A_reversed_range_is_refused() {
        // Arrange
        ushort anyHigh = Any.UInt16().LessThan(ushort.MaxValue).Generate();
        ushort anyLow  = Any.UInt16().GreaterThan(anyHigh).Generate();

        // Act
        Exception? thrown = Record.Exception(() => new PortRange(anyLow, anyHigh));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void A_range_whose_ends_are_equal_is_a_single_port() {
        // Arrange
        ushort anyPort = Any.UInt16().Generate();

        // Act
        PortRange range = new PortRange(anyPort, anyPort);

        // Assert
        Assert.Equal(1, range.Count);
        Assert.Equal(PortRange.Single(anyPort), range);
    }

    [Fact]
    public void The_count_is_the_number_of_ports_it_holds() {
        // Arrange
        PortRange anyRange = Any.PortRange().Generate();

        // Assert
        Assert.Equal(anyRange.High - anyRange.Low + 1, anyRange.Count);
    }

    [Fact]
    public void All_is_every_port() {
        // Assert
        Assert.Equal(0, PortRange.All.Low);
        Assert.Equal(65535, PortRange.All.High);
        Assert.Equal(65536, PortRange.All.Count);
        Assert.True(PortRange.All.IsAll);
        Assert.Equal(PortRange.All, new PortRange(0, 65535));
    }

    [Theory]
    [InlineData(0, 65534)]
    [InlineData(1, 65535)]
    [InlineData(80, 80)]
    public void A_range_short_of_either_end_is_not_all(ushort low, ushort high) {
        // Assert
        Assert.False(new PortRange(low, high).IsAll);
    }

    [Fact]
    public void A_range_contains_its_ends_and_what_lies_between_and_nothing_else() {
        // Arrange
        PortRange anyRange = Any.PortRange().WithLow(low => low.GreaterThan((ushort)1)).WithHigh(high => high.LessThan(ushort.MaxValue)).Generate();
        ushort    between  = Any.UInt16().Between(anyRange.Low, anyRange.High).Generate();

        // Assert
        Assert.True(anyRange.Contains(anyRange.Low));
        Assert.True(anyRange.Contains(anyRange.High));
        Assert.True(anyRange.Contains(between));
        Assert.False(anyRange.Contains((ushort)(anyRange.Low - 1)));
        Assert.False(anyRange.Contains((ushort)(anyRange.High + 1)));
    }

    [Theory]
    [InlineData(1, 1024, 80, 443, true)]
    [InlineData(1, 1024, 1, 1024, true)]
    [InlineData(1, 1024, 1, 1025, false)]
    [InlineData(1, 1024, 0, 1024, false)]
    [InlineData(1000, 2000, 500, 3000, false)]
    [InlineData(0, 65535, 0, 65535, true)]
    [InlineData(80, 80, 80, 80, true)]
    [InlineData(80, 80, 79, 80, false)]
    public void A_range_contains_a_range_it_encloses(ushort low, ushort high, ushort otherLow, ushort otherHigh, bool expected) {
        // Act
        bool contains = new PortRange(low, high).Contains(new PortRange(otherLow, otherHigh));

        // Assert
        Assert.Equal(expected, contains);
    }

    [Theory]
    [InlineData(1, 10, 10, 20, true)]
    [InlineData(1, 10, 11, 20, false)]
    [InlineData(11, 20, 1, 10, false)]
    [InlineData(10, 20, 1, 10, true)]
    [InlineData(1, 100, 40, 60, true)]
    [InlineData(40, 60, 1, 100, true)]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 65535, 65535, false)]
    [InlineData(0, 65535, 65535, 65535, true)]
    public void Ranges_overlap_when_they_share_a_port(ushort low, ushort high, ushort otherLow, ushort otherHigh, bool expected) {
        // Arrange
        PortRange range = new PortRange(low, high);
        PortRange other = new PortRange(otherLow, otherHigh);

        // Assert
        Assert.Equal(expected, range.Overlaps(other));
        Assert.Equal(expected, other.Overlaps(range));
    }

    [Fact]
    public void Contains_and_Overlaps_refuse_null() {
        // Arrange
        PortRange anyRange = Any.PortRange().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyRange.Contains(null!));
        Assert.Throws<ArgumentNullException>(() => anyRange.Overlaps(null!));
    }

    [Fact]
    public void Ranges_are_equal_by_their_ends() {
        // Arrange
        PortRange anyRange = Any.PortRange().Generate();
        PortRange copy     = new PortRange(anyRange.Low, anyRange.High);

        // Assert
        Assert.Equal(anyRange, copy);
        Assert.Equal(anyRange.GetHashCode(), copy.GetHashCode());
        Assert.False(anyRange.Equals(null));
    }

    [Fact]
    public void A_range_reads_as_low_dash_high_a_single_port_as_itself_and_all_as_any() {
        // Assert
        Assert.Equal("1-1024", new PortRange(1, 1024).ToString());
        Assert.Equal("443", PortRange.Single(443).ToString());
        Assert.Equal("any", PortRange.All.ToString());
    }

}
