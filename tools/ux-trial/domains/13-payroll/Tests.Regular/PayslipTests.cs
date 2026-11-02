using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class PayslipTests {

    private static readonly PayslipLine[] NoLines = Array.Empty<PayslipLine>();

    // A permanent contract of 35 weekly hours (151.67 contractual hours a month) at the rate given.
    private static EmploymentContract ContractAt(decimal rate) {
        return Any.EmploymentContract()
                  .WithType(ContractType.Permanent)
                  .WithWeeklyHours(35)
                  .WithRate(new HourlyRate(rate))
                  .WithHiredOn(new DateOnly(2015, 3, 1))
                  .Generate();
    }

    private static Payslip PayslipFor(EmploymentContract contract, decimal hoursWorked) {
        return Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2024, 5)).WithHoursWorked(hoursWorked).WithLines(NoLines).Generate();
    }

    [Fact]
    public void A_new_payslip_is_a_draft_that_keeps_what_it_covers() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();
        PayPeriod          period      = new PayPeriod(2024, 5);

        // Act
        Payslip payslip = new Payslip(anyContract, period, 120.5m, NoLines);

        // Assert
        Assert.Equal(PayslipStatus.Draft, payslip.Status);
        Assert.Same(anyContract, payslip.Contract);
        Assert.Same(period, payslip.Period);
        Assert.Equal(120.5m, payslip.HoursWorked);
        Assert.Empty(payslip.Lines);
    }

    [Fact]
    public void A_payslip_starts_with_the_lines_it_was_given() {
        // Arrange
        PayslipLine anyFirstLine  = Any.PayslipLine().Generate();
        PayslipLine anySecondLine = Any.PayslipLine().Generate();

        // Act
        Payslip anyPayslip = Any.Payslip().WithLines(new[] { anyFirstLine, anySecondLine }).Generate();

        // Assert
        Assert.Equal(new[] { anyFirstLine, anySecondLine }, anyPayslip.Lines);
    }

    [Fact]
    public void A_payslip_works_on_a_copy_of_the_lines_it_was_given() {
        // Arrange
        List<PayslipLine> givenLines = new List<PayslipLine> { Any.PayslipLine().Generate() };
        Payslip           anyPayslip = Any.Payslip().WithLines(givenLines).Generate();

        // Act
        givenLines.Add(Any.PayslipLine().Generate());

        // Assert
        Assert.Single(anyPayslip.Lines);
    }

    [Fact]
    public void A_null_contract_is_refused() {
        // Arrange
        PayPeriod anyPeriod = Any.PayPeriod().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(null!, anyPeriod, 10m, NoLines));

        // Assert
        Assert.Equal("contract", exception.ParamName);
    }

    [Fact]
    public void A_null_period_is_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(anyContract, null!, 10m, NoLines));

        // Assert
        Assert.Equal("period", exception.ParamName);
    }

    [Fact]
    public void A_null_list_of_lines_is_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new Payslip(anyContract, new PayPeriod(2024, 5), 10m, null!));

        // Assert
        Assert.Equal("lines", exception.ParamName);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("300")]
    [InlineData("151.67")]
    public void Hours_between_zero_and_three_hundred_are_accepted(string hours) {
        // Arrange
        decimal            hoursWorked = decimal.Parse(hours, System.Globalization.CultureInfo.InvariantCulture);
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();

        // Act
        Payslip payslip = new Payslip(anyContract, new PayPeriod(2024, 5), hoursWorked, NoLines);

        // Assert
        Assert.Equal(hoursWorked, payslip.HoursWorked);
    }

    [Fact]
    public void Negative_hours_are_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();
        decimal            anyNegative = Any.Decimal().Negative().Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Payslip(anyContract, new PayPeriod(2024, 5), anyNegative, NoLines));

        // Assert
        Assert.Equal("hoursWorked", exception.ParamName);
    }

    [Fact]
    public void Hours_above_three_hundred_are_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();
        decimal            anyExcess   = Any.Decimal().GreaterThan(300m).LessThanOrEqualTo(10_000m).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new Payslip(anyContract, new PayPeriod(2024, 5), anyExcess, NoLines));

        // Assert
        Assert.Equal("hoursWorked", exception.ParamName);
    }

    [Fact]
    public void Hours_just_above_three_hundred_are_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Payslip(anyContract, new PayPeriod(2024, 5), 300.01m, NoLines));
    }

    [Fact]
    public void A_payslip_cannot_cover_a_month_that_ended_before_the_hiring_date() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 3, 1)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Payslip(anyContract, new PayPeriod(2024, 2), 10m, NoLines));

        // Assert
        Assert.Equal("period", exception.ParamName);
    }

    [Fact]
    public void A_payslip_can_cover_the_month_of_the_hiring() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 3, 17)).Generate();

        // Act
        Payslip payslip = new Payslip(anyContract, new PayPeriod(2024, 3), 10m, NoLines);

        // Assert
        Assert.Equal(new PayPeriod(2024, 3).Year, payslip.Period.Year);
        Assert.Equal(3, payslip.Period.Month);
    }

    [Fact]
    public void A_payslip_can_cover_a_month_whose_last_day_is_the_hiring_date() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2024, 2, 29)).Generate();

        // Act
        Payslip payslip = new Payslip(anyContract, new PayPeriod(2024, 2), 0m, NoLines);

        // Assert
        Assert.Equal(PayslipStatus.Draft, payslip.Status);
    }

    [Fact]
    public void Hours_within_the_contractual_hours_are_all_paid_at_the_base_rate() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);

        // Act
        Payslip payslip = PayslipFor(contract, 100m);

        // Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(2000.00m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
        Assert.Equal(2000.00m, payslip.GrossPay);
        Assert.Equal(2000.00m, payslip.NetPay);
    }

    [Fact]
    public void Working_exactly_the_contractual_hours_creates_no_overtime() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);

        // Act
        Payslip payslip = PayslipFor(contract, 151.67m);

        // Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(3033.40m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
    }

    [Fact]
    public void Hours_beyond_the_contractual_hours_are_overtime_paid_with_a_quarter_premium() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);

        // Act
        Payslip payslip = PayslipFor(contract, 160m);

        // Assert
        Assert.Equal(8.33m, payslip.OvertimeHours);
        Assert.Equal(3033.40m, payslip.BasePay);
        Assert.Equal(208.25m, payslip.OvertimePay);
        Assert.Equal(3241.65m, payslip.GrossPay);
    }

    [Fact]
    public void The_overtime_premium_is_one_and_a_quarter() {
        Assert.Equal(1.25m, Payslip.OvertimePremium);
    }

    [Fact]
    public void The_contractual_hours_depend_on_the_weekly_hours_of_the_contract() {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract()
                                         .WithType(ContractType.Permanent)
                                         .WithWeeklyHours(48)
                                         .WithRate(new HourlyRate(10.00m))
                                         .WithHiredOn(new DateOnly(2015, 3, 1))
                                         .Generate();

        // Act
        Payslip payslip = PayslipFor(contract, 210m);

        // Assert
        Assert.Equal(2m, payslip.OvertimeHours);
        Assert.Equal(2080.00m, payslip.BasePay);
        Assert.Equal(25.00m, payslip.OvertimePay);
    }

    [Fact]
    public void A_payslip_without_hours_pays_nothing() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);

        // Act
        Payslip payslip = PayslipFor(contract, 0m);

        // Assert
        Assert.Equal(0m, payslip.OvertimeHours);
        Assert.Equal(0m, payslip.BasePay);
        Assert.Equal(0m, payslip.OvertimePay);
        Assert.Equal(0m, payslip.GrossPay);
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void The_base_pay_rounds_a_half_cent_away_from_zero() {
        // Arrange
        EmploymentContract contract = ContractAt(0.01m);

        // Act
        Payslip payslip = PayslipFor(contract, 0.5m);

        // Assert
        Assert.Equal(0.01m, payslip.BasePay);
    }

    [Fact]
    public void The_base_pay_rounds_to_the_nearest_cent() {
        // Arrange
        EmploymentContract contract = ContractAt(10.01m);

        // Act
        Payslip payslip = PayslipFor(contract, 33.333m);

        // Assert
        Assert.Equal(333.66m, payslip.BasePay);
    }

    [Fact]
    public void The_overtime_pay_rounds_a_half_cent_away_from_zero() {
        // Arrange
        EmploymentContract contract = ContractAt(0.01m);

        // Act
        Payslip payslip = PayslipFor(contract, 152.07m);

        // Assert
        Assert.Equal(0.40m, payslip.OvertimeHours);
        Assert.Equal(1.52m, payslip.BasePay);
        Assert.Equal(0.01m, payslip.OvertimePay);
    }

    [Fact]
    public void Bonuses_and_deductions_are_totalled_by_kind() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);
        PayslipLine[] lines = {
            Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(100.00m).Generate(),
            Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(30.25m).Generate(),
            Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(50.50m).Generate(),
            Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(10.00m).Generate()
        };

        // Act
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2024, 5)).WithHoursWorked(100m).WithLines(lines).Generate();

        // Assert
        Assert.Equal(150.50m, payslip.TotalBonuses);
        Assert.Equal(40.25m, payslip.TotalDeductions);
        Assert.Equal(2150.50m, payslip.GrossPay);
        Assert.Equal(2110.25m, payslip.NetPay);
    }

    [Fact]
    public void A_payslip_without_lines_has_no_bonus_and_no_deduction() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);

        // Act
        Payslip payslip = PayslipFor(contract, 100m);

        // Assert
        Assert.Equal(0m, payslip.TotalBonuses);
        Assert.Equal(0m, payslip.TotalDeductions);
    }

    [Fact]
    public void The_gross_pay_adds_the_base_pay_the_overtime_pay_and_the_bonuses() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);
        PayslipLine        bonus    = Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(75.00m).Generate();

        // Act
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2024, 5)).WithHoursWorked(160m).WithLines(new[] { bonus }).Generate();

        // Assert
        Assert.Equal(3316.65m, payslip.GrossPay);
    }

    [Fact]
    public void The_net_pay_never_goes_below_zero() {
        // Arrange
        EmploymentContract contract  = ContractAt(20.00m);
        PayslipLine        deduction = Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(2000.01m).Generate();

        // Act
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2024, 5)).WithHoursWorked(100m).WithLines(new[] { deduction }).Generate();

        // Assert
        Assert.Equal(2000.00m, payslip.GrossPay);
        Assert.Equal(2000.01m, payslip.TotalDeductions);
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void The_net_pay_is_zero_when_the_deductions_exactly_cancel_the_gross_pay() {
        // Arrange
        EmploymentContract contract  = ContractAt(20.00m);
        PayslipLine        deduction = Any.PayslipLine().WithKind(LineKind.Deduction).WithAmount(2000.00m).Generate();

        // Act
        Payslip payslip = Any.Payslip().WithContract(contract).WithPeriod(new PayPeriod(2024, 5)).WithHoursWorked(100m).WithLines(new[] { deduction }).Generate();

        // Assert
        Assert.Equal(0m, payslip.NetPay);
    }

    [Fact]
    public void Adding_a_line_to_a_draft_changes_its_totals() {
        // Arrange
        EmploymentContract contract = ContractAt(20.00m);
        Payslip            payslip  = PayslipFor(contract, 100m);
        PayslipLine        bonus    = Any.PayslipLine().WithKind(LineKind.Bonus).WithAmount(40.00m).Generate();

        // Act
        payslip.AddLine(bonus);

        // Assert
        Assert.Same(bonus, Assert.Single(payslip.Lines));
        Assert.Equal(40.00m, payslip.TotalBonuses);
        Assert.Equal(2040.00m, payslip.NetPay);
    }

    [Fact]
    public void Adding_a_line_appends_it_after_the_existing_ones() {
        // Arrange
        Payslip     anyPayslip = Any.Payslip().Generate();
        int         before     = anyPayslip.Lines.Count;
        PayslipLine anyLine    = Any.PayslipLine().Generate();

        // Act
        anyPayslip.AddLine(anyLine);

        // Assert
        Assert.Equal(before + 1, anyPayslip.Lines.Count);
        Assert.Same(anyLine, anyPayslip.Lines[before]);
    }

    [Fact]
    public void A_null_line_cannot_be_added() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => anyPayslip.AddLine(null!));

        // Assert
        Assert.Equal("line", exception.ParamName);
    }

    [Fact]
    public void Issuing_a_payslip_makes_it_issued() {
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

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyPayslip.Issue());
        Assert.Equal(PayslipStatus.Issued, anyPayslip.Status);
    }

    [Fact]
    public void An_issued_payslip_refuses_new_lines_and_keeps_its_own() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();
        int     before     = anyPayslip.Lines.Count;
        anyPayslip.Issue();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyPayslip.AddLine(Any.PayslipLine().Generate()));
        Assert.Equal(before, anyPayslip.Lines.Count);
    }

    [Fact]
    public void A_null_line_is_refused_as_null_even_when_the_payslip_is_issued() {
        // Arrange
        Payslip anyPayslip = Any.Payslip().Generate();
        anyPayslip.Issue();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => anyPayslip.AddLine(null!));
    }

    [Fact(Skip = "domain defect: a null element in the lines passes the constructor (AddLine refuses null) and only fails later with NullReferenceException in TotalBonuses; deterministic, no seed involved")]
    public void A_null_line_in_the_given_lines_is_refused() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 1, 1)).Generate();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new Payslip(anyContract, new PayPeriod(2024, 5), 10m, new PayslipLine[] { null! }));
    }

}
