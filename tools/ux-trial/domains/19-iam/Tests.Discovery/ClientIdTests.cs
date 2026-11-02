using Identity.Domain;

using Xunit;

namespace Identity.Tests.Discovery;

public sealed class ClientIdTests {

    private static readonly Guid Registered = Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301");

    [Fact]
    public void The_empty_guid_is_not_a_client_id() {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ClientId(Guid.Empty));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void A_client_id_exposes_the_guid_it_wraps() {
        // Act
        ClientId id = new ClientId(Registered);

        // Assert
        Assert.Equal(Registered, id.Value);
    }

    [Fact]
    public void A_client_id_prints_as_the_guid_in_D_format() {
        // Act
        string text = new ClientId(Registered).ToString();

        // Assert
        Assert.Equal("3f2504e0-4f89-11d3-9a0c-0305e82c3301", text);
    }

    [Fact]
    public void Two_client_ids_over_the_same_guid_are_equal_and_hash_alike() {
        // Arrange
        ClientId first  = new ClientId(Registered);
        ClientId second = new ClientId(Registered);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_client_ids_over_different_guids_are_different() {
        // Arrange
        ClientId first  = new ClientId(Registered);
        ClientId second = new ClientId(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3302"));

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void A_client_id_is_not_equal_to_null_nor_to_a_bare_guid() {
        // Arrange
        ClientId id = new AnyClientId().Generate();

        // Assert
        Assert.False(id.Equals(null));
        Assert.False(id.Equals((object)id.Value));
    }

}
