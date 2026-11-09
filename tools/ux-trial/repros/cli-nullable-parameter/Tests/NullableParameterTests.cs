using JustDummies;
using Xunit;

namespace NullRepro;

public sealed class NullableParameterTests {
    [Fact]
    public void Pinning_a_nullable_reference_parameter_to_null_with_a_cast_warns() {
        Party anyPartyWithoutAgent = new AnyParty().WithAgent((Bic?)null).WithNote((string?)null).Generate();
        Assert.Null(anyPartyWithoutAgent.Agent);
        Assert.Null(anyPartyWithoutAgent.Note);
    }

    [Fact]
    public void Pinning_a_nullable_value_parameter_to_null_compiles_cleanly() {
        Party anyPartyWithoutPriority = new AnyParty().WithPriority((int?)null).Generate();
        Assert.Null(anyPartyWithoutPriority.Priority);
    }

    [Fact]
    public void The_default_recipe_never_draws_null_for_a_nullable_reference_parameter() {
        Party anyParty = new AnyParty().Generate();
        Assert.NotNull(anyParty.Agent);
        Assert.NotNull(anyParty.Note);
    }
}
