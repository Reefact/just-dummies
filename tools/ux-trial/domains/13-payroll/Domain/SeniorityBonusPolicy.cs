namespace Payroll.Domain;

/// <summary>A seniority bonus, as a share of base pay, by completed years of service.</summary>
public static class SeniorityBonusPolicy {

    public static decimal RateFor(EmploymentContract contract, DateOnly asOf) {
        ArgumentNullException.ThrowIfNull(contract);
        if (contract.Type is ContractType.Apprenticeship or ContractType.Internship) {
            return 0m;
        }

        int years = contract.CompletedYearsOfService(asOf);
        return years switch {
            >= 10 => 0.06m,
            >= 3  => 0.03m,
            _     => 0m
        };
    }

    public static PayslipLine? BonusLineFor(Payslip payslip) {
        ArgumentNullException.ThrowIfNull(payslip);
        decimal rate   = RateFor(payslip.Contract, payslip.Period.LastDay);
        decimal amount = decimal.Round(payslip.BasePay * rate, 2, MidpointRounding.AwayFromZero);

        return amount > 0m ? new PayslipLine("Seniority bonus", LineKind.Bonus, amount) : null;
    }

}
