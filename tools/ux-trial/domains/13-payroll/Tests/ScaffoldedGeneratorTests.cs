using JustDummies;
using JustDummies.Xunit;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

/// <summary>Harness: each scaffolded generator, as resolved by hand, must never trip its type's constructor.</summary>
[Reproducible]
public sealed class ScaffoldedGeneratorTests {

    private const int Draws = 500;

    private static void AlwaysBuilds<T>(IAny<T> generator) {
        for (int draw = 0; draw < Draws; draw++) {
            generator.Generate();
        }
    }

    [Fact]
    public void Scaffolded_AnyEmployeeId_always_builds_a_valid_EmployeeId() {
        AlwaysBuilds(Any.EmployeeId());
    }

    [Fact]
    public void Scaffolded_AnyHourlyRate_always_builds_a_valid_HourlyRate() {
        AlwaysBuilds(Any.HourlyRate());
    }

    [Fact]
    public void Scaffolded_AnyPayPeriod_always_builds_a_valid_PayPeriod() {
        AlwaysBuilds(Any.PayPeriod());
    }

    [Fact]
    public void Scaffolded_AnyPayslipLine_always_builds_a_valid_PayslipLine() {
        AlwaysBuilds(Any.PayslipLine());
    }

    [Fact]
    public void Scaffolded_AnyEmploymentContract_always_builds_a_valid_EmploymentContract() {
        AlwaysBuilds(Any.EmploymentContract());
    }

    [Fact]
    public void Scaffolded_AnyPayslip_always_builds_a_valid_Payslip() {
        AlwaysBuilds(Any.Payslip());
    }

}
