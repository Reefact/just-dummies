using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Discovery;

public sealed class EmploymentContractTests {

    private static readonly DateOnly Hired = new DateOnly(2020, 3, 15);

    [Fact]
    public void A_permanent_contract_keeps_its_terms_and_has_no_end_date() {
        // Arrange
        EmployeeId employee = new EmployeeId("EMP-004217");
        HourlyRate rate     = new HourlyRate(18.40m);

        // Act
        EmploymentContract contract = new EmploymentContract(employee, ContractType.Permanent, Hired, null, rate, 39);

        // Assert
        Assert.Same(employee, contract.Employee);
        Assert.Equal(ContractType.Permanent, contract.Type);
        Assert.Equal(Hired, contract.HiredOn);
        Assert.Null(contract.EndsOn);
        Assert.Same(rate, contract.Rate);
        Assert.Equal(39, contract.WeeklyHours);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_that_is_not_permanent_keeps_its_end_date(ContractType type) {
        DateOnly endsOn = new DateOnly(2021, 3, 14);

        EmploymentContract contract = new EmploymentContract(Any.EmployeeId().Generate(), type, Hired, endsOn, Any.HourlyRate().Generate(), 20);

        Assert.Equal(endsOn, contract.EndsOn);
    }

    [Fact]
    public void The_earliest_hiring_date_is_the_first_of_january_1970() {
        Assert.Equal(new DateOnly(1970, 1, 1), EmploymentContract.EarliestHiringDate);
    }

    [Fact]
    public void A_contract_can_start_on_the_earliest_hiring_date() {
        EmploymentContract contract = new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, EmploymentContract.EarliestHiringDate,
                                                             null, Any.HourlyRate().Generate(), 35);

        Assert.Equal(EmploymentContract.EarliestHiringDate, contract.HiredOn);
    }

    [Fact]
    public void A_contract_cannot_start_before_1970() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, new DateOnly(1969, 12, 31), null, Any.HourlyRate().Generate(), 35));

        Assert.Equal("hiredOn", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public void An_undefined_contract_type_is_refused(int rawType) {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), (ContractType)rawType, Hired, null, Any.HourlyRate().Generate(), 35));

        Assert.Equal("type", exception.ParamName);
    }

    [Theory]
    [InlineData(ContractType.Permanent, 1)]
    [InlineData(ContractType.Permanent, 48)]
    [InlineData(ContractType.FixedTerm, 1)]
    [InlineData(ContractType.FixedTerm, 48)]
    [InlineData(ContractType.Apprenticeship, 1)]
    [InlineData(ContractType.Apprenticeship, 35)]
    [InlineData(ContractType.Internship, 1)]
    [InlineData(ContractType.Internship, 35)]
    public void Weekly_hours_up_to_the_maximum_of_the_type_are_accepted(ContractType type, int weeklyHours) {
        DateOnly? endsOn = type == ContractType.Permanent ? null : Hired.AddYears(1);

        EmploymentContract contract = new EmploymentContract(Any.EmployeeId().Generate(), type, Hired, endsOn, Any.HourlyRate().Generate(), weeklyHours);

        Assert.Equal(weeklyHours, contract.WeeklyHours);
    }

    [Theory]
    [InlineData(ContractType.Permanent, 0)]
    [InlineData(ContractType.Permanent, 49)]
    [InlineData(ContractType.Permanent, -5)]
    [InlineData(ContractType.FixedTerm, 0)]
    [InlineData(ContractType.FixedTerm, 49)]
    [InlineData(ContractType.Apprenticeship, 0)]
    [InlineData(ContractType.Apprenticeship, 36)]
    [InlineData(ContractType.Internship, 0)]
    [InlineData(ContractType.Internship, 36)]
    public void Weekly_hours_outside_one_to_the_maximum_of_the_type_are_refused(ContractType type, int weeklyHours) {
        DateOnly? endsOn = type == ContractType.Permanent ? null : Hired.AddYears(1);

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), type, Hired, endsOn, Any.HourlyRate().Generate(), weeklyHours));

        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Fact]
    public void A_permanent_contract_with_an_end_date_is_refused() {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, Hired, Hired.AddYears(1), Any.HourlyRate().Generate(), 35));

        Assert.Equal("endsOn", exception.ParamName);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_that_is_not_permanent_without_an_end_date_is_refused(ContractType type) {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), type, Hired, null, Any.HourlyRate().Generate(), 20));

        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_cannot_end_on_the_day_it_starts() {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.FixedTerm, Hired, Hired, Any.HourlyRate().Generate(), 35));

        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_cannot_end_before_it_starts() {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.FixedTerm, Hired, Hired.AddDays(-1), Any.HourlyRate().Generate(), 35));

        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_can_end_the_day_after_it_starts() {
        EmploymentContract contract = new EmploymentContract(Any.EmployeeId().Generate(), ContractType.FixedTerm, Hired, Hired.AddDays(1), Any.HourlyRate().Generate(), 35);

        Assert.Equal(Hired.AddDays(1), contract.EndsOn);
    }

    [Fact]
    public void A_missing_employee_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new EmploymentContract(null!, ContractType.Permanent, Hired, null, Any.HourlyRate().Generate(), 35));

        Assert.Equal("employee", exception.ParamName);
    }

    [Fact]
    public void A_missing_rate_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, Hired, null, null!, 35));

        Assert.Equal("rate", exception.ParamName);
    }

    // The order of the checks is documented: the rules on the values come first, the null checks last.
    [Fact]
    public void The_rules_on_the_values_are_checked_before_the_null_checks() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(null!, ContractType.Permanent, Hired, null, null!, 0));

        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Fact]
    public void The_type_is_checked_before_the_hiring_date() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), (ContractType)99, new DateOnly(1960, 1, 1), null, Any.HourlyRate().Generate(), 35));

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void The_hiring_date_is_checked_before_the_weekly_hours() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, new DateOnly(1960, 1, 1), null, Any.HourlyRate().Generate(), 0));

        Assert.Equal("hiredOn", exception.ParamName);
    }

    [Fact]
    public void The_weekly_hours_are_checked_before_the_end_date() {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new EmploymentContract(Any.EmployeeId().Generate(), ContractType.Permanent, Hired, Hired.AddYears(1), Any.HourlyRate().Generate(), 49));

        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Theory]
    [InlineData(1, "4.33")]
    [InlineData(24, "104")]
    [InlineData(35, "151.67")]
    [InlineData(37, "160.33")]
    [InlineData(39, "169")]
    [InlineData(48, "208")]
    public void Monthly_contractual_hours_are_weekly_hours_times_52_over_12_to_the_hundredth(int weeklyHours, string expected) {
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithWeeklyHours(weeklyHours).Generate();

        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), contract.MonthlyContractualHours);
    }

    [Theory]
    [InlineData("2020-03-15", 0)]
    [InlineData("2021-03-14", 0)]
    [InlineData("2021-03-15", 1)]
    [InlineData("2023-03-14", 2)]
    [InlineData("2023-03-15", 3)]
    [InlineData("2030-03-15", 10)]
    [InlineData("2019-03-15", 0)]
    [InlineData("1990-01-01", 0)]
    public void Completed_years_are_the_whole_anniversaries_elapsed_and_never_below_zero(string asOfText, int expected) {
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(Hired).Generate();
        DateOnly           asOf        = DateOnly.ParseExact(asOfText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, anyContract.CompletedYearsOfService(asOf));
    }

    [Theory]
    [InlineData("2021-02-27", 0)]
    [InlineData("2021-02-28", 1)]
    [InlineData("2024-02-28", 3)]
    [InlineData("2024-02-29", 4)]
    public void An_employee_hired_on_the_29th_of_february_reaches_the_anniversary_on_the_28th_in_other_years(string asOfText, int expected) {
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2020, 2, 29)).Generate();
        DateOnly           asOf        = DateOnly.ParseExact(asOfText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, anyContract.CompletedYearsOfService(asOf));
    }

}
