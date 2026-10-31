namespace RadiocarbonChronology.Domain;

/// <summary>One node of a calibration curve: the radiocarbon age the atmosphere recorded in a calendar year.</summary>
public sealed class CalibrationPoint
{
    public CalibrationPoint(int calendarYear, double radiocarbonYearsBp, double uncertainty)
    {
        if (calendarYear < CalendarInterval.EarliestYear || calendarYear > CalendarInterval.LatestYear)
        {
            throw new ArgumentOutOfRangeException(nameof(calendarYear), calendarYear, $"A calendar year must lie between {CalendarInterval.EarliestYear} and {CalendarInterval.LatestYear}.");
        }

        if (double.IsNaN(radiocarbonYearsBp) || radiocarbonYearsBp < 0 || radiocarbonYearsBp > RadiocarbonAge.MaximumYearsBp)
        {
            throw new ArgumentOutOfRangeException(nameof(radiocarbonYearsBp), radiocarbonYearsBp, $"A curve age must lie between 0 and {RadiocarbonAge.MaximumYearsBp} years BP.");
        }

        if (double.IsNaN(uncertainty) || uncertainty <= 0 || uncertainty > RadiocarbonAge.MaximumUncertainty)
        {
            throw new ArgumentOutOfRangeException(nameof(uncertainty), uncertainty, $"A curve uncertainty must be positive and at most {RadiocarbonAge.MaximumUncertainty} years.");
        }

        CalendarYear = calendarYear;
        RadiocarbonYearsBp = radiocarbonYearsBp;
        Uncertainty = uncertainty;
    }

    public int CalendarYear { get; }

    public double RadiocarbonYearsBp { get; }

    public double Uncertainty { get; }
}
