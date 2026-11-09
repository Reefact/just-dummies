using JustDummies;
using Tree.Domain;
using Xunit;

namespace Tree.Tests;

public class NodeTests {

    [Fact]
    public void Scaffolded_default_recipe_generates_a_node() {
        Node node = new AnyNode().Generate();

        Assert.NotNull(node);
    }

}
