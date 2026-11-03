using JustDummies;
using VesselStowage.Domain.Tests.Generators;
using Xunit;

namespace VesselStowage.Domain.Tests;

public sealed class RotationTests {

    // The order of the calls is what these tests are about, so the three ports are literals.
    private static readonly PortCode Rotterdam = new PortCode("NLRTM");
    private static readonly PortCode Antwerp   = new PortCode("BEANR");
    private static readonly PortCode Hamburg   = new PortCode("DEHAM");

    [Fact]
    public void The_ports_keep_the_order_of_the_calls() {
        // Act
        Rotation rotation = new Rotation(new[] { Rotterdam, Antwerp, Hamburg });

        // Assert
        Assert.Equal(new[] { Rotterdam, Antwerp, Hamburg }, rotation.Ports);
    }

    [Fact]
    public void IndexOf_gives_the_zero_based_position_of_the_call() {
        // Arrange
        Rotation rotation = new Rotation(new[] { Rotterdam, Antwerp, Hamburg });

        // Assert
        Assert.Equal(0, rotation.IndexOf(Rotterdam));
        Assert.Equal(1, rotation.IndexOf(Antwerp));
        Assert.Equal(2, rotation.IndexOf(Hamburg));
    }

    [Fact]
    public void IndexOf_finds_a_port_by_its_code_not_by_its_instance() {
        // Arrange
        Rotation rotation = new Rotation(new[] { Rotterdam, Antwerp, Hamburg });

        // Assert
        Assert.Equal(1, rotation.IndexOf(new PortCode("BEANR")));
    }

    [Fact]
    public void IndexOf_refuses_a_port_the_vessel_does_not_call_at() {
        // Arrange
        Rotation rotation = new Rotation(new[] { Rotterdam, Antwerp });

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => rotation.IndexOf(Hamburg));

        // Assert
        Assert.Contains("DEHAM", refusal.Message);
    }

    [Fact]
    public void A_rotation_calls_at_the_ports_it_was_built_from_and_nowhere_else() {
        // Arrange
        Rotation rotation = new Rotation(new[] { Rotterdam, Antwerp });

        // Assert
        Assert.True(rotation.Calls(Rotterdam));
        Assert.True(rotation.Calls(new PortCode("BEANR")));
        Assert.False(rotation.Calls(Hamburg));
    }

    [Fact]
    public void Changing_the_list_it_was_built_from_does_not_change_the_rotation() {
        // Arrange
        List<PortCode> ports    = new List<PortCode> { Rotterdam, Antwerp };
        Rotation       rotation = new Rotation(ports);

        // Act
        ports.Add(Hamburg);

        // Assert
        Assert.Equal(2, rotation.Ports.Count);
        Assert.False(rotation.Calls(Hamburg));
    }

    [Fact]
    public void A_rotation_of_two_ports_is_the_smallest_there_is() {
        // Arrange
        PortCode anyFirstPort  = Any.PortCode().Generate();
        PortCode anySecondPort = Any.PortCode().DifferentFrom(anyFirstPort).Generate();

        // Act
        Rotation rotation = new Rotation(new[] { anyFirstPort, anySecondPort });

        // Assert
        Assert.Equal(2, rotation.Ports.Count);
    }

    [Fact]
    public void A_rotation_without_any_port_is_refused() {
        Assert.Throws<ArgumentException>(() => new Rotation(Array.Empty<PortCode>()));
    }

    [Fact]
    public void A_rotation_with_a_single_port_is_refused() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Rotation(new[] { anyPort }));

        // Assert
        Assert.Contains("at least two", refusal.Message);
    }

    [Fact]
    public void A_port_called_twice_is_refused() {
        // Arrange
        PortCode anyPort      = Any.PortCode().Generate();
        PortCode anyOtherPort = Any.PortCode().DifferentFrom(anyPort).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Rotation(new[] { anyPort, anyOtherPort, new PortCode(anyPort.Value) }));

        // Assert
        Assert.Contains("only once", refusal.Message);
    }

    [Fact]
    public void A_null_port_is_refused() {
        // Arrange
        PortCode anyPort = Any.PortCode().Generate();

        // Assert
        Assert.Throws<ArgumentException>(() => new Rotation(new PortCode[] { anyPort, null! }));
    }

    [Fact]
    public void A_missing_list_of_ports_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Rotation(null!));
    }

    [Fact]
    public void A_missing_port_cannot_be_looked_up() {
        // Arrange
        Rotation anyRotation = Any.Rotation().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => anyRotation.IndexOf(null!));
    }

}
