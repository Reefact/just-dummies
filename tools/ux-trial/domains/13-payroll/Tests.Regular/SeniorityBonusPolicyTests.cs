using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class SeniorityBonusPolicyTests {

    private static readonly DateOnly HiredOn = new DateOnly(2010, 6, 15);

    public static TheoryData<ContractType> ContractTypesWithBonus => new TheoryData<ContractType> { ContractType.Permanent, ContractType.FixedTerm };

    public static TheoryData<ContractType> ContractTypesWithoutBonus => new TheoryData<ContractType> { ContractType.Apprenticeship, ContractType.Internship };

    public static TheoryData<DateOnly, decimal> RateByAsOf => new TheoryData<DateOnly, decimal> {
        { new DateOnly(2010, 6, 15), 0m },
        { new DateOnly(2013, 6, 14), 0m },
        { new DateOnly(2013, 6, 15), 0.03m },
        { new DateOnly(2020, 6, 14), 0.03m },
        { new DateOnly(2020, 6, 15), 0.06m },
        { new DateOnly(2060, 1, 1), 0.06m },
        { new DateOnly(2005, 1, 1), 0m }
    };

    private static EmploymentContract ContractOf(ContractType type, DateOnly hiredOn) {
        return Any.EmploymentContract().WithType(type).WithHiredOn(hiredOn).Generate();
    }


    [Theory]
    [MemberData(nameof(RateByAsOf))]
    public void The_rate_grows_with_the_completed_years_of_a_permanent_contract(DateOnly asOf, decimal expected) {
        // Arrange
        EmploymentContract contract = ContractOf(ContractType.Permanent, HiredOn);

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(contract, asOf);

        // Assert
        Assert.Equal(expected, rate);
    }

    [Theory]
    [MemberData(nameof(ContractTypesWithBonus))]
    public void A_fixed_term_contract_earns_the_bonus_like_a_permanent_one(ContractType type) {
        // Arrange
        EmploymentContract contract = ContractOf(type, HiredOn);

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(contract, new DateOnly(2021, 1, 1));

        // Assert
        Assert.Equal(0.06m, rate);
    }

    [Theory]
    [MemberData(nameof(ContractTypesWithoutBonus))]
    public void Apprentices_and_interns_never_earn_a_seniority_bonus(ContractType type) {
        // Arrange
        EmploymentContract contract = ContractOf(type, HiredOn);

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(contract, new DateOnly(2060, 1, 1));

        // Assert
        Assert.Equal(0m, rate);
    }

    [Fact]
    public void A_null_contract_has_no_rate() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.RateFor(null!, anyDate));

        // Assert
        Assert.Equal("contract", exception.ParamName);
    }

    [Fact]
    public void A_null_payslip_has_no_bonus_line() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.BonusLineFor(null!));

        // Assert
        Assert.Equal("payslip", exception.ParamName);
    }

    [Fact]
    public void Ten_years_of_service_earn_six_percent_of_the_base_pay() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal("Seniority bonus", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(120.00m, line.Amount);
    }

    [Fact]
    public void Three_years_of_service_earn_three_percent_of_the_base_pay() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2021, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(60.00m, line.Amount);
    }

    [Fact]
    public void The_bonus_is_computed_on_the_base_pay_only_not_on_the_overtime() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(160m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(182.00m, line.Amount); // 6 % of the 3 033.40 base pay, 182.004
    }

    [Fact]
    public void The_bonus_is_computed_on_the_base_pay_whatever_the_lines_of_the_payslip_are() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip anyPayslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(anyPayslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(120.00m, line.Amount);
    }

    [Fact]
    public void The_bonus_line_is_not_added_to_the_payslip() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip anyPayslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();
        int     before     = anyPayslip.Lines.Count;

        // Act
        SeniorityBonusPolicy.BonusLineFor(anyPayslip);

        // Assert
        Assert.Equal(before, anyPayslip.Lines.Count);
    }

    [Fact]
    public void The_rate_is_evaluated_on_the_last_day_of_the_pay_period() {
        // Arrange: hired on the last day of June 2015, ten years are complete on 30 June 2025.
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 6, 30))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(120.00m, line.Amount);
    }

    [Fact]
    public void The_rate_of_the_previous_step_applies_when_the_anniversary_falls_after_the_period() {
        // Arrange: hired on the first of July 2015, only nine years are complete on 30 June 2025.
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 7, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(60.00m, line.Amount);
    }

    [Fact]
    public void The_bonus_rounds_a_half_cent_away_from_zero_at_three_percent() {
        // Arrange: 3 % of 1.50 is 0.045.
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2021, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(1.50m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(1m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(0.05m, line.Amount);
    }

    [Fact]
    public void The_bonus_rounds_a_half_cent_away_from_zero_at_six_percent() {
        // Arrange: 6 % of 0.75 is 0.045.
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2015, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(0.75m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(1m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(0.05m, line.Amount);
    }

    [Fact]
    public void There_is_no_bonus_line_before_three_years_of_service() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2022, 7, 1))
                                         .WithRate(new HourlyRate(20.00m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Theory]
    [MemberData(nameof(ContractTypesWithoutBonus))]
    public void There_is_no_bonus_line_for_an_apprentice_or_an_intern(ContractType type) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(type).WithHiredOn(new DateOnly(2000, 1, 1)).WithRate(new HourlyRate(20.00m)).Generate();
        Payslip            payslip  = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(100m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void There_is_no_bonus_line_when_no_hours_were_worked() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        Payslip            payslip  = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(0m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void There_is_no_bonus_line_when_the_bonus_rounds_to_zero() {
        // Arrange: 3 % of 0.10 is 0.003.
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2021, 1, 1))
                                         .WithWeeklyHours(35).WithRate(new HourlyRate(0.10m)).Generate();
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2025, 6)).WithHoursWorked(1m).Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

}
