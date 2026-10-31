namespace CarbonLedger.Domain;

/// <summary>Who a retirement offsets emissions for, why, and for which reporting year.</summary>
public sealed class RetirementClaim
{
    public const int MinReportingYear = 2000;
    public const int MaxReportingYear = 2100;
    public const int MaxTextLength = 500;

    public RetirementClaim(string beneficiary, string purpose, int reportingYear)
    {
        ArgumentNullException.ThrowIfNull(beneficiary);
        ArgumentNullException.ThrowIfNull(purpose);
        if (string.IsNullOrWhiteSpace(beneficiary) || beneficiary.Length > MaxTextLength)
        {
            throw new ArgumentException($"A beneficiary is 1 to {MaxTextLength} non-blank characters.", nameof(beneficiary));
        }

        if (string.IsNullOrWhiteSpace(purpose) || purpose.Length > MaxTextLength)
        {
            throw new ArgumentException($"A purpose is 1 to {MaxTextLength} non-blank characters.", nameof(purpose));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(reportingYear, MinReportingYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(reportingYear, MaxReportingYear);

        Beneficiary = beneficiary;
        Purpose = purpose;
        ReportingYear = reportingYear;
    }

    public string Beneficiary { get; }

    public string Purpose { get; }

    public int ReportingYear { get; }
}
