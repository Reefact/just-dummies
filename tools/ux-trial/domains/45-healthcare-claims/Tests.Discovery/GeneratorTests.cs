using Clinical.Domain;

using Insurance.Domain;

using JustDummies;

using Payments.Domain;

using Xunit;

namespace HealthcareClaims.Domain;

/// <summary>The hand-completed generators must only ever draw values the domain accepts.</summary>
public sealed class GeneratorTests {

    [Fact]
    public void Drawn_policies_respect_every_rule_of_the_domain() {
        // Act
        List<SupplementaryPolicy> anyPolicies = Any.ListOf(Any.SupplementaryPolicy()).WithCount(200).Generate();

        // Assert
        Assert.All(anyPolicies, policy => {
            Assert.True(policy.Deductible.Amount < policy.AnnualCeiling.Amount);
            Assert.True(policy.Period.Start >= policy.Insured.BirthDate);
            Assert.True(policy.Period.Start.Year >= policy.IssueYear);
        });
    }

    [Fact]
    public void Drawn_claims_are_always_on_a_dispensed_prescription_and_its_own_lines() {
        // Act
        List<ReimbursementClaim> anyClaims = Any.ListOf(Any.ReimbursementClaim()).WithCount(200).Generate();

        // Assert
        Assert.All(anyClaims, claim => {
            Assert.Equal(PrescriptionStatus.Dispensed, claim.Prescription.Status);
            Assert.Equal(ClaimStatus.Submitted, claim.Status);
            Assert.All(claim.Lines, line => Assert.Contains(line.PrescriptionLine, claim.Prescription.Lines));
        });
    }

    [Fact]
    public void Drawn_dispensed_lines_never_get_a_statutory_reimbursement_above_the_price_paid() {
        // Act
        List<DispensedLine> anyLines = Any.ListOf(Any.DispensedLine()).WithCount(500).Generate();

        // Assert
        Assert.All(anyLines, line => {
            Assert.True(line.RemainingCharge >= 0m);
            if (line.Category == ReimbursementCategory.Unlisted) {
                Assert.Null(line.StatutoryReimbursement);
            }
        });
    }

    [Fact]
    public void Drawn_assessments_never_let_deductible_and_reduction_exceed_the_covered_amount() {
        // Act
        List<ClaimAssessment> anyAssessments = Any.ListOf(Any.ClaimAssessment()).WithCount(500).Generate();

        // Assert
        Assert.All(anyAssessments, assessment => Assert.True(assessment.Payable >= 0m));
    }

}
