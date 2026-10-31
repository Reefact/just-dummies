namespace MusicScore.Domain;

/// <summary>One bar of a staff: its meter, its dynamic, its voices and its repeat barlines.</summary>
public sealed class Measure
{
    public const int MaxRepeatTimes = 8;

    /// <param name="repeatEndTimes">0 when the measure carries no end-repeat barline; otherwise how many times the repeated section is played (2 to 8).</param>
    public Measure(int number, TimeSignature timeSignature, Dynamic dynamic, IReadOnlyList<Voice> voices, bool repeatStart, int repeatEndTimes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(number, 1);
        ArgumentNullException.ThrowIfNull(timeSignature);
        if (!Enum.IsDefined(dynamic))
        {
            throw new ArgumentOutOfRangeException(nameof(dynamic), dynamic, "Unknown dynamic marking.");
        }

        ArgumentNullException.ThrowIfNull(voices);
        if (voices.Count == 0 || voices.Any(voice => voice is null))
        {
            throw new ArgumentException("A measure holds at least one voice and no null voice.", nameof(voices));
        }

        if (voices.Select(voice => voice.Number).Distinct().Count() != voices.Count)
        {
            throw new ArgumentException($"Measure {number} declares the same voice number twice.", nameof(voices));
        }

        Duration expected = timeSignature.MeasureLength;
        foreach (Voice voice in voices)
        {
            if (voice.Length != expected)
            {
                throw new ArgumentException($"Voice {voice.Number} of measure {number} lasts {voice.Length} but a {timeSignature} measure lasts {expected}.", nameof(voices));
            }
        }

        if (repeatEndTimes != 0 && repeatEndTimes is < 2 or > MaxRepeatTimes)
        {
            throw new ArgumentOutOfRangeException(nameof(repeatEndTimes), repeatEndTimes, $"An end repeat plays its section 2 to {MaxRepeatTimes} times (0 means no end repeat).");
        }

        Number = number;
        TimeSignature = timeSignature;
        Dynamic = dynamic;
        Voices = voices.OrderBy(voice => voice.Number).ToArray();
        RepeatStart = repeatStart;
        RepeatEndTimes = repeatEndTimes;
    }

    public int Number { get; }

    public TimeSignature TimeSignature { get; }

    public Dynamic Dynamic { get; }

    public IReadOnlyList<Voice> Voices { get; }

    public bool RepeatStart { get; }

    public int RepeatEndTimes { get; }

    public bool RepeatEnd => RepeatEndTimes > 0;

    public Duration Length => TimeSignature.MeasureLength;

    public Voice? FindVoice(int number) => Voices.FirstOrDefault(voice => voice.Number == number);
}
