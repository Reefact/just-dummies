using Payroll.Domain;

namespace Payroll.Tests.Expert.Generators;

// Written by hand, beside the scaffolded file: `any generate EmploymentContract --force` leaves it alone.
public sealed partial class AnyEmploymentContract {

    /// <summary>A contract that is neither an apprenticeship nor an internship, the two types the seniority policy treats apart.</summary>
    public AnyEmploymentContract NotATraineeship() {
        return WithType(type => type.Except(ContractType.Apprenticeship, ContractType.Internship));
    }

}
