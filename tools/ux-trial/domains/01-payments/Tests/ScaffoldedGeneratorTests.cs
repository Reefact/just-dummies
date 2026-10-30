using JustDummies;
using JustDummies.Xunit;
using Payments.Domain;
using Xunit;

namespace Payments.Tests;

/// <summary>Harness: every scaffolded (and hand-resolved) generator must only build values its constructor accepts.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void DrawMany<T>(IAny<T> generator) {
        for (int i = 0; i < Draws; i++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyIban_always_builds_a_valid_Iban() => DrawMany(new AnyIban());

    [Fact]
    public void Scaffolded_AnyBic_always_builds_a_valid_Bic() => DrawMany(new AnyBic());

    [Fact]
    public void Scaffolded_AnyMoney_always_builds_a_valid_Money() => DrawMany(new AnyMoney());

    [Fact]
    public void Scaffolded_AnyRemittanceInformation_always_builds_a_valid_RemittanceInformation() => DrawMany(new AnyRemittanceInformation());

    [Fact]
    public void Scaffolded_AnyParty_always_builds_a_valid_Party() => DrawMany(new AnyParty());

    [Fact]
    public void Scaffolded_AnyCreditTransfer_always_builds_a_valid_CreditTransfer() => DrawMany(new AnyCreditTransfer());

    [Fact]
    public void Scaffolded_AnyFeePolicy_always_builds_a_valid_FeePolicy() => DrawMany(new AnyFeePolicy());

    [Fact]
    public void Scaffolded_AnyPaymentBatch_always_builds_a_valid_PaymentBatch() {
        for (int i = 0; i < 50; i++) {
            new AnyPaymentBatch().Generate();
        }
    }

}
