using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class PayslipTests {

    private static readonly IReadOnlyList<PayslipLine> NoLines = new List<PayslipLine>();

    // A contract paid on a permanent-like basis (never an apprenticeship or an internship, which stop at 35 hours a week),
    // with the weekly hours and the rate a calculation is about. The employee, the type among the permitted ones and the hiring date are dummies.
    private static AnyEmploymentContract AnyContractPaid(decimal rate, int weeklyHours) {
        return Any.EmploymentContract()
                  .WithType(t => t.Except(ContractType.Apprenticeship, ContractType.Internship))
                  .WithWeeklyHours(weeklyHours)
                  .WithRate(r => r.WithValue(rate));
    }

    private static Payslip PayslipOf(decimal rate, int weeklyHours, decimal hoursWorked, params PayslipLine[] lines) {
        return Any.Payslip()
                  .WithContract(AnyContractPaid(rate, weeklyHours).Generate())
                  .WithHoursWorked(hoursWorked)
                  .WithLines(lines)
                  .Generate();
    }

    // ---- construction ----

    [Fact]
    public void A_new_payslip_keeps_its_contract_period_and_hours_and_is_a_draft() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();
        PayPeriod          period   = new PayPeriod(2023, 5);

        // Act
        Payslip payslip = new Payslip(contract, period, 151.5m, NoLines);

        // Assert
        Assert.Same(contract, payslip.Contract);
        Assert.Same(period, payslip.Period);
        Assert.Equal(151.5m, payslip.HoursWorked);
        Assert.Equal(PayslipStatus.Draft, payslip.Status);
    }

    [Fact]
    public void A_payslip_copies_the_lines_it_is_given() {
        // Arrange
        List<PayslipLine> lines = new List<PayslipLine> { new PayslipLine("Bonus", LineKind.Bonus, 10m) };

        // Act
        Payslip payslip = Any.Payslip().WithLines(lines).Generate();
        lines.Add(new PayslipLine("Dues", LineKind.Deduction, 5m));

        // Assert
        Assert.Single(payslip.Lines);
    }

    [Fact]
    public void A_payslip_keeps_the_lines_in_order() {
        PayslipLine first  = new PayslipLine("A", LineKind.Bonus, 10m);
        PayslipLine second = new PayslipLine("B", LineKind.Deduction, 5m);

        Payslip payslip = Any.Payslip().WithLines(new List<PayslipLine> { first, second }).Generate();

        Assert.Collection(payslip.Lines, line => Assert.Same(first, line), line => Assert.Same(second, line));
    }

    [Fact]
    public void A_missing_contract_is_refused() {
        PayPeriod anyPeriod = Any.PayPeriod().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(null!, anyPeriod, 10m, NoLines));

        Assert.Equal("contract", exception.ParamName);
    }

    [Fact]
    public void A_missing_period_is_refused() {
        EmploymentContract anyContract = Any.EmploymentContract().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(anyContract, null!, 10m, NoLines));

        Assert.Equal("period", exception.ParamName);
    }

    [Fact]
    public void Missing_lines_are_refused() {
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        PayPeriod          anyPeriod   = Any.PayPeriod().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(anyContract, anyPeriod, 10m, null!));

        Assert.Equal("lines", exception.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("300")]
    [InlineData("151.67")]
    public void Hours_worked_between_zero_and_300_are_accepted(string hoursText) {
        decimal            hours       = decimal.Parse(hoursText, System.Globalization.CultureInfo.InvariantCulture);
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        PayPeriod          anyPeriod   = new PayPeriod(2024, 1);

        Payslip payslip = new Payslip(anyContract, anyPeriod, hours, NoLines);

        Assert.Equal(hours, payslip.HoursWorked);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("300.01")]
    [InlineData("1000")]
    public void Hours_worked_outside_zero_to_300_are_refused(string hoursText) {
        decimal            hours       = decimal.Parse(hoursText, System.Globalization.CultureInfo.InvariantCulture);
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        PayPeriod          anyPeriod   = new PayPeriod(2024, 1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Payslip(anyContract, anyPeriod, hours, NoLines));

        Assert.Equal("hoursWorked", exception.ParamName);
    }

    [Fact]
    public void A_payslip_cannot_cover_a_month_that_ends_before_the_hiring_date() {
        EmploymentContract contract = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 4, 1)).Generate();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Payslip(contract, new PayPeriod(2024, 3), 10m, NoLines));

        Assert.Equal("period", exception.ParamName);
    }

    [Theory]
    [InlineData(2024, 3, 31)]
    [InlineData(2024, 3, 1)]
    [InlineData(2024, 3, 15)]
    public void A_payslip_can_cover_the_month_of_the_hiring_date_whatever_the_day(int year, int month, int hiredDay) {
        EmploymentContract contract = Any.EmploymentContract().WithHiredOn(new DateOnly(year, month, hiredDay)).Generate();

        Payslip payslip = new Payslip(contract, new PayPeriod(year, month), 10m, NoLines);

        Assert.Equal(new PayPeriod(year, month).Year, payslip.Period.Year);
    }

    [Fact]
    public void A_payslip_is_not_checked_against_the_end_of_the_contract() {
        // Arrange: a fixed-term contract that ended in 2020.
        EmploymentContract ended = Any.EmploymentContract()
                                      .WithType(ContractType.FixedTerm)
                                      .WithHiredOn(new DateOnly(2020, 1, 1))
                                      .WithEndsOn(new DateOnly(2020, 6, 30))
                                      .Generate();

        // Act
        Payslip payslip = new Payslip(ended, new PayPeriod(2023, 1), 10m, NoLines);

        // Assert
        Assert.Equal(2023, payslip.Period.Year);
    }

    [Fact(Skip = "domain defect: a null element in 'lines' is accepted at construction and only blows up later (NullReferenceException in TotalBonuses/TotalDeductions); the model says elements are not null-checked, which is arguably an oversight")]
    public void A_null_line_among_the_lines_is_refused_at_construction() {
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        List<PayslipLine>  lines       = new List<PayslipLine> { null! };

        Assert.ThrowsAny<ArgumentException>(() => new Payslip(anyContract, new PayPeriod(2024, 1), 10m, lines));
    }

    [Fact]
    public void A_null_line_among_the_lines_makes_the_totals_fail_when_they_are_read() {
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2000, 1, 1)).Generate();
        List<PayslipLine>  lines       = new List<PayslipLine> { null! };
        Payslip            payslip     = new Payslip(anyContract, new PayPeriod(2024, 1), 10m, lines);

        Assert.Throws<NullReferenceException>(() => payslip.TotalBonuses);
    }

    // ---- hours and base pay ----

    [Fact]
    public void Hours_below_the_contractual_hours_are_all_paid_at_the_base_rate() {
        // 39 h a week = 169 h a month.
        Payslip payslip = PayslipOf(rate: 10.00m, weeklyHours: 39, hoursWorked: 100m);

        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(1000.00m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void Exactly_the_contractual_hours_make_no_overtime() {
        Payslip payslip = PayslipOf(rate: 10.00m, weeklyHours: 39, hoursWorked: 169m);

        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(1690.00m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void Hours_above_the_contractual_hours_are_overtime_paid_with_a_quarter_premium() {
        Payslip payslip = PayslipOf(rate: 10.00m, weeklyHours: 39, hoursWorked: 179m);

        Assert.Equal(10m, payslip.OvertimeHours);
        Assert.Equal(1690.00m, payslip.BasePay);
        Assert.Equal(125.00m, payslip.OvertimePay);
    }

    [Fact]
    public void The_overtime_premium_is_one_and_a_quarter() {
        Assert.Equal(1.25m, Payslip.OvertimePremium);
    }

    [Fact]
    public void No_hours_worked_means_no_pay() {
        Payslip payslip = PayslipOf(rate: 10.00m, weeklyHours: 39, hoursWorked: 0m);

        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(0m, payslip.BasePay);
        Assert.Equal(0m, payslip.GrossPay);
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void Contractual_hours_with_a_fraction_are_used_as_rounded_to_the_hundredth() {
        // 35 h a week = 151.67 h a month; 160 h worked leaves 8.33 h of overtime.
        Payslip payslip = PayslipOf(rate: 20.00m, weeklyHours: 35, hoursWorked: 160m);

        Assert.Equal(8.33m, payslip.OvertimeHours);
        Assert.Equal(3033.40m, payslip.BasePay);
        Assert.Equal(208.25m, payslip.OvertimePay);
    }

    [Fact]
    public void Base_pay_rounds_a_half_cent_away_from_zero() {
        // 0.5 h at 0.01 euro is 0.005: away from zero gives 0.01 (banker's rounding would give 0.00).
        Payslip payslip = PayslipOf(rate: 0.01m, weeklyHours: 39, hoursWorked: 0.5m);

        Assert.Equal(0.01m, payslip.BasePay);
    }

    [Fact]
    public void Overtime_pay_rounds_a_half_cent_away_from_zero() {
        // 1 h a week = 4.33 h a month. 4.35 h worked is 0.02 h of overtime: 0.02 * 0.20 * 1.25 = 0.005 -> 0.01.
        Payslip payslip = PayslipOf(rate: 0.20m, weeklyHours: 1, hoursWorked: 4.35m);

        Assert.Equal(0.02m, payslip.OvertimeHours);
        Assert.Equal(0.01m, payslip.OvertimePay);
        Assert.Equal(0.87m, payslip.BasePay);
    }

    // ---- lines and totals ----

    [Fact]
    public void Totals_add_up_the_lines_by_kind() {
        Payslip payslip = PayslipOf(10.00m, 39, 100m,
                                    new PayslipLine("Bonus A", LineKind.Bonus, 100.00m),
                                    new PayslipLine("Bonus B", LineKind.Bonus, 25.50m),
                                    new PayslipLine("Dues", LineKind.Deduction, 40.00m));

        Assert.Equal(125.50m, payslip.TotalBonuses);
        Assert.Equal(40.00m, payslip.TotalDeductions);
    }

    [Fact]
    public void Without_lines_the_totals_are_zero() {
        Payslip payslip = PayslipOf(10.00m, 39, 100m);

        Assert.Equal(0m, payslip.TotalBonuses);
        Assert.Equal(0m, payslip.TotalDeductions);
    }

    [Fact]
    public void Gross_pay_is_base_plus_overtime_plus_bonuses_and_ignores_deductions() {
        Payslip payslip = PayslipOf(10.00m, 39, 179m,
                                    new PayslipLine("Bonus", LineKind.Bonus, 100.00m),
                                    new PayslipLine("Dues", LineKind.Deduction, 40.00m));

        Assert.Equal(1690.00m + 125.00m + 100.00m, payslip.GrossPay);
    }

    [Fact]
    public void Net_pay_is_gross_pay_minus_deductions() {
        Payslip payslip = PayslipOf(10.00m, 39, 100m,
                                    new PayslipLine("Bonus", LineKind.Bonus, 100.00m),
                                    new PayslipLine("Dues", LineKind.Deduction, 40.00m));

        Assert.Equal(1060.00m, payslip.NetPay);
    }

    [Fact]
    public void Net_pay_never_goes_below_zero() {
        Payslip payslip = PayslipOf(10.00m, 39, 10m, new PayslipLine("Advance", LineKind.Deduction, 500.00m));

        Assert.Equal(100.00m, payslip.GrossPay);
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void Net_pay_can_be_exactly_zero() {
        Payslip payslip = PayslipOf(10.00m, 39, 10m, new PayslipLine("Advance", LineKind.Deduction, 100.00m));

        Assert.Equal(0m, payslip.NetPay);
    }

    // ---- life cycle ----

    [Fact]
    public void A_line_can_be_added_to_a_draft() {
        Payslip     anyPayslip = Any.Payslip().Generate();
        int         before     = anyPayslip.Lines.Count;
        PayslipLine line       = new PayslipLine("Late fee", LineKind.Deduction, 7.00m);

        anyPayslip.AddLine(line);

        Assert.Equal(before + 1, anyPayslip.Lines.Count);
        Assert.Same(line, anyPayslip.Lines[^1]);
    }

    [Fact]
    public void An_added_line_counts_in_the_totals() {
        Payslip payslip = PayslipOf(10.00m, 39, 100m);

        payslip.AddLine(new PayslipLine("Late fee", LineKind.Deduction, 7.00m));

        Assert.Equal(7.00m, payslip.TotalDeductions);
        Assert.Equal(993.00m, payslip.NetPay);
    }

    [Fact]
    public void A_null_line_cannot_be_added() {
        Payslip anyPayslip = Any.Payslip().Generate();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => anyPayslip.AddLine(null!));

        Assert.Equal("line", exception.ParamName);
    }

    [Fact]
    public void Issuing_a_draft_makes_it_issued() {
        Payslip anyPayslip = Any.Payslip().Generate();

        anyPayslip.Issue();

        Assert.Equal(PayslipStatus.Issued, anyPayslip.Status);
    }

    [Fact]
    public void A_payslip_cannot_be_issued_twice() {
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();

        Assert.Throws<InvalidOperationException>(() => anyPayslip.Issue());
        Assert.Equal(PayslipStatus.Issued, anyPayslip.Status);
    }

    [Fact]
    public void An_issued_payslip_refuses_new_lines_and_keeps_the_ones_it_had() {
        Payslip anyPayslip = Any.Payslip().Generate();
        int     before     = anyPayslip.Lines.Count;
        anyPayslip.Issue();

        Assert.Throws<InvalidOperationException>(() => anyPayslip.AddLine(new PayslipLine("Late fee", LineKind.Deduction, 7.00m)));
        Assert.Equal(before, anyPayslip.Lines.Count);
    }

    [Fact]
    public void A_null_line_is_refused_as_a_null_even_on_an_issued_payslip() {
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();

        Assert.Throws<ArgumentNullException>(() => anyPayslip.AddLine(null!));
    }

}
