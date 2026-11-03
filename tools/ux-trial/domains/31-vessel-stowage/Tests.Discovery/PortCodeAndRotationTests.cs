using VesselStowage.Domain;

namespace VesselStowage.Tests;

public sealed class PortCodeTests {

    [Theory]
    [InlineData("NLRTM")]
    [InlineData("US2NY")]
    [InlineData("AA999")]
    [InlineData("ZZZZZ")]
    public void A_two_letter_country_and_three_location_characters_is_accepted(string text) {
        Assert.Equal(text, new PortCode(text).Value);
    }

    [Fact]
    public void A_generated_port_code_is_accepted_by_the_constructor() {
        PortCode anyPort = Any.PortCode().Generate();

        Assert.NotNull(new PortCode(anyPort.Value));
    }

    [Fact]
    public void A_null_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new PortCode(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("NLRT")]
    [InlineData("NLRTMX")]
    public void A_code_of_the_wrong_length_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PortCode(text));

        Assert.Contains("5 characters", refusal.Message);
    }

    [Theory]
    [InlineData("nlRTM")]
    [InlineData("N1RTM")]
    [InlineData("1LRTM")]
    public void A_country_code_that_is_not_two_upper_case_letters_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PortCode(text));

        Assert.Contains("country code", refusal.Message);
    }

    [Theory]
    [InlineData("NL0TM")]
    [InlineData("NL1TM")]
    [InlineData("NLrTM")]
    [InlineData("NLR-M")]
    [InlineData("NLRT ")]
    public void A_location_outside_A_to_Z_and_2_to_9_is_refused(string text) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new PortCode(text));

        Assert.Contains("A-Z or digits 2-9", refusal.Message);
    }

    [Fact]
    public void Two_codes_with_the_same_text_are_equal_and_hash_alike() {
        PortCode first  = new PortCode("NLRTM");
        PortCode second = new PortCode("NLRTM");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, new PortCode("DEHAM"));
    }

    [Fact]
    public void A_code_prints_as_its_text() {
        Assert.Equal("NLRTM", new PortCode("NLRTM").ToString());
    }

}

public sealed class RotationTests {

    [Fact]
    public void A_rotation_keeps_the_call_order_it_was_given() {
        // Arrange
        List<PortCode> ports = new List<PortCode> { Scene.Felixstowe, Scene.Rotterdam, Scene.LeHavre };

        // Act
        Rotation rotation = new Rotation(ports);

        // Assert
        Assert.Equal(ports, rotation.Ports);
        Assert.Equal(0, rotation.IndexOf(Scene.Felixstowe));
        Assert.Equal(1, rotation.IndexOf(Scene.Rotterdam));
        Assert.Equal(2, rotation.IndexOf(Scene.LeHavre));
    }

    [Fact]
    public void A_generated_rotation_calls_at_least_two_distinct_ports() {
        // Arrange
        Rotation anyRotation = Any.Rotation().Generate();

        // Act
        int distinct = anyRotation.Ports.Distinct().Count();

        // Assert: what the generator promises, that the constructor then enforces
        Assert.True(anyRotation.Ports.Count >= 2);
        Assert.Equal(anyRotation.Ports.Count, distinct);
    }

    [Fact]
    public void A_null_list_of_ports_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Rotation(null!));
    }

    [Fact]
    public void An_empty_rotation_is_refused() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Rotation(new List<PortCode>()));

        Assert.Contains("at least two", refusal.Message);
    }

    [Fact]
    public void A_rotation_of_a_single_port_is_refused() {
        PortCode anyPort = Any.PortCode().Generate();

        Assert.Throws<ArgumentException>(() => new Rotation(new List<PortCode> { anyPort }));
    }

    [Fact]
    public void A_null_port_is_refused() {
        PortCode anyPort = Any.PortCode().Generate();

        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Rotation(new List<PortCode> { anyPort, null! }));

        Assert.Contains("null port", refusal.Message);
    }

    [Fact]
    public void A_port_called_twice_is_refused_even_when_the_two_codes_are_distinct_instances() {
        Assert.Throws<ArgumentException>(() => new Rotation(new List<PortCode> { new PortCode("NLRTM"), new PortCode("DEHAM"), new PortCode("NLRTM") }));
    }

    [Fact]
    public void Calls_tells_whether_a_port_is_part_of_the_rotation() {
        Rotation rotation = Scene.FourPortRotation();

        Assert.True(rotation.Calls(new PortCode("NLRTM")));
        Assert.False(rotation.Calls(new PortCode("BEANR")));
    }

    [Fact]
    public void The_index_of_a_port_outside_the_rotation_is_refused() {
        // Arrange: a port that is not one of the four of the scene
        PortCode anyOtherPort = Any.PortCode().Generate();
        Rotation rotation     = new Rotation(new List<PortCode> { Scene.Rotterdam, Scene.Hamburg });

        // Act / Assert
        if (rotation.Calls(anyOtherPort)) { return; }
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => rotation.IndexOf(anyOtherPort));
        Assert.Contains("not in the rotation", refusal.Message);
    }

    [Fact]
    public void The_index_of_a_null_port_is_refused() {
        Assert.Throws<ArgumentNullException>(() => Scene.FourPortRotation().IndexOf(null!));
    }

    [Fact]
    public void Changing_the_list_given_to_the_constructor_does_not_change_the_rotation() {
        // Arrange
        List<PortCode> ports    = new List<PortCode> { Scene.Rotterdam, Scene.Hamburg };
        Rotation       rotation = new Rotation(ports);

        // Act
        ports.Add(Scene.LeHavre);

        // Assert
        Assert.Equal(2, rotation.Ports.Count);
    }

}
