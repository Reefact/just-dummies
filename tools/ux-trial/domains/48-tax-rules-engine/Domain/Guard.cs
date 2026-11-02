namespace TaxRules.Domain;

internal static class Guard
{
    public const int MaxLabelLength = 80;

    public static IncomeKind IncomeKinds(IncomeKind kinds, string parameterName)
    {
        if (kinds == IncomeKind.None || (kinds & ~IncomeKind.All) != 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, kinds, "A tax base names at least one known income kind.");
        }

        return kinds;
    }

    public static string Label(string label, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label, parameterName);
        if (label.Length > MaxLabelLength)
        {
            throw new ArgumentException($"A label holds at most {MaxLabelLength} characters.", parameterName);
        }

        return label;
    }
}
