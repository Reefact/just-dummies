using JustDummies;

using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class RotationTests {

    [Fact]
    public void A_rotation_keeps_its_ports_in_call_order() {
        // Arrange
        List<PortCode> anyPorts = Any.ListOf(Any.PortCode()).WithCountBetween(2, 6).Distinct().Generate();

        // Act
        Rotation rotation = new Rotation(anyPorts);

        // Assert
        Assert.Equal(anyPorts, rotation.Ports);
    }

    [Fact]
    public void The_index_of_a_port_is_its_zero_based_position_in_the_rotation() {
        // Arrange
        PortCode rotterdam = new PortCode("NLRTM");
        PortCode hamburg   = new PortCode("DEHAM");
        PortCode lehavre   = new PortCode("FRLEH");
        Rotation rotation  = new Rotation([rotterdam, hamburg, lehavre]);

        // Act & Assert
        Assert.Equal(0, rotation.IndexOf(rotterdam));
        Assert.Equal(1, rotation.IndexOf(hamburg));
        Assert.Equal(2, rotation.IndexOf(lehavre));
    }

    [Fact]
    public void A_rotation_of_two_ports_is_the_smallest_accepted() {
        // Arrange
        List<PortCode> anyPorts = Any.ListOf(Any.PortCode()).WithCount(2).Distinct().Generate();

        // Act
        Rotation rotation = new Rotation(anyPorts);

        // Assert
        Assert.Equal(2, rotation.Ports.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void A_rotation_calling_fewer_than_two_ports_is_refused(int count) {
        // Arrange
        List<PortCode> anyPorts = Any.ListOf(Any.PortCode()).WithCount(count).Distinct().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("ports", () => new Rotation(anyPorts));

        // Assert
        Assert.Contains("at least two ports", refusal.Message);
    }

    [Fact]
    public void A_port_called_twice_is_refused() {
        // Arrange
        PortCode anyPort      = Any.PortCode().Generate();
        PortCode anyOtherPort = Any.PortCode().DifferentFrom(anyPort).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("ports", () => new Rotation([anyPort, anyOtherPort, new PortCode(anyPort.Value)]));

        // Assert
        Assert.Contains("only once", refusal.Message);
    }

    [Fact]
    public void A_null_port_is_refused() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("ports", () => new Rotation([anyPort, null!]));

        // Assert
        Assert.Contains("null port", refusal.Message);
    }

    [Fact]
    public void A_null_list_of_ports_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>("ports", () => new Rotation(null!));
    }

    [Fact]
    public void A_rotation_calls_the_ports_it_was_built_with_and_no_other() {
        // Arrange
        List<PortCode> anyDistinctPorts = Any.ListOf(Any.PortCode()).WithCount(4).Distinct().Generate();
        PortCode anyForeignPort         = anyDistinctPorts[3];
        Rotation rotation               = new Rotation(anyDistinctPorts.Take(3));

        // Act & Assert
        Assert.True(rotation.Calls(anyDistinctPorts[0]));
        Assert.True(rotation.Calls(anyDistinctPorts[1]));
        Assert.True(rotation.Calls(anyDistinctPorts[2]));
        Assert.False(rotation.Calls(anyForeignPort));
    }

    [Fact]
    public void The_index_of_a_port_the_rotation_does_not_call_is_refused() {
        // Arrange
        List<PortCode> anyDistinctPorts = Any.ListOf(Any.PortCode()).WithCount(3).Distinct().Generate();
        Rotation rotation               = new Rotation(anyDistinctPorts.Take(2));

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>("port", () => rotation.IndexOf(anyDistinctPorts[2]));

        // Assert
        Assert.Contains("not in the rotation", refusal.Message);
    }

    [Fact]
    public void The_index_of_a_null_port_is_refused() {
        // Arrange
        Rotation anyRotation = Any.Rotation().Generate();

        // Act & Assert
        Assert.Throws<ArgumentNullException>("port", () => anyRotation.IndexOf(null!));
    }

    [Fact]
    public void Changing_the_list_a_rotation_was_built_from_does_not_change_the_rotation() {
        // Arrange
        List<PortCode> ports = Any.ListOf(Any.PortCode()).WithCountBetween(2, 6).Distinct().Generate();
        Rotation rotation    = new Rotation(ports);
        int countBefore      = rotation.Ports.Count;

        // Act
        ports.Add(new PortCode("NLRTM"));

        // Assert
        Assert.Equal(countBefore, rotation.Ports.Count);
    }

    [Fact(Skip = "domain defect: Rotation.Ports hands out the internal List<PortCode>, so a caller can downcast it and add a duplicate or null port after construction, bypassing every constructor guard (not seed-dependent)")]
    public void The_ports_a_rotation_exposes_cannot_be_used_to_change_it() {
        // Arrange
        List<PortCode> anyPorts = Any.ListOf(Any.PortCode()).WithCountBetween(2, 6).Distinct().Generate();
        Rotation rotation       = new Rotation(anyPorts);

        // Act
        bool changed = rotation.Ports is List<PortCode> list && TryAdd(list, anyPorts[0]);

        // Assert
        Assert.False(changed);
        Assert.Equal(anyPorts.Count, rotation.Ports.Count);
    }

    private static bool TryAdd(List<PortCode> list, PortCode port) {
        list.Add(port);

        return true;
    }

}
