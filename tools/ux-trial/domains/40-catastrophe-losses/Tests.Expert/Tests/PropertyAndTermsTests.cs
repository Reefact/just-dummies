using CatastropheLosses.Domain;

using JustDummies;

namespace CatastropheLosses.Tests.Expert.Tests;

public sealed class InsuredPropertyTests {

    [Fact]
    public void The_ground_up_loss_is_the_insured_value_times_the_damage_ratio_rounded_to_cents() {
        // Arrange
        InsuredProperty anyProperty = Any.InsuredProperty()
                                         .WithInsuredValue(333_333.33m)
                                         .WithVulnerability(c => c.WithIntensities([0.0, 100.0]).WithDamageRatios([0.0, 1.0]))
                                         .Generate();

        // Act
        decimal groundUp = anyProperty.GroundUpLoss(30.0); // ratio 0.3: 99 999.999

        // Assert
        Assert.Equal(100_000.00m, groundUp);
    }

    [Theory]
    [InlineData("PRP-12345")]
    [InlineData("PRP-1234567")]
    [InlineData("prp-123456")]
    public void A_property_id_off_the_PRP_six_digit_format_is_refused(string malformedId) {
        // Arrange
        AnyInsuredProperty anyPropertyWithMalformedId = Any.InsuredProperty().WithId(malformedId);

        // Act
        Exception? refusal = Record.Exception(() => anyPropertyWithMalformedId.Generate());

        // Assert
        Assert.IsType<FormatException>(refusal);
    }

}

public sealed class PolicyTermsTests {

    [Theory]
    [InlineData(800,    0)]     // retained entirely by the insured
    [InlineData(1_000,  0)]     // exactly the deductible
    [InlineData(3_500,  2_500)] // the excess over the deductible
    [InlineData(9_000,  5_000)] // capped at the limit
    public void Terms_retain_the_deductible_and_pay_up_to_the_limit(int groundUpLoss, int expectedPayout) {
        // Arrange
        PolicyTerms terms = new(deductible: 1_000m, limit: 5_000m);

        // Act
        decimal payout = terms.Apply(groundUpLoss);

        // Assert
        Assert.Equal(expectedPayout, payout);
    }

}
