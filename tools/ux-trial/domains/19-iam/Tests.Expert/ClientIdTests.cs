using Identity.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace Identity.Domain.Tests.Expert;

public sealed class ClientIdTests {

    [Fact]
    public void The_empty_guid_is_not_a_client_id() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ClientId(Guid.Empty));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_client_id_is_written_as_its_guid_in_the_D_format() {
        // Arrange
        ClientId clientId = new ClientId(Guid.Parse("3f2504e0-4f89-11d3-9a0c-0305e82c3301"));

        // Act
        string text = clientId.ToString();

        // Assert
        Assert.Equal("3f2504e0-4f89-11d3-9a0c-0305e82c3301", text);
    }

    [Fact]
    public void Two_client_ids_built_from_the_same_guid_are_equal() {
        // Arrange
        Guid anyGuid = Any.Guid().NonEmpty().Generate();

        ClientId first  = new ClientId(anyGuid);
        ClientId second = new ClientId(anyGuid);

        // Act + Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_client_ids_built_from_different_guids_are_different() {
        // Arrange
        Guid     anyGuid      = Any.Guid().NonEmpty().Generate();
        Guid     anyOtherGuid = Any.Guid().NonEmpty().DifferentFrom(anyGuid).Generate();
        ClientId first        = new ClientId(anyGuid);
        ClientId second       = new ClientId(anyOtherGuid);

        // Act + Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void A_client_id_is_not_equal_to_nothing_nor_to_a_bare_guid() {
        // Arrange
        Guid     anyGuid     = Any.Guid().NonEmpty().Generate();
        ClientId anyClientId = new ClientId(anyGuid);

        // Act + Assert
        Assert.False(anyClientId.Equals((ClientId?)null));
        Assert.False(anyClientId.Equals((object?)null));
        Assert.False(anyClientId.Equals((object)anyGuid));
    }

}
