namespace Payroll.Tests.Expert;

public sealed class EmploymentContractTests {

    // ---- construction ----------------------------------------------------------------------------------------

    [Fact]
    public void A_contract_keeps_the_terms_it_was_signed_with() {
        // Arrange
        EmployeeId employee = new EmployeeId("EMP-000042");
        HourlyRate rate     = new HourlyRate(15.50m);

        // Act
        EmploymentContract contract = new EmploymentContract(employee, ContractType.FixedTerm, new DateOnly(2024, 3, 1), new DateOnly(2024, 9, 1), rate, 35);

        // Assert
        Assert.Same(employee, contract.Employee);
        Assert.Equal(ContractType.FixedTerm, contract.Type);
        Assert.Equal(new DateOnly(2024, 3, 1), contract.HiredOn);
        Assert.Equal(new DateOnly(2024, 9, 1), contract.EndsOn);
        Assert.Same(rate, contract.Rate);
        Assert.Equal(35, contract.WeeklyHours);
    }

    // ---- type --------------------------------------------------------------------------------------------------

    [Fact]
    public void A_contract_of_an_undefined_type_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.EmploymentContract().WithType((ContractType)99).Generate());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    // ---- hiring date -------------------------------------------------------------------------------------------

    [Fact]
    public void A_hiring_date_on_the_earliest_hiring_date_is_accepted() {
        // Act
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(EmploymentContract.EarliestHiringDate).Generate();

        // Assert
        Assert.Equal(new DateOnly(1970, 1, 1), contract.HiredOn);
    }

    [Fact]
    public void A_hiring_date_the_day_before_the_earliest_hiring_date_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(1969, 12, 31)).Generate());

        // Assert
        Assert.Equal("hiredOn", refusal.ParamName);
    }

    [Fact]
    public void The_earliest_hiring_date_is_the_first_of_january_1970() {
        Assert.Equal(new DateOnly(1970, 1, 1), EmploymentContract.EarliestHiringDate);
    }

    // ---- weekly hours -----------------------------------------------------------------------------------------

    [Theory]
    [InlineData(ContractType.Permanent, 1)]
    [InlineData(ContractType.Permanent, 48)]
    [InlineData(ContractType.FixedTerm, 1)]
    [InlineData(ContractType.FixedTerm, 48)]
    [InlineData(ContractType.Apprenticeship, 1)]
    [InlineData(ContractType.Apprenticeship, 35)]
    [InlineData(ContractType.Internship, 1)]
    [InlineData(ContractType.Internship, 35)]
    public void Weekly_hours_at_the_edge_of_what_the_contract_type_allows_are_accepted(ContractType type, int weeklyHours) {
        // Act
        EmploymentContract contract = Any.EmploymentContract().WithType(type).WithWeeklyHours(weeklyHours).Generate();

        // Assert
        Assert.Equal(weeklyHours, contract.WeeklyHours);
    }

    [Theory]
    [InlineData(ContractType.Permanent, 0)]
    [InlineData(ContractType.Permanent, 49)]
    [InlineData(ContractType.Permanent, -1)]
    [InlineData(ContractType.FixedTerm, 0)]
    [InlineData(ContractType.FixedTerm, 49)]
    [InlineData(ContractType.Apprenticeship, 0)]
    [InlineData(ContractType.Apprenticeship, 36)]
    [InlineData(ContractType.Internship, 0)]
    [InlineData(ContractType.Internship, 36)]
    public void Weekly_hours_outside_what_the_contract_type_allows_are_refused(ContractType type, int weeklyHours) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => Any.EmploymentContract().WithType(type).WithWeeklyHours(weeklyHours).Generate());

        // Assert
        Assert.Equal("weeklyHours", refusal.ParamName);
    }

    // ---- end date ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_permanent_contract_with_an_end_date_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(2024, 1, 15)).WithEndsOn(new DateOnly(2025, 1, 15)).Generate());

        // Assert
        Assert.Equal("endsOn", refusal.ParamName);
        Assert.Contains("permanent", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_that_is_not_permanent_without_an_end_date_is_refused(ContractType type) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.EmploymentContract().WithType(type).WithoutEndsOn().Generate());

        // Assert
        Assert.Equal("endsOn", refusal.ParamName);
        Assert.Contains("end date", refusal.Message);
    }

    [Theory]
    [InlineData(ContractType.FixedTerm)]
    [InlineData(ContractType.Apprenticeship)]
    [InlineData(ContractType.Internship)]
    public void A_contract_ending_the_day_it_starts_is_refused(ContractType type) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.EmploymentContract().WithType(type).WithHiredOn(new DateOnly(2024, 5, 10)).WithEndsOn(new DateOnly(2024, 5, 10)).Generate());

        // Assert
        Assert.Equal("endsOn", refusal.ParamName);
        Assert.Contains("ends after it starts", refusal.Message);
    }

    [Fact]
    public void A_contract_ending_before_it_starts_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.EmploymentContract().WithType(ContractType.FixedTerm).WithHiredOn(new DateOnly(2024, 5, 10)).WithEndsOn(new DateOnly(2024, 5, 9)).Generate());

        // Assert
        Assert.Equal("endsOn", refusal.ParamName);
        Assert.Contains("ends after it starts", refusal.Message);
    }

    [Fact]
    public void A_contract_ending_the_day_after_it_starts_is_accepted() {
        // Act
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.FixedTerm).WithHiredOn(new DateOnly(2024, 5, 10)).WithEndsOn(new DateOnly(2024, 5, 11)).Generate();

        // Assert
        Assert.Equal(new DateOnly(2024, 5, 11), contract.EndsOn);
    }

    // ---- the order in which the rules are checked --------------------------------------------------------------

    [Fact]
    public void An_undefined_type_is_reported_before_a_hiring_date_that_is_too_early() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.EmploymentContract().WithType((ContractType)99).WithHiredOn(new DateOnly(1960, 1, 1)).Generate());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void A_hiring_date_that_is_too_early_is_reported_before_weekly_hours_out_of_range() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.EmploymentContract().WithType(ContractType.Permanent).WithHiredOn(new DateOnly(1960, 1, 1)).WithWeeklyHours(0).Generate());

        // Assert
        Assert.Equal("hiredOn", refusal.ParamName);
    }

    [Fact]
    public void Weekly_hours_out_of_range_are_reported_before_a_missing_end_date() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.EmploymentContract().WithType(ContractType.FixedTerm).WithWeeklyHours(0).WithoutEndsOn().Generate());

        // Assert
        Assert.Equal("weeklyHours", refusal.ParamName);
    }

    [Fact]
    public void A_missing_employee_is_reported_after_the_other_rules() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => Any.EmploymentContract().WithEmployee((EmployeeId)null!).WithType((ContractType)99).Generate());

        // Assert
        Assert.Equal("type", refusal.ParamName);
    }

    [Fact]
    public void A_missing_employee_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.EmploymentContract().WithEmployee((EmployeeId)null!).Generate());

        // Assert
        Assert.Equal("employee", refusal.ParamName);
    }

    [Fact]
    public void A_missing_rate_is_refused() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => Any.EmploymentContract().WithRate((HourlyRate)null!).Generate());

        // Assert
        Assert.Equal("rate", refusal.ParamName);
    }

    // ---- contractual hours ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(35, 151.67)]   // 151.666...
    [InlineData(39, 169.00)]
    [InlineData(40, 173.33)]   // 173.333...
    [InlineData(48, 208.00)]
    [InlineData(24, 104.00)]
    [InlineData(1, 4.33)]      // 4.333...
    [InlineData(37, 160.33)]   // 160.333...
    [InlineData(10, 43.33)]    // 43.333...
    public void A_month_is_paid_at_the_weekly_hours_times_52_over_12_to_the_hundredth(int weeklyHours, double expected) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithType(ContractType.Permanent).WithWeeklyHours(weeklyHours).Generate();

        // Act
        decimal monthlyContractualHours = contract.MonthlyContractualHours;

        // Assert
        Assert.Equal((decimal)expected, monthlyContractualHours);
    }

    // ---- seniority ---------------------------------------------------------------------------------------------

    public static TheoryData<DateOnly, DateOnly, int> CompletedYears => new TheoryData<DateOnly, DateOnly, int> {
        { new DateOnly(2020, 3, 15), new DateOnly(2020, 3, 15), 0 },    // the day of the hiring
        { new DateOnly(2020, 3, 15), new DateOnly(2021, 3, 14), 0 },    // the day before the first anniversary
        { new DateOnly(2020, 3, 15), new DateOnly(2021, 3, 15), 1 },    // the first anniversary
        { new DateOnly(2020, 3, 15), new DateOnly(2030, 3, 14), 9 },
        { new DateOnly(2020, 3, 15), new DateOnly(2030, 3, 15), 10 },
        { new DateOnly(2020, 3, 15), new DateOnly(2019, 3, 15), 0 },    // before the hiring: never below zero
        { new DateOnly(2020, 3, 15), new DateOnly(2020, 3, 14), 0 },    // the day before the hiring
        { new DateOnly(2020, 12, 31), new DateOnly(2021, 1, 1), 0 },    // a new calendar year is not an anniversary
        { new DateOnly(2020, 2, 29), new DateOnly(2021, 2, 27), 0 },    // hired on a leap day: the anniversary falls on 28 February
        { new DateOnly(2020, 2, 29), new DateOnly(2021, 2, 28), 1 },
        { new DateOnly(2020, 2, 29), new DateOnly(2024, 2, 28), 3 },
        { new DateOnly(2020, 2, 29), new DateOnly(2024, 2, 29), 4 },
        { new DateOnly(1970, 1, 1), new DateOnly(2100, 12, 31), 130 }
    };

    [Theory]
    [MemberData(nameof(CompletedYears))]
    public void Completed_years_of_service_are_the_whole_anniversaries_elapsed(DateOnly hiredOn, DateOnly asOf, int expectedYears) {
        // Arrange
        EmploymentContract contract = Any.EmploymentContract().WithHiredOn(hiredOn).Generate();

        // Act
        int years = contract.CompletedYearsOfService(asOf);

        // Assert
        Assert.Equal(expectedYears, years);
    }

}
