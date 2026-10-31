namespace MusicScore.Domain;

/// <summary>A named staff (e.g. "Violin I", "Piano RH") holding consecutive measures numbered from 1.</summary>
public sealed class Staff
{
    public const int MaxNameLength = 40;

    public Staff(string name, IReadOnlyList<Measure> measures)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > MaxNameLength || name.Trim() != name)
        {
            throw new ArgumentException($"A staff name has at most {MaxNameLength} characters and no surrounding blanks.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(measures);
        if (measures.Count == 0 || measures.Any(measure => measure is null))
        {
            throw new ArgumentException("A staff holds at least one measure and no null measure.", nameof(measures));
        }

        for (int index = 0; index < measures.Count; index++)
        {
            if (measures[index].Number != index + 1)
            {
                throw new ArgumentException($"Staff '{name}' expects measure {index + 1} at position {index + 1} but found measure {measures[index].Number}.", nameof(measures));
            }
        }

        Name = name;
        Measures = measures.ToArray();
    }

    public string Name { get; }

    public IReadOnlyList<Measure> Measures { get; }
}
