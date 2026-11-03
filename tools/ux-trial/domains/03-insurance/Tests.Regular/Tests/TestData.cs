using Insurance.Domain;

using JustDummies;

namespace Insurance.Tests.Regular;

internal static class TestData {

    /// <summary>One claim per responsibility given, each dated inside the period, so they can be handed to a policy over it.</summary>
    public static IReadOnlyList<Claim> ClaimsIn(CoveragePeriod period, params Responsibility[] responsibilities) {
        List<Claim> claims = new List<Claim>();
        foreach (Responsibility responsibility in responsibilities) {
            DateOnly day = Any.DateOnly().Between(period.Start, period.End).Generate();
            claims.Add(Any.Claim().WithOccurredOn(day).WithResponsibility(responsibility).Generate());
        }

        return claims;
    }

    public static Claim ClaimOn(DateOnly day) {
        return Any.Claim().WithOccurredOn(day).Generate();
    }

    public static DateOnly Day(string iso) {
        return DateOnly.ParseExact(iso, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

}
