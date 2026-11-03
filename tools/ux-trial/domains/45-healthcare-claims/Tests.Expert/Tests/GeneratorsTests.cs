using System.Text.RegularExpressions;
using Clinical.Domain;
using HealthcareClaims.Domain;
using JustDummies;
using Xunit;

namespace HealthcareClaims.Tests.Expert.Tests;

/// <summary>
///     The generators written or completed by hand are test code like any other: they must never draw a value the
///     domain refuses, nor one that breaks the ties they were written to keep. Each test here is about a generator.
/// </summary>
public sealed class GeneratorsTests {

    private const int Draws = 200;

    [Fact]
    public void A_drawn_policy_is_always_buildable_with_its_deductible_below_its_ceiling() {
        for (int draw = 0; draw < Draws; draw++) {
            SupplementaryPolicy policy = Any.SupplementaryPolicy().Generate();

            Assert.True(policy.Deductible.Amount < policy.AnnualCeiling.Amount);
        }
    }

    [Fact]
    public void A_drawn_claim_is_always_a_valid_one_for_some_lines_of_a_dispensed_prescription() {
        for (int draw = 0; draw < Draws; draw++) {
            ReimbursementClaim claim = Any.ReimbursementClaim().Generate();

            Assert.NotEmpty(claim.Lines);
            Assert.True(claim.Lines.Count <= claim.Prescription.Lines.Count);
        }
    }

    [Fact]
    public void A_drawn_schedule_covers_at_least_one_category() {
        for (int draw = 0; draw < Draws; draw++) {
            ReimbursementSchedule schedule = Any.ReimbursementSchedule().Generate();

            Assert.NotEmpty(schedule.Rates);
        }
    }

    [Fact]
    public void A_drawn_reference_has_the_shape_of_a_claim_reference() {
        for (int draw = 0; draw < Draws; draw++) {
            ClaimReference reference = Any.ClaimReference().Generate();

            Assert.Matches(new Regex(@"^CLM-20[0-9]{2}-[0-9]{6}$"), reference.Value);
        }
    }

    [Fact]
    public void A_pinned_nir_gives_a_patient_born_in_the_year_and_month_it_carries() {
        for (int draw = 0; draw < Draws; draw++) {
            Nir     anyNir  = Any.Nir().Generate();
            Patient patient = Any.Patient().WithNir(anyNir).Generate();

            Assert.Equal(anyNir.BirthMonth, patient.BirthDate.Month);
            Assert.Equal(anyNir.BirthYearTwoDigits, patient.BirthDate.Year % 100);
        }
    }

    [Fact]
    public void A_drawn_assessment_holds_whatever_a_test_pinned_of_it() {
        for (int draw = 0; draw < Draws; draw++) {
            ClaimAssessment withDeductible = Any.ClaimAssessment().WithDeductibleApplied(75.50m).Generate();
            ClaimAssessment withReduction  = Any.ClaimAssessment().WithCeilingReduction(12.25m).Generate();
            ClaimAssessment withBoth       = Any.ClaimAssessment().WithDeductibleApplied(40m).WithCeilingReduction(60m).Generate();

            Assert.Equal(75.50m, withDeductible.DeductibleApplied);
            Assert.Equal(12.25m, withReduction.CeilingReduction);
            Assert.True(withBoth.CoveredAmount >= 100m);
        }
    }

    [Fact]
    public void The_same_seed_draws_the_same_policy_and_the_same_claim() {
        (string Policy, string Claim, int Lines, decimal Paid) Draw() {
            using (Any.UseSeed(20261103)) {
                SupplementaryPolicy policy = Any.SupplementaryPolicy().Generate();
                ReimbursementClaim  claim  = Any.ReimbursementClaim().ForInsured(policy.Insured).Generate();

                return (policy.Number.Value + policy.Payee.Account.Value, claim.Reference.Value + claim.Prescription.Id, claim.Lines.Count, claim.TotalPaid);
            }
        }

        Assert.Equal(Draw(), Draw());
    }

}
