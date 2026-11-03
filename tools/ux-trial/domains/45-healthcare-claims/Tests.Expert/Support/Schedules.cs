using HealthcareClaims.Domain;

namespace HealthcareClaims.Tests.Expert.Support;

internal static class Schedules {

    /// <summary>A guarantee table of the given percentages; a category left out is not covered.</summary>
    public static ReimbursementSchedule Of(params (ReimbursementCategory Category, decimal Percent)[] rates) {
        Dictionary<ReimbursementCategory, ReimbursementRate> table = new();
        foreach ((ReimbursementCategory category, decimal percent) in rates) {
            table.Add(category, new ReimbursementRate(percent));
        }

        return new ReimbursementSchedule(table);
    }

}
