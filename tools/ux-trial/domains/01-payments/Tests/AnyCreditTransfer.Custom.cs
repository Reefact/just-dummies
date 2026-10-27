namespace Payments.Domain;

public sealed partial class AnyCreditTransfer {

    // No constraint says "a TARGET2 business day", so the base draws from the business days of a window.
    private static readonly DateOnly[] BusinessDays = Enumerable.Range(0, 366 * 15)
                                                                .Select(offset => new DateOnly(2020, 1, 1).AddDays(offset))
                                                                .Where(BusinessCalendar.IsBusinessDay)
                                                                .ToArray();

}
