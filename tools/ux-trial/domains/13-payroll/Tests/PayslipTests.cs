using JustDummies;
using JustDummies.Xunit;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

[Reproducible]
public sealed class PayslipTests {

    // 35 h a week is 151.67 h a month: the base-rate ceiling these tests compute against.
    private static readonly HourlyRate TwelveFifty = new HourlyRate(12.50m);

    // A lambda, not a pinned contract: it keeps the base's rule that the contract predates the period.
    private static AnyPayslip AnyPayslipAt35HoursAndTwelveFifty() {
        return Any.Payslip().WithContract(c => c.WithWeeklyHours(35).WithRate(TwelveFifty));
    }

    [Fact]
    public void Hours_within_the_contractual_month_are_paid_at_the_base_rate() {
        // Arrange
        Payslip payslip = AnyPayslipAt35HoursAndTwelveFifty().WithHoursWorked(100m).Generate();

        // Act
        decimal basePay     = payslip.BasePay;
        decimal overtimePay = payslip.OvertimePay;

        // Assert
        Assert.Equal(1_250.00m, basePay);
        Assert.Equal(0m, overtimePay);
    }

    [Fact]
    public void Hours_beyond_the_contractual_month_are_paid_at_125_percent() {
        // Arrange
        Payslip payslip = AnyPayslipAt35HoursAndTwelveFifty().WithHoursWorked(161.67m).Generate();

        // Act
        decimal overtimePay = payslip.OvertimePay;

        // Assert
        Assert.Equal(10m, payslip.OvertimeHours);
        Assert.Equal(156.25m, overtimePay);   // 10 h × 12.50 × 1.25
    }

    [Fact]
    public void Gross_pay_adds_the_bonuses_and_ignores_the_deductions() {
        // Arrange
        List<PayslipLine> anyDeductions = Any.ListOf(Any.PayslipLine().WithKind(LineKind.Deduction)).WithMaxCount(4).Generate();
        List<PayslipLine> lines = [
            Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(100.00m).Generate(),
            Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(50.50m).Generate(),
            .. anyDeductions
        ];
        Payslip payslip = AnyPayslipAt35HoursAndTwelveFifty().WithHoursWorked(100m).WithLines(lines).Generate();

        // Act
        decimal gross = payslip.GrossPay;

        // Assert
        Assert.Equal(1_400.50m, gross);
    }

    [Fact]
    public void Net_pay_is_gross_pay_less_the_deductions() {
        // Arrange
        List<PayslipLine> lines = [
            Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(230.10m).Generate(),
            Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(19.90m).Generate()
        ];
        Payslip payslip = AnyPayslipAt35HoursAndTwelveFifty().WithHoursWorked(100m).WithLines(lines).Generate();

        // Act
        decimal net = payslip.NetPay;

        // Assert
        Assert.Equal(1_000.00m, net);
    }

    [Fact]
    public void Net_pay_stops_at_zero_when_deductions_exceed_a_month_without_hours() {
        // Arrange
        PayslipLine anyDeduction = Any.PayslipLine().WithKind(LineKind.Deduction).Generate();
        Payslip     payslip      = Any.Payslip().WithHoursWorked(0m).WithLines([anyDeduction]).Generate();

        // Act
        decimal net = payslip.NetPay;

        // Assert
        Assert.Equal(0m, net);
    }

    [Fact]
    public void A_draft_payslip_takes_a_new_line() {
        // Arrange
        Payslip     anyDraft = Any.Payslip().Generate();
        PayslipLine anyLine  = Any.PayslipLine().Generate();
        int         before   = anyDraft.Lines.Count;

        // Act
        anyDraft.AddLine(anyLine);

        // Assert
        Assert.Equal(before + 1, anyDraft.Lines.Count);
        Assert.Same(anyLine, anyDraft.Lines[^1]);
    }

    [Fact]
    public void An_issued_payslip_refuses_a_new_line() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();
        PayslipLine anyLine = Any.PayslipLine().Generate();

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyPayslip.AddLine(anyLine));

        // Assert
        Assert.Equal("An issued payslip cannot be changed.", refusal.Message);
    }

    [Fact]
    public void A_payslip_cannot_be_issued_twice() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();

        // Act
        Action issueAgain = anyPayslip.Issue;

        // Assert
        Assert.Throws<InvalidOperationException>(issueAgain);
    }

    [Fact]
    public void A_payslip_cannot_cover_a_month_before_the_hiring_date() {
        // Arrange
        EmploymentContract anyContractHiredInMarch2024 = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 3, 1)).Generate();
        PayPeriod          february2024                = new PayPeriod(2024, 2);
        decimal            anyHours                    = Any.Decimal().Between(0m, 300m).WithScale(2).Generate();

        // Act
        Action build = () => new Payslip(anyContractHiredInMarch2024, february2024, anyHours, []);

        // Assert
        Assert.Throws<ArgumentException>("period", build);
    }

}
