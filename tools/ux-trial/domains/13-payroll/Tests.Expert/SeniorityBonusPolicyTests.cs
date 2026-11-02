namespace Payroll.Tests.Expert;

public sealed class SeniorityBonusPolicyTests {

    // ---- the rate by completed years -------------------------------------------------------------------------

    public static TheoryData<DateOnly, DateOnly, decimal> RatesByYearsOfService => new TheoryData<DateOnly, DateOnly, decimal> {
        { new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 1), 0m },       // 0 years
        { new DateOnly(2020, 1, 1), new DateOnly(2022, 12, 31), 0m },     // 2 years, one day short of the third anniversary
        { new DateOnly(2020, 1, 1), new DateOnly(2023, 1, 1), 0.03m },    // 3 years: the first tier
        { new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31), 0.03m },  // 9 years, one day short of the tenth anniversary
        { new DateOnly(2020, 1, 1), new DateOnly(2030, 1, 1), 0.06m },    // 10 years: the second tier
        { new DateOnly(2020, 1, 1), new DateOnly(2090, 1, 1), 0.06m },    // far beyond
        { new DateOnly(2020, 1, 1), new DateOnly(2019, 1, 1), 0m }        // before the hiring
    };

    [Theory]
    [MemberData(nameof(RatesByYearsOfService))]
    public void The_seniority_rate_follows_the_completed_years_of_service(DateOnly hiredOn, DateOnly asOf, decimal expectedRate) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().NotATraineeship().WithHiredOn(hiredOn).Generate();

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(contract, asOf);

        // Assert
        Assert.Equal(expectedRate, rate);
    }

    [Theory]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void An_apprentice_or_an_intern_earns_no_seniority_bonus_whatever_the_years_of_service(ContractType type) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(type).WithHiredOn(new DateOnly(2000, 1, 1)).Generate();

        // Act
        decimal rate = SeniorityBonusPolicy.RateFor(contract, new DateOnly(2050, 1, 1));

        // Assert
        Assert.Equal(0m, rate);
    }

    [Fact]
    public void A_fixed_term_contract_earns_the_same_seniority_bonus_as_a_permanent_one() {
        // Arrange
        EmploymentContract fixedTerm = Any.EmploymentContract().WithType(ContractType.FixedTerm).WithHiredOn(new DateOnly(2010, 1, 1)).Generate();
        EmploymentContract permanent = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2010, 1, 1)).Generate();

        // Act & Assert
        Assert.Equal(0.06m, SeniorityBonusPolicy.RateFor(fixedTerm, new DateOnly(2025, 1, 1)));
        Assert.Equal(0.06m, SeniorityBonusPolicy.RateFor(permanent, new DateOnly(2025, 1, 1)));
    }

    [Fact]
    public void The_rate_of_a_missing_contract_is_refused() {
        // Arrange
        DateOnly anyDate = Any.DateOnly().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.RateFor(null!, anyDate));

        // Assert
        Assert.Equal("contract", refusal.ParamName);
    }

    // ---- the bonus line -------------------------------------------------------------------------------------

    // A 39-hour contract hired on a chosen day; 100 h worked at 20.00 make a base pay of 2000.00 whatever the period.
    private static Payslip PayslipForBasePayOf2000(DateOnly hiredOn, PayPeriod period) {
        return Any.Payslip()
                  .WithContract(contract => contract.NotATraineeship().WithHiredOn(hiredOn).WithWeeklyHours(39).WithRate(new HourlyRate(20.00m)))
                  .WithPeriod(period)
                  .WithHoursWorked(100m)
                  .Generate();
    }

    [Fact]
    public void The_bonus_line_is_the_seniority_rate_applied_to_the_base_pay() {
        // Arrange: 3 completed years at the end of June 2024
        Payslip payslip = PayslipForBasePayOf2000(new DateOnly(2021, 6, 30), new PayPeriod(2024, 6));

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal("Seniority bonus", line.Label);
        Assert.Equal(LineKind.Bonus, line.Kind);
        Assert.Equal(60.00m, line.Amount);   // 2000.00 x 0.03
    }

    [Fact]
    public void The_bonus_line_of_a_ten_year_veteran_is_six_percent_of_the_base_pay() {
        // Arrange
        Payslip payslip = PayslipForBasePayOf2000(new DateOnly(2014, 6, 30), new PayPeriod(2024, 6));

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(120.00m, line.Amount);  // 2000.00 x 0.06
    }

    [Fact]
    public void The_rate_is_evaluated_on_the_last_day_of_the_pay_period() {
        // Arrange: hired on 31 January 2021, the third anniversary falls on the last day of January 2024
        Payslip januaryPayslip  = PayslipForBasePayOf2000(new DateOnly(2021, 1, 31), new PayPeriod(2024, 1));
        Payslip decemberPayslip = PayslipForBasePayOf2000(new DateOnly(2021, 1, 31), new PayPeriod(2023, 12));

        // Act
        PayslipLine? lineOfJanuary  = SeniorityBonusPolicy.BonusLineFor(januaryPayslip);
        PayslipLine? lineOfDecember = SeniorityBonusPolicy.BonusLineFor(decemberPayslip);

        // Assert
        Assert.NotNull(lineOfJanuary);
        Assert.Equal(60.00m, lineOfJanuary.Amount);
        Assert.Null(lineOfDecember);
    }

    [Fact]
    public void The_third_anniversary_falling_later_in_the_month_does_not_count_for_that_month() {
        // Arrange: hired on 1 February 2021, the third anniversary is after the last day of January 2024
        Payslip payslip = PayslipForBasePayOf2000(new DateOnly(2021, 2, 1), new PayPeriod(2024, 1));

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void There_is_no_bonus_line_before_the_third_anniversary() {
        // Arrange
        Payslip payslip = PayslipForBasePayOf2000(new DateOnly(2022, 6, 30), new PayPeriod(2024, 6));

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Theory]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void There_is_no_bonus_line_for_an_apprentice_or_an_intern(ContractType type) {
        // Arrange
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.WithType(type).WithHiredOn(new DateOnly(2000, 1, 1)).WithRate(new HourlyRate(20.00m)))
                             .WithPeriod(new PayPeriod(2030, 1))
                             .WithHoursWorked(100m)
                             .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void There_is_no_bonus_line_when_nothing_was_worked() {
        // Arrange
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.NotATraineeship().WithHiredOn(new DateOnly(2000, 1, 1)))
                             .WithPeriod(new PayPeriod(2024, 6))
                             .WithHoursWorked(0m)
                             .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void There_is_no_bonus_line_when_the_bonus_rounds_to_zero() {
        // Arrange: 1 h at 0.10 = 0.10 base pay; 0.10 x 0.03 = 0.003
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.NotATraineeship().WithHiredOn(new DateOnly(2020, 1, 1)).WithRate(new HourlyRate(0.10m)))
                             .WithPeriod(new PayPeriod(2024, 6))
                             .WithHoursWorked(1m)
                             .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Null(line);
    }

    [Fact]
    public void The_bonus_rounds_a_half_cent_away_from_zero() {
        // Arrange: 1 h at 1.50 = 1.50 base pay; 1.50 x 0.03 = 0.045
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.NotATraineeship().WithHiredOn(new DateOnly(2020, 1, 1)).WithRate(new HourlyRate(1.50m)))
                             .WithPeriod(new PayPeriod(2024, 6))
                             .WithHoursWorked(1m)
                             .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(0.05m, line.Amount);
    }

    [Fact]
    public void The_bonus_is_computed_on_the_base_pay_only_not_on_overtime_or_other_lines() {
        // Arrange: 179 h on a 39-hour contract at 20.00 = 3380.00 base pay + 250.00 overtime; a 1000.00 bonus line
        PayslipLine[] lines = [new PayslipLine("Night shift", LineKind.Bonus, 1000m)];
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.NotATraineeship().WithHiredOn(new DateOnly(2014, 1, 1)).WithWeeklyHours(39).WithRate(new HourlyRate(20.00m)))
                             .WithPeriod(new PayPeriod(2024, 6))
                             .WithHoursWorked(179m)
                             .WithLines(lines)
                             .Generate();

        // Act
        PayslipLine? line = SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.NotNull(line);
        Assert.Equal(202.80m, line.Amount);   // 3380.00 x 0.06
    }

    [Fact]
    public void The_bonus_line_is_not_added_to_the_payslip() {
        // Arrange
        Payslip payslip = PayslipForBasePayOf2000(new DateOnly(2014, 6, 30), new PayPeriod(2024, 6));
        int linesBefore = payslip.Lines.Count;

        // Act
        SeniorityBonusPolicy.BonusLineFor(payslip);

        // Assert
        Assert.Equal(linesBefore, payslip.Lines.Count);
    }

    [Fact]
    public void The_bonus_line_of_a_missing_payslip_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => SeniorityBonusPolicy.BonusLineFor(null!));

        // Assert
        Assert.Equal("payslip", refusal.ParamName);
    }

}
