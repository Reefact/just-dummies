namespace MusicScore.Domain;

/// <summary>A monophonic line within one measure of a staff.</summary>
public sealed class Voice
{
    public const int MaxVoices = 4;

    public Voice(int number, IReadOnlyList<VoiceEntry> entries)
    {
        if (number is < 1 or > MaxVoices)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, $"A voice is numbered from 1 to {MaxVoices}.");
        }

        ArgumentNullException.ThrowIfNull(entries);
        if (entries.Count == 0)
        {
            throw new ArgumentException("A voice holds at least one note or rest.", nameof(entries));
        }

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException("A voice cannot hold a null entry.", nameof(entries));
        }

        Number = number;
        Entries = entries.ToArray();
        Length = Entries.Aggregate(Duration.Zero, (total, entry) => total + entry.Duration);
    }

    public int Number { get; }

    public IReadOnlyList<VoiceEntry> Entries { get; }

    public Duration Length { get; }
}
