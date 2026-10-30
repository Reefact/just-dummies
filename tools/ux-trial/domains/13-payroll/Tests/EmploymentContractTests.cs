using JustDummies;
using JustDummies.Xunit;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

[Reproducible]
public sealed class EmploymentContractTests {

    [Fact]
    public void A_fixed_term_contract_needs_an_end_date() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(EmploymentContract.EarliestHiringDate, new DateOnly(2100, 12, 31)).Generate();
        HourlyRate anyRate     = Any.HourlyRate().Generate();
        int        anyHours    = Any.Int32().Between(1, 48).Generate();

        // Act
        Action build = () => new EmploymentContract(anyEmployee, ContractType.FixedTerm, anyHiredOn, endsOn: null, anyRate, anyHours);

        // Assert
        Assert.Throws<ArgumentException>("endsOn", build);
    }

    [Fact]
    public void An_apprentice_cannot_be_contracted_for_36_hours_a_week() {
        // Arrange
        EmployeeId anyEmployee = Any.EmployeeId().Generate();
        DateOnly   anyHiredOn  = Any.DateOnly().Between(new DateOnly(2000, 1, 1), new DateOnly(2090, 1, 1)).Generate();
        DateOnly   anyEndsOn   = anyHiredOn.AddDays(Any.Int32().Between(1, 1_095).Generate());
        HourlyRate anyRate     = Any.HourlyRate().Generate();

        // Act
        Action build = () => new EmploymentContract(anyEmployee, ContractType.Apprenticeship, anyHiredOn, anyEndsOn, anyRate, weeklyHours: 36);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>("weeklyHours", build);
    }

    [Fact]
    public void Monthly_contractual_hours_are_weekly_hours_spread_over_the_year() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithWeeklyHours(35).Generate();

        // Act
        decimal monthly = anyContract.MonthlyContractualHours;

        // Assert
        Assert.Equal(151.67m, monthly);
    }

    [Fact]
    public void A_year_of_service_completes_on_the_anniversary_not_the_day_before() {
        // Arrange
        EmploymentContract anyContract = Any.EmploymentContract().WithHiredOn(new DateOnly(2016, 7, 1)).Generate();

        // Act
        int dayBefore   = anyContract.CompletedYearsOfService(new DateOnly(2026, 6, 30));
        int anniversary = anyContract.CompletedYearsOfService(new DateOnly(2026, 7, 1));

        // Assert
        Assert.Equal(9, dayBefore);
        Assert.Equal(10, anniversary);
    }

}
