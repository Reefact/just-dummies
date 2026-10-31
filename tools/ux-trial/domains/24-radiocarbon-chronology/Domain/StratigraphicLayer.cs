using System.Text.RegularExpressions;

namespace RadiocarbonChronology.Domain;

/// <summary>A stratigraphic unit of the excavation (for example "US12") and the samples taken from it.</summary>
public sealed partial class StratigraphicLayer
{
    private readonly List<Sample> _samples = new();

    public StratigraphicLayer(string code, string description)
    {
        if (code is null)
        {
            throw new ArgumentNullException(nameof(code));
        }

        if (!CodeFormat().IsMatch(code))
        {
            throw new FormatException($"'{code}' is not a layer code: expected one to three capital letters then one to four digits, such as 'US12'.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A layer needs a description.", nameof(description));
        }

        if (description.Length > 500)
        {
            throw new ArgumentException("A layer description is limited to 500 characters.", nameof(description));
        }

        Code = code;
        Description = description;
    }

    public string Code { get; }

    public string Description { get; }

    public IReadOnlyList<Sample> Samples => _samples;

    internal void Add(Sample sample) => _samples.Add(sample);

    [GeneratedRegex("^[A-Z]{1,3}[0-9]{1,4}$")]
    private static partial Regex CodeFormat();
}
