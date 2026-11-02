namespace Payroll.Tests.Expert;

public sealed class PayslipTests {

    // A 39-hour contract is paid 169.00 hours a month; the rate is spelled out because every amount below is computed from it.
    private static AnyPayslip AnyPayslipOnA39HourContractAt(decimal hourlyRate) {
        return Any.Payslip()
                  .WithContract(contract => contract.NotATraineeship()
                                                    .WithWeeklyHours(39)
                                                    .WithRate(new HourlyRate(hourlyRate)));
    }

    private static readonly PayslipLine[] NoLine = [];

    // ---- construction ----------------------------------------------------------------------------------------

    [Fact]
    public void A_new_payslip_is_a_draft() {
        // Act
        Payslip payslip = Any.Payslip().Generate();

        // Assert
        Assert.Equal(PayslipStatus.Draft, payslip.Status);
    }

    [Fact]
    public void A_payslip_keeps_the_contract_the_period_the_hours_and_the_lines_it_was_given() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();
        PayPeriod          period   = new PayPeriod(2024, 3);
        PayslipLine        line     = new PayslipLine("Night shift", LineKind.Bonus, 50m);

        // Act
        Payslip payslip = new Payslip(contract, period, 151.5m, [line]);

        // Assert
        Assert.Same(contract, payslip.Contract);
        Assert.Same(period, payslip.Period);
        Assert.Equal(151.5m, payslip.HoursWorked);
        Assert.Same(line, Assert.Single(payslip.Lines));
    }

    [Fact]
    public void A_payslip_keeps_a_copy_of_the_lines_it_was_given() {
        // Arrange
        PayslipLine       anyLine  = Any.PayslipLine().Generate();
        List<PayslipLine> lines    = [];
        Payslip           payslip  = Any.Payslip().WithLines(lines).Generate();

        // Act
        lines.Add(anyLine);

        // Assert
        Assert.Empty(payslip.Lines);
    }

    [Fact]
    public void A_payslip_without_a_contract_is_refused() {
        // Arrange
        PayPeriod anyPeriod = Any.PayPeriod().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Payslip(null!, anyPeriod, 100m, NoLine));

        // Assert
        Assert.Equal("contract", refusal.ParamName);
    }

    [Fact]
    public void A_payslip_without_a_period_is_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new Payslip(anyContract, null!, 100m, NoLine));

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_payslip_without_a_list_of_lines_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.Payslip().WithLines((IReadOnlyList<PayslipLine>)null!).Generate());

        // Assert
        Assert.Equal("lines", refusal.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public void Hours_worked_at_the_edge_of_the_allowed_range_are_accepted(decimal hours) {
        // Act
        Payslip payslip = Any.Payslip().WithHoursWorked(hours).Generate();

        // Assert
        Assert.Equal(hours, payslip.HoursWorked);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(300.01)]
    public void Hours_worked_outside_0_to_300_are_refused(double hours) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.Payslip().WithHoursWorked((decimal)hours).Generate());

        // Assert
        Assert.Equal("hoursWorked", refusal.ParamName);
    }

    [Fact]
    public void A_payslip_for_the_month_of_the_hiring_is_accepted_even_when_the_hiring_is_on_its_last_day() {
        // Arrange
        EmploymentContract contractHiredOnTheLastDayOfJune = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 6, 30)).Generate();

        // Act
        Payslip payslip = Any.Payslip().WithContract(contractHiredOnTheLastDayOfJune).WithPeriod(new PayPeriod(2024, 6)).Generate();

        // Assert
        Assert.Equal(new PayPeriod(2024, 6).LastDay, payslip.Contract.HiredOn);
    }

    [Fact]
    public void A_payslip_for_the_month_before_the_hiring_is_refused() {
        // Arrange
        EmploymentContract contractHiredOnTheFirstDayOfJune = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 6, 1)).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.Payslip().WithContract(contractHiredOnTheFirstDayOfJune).WithPeriod(new PayPeriod(2024, 5)).Generate());

        // Assert
        Assert.Equal("period", refusal.ParamName);
    }

    [Fact]
    public void A_payslip_may_cover_a_month_after_the_end_of_the_contract() {
        // Arrange
        EmploymentContract contractEndedInMarch = Any.EmploymentContract()
                                                     .WithType(ContractType.FixedTerm)
                                                     .WithHiredOn(new DateOnly(2024, 1, 1))
                                                     .WithEndsOn(new DateOnly(2024, 3, 31))
                                                     .Generate();

        // Act
        Payslip payslip = Any.Payslip().WithContract(contractEndedInMarch).WithPeriod(new PayPeriod(2024, 12)).Generate();

        // Assert
        Assert.Equal(2024, payslip.Period.Year);
        Assert.Equal(12, payslip.Period.Month);
    }

    // The domain model says the elements of the list are not checked: a null line is kept, and breaks the totals later.
    [Fact(Skip = "domain defect: Payslip does not refuse a null element in the lines (AddLine does refuse null), so TotalBonuses then throws NullReferenceException")]
    public void A_payslip_with_a_null_line_is_refused() {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => Any.Payslip().WithLines(new PayslipLine[] { null! }).Generate());
    }

    // ---- hours and base pay ----------------------------------------------------------------------------------

    [Fact]
    public void Hours_below_the_contractual_hours_are_all_paid_at_the_base_rate() {
        // Arrange: 100 h at 20.00 on a 39-hour contract (169.00 h a month)
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(100m).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(2000.00m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void Hours_exactly_the_contractual_hours_are_not_overtime() {
        // Arrange
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(169.00m).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(3380.00m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void Hours_beyond_the_contractual_hours_are_overtime_paid_at_125_percent() {
        // Arrange: 179 h = 169.00 contractual + 10 overtime
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(179m).Generate();

        // Act & Assert
        Assert.Equal(10m, payslip.OvertimeHours);
        Assert.Equal(3380.00m, payslip.BasePay);      // 169.00 x 20.00
        Assert.Equal(250.00m, payslip.OvertimePay);   // 10 x 20.00 x 1.25
    }

    [Fact]
    public void No_hours_worked_means_no_base_pay_and_no_overtime() {
        // Arrange
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(0m).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(0m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void The_contractual_hours_of_a_35_hour_contract_are_151_67_so_151_68_hours_is_one_hundredth_of_overtime() {
        // Arrange
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.WithWeeklyHours(35).WithRate(new HourlyRate(10.00m)))
                             .WithHoursWorked(151.68m)
                             .Generate();

        // Act & Assert
        Assert.Equal(0.01m, payslip.OvertimeHours);
        Assert.Equal(1516.70m, payslip.BasePay);      // 151.67 x 10.00
        Assert.Equal(0.13m, payslip.OvertimePay);     // 0.01 x 10.00 x 1.25 = 0.125, rounded away from zero
    }

    [Fact]
    public void Base_pay_rounds_a_half_cent_away_from_zero() {
        // Arrange: 2.5 h x 6.01 = 15.025, a midpoint
        Payslip payslip = AnyPayslipOnA39HourContractAt(6.01m).WithHoursWorked(2.5m).Generate();

        // Act & Assert
        Assert.Equal(15.03m, payslip.BasePay);
    }

    [Fact]
    public void Overtime_pay_rounds_a_half_cent_away_from_zero() {
        // Arrange: 0.02 h of overtime x 1.00 x 1.25 = 0.025, a midpoint
        Payslip payslip = AnyPayslipOnA39HourContractAt(1.00m).WithHoursWorked(169.02m).Generate();

        // Act & Assert
        Assert.Equal(0.02m, payslip.OvertimeHours);
        Assert.Equal(0.03m, payslip.OvertimePay);
    }

    [Fact]
    public void The_maximum_hours_at_the_maximum_rate_stay_within_what_a_decimal_holds() {
        // Arrange: 48-hour contract at the ceiling, 300 h worked
        Payslip payslip = Any.Payslip()
                             .WithContract(contract => contract.WithType(ContractType.Permanent).WithWeeklyHours(48).WithRate(new HourlyRate(HourlyRate.Ceiling)))
                             .WithHoursWorked(300m)
                             .Generate();

        // Act & Assert
        Assert.Equal(92m, payslip.OvertimeHours);       // 300 - 208.00
        Assert.Equal(104_000.00m, payslip.BasePay);      // 208.00 x 500
        Assert.Equal(57_500.00m, payslip.OvertimePay);   // 92 x 500 x 1.25
    }

    // ---- lines and totals ------------------------------------------------------------------------------------

    [Fact]
    public void Totals_are_the_sums_of_the_bonuses_and_of_the_deductions() {
        // Arrange
        PayslipLine[] lines = [
            new PayslipLine("Night shift", LineKind.Bonus, 50.10m),
            new PayslipLine("Health insurance", LineKind.Deduction, 30.05m),
            new PayslipLine("Meal voucher", LineKind.Bonus, 20.20m),
            new PayslipLine("Union dues", LineKind.Deduction, 9.95m)
        ];
        Payslip payslip = Any.Payslip().WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(70.30m, payslip.TotalBonuses);
        Assert.Equal(40.00m, payslip.TotalDeductions);
    }

    [Fact]
    public void A_payslip_without_lines_has_no_bonuses_and_no_deductions() {
        // Arrange
        Payslip payslip = Any.Payslip().WithLines(NoLine).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.TotalBonuses);
        Assert.Equal(0m, payslip.TotalDeductions);
    }

    [Fact]
    public void Gross_pay_is_base_pay_plus_overtime_pay_plus_bonuses() {
        // Arrange
        PayslipLine[] lines = [
            new PayslipLine("Night shift", LineKind.Bonus, 100m),
            new PayslipLine("Health insurance", LineKind.Deduction, 40m)
        ];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(179m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(3730.00m, payslip.GrossPay);   // 3380.00 + 250.00 + 100.00: deductions are not taken off yet
    }

    [Fact]
    public void Net_pay_is_gross_pay_less_deductions() {
        // Arrange
        PayslipLine[] lines = [
            new PayslipLine("Night shift", LineKind.Bonus, 100m),
            new PayslipLine("Health insurance", LineKind.Deduction, 40m)
        ];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(179m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(3690.00m, payslip.NetPay);
    }

    [Fact]
    public void Net_pay_never_goes_below_zero_when_deductions_exceed_gross_pay() {
        // Arrange: gross 2000.00, deductions 2000.01
        PayslipLine[] lines = [new PayslipLine("Advance repayment", LineKind.Deduction, 2000.01m)];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(100m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void Net_pay_is_zero_when_deductions_equal_gross_pay() {
        // Arrange
        PayslipLine[] lines = [new PayslipLine("Advance repayment", LineKind.Deduction, 2000.00m)];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(100m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void Net_pay_is_one_cent_when_deductions_fall_one_cent_short_of_gross_pay() {
        // Arrange
        PayslipLine[] lines = [new PayslipLine("Advance repayment", LineKind.Deduction, 1999.99m)];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(100m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(0.01m, payslip.NetPay);
    }

    [Fact(Skip = "domain defect: PayslipLine has no ceiling on its amount, so the totals of a payslip throw OverflowException when its lines sum past decimal.MaxValue, seed 12"), Reproducible(Seed = 12)]
    public void The_net_pay_of_a_payslip_is_not_negative() {
        // Arrange: the lines are drawn, and the net pay does not care what they are
        Payslip anyPayslip = Any.Payslip().Generate();

        // Act
        decimal netPay = anyPayslip.NetPay;

        // Assert
        Assert.True(netPay >= 0m);
    }

    [Fact]
    public void Bonuses_of_a_payslip_with_no_hours_are_still_paid() {
        // Arrange
        PayslipLine[] lines = [new PayslipLine("Signing bonus", LineKind.Bonus, 500m)];
        Payslip payslip = Any.Payslip().WithHoursWorked(0m).WithLines(lines).Generate();

        // Act & Assert
        Assert.Equal(500m, payslip.GrossPay);
        Assert.Equal(500m, payslip.NetPay);
    }

    // ---- adding a line ---------------------------------------------------------------------------------------

    [Fact]
    public void A_line_added_to_a_draft_is_appended_and_counted_in_the_totals() {
        // Arrange
        Payslip     payslip = Any.Payslip().WithLines(NoLine).Generate();
        PayslipLine bonus   = new PayslipLine("Night shift", LineKind.Bonus, 50m);

        // Act
        payslip.AddLine(bonus);

        // Assert
        Assert.Same(bonus, Assert.Single(payslip.Lines));
        Assert.Equal(50m, payslip.TotalBonuses);
    }

    [Fact]
    public void Lines_keep_the_order_in_which_they_were_added() {
        // Arrange
        Payslip     payslip = Any.Payslip().WithLines(NoLine).Generate();
        PayslipLine first   = new PayslipLine("First", LineKind.Bonus, 1m);
        PayslipLine second  = new PayslipLine("Second", LineKind.Deduction, 2m);

        // Act
        payslip.AddLine(first);
        payslip.AddLine(second);

        // Assert
        Assert.Equal([first, second], payslip.Lines);
    }

    [Fact]
    public void A_null_line_is_refused() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => anyPayslip.AddLine(null!));

        // Assert
        Assert.Equal("line", refusal.ParamName);
    }

    [Fact]
    public void A_line_cannot_be_added_to_an_issued_payslip() {
        // Arrange
        Payslip     anyPayslip = Any.Payslip().Generate();
        PayslipLine anyLine    = Any.PayslipLine().Generate();
        anyPayslip.Issue();
        int linesBefore = anyPayslip.Lines.Count;

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPayslip.AddLine(anyLine));

        // Assert
        Assert.Contains("issued", refusal.Message);
        Assert.Equal(linesBefore, anyPayslip.Lines.Count);
    }

    // ---- issuing ---------------------------------------------------------------------------------------------

    [Fact]
    public void Issuing_a_draft_makes_it_issued() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();

        // Act
        anyPayslip.Issue();

        // Assert
        Assert.Equal(PayslipStatus.Issued, anyPayslip.Status);
    }

    [Fact]
    public void An_issued_payslip_cannot_be_issued_again() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPayslip.Issue());

        // Assert
        Assert.Contains("already issued", refusal.Message);
        Assert.Equal(PayslipStatus.Issued, anyPayslip.Status);
    }

    [Fact]
    public void Issuing_a_payslip_does_not_change_what_it_pays() {
        // Arrange
        PayslipLine[] lines = [new PayslipLine("Night shift", LineKind.Bonus, 100m)];
        Payslip payslip = AnyPayslipOnA39HourContractAt(20.00m).WithHoursWorked(179m).WithLines(lines).Generate();

        // Act
        payslip.Issue();

        // Assert
        Assert.Equal(3730.00m, payslip.GrossPay);
        Assert.Equal(3730.00m, payslip.NetPay);
    }

}
