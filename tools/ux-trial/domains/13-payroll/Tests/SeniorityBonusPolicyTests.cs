using JustDummies;
using JustDummies.Xunit;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

[Reproducible]
public sealed class SeniorityBonusPolicyTests {

    private static readonly DateOnly EndOfJune2026 = new DateOnly(2026, 6, 30);

    // Employees on these two contracts earn seniority; which one does not matter to the rate.
    private static AnyEmploymentContract AnySeniorityEarningContract() {
        return Any.EmploymentContract().WithType(t => t.OneOf(ContractType.Permanent, ContractType.FixedTerm));
    }

    [Fact]
    public void Five_years_of_service_earn_three_percent() {
        // Arrange
        EmploymentContract anyContract = AnySeniorityEarningContract().WithHiredOn(new DateOnly(2021, 6, 30)).Generate();

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(anyContract, EndOfJune2026);

        // Assert
        Assert.Equal(0.03m, rate);
    }

    [Fact]
    public void Twelve_years_of_service_earn_six_percent() {
        // Arrange
        EmploymentContract anyContract = AnySeniorityEarningContract().WithHiredOn(new DateOnly(2014, 3, 1)).Generate();

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(anyContract, EndOfJune2026);

        // Assert
        Assert.Equal(0.06m, rate);
    }

    [Fact]
    public void Apprentices_and_interns_earn_no_seniority_however_long_they_stayed() {
        // Arrange
        EmploymentContract anyTrainee = Any.EmploymentContract()
                                           .WithType(t => t.OneOf(ContractType.Apprenticeship, ContractType.Internship))
                                           .WithHiredOn(new DateOnly(2011, 9, 1))
                                           .Generate();

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(anyTrainee, EndOfJune2026);

        // Assert
        Assert.Equal(0m, rate);
    }

    [Fact]
    public void The_seniority_line_is_a_bonus_on_base_pay_at_the_end_of_the_period() {
        // Arrange
        EmploymentContract anyContract = AnySeniorityEarningContract()
                                         .WithHiredOn(new DateOnly(2021, 6, 30))
                                         .WithWeeklyHours(35)
                                         .WithRate(new HourlyRate(20.00m))
                                         .Generate();
        Payslip anyJunePayslip = Any.Payslip()
                                    .WithContract(anyContract)
                                    .WithPeriod(new PayPeriod(2026, 6))
                                    .WithHoursWorked(150m)
                                    .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(anyJunePayslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(90.00m, line.Amount);   // 150 h × 20.00 × 3 %
    }

    [Fact]
    public void A_junior_employee_gets_no_seniority_line() {
        // Arrange
        EmploymentContract anyJunior = AnySeniorityEarningContract().WithHiredOn(new DateOnly(2025, 1, 6)).Generate();
        Payslip anyJunePayslip = Any.Payslip().WithContract(anyJunior).WithPeriod(new PayPeriod(2026, 6)).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(anyJunePayslip);

        // Assert
        Assert.Null(line);
    }

}
