using Identity.Domain;

using JustDummies;

using Xunit;

namespace Identity.Tests.Regular;

public sealed class ClientIdTests {

    [Fact]
    public void An_empty_guid_is_not_a_client_id() {
        // Act
        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new ClientId(Guid.Empty));

        // Assert
        Assert.Equal("value", thrown.ParamName);
    }

    [Fact]
    public void Two_client_ids_built_from_the_same_guid_are_equal() {
        // Arrange
        Guid     anyGuid = Any.Guid().NonEmpty().Generate();
        ClientId first   = new ClientId(anyGuid);
        ClientId second  = new ClientId(anyGuid);

        // Act
        bool equal = first.Equals(second);

        // Assert
        Assert.True(equal);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_client_ids_built_from_different_guids_are_not_equal() {
        // Arrange
        Guid     firstGuid  = Any.Guid().NonEmpty().Generate();
        Guid     secondGuid = Any.Guid().NonEmpty().DifferentFrom(firstGuid).Generate();
        ClientId first      = new ClientId(firstGuid);
        ClientId second     = new ClientId(secondGuid);

        // Act
        bool equal = first.Equals(second);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_client_id_is_never_equal_to_null() {
        // Arrange
        ClientId anyClientId = Any.ClientId().Generate();

        // Act
        bool equal = anyClientId.Equals(null);

        // Assert
        Assert.False(equal);
    }

    [Fact]
    public void A_client_id_prints_as_a_guid_in_the_d_format() {
        // Arrange
        Guid     guid     = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
        ClientId clientId = new ClientId(guid);

        // Act
        string text = clientId.ToString();

        // Assert
        Assert.Equal("0f8fad5b-d9cb-469f-a165-70867728950e", text);
    }

}
