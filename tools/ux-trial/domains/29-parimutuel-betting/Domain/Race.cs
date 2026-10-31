using System.Text.RegularExpressions;

namespace ParimutuelBetting.Domain;

/// <summary>A race on a meeting card, identified by a code such as <c>LCH-R4</c>.</summary>
public sealed partial class Race
{
    /// <summary>The fewest declared runners a race may have.</summary>
    public const int MinimumFieldSize = 2;

    public Race(string code, int fieldSize)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (!CodeFormat().IsMatch(code))
        {
            throw new FormatException($"A race code is a three-letter course code, a dash and R1 to R16 (for example LCH-R4); got '{code}'.");
        }

        if (fieldSize < MinimumFieldSize || fieldSize > HorseNumber.Maximum)
        {
            throw new ArgumentOutOfRangeException(nameof(fieldSize), fieldSize, $"A field has between {MinimumFieldSize} and {HorseNumber.Maximum} runners.");
        }

        Code = code;
        FieldSize = fieldSize;
    }

    public string Code { get; }

    /// <summary>The number of declared runners, numbered 1 to <see cref="FieldSize"/>.</summary>
    public int FieldSize { get; }

    /// <summary>Whether the horse is one of the declared runners.</summary>
    public bool IsRunner(HorseNumber horse)
    {
        ArgumentNullException.ThrowIfNull(horse);
        return horse.Value <= FieldSize;
    }

    [GeneratedRegex("^[A-Z]{3}-R([1-9]|1[0-6])$")]
    private static partial Regex CodeFormat();
}
