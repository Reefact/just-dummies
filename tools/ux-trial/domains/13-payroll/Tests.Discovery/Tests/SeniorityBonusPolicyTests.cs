using System.Globalization;

using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class SeniorityBonusPolicyTests {

    private static readonly DateOnly Hired = new DateOnly(2010, 6, 15);

    public static TheoryData<ContractType, int, decimal> RatesByYears => new TheoryData<ContractType, int, decimal> {
        { ContractType.Permanent, 0, 0m },
        { ContractType.Permanent, 2, 0m },
        { ContractType.Permanent, 3, 0.03m },
        { ContractType.Permanent, 9, 0.03m },
        { ContractType.Permanent, 10, 0.06m },
        { ContractType.Permanent, 25, 0.06m },
        { ContractType.FixedTerm, 2, 0m },
        { ContractType.FixedTerm, 3, 0.03m },
        { ContractType.FixedTerm, 10, 0.06m },
        { ContractType.Apprenticeship, 3, 0m },
        { ContractType.Apprenticeship, 10, 0m },
        { ContractType.Internship, 3, 0m },
        { ContractType.Internship, 10, 0m }
    };

    [Theory]
    [MemberData(nameof(RatesByYears))]
    public void The_rate_depends_on_the_completed_years_and_the_type(ContractType type, int years, decimal expected) {
        EmploymentContract contract = Any.EmploymentContract().WithType(type).WithHiredOn(Hired).Generate();

        decimal rate = SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(years));

        Assert.Equal(expected, rate);
    }

    [Fact]
    public void The_day_before_the_third_anniversary_earns_nothing_and_the_anniversary_earns_three_percent() {
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(Hired).Generate();

        Assert.Equal(0m, SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(3).AddDays(-1)));
        Assert.Equal(0.03m, SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(3)));
    }

    [Fact]
    public void The_day_before_the_tenth_anniversary_earns_three_percent_and_the_anniversary_earns_six() {
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(Hired).Generate();

        Assert.Equal(0.03m, SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(10).AddDays(-1)));
        Assert.Equal(0.06m, SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(10)));
    }

    [Fact]
    public void Before_the_hiring_date_the_rate_is_zero() {
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(Hired).Generate();

        Assert.Equal(0m, SeniorityBonusPolicy.RateFor(contract, Hired.AddYears(-5)));
    }

    [Fact]
    public void A_missing_contract_is_refused_for_the_rate() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.RateFor(null!, Hired));

        Assert.Equal("contract", exception.ParamName);
    }

    [Fact]
    public void A_missing_payslip_is_refused_for_the_bonus_line() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.BonusLineFor(null!));

        Assert.Equal("payslip", exception.ParamName);
    }

    // A permanent-like contract, 39 h a week (169 h a month) at 10.00 an hour: 169 h worked is a base pay of 1 690.00.
    private static Payslip PayslipOfAContractHiredOn(DateOnly hiredOn, ContractType type, int year, int month, decimal hoursWorked = 169m, decimal rate = 10.00m, int weeklyHours = 39) {
        EmploymentContract contract = Any.EmploymentContract()
                                         .WithType(type)
                                         .WithHiredOn(hiredOn)
                                         .WithWeeklyHours(weeklyHours)
                                         .WithRate(r => r.WithValue(rate))
                                         .Generate();

        return Any.Payslip()
                  .WithContract(contract)
                  .WithPeriod(new PayPeriod(year, month))
                  .WithHoursWorked(hoursWorked)
                  .WithLines(new List<PayslipLine>())
                  .Generate();
    }

    [Fact]
    public void The_bonus_line_is_three_percent_of_base_pay_after_three_years() {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6);

        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.NotNull(line);
        Assert.Equal("Seniority bonus", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(50.70m, line.Amount);
    }

    [Fact]
    public void The_bonus_line_is_six_percent_of_base_pay_after_ten_years() {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2010, 1, 1), ContractType.Permanent, 2023, 6);

        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.NotNull(line);
        Assert.Equal(101.40m, line.Amount);
    }

    [Fact]
    public void The_rate_is_evaluated_on_the_last_day_of_the_period() {
        // Hired 2020-01-15: on the last day of January 2023 (31st) three years are complete; on the first day (1st) they are not.
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 15), ContractType.Permanent, 2023, 1);

        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.NotNull(line);
        Assert.Equal(50.70m, line.Amount);
    }

    [Fact]
    public void A_third_anniversary_falling_after_the_period_has_not_been_reached() {
        // Hired 2020-02-01: the last day of January 2023 is one day short of the third anniversary.
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 2, 1), ContractType.Permanent, 2023, 1);

        Assert.Null(SeniorityBonusPolicy.BonusLineFor(payslip));
    }

    [Fact]
    public void Below_three_years_there_is_no_bonus_line() {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2022, 1, 1), ContractType.Permanent, 2023, 6);

        Assert.Null(SeniorityBonusPolicy.BonusLineFor(payslip));
    }

    [Theory]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void An_apprentice_or_an_intern_never_gets_a_bonus_line(ContractType type) {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2000, 1, 1), type, 2023, 6, weeklyHours: 35);

        Assert.Null(SeniorityBonusPolicy.BonusLineFor(payslip));
    }

    [Fact]
    public void An_amount_that_rounds_to_zero_gives_no_line() {
        // 0.1 h at 0.10 an hour = 0.01 of base pay; 3 % of it is 0.0003.
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6, hoursWorked: 0.1m, rate: 0.10m);

        Assert.Null(SeniorityBonusPolicy.BonusLineFor(payslip));
    }

    [Fact]
    public void A_no_hours_payslip_gives_no_line() {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6, hoursWorked: 0m);

        Assert.Null(SeniorityBonusPolicy.BonusLineFor(payslip));
    }

    [Fact]
    public void The_amount_rounds_a_half_cent_away_from_zero() {
        // Base pay 1.50 (1.5 h at 1.00); 3 % of it is 0.045: away from zero gives 0.05 (banker's rounding would give 0.04).
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6, hoursWorked: 1.5m, rate: 1.00m);

        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.NotNull(line);
        Assert.Equal(0.05m, line.Amount);
    }

    [Fact]
    public void The_bonus_is_computed_on_base_pay_only_not_on_overtime() {
        // 200 h worked on 169 contractual hours: 31 h of overtime, which the bonus ignores.
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6, hoursWorked: 200m);

        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.NotNull(line);
        Assert.Equal(50.70m, line.Amount);
    }

    [Fact]
    public void The_bonus_line_is_not_added_to_the_payslip() {
        Payslip payslip = PayslipOfAContractHiredOn(new DateOnly(2020, 1, 1), ContractType.Permanent, 2023, 6);

        SeniorityBonusPolicy.BonusLineFor(payslip);

        Assert.Empty(payslip.Lines);
    }

}
