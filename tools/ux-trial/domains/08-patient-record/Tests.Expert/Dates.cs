using System.Globalization;

namespace Clinical.Tests.Expert;

internal static class Dates {

    /// <summary>A date written as a reader writes it: <c>Dates.On("2025-03-10")</c>.</summary>
    public static DateOnly On(string isoDate) {
        return DateOnly.ParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

}
