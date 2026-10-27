using JustDummies;
using JustDummies.Xunit;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, draws 500 values its constructor accepts.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyMsisdn_always_builds_a_valid_Msisdn() => AlwaysBuilds(Arbitrary.Msisdn());

    [Fact]
    public void Scaffolded_AnyImei_always_builds_a_valid_Imei() => AlwaysBuilds(Arbitrary.Imei());

    [Fact]
    public void Scaffolded_AnyIccid_always_builds_a_valid_Iccid() => AlwaysBuilds(Arbitrary.Iccid());

    [Fact]
    public void Scaffolded_AnyDataBundle_always_builds_a_valid_DataBundle() => AlwaysBuilds(Arbitrary.DataBundle());

    [Fact]
    public void Scaffolded_AnyPlan_always_builds_a_valid_Plan() => AlwaysBuilds(Arbitrary.Plan());

    [Fact]
    public void Scaffolded_AnyBillingPeriod_always_builds_a_valid_BillingPeriod() => AlwaysBuilds(Arbitrary.BillingPeriod());

    [Fact]
    public void Scaffolded_AnyUsageRecord_always_builds_a_valid_UsageRecord() => AlwaysBuilds(Arbitrary.UsageRecord());

    [Fact]
    public void Scaffolded_AnyMobileLine_always_builds_a_valid_MobileLine() => AlwaysBuilds(Arbitrary.MobileLine());

}
