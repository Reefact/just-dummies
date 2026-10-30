using Freight.Domain;
using JustDummies;
using JustDummies.Xunit;
using Xunit;

namespace Freight.Tests;

/// <summary>Harness checks: every scaffolded (then hand-resolved) generator only ever builds valid values.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyWeight_always_builds_a_valid_Weight() => AlwaysBuilds(new AnyWeight());

    [Fact]
    public void Scaffolded_AnyVolume_always_builds_a_valid_Volume() => AlwaysBuilds(new AnyVolume());

    [Fact]
    public void Scaffolded_AnyUnLocode_always_builds_a_valid_UnLocode() => AlwaysBuilds(new AnyUnLocode());

    [Fact]
    public void Scaffolded_AnyContainerCode_always_builds_a_valid_ContainerCode() => AlwaysBuilds(Arbitrary.ContainerCode());

    [Fact]
    public void Scaffolded_AnyFreightRate_always_builds_a_valid_FreightRate() => AlwaysBuilds(Arbitrary.FreightRate());

    [Fact]
    public void Scaffolded_AnyLeg_always_builds_a_valid_Leg() => AlwaysBuilds(Arbitrary.Leg());

    [Fact]
    public void Scaffolded_AnyShipment_always_builds_a_valid_Shipment() => AlwaysBuilds(Arbitrary.Shipment());

}
