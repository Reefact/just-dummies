using JustDummies;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests.Regular;

public sealed class EmploymentContractTests {

    private static readonly DateOnly LatestDummyHiring = new DateOnly(2090, 12, 31);

    public static TheoryData<ContractType, int> AcceptedWeeklyHours => new TheoryData<ContractType, int> {
        { ContractType.Permanent, 1 },
        { ContractType.Permanent, 48 },
        { ContractType.FixedTerm, 1 },
        { ContractType.FixedTerm, 48 },
        { ContractType.Apprenticeship, 1 },
        { ContractType.Apprenticeship, 35 },
        { ContractType.Internship, 1 },
        { ContractType.Internship, 35 }
    };

    public static TheoryData<ContractType, int> RefusedWeeklyHours => new TheoryData<ContractType, int> {
        { ContractType.Permanent, 0 },
        { ContractType.Permanent, 49 },
        { ContractType.Permanent, -5 },
        { ContractType.FixedTerm, 0 },
        { ContractType.FixedTerm, 49 },
        { ContractType.Apprenticeship, 0 },
        { ContractType.Apprenticeship, 36 },
        { ContractType.Apprenticeship, 48 },
        { ContractType.Internship, 0 },
        { ContractType.Internship, 36 },
        { ContractType.Internship, 48 }
    };

    public static TheoryData<int, decimal> ContractualHours => new TheoryData<int, decimal> {
        { 1, 4.33m },
        { 24, 104.00m },
        { 35, 151.67m },
        { 37, 160.33m },
        { 39, 169.00m },
        { 40, 173.33m },
        { 48, 208.00m }
    };

    public static TheoryData<DateOnly, DateOnly, int> Seniority => new TheoryData<DateOnly, DateOnly, int> {
        { new DateOnly(2020, 6, 15), new DateOnly(2020, 6, 15), 0 },
        { new DateOnly(2020, 6, 15), new DateOnly(2021, 6, 14), 0 },
        { new DateOnly(2020, 6, 15), new DateOnly(2021, 6, 15), 1 },
        { new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 14), 2 },
        { new DateOnly(2020, 6, 15), new DateOnly(2023, 6, 15), 3 },
        { new DateOnly(2020, 6, 15), new DateOnly(2030, 6, 15), 10 },
        { new DateOnly(2020, 12, 31), new DateOnly(2021, 1, 1), 0 },
        { new DateOnly(2020, 1, 1), new DateOnly(2020, 12, 31), 0 },
        { new DateOnly(2000, 2, 29), new DateOnly(2001, 2, 27), 0 },
        { new DateOnly(2000, 2, 29), new DateOnly(2001, 2, 28), 1 },
        { new DateOnly(2000, 2, 29), new DateOnly(2004, 2, 29), 4 },
        { new DateOnly(2020, 6, 15), new DateOnly(2020, 6, 14), 0 },
        { new DateOnly(2020, 6, 15), new DateOnly(2001, 1, 1), 0 }
    };

    [Fact]
    public void A_permanent_contract_keeps_its_terms() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();

        // Act
        EmploymentContract contract = new EmploymentContract(anyEmployee, ContractType.Permanent, new DateOnly(2022, 9, 1), null, new HourlyRate(23.40m), 39);

        // Assert
        Assert.Same(anyEmployee, contract.Employee);
        Assert.Equal(ContractType.Permanent, contract.Type);
        Assert.Equal(new DateOnly(2022, 9, 1), contract.HiredOn);
        Assert.Null(contract.EndsOn);
        Assert.Equal(23.40m, contract.Rate.Value);
        Assert.Equal(39, contract.WeeklyHours);
    }

    [Fact]
    public void A_fixed_term_contract_keeps_its_end_date() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();

        // Act
        EmploymentContract contract = new EmploymentContract(anyEmployee, ContractType.FixedTerm, new DateOnly(2024, 1, 15), new DateOnly(2024, 7, 14), anyRate, 35);

        // Assert
        Assert.Equal(new DateOnly(2024, 7, 14), contract.EndsOn);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_that_is_not_permanent_has_an_end_date(ContractType type) {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmploymentContract(anyEmployee, type, anyHiredOn, null, anyRate, 30));

        // Assert
        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_permanent_contract_has_no_end_date() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmploymentContract(anyEmployee, ContractType.Permanent, anyHiredOn, anyHiredOn.AddDays(30), anyRate, 30));

        // Assert
        Assert.Equal("endsOn", exception.ParamName);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_cannot_end_on_the_day_it_starts(ContractType type) {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmploymentContract(anyEmployee, type, anyHiredOn, anyHiredOn, anyRate, 30));

        // Assert
        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_cannot_end_before_it_starts() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate.AddDays(400), LatestDummyHiring).Generate();
        DateOnly   anyEarlierEnd = Any.DateOnly().Between(anyHiredOn.AddDays(-400), anyHiredOn.AddDays(-1)).Generate();

        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new EmploymentContract(anyEmployee, ContractType.FixedTerm, anyHiredOn, anyEarlierEnd, anyRate, 30));

        // Assert
        Assert.Equal("endsOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_may_end_the_day_after_it_starts() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();

        // Act
        EmploymentContract contract = new EmploymentContract(anyEmployee, ContractType.FixedTerm, new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 1), anyRate, 30);

        // Assert
        Assert.Equal(new DateOnly(2025, 1, 1), contract.EndsOn);
    }

    [Fact]
    public void A_contract_cannot_start_before_1970() {
        // Arrange
        EmployeeId anyEmployee   = Any.EmployeeId().Generate();
        HourlyRate anyRate       = Any.HourlyRate().Generate();
        DateOnly   anyEarlyHiring = Any.DateOnly().Before(EmploymentContract.EarliestHiringDate).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(anyEmployee, ContractType.Permanent, anyEarlyHiring, null, anyRate, 30));

        // Assert
        Assert.Equal("hiredOn", exception.ParamName);
    }

    [Fact]
    public void A_contract_may_start_on_the_first_day_of_1970() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();

        // Act
        EmploymentContract contract = new EmploymentContract(anyEmployee, ContractType.Permanent, new DateOnly(1970, 1, 1), null, anyRate, 30);

        // Assert
        Assert.Equal(new DateOnly(1970, 1, 1), contract.HiredOn);
        Assert.Equal(contract.HiredOn, EmploymentContract.EarliestHiringDate);
    }

    [Fact]
    public void A_contract_of_an_undefined_type_is_refused() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(anyEmployee, (ContractType)42, anyHiredOn, null, anyRate, 30));

        // Assert
        Assert.Equal("type", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(AcceptedWeeklyHours))]
    public void Weekly_hours_up_to_the_ceiling_of_the_contract_type_are_accepted(ContractType type, int weeklyHours) {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();
        DateOnly?  anyEnd      = type == ContractType.Permanent ? null : anyHiredOn.AddDays(Any.Int32().Between(1, 365).Generate());

        // Act
        EmploymentContract contract = new EmploymentContract(anyEmployee, type, anyHiredOn, anyEnd, anyRate, weeklyHours);

        // Assert
        Assert.Equal(weeklyHours, contract.WeeklyHours);
    }

    [Theory]
    [MemberData(nameof(RefusedWeeklyHours))]
    public void Weekly_hours_outside_the_range_of_the_contract_type_are_refused(ContractType type, int weeklyHours) {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, LatestDummyHiring).Generate();
        DateOnly?  anyEnd      = type == ContractType.Permanent ? null : anyHiredOn.AddDays(Any.Int32().Between(1, 365).Generate());

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(anyEmployee, type, anyHiredOn, anyEnd, anyRate, weeklyHours));

        // Assert
        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Fact]
    public void A_null_employee_is_refused() {
        // Arrange
        HourlyRate anyRate = Any.HourlyRate().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new EmploymentContract(null!, ContractType.Permanent, new DateOnly(2020, 1, 1), null, anyRate, 35));

        // Assert
        Assert.Equal("employee", exception.ParamName);
    }

    [Fact]
    public void A_null_rate_is_refused() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();

        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new EmploymentContract(anyEmployee, ContractType.Permanent, new DateOnly(2020, 1, 1), null, null!, 35));

        // Assert
        Assert.Equal("rate", exception.ParamName);
    }

    [Fact]
    public void The_terms_are_checked_before_the_employee_and_the_rate_are_looked_at() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(null!, ContractType.Permanent, new DateOnly(2020, 1, 1), null, null!, 49));

        // Assert
        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Fact]
    public void The_type_is_checked_before_everything_else() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(null!, (ContractType)7, new DateOnly(1960, 1, 1), new DateOnly(1950, 1, 1), null!, 0));

        // Assert
        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void The_hiring_date_is_checked_before_the_weekly_hours() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(null!, ContractType.Permanent, new DateOnly(1969, 12, 31), null, null!, 0));

        // Assert
        Assert.Equal("hiredOn", exception.ParamName);
    }

    [Fact]
    public void The_weekly_hours_are_checked_before_the_end_date() {
        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new EmploymentContract(null!, ContractType.Permanent, new DateOnly(2020, 1, 1), new DateOnly(2019, 1, 1), null!, 0));

        // Assert
        Assert.Equal("weeklyHours", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(ContractualHours))]
    public void The_monthly_contractual_hours_are_the_weekly_hours_spread_over_a_month(int weeklyHours, decimal expected) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithWeeklyHours(weeklyHours).WithType(ContractType.Permanent).Generate();

        // Act
        decimal monthlyHours = contract.MonthlyContractualHours;

        // Assert
        Assert.Equal(expected, monthlyHours);
    }

    [Theory]
    [MemberData(nameof(Seniority))]
    public void Completed_years_of_service_count_whole_anniversaries(DateOnly hiredOn, DateOnly asOf, int expected) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(hiredOn).Generate();

        // Act
        int years = contract.CompletedYearsOfService(asOf);

        // Assert
        Assert.Equal(expected, years);
    }

}
