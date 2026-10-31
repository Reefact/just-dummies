namespace MusicScore.Domain;

/// <summary>
/// The aggregate: a titled score whose staves share the same measures, meters and repeat barlines,
/// interpreted into the sequence of notes to perform.
/// </summary>
public sealed class Score
{
    public Score(string title, Tempo tempo, IReadOnlyList<Staff> staves)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(tempo);
        ArgumentNullException.ThrowIfNull(staves);
        if (staves.Count == 0 || staves.Any(staff => staff is null))
        {
            throw new ArgumentException("A score holds at least one staff and no null staff.", nameof(staves));
        }

        if (staves.Select(staff => staff.Name).Distinct(StringComparer.Ordinal).Count() != staves.Count)
        {
            throw new ArgumentException("Two staves of a score cannot share a name.", nameof(staves));
        }

        Staff reference = staves[0];
        foreach (Staff staff in staves.Skip(1))
        {
            if (staff.Measures.Count != reference.Measures.Count)
            {
                throw new ArgumentException($"Staff '{staff.Name}' has {staff.Measures.Count} measures but staff '{reference.Name}' has {reference.Measures.Count}.", nameof(staves));
            }

            for (int index = 0; index < reference.Measures.Count; index++)
            {
                Measure expected = reference.Measures[index];
                Measure actual = staff.Measures[index];
                if (!actual.TimeSignature.Equals(expected.TimeSignature))
                {
                    throw new ArgumentException($"Measure {actual.Number} is in {actual.TimeSignature} on staff '{staff.Name}' but in {expected.TimeSignature} on staff '{reference.Name}'.", nameof(staves));
                }

                if (actual.RepeatStart != expected.RepeatStart || actual.RepeatEndTimes != expected.RepeatEndTimes)
                {
                    throw new ArgumentException($"Measure {actual.Number} carries different repeat barlines on staff '{staff.Name}' and staff '{reference.Name}'.", nameof(staves));
                }
            }
        }

        bool open = false;
        foreach (Measure measure in reference.Measures)
        {
            if (measure.RepeatStart)
            {
                if (open)
                {
                    throw new ArgumentException($"Measure {measure.Number} opens a repeat inside a repeat that is still open; nested repeats are not supported.", nameof(staves));
                }

                open = true;
            }

            if (measure.RepeatEnd)
            {
                open = false;
            }
        }

        if (open)
        {
            throw new ArgumentException("A start repeat is never closed by an end repeat.", nameof(staves));
        }

        Title = title;
        Tempo = tempo;
        Staves = staves.ToArray();
    }

    public string Title { get; }

    public Tempo Tempo { get; }

    public IReadOnlyList<Staff> Staves { get; }

    public int MeasureCount => Staves[0].Measures.Count;

    /// <summary>
    /// The measure numbers in the order they are played. An end repeat sends playing back to the
    /// latest start repeat, or to the measure after the previous end repeat, or to the beginning.
    /// </summary>
    public IReadOnlyList<int> PlayedMeasureOrder()
    {
        IReadOnlyList<Measure> measures = Staves[0].Measures;
        List<int> order = new();
        int sectionStart = 1;
        foreach (Measure measure in measures)
        {
            if (measure.RepeatStart)
            {
                sectionStart = measure.Number;
            }

            order.Add(measure.Number);
            if (measure.RepeatEnd)
            {
                for (int pass = 2; pass <= measure.RepeatEndTimes; pass++)
                {
                    for (int number = sectionStart; number <= measure.Number; number++)
                    {
                        order.Add(number);
                    }
                }

                sectionStart = measure.Number + 1;
            }
        }

        return order;
    }

    /// <summary>The exact played length, repeats included, in whole notes.</summary>
    public Duration PlayedLength() =>
        PlayedMeasureOrder().Aggregate(Duration.Zero, (total, number) => total + Staves[0].Measures[number - 1].Length);

    /// <summary>The played length in seconds at the score's tempo.</summary>
    public double PlayedSeconds() => Tempo.SecondsFor(PlayedLength());

    /// <summary>
    /// Turns the score into the notes to perform, in played order: repeats expanded, rests silent,
    /// tied notes merged into one sounding note, simultaneous voices and staves overlapping.
    /// Ordered by onset, then staff, then voice.
    /// </summary>
    /// <exception cref="InvalidOperationException">A tie does not resolve onto the same pitch in the same voice.</exception>
    public IReadOnlyList<PerformedEvent> Interpret()
    {
        IReadOnlyList<int> order = PlayedMeasureOrder();
        List<PerformedEvent> events = new();
        for (int staffIndex = 0; staffIndex < Staves.Count; staffIndex++)
        {
            Staff staff = Staves[staffIndex];
            IEnumerable<int> voiceNumbers = staff.Measures.SelectMany(measure => measure.Voices).Select(voice => voice.Number).Distinct();
            foreach (int voiceNumber in voiceNumbers)
            {
                InterpretVoice(staff, voiceNumber, order, events);
            }
        }

        Dictionary<string, int> staffRank = Staves.Select((staff, index) => (staff.Name, index)).ToDictionary(pair => pair.Name, pair => pair.index, StringComparer.Ordinal);
        return events
            .OrderBy(performed => performed.Onset)
            .ThenBy(performed => staffRank[performed.StaffName])
            .ThenBy(performed => performed.VoiceNumber)
            .ToArray();
    }

    private void InterpretVoice(Staff staff, int voiceNumber, IReadOnlyList<int> order, List<PerformedEvent> events)
    {
        Duration measureOnset = Duration.Zero;
        Duration? tieOnset = null;
        Duration tieLength = Duration.Zero;
        Pitch? tiePitch = null;
        Dynamic tieDynamic = Dynamic.MezzoForte;

        foreach (int number in order)
        {
            Measure measure = staff.Measures[number - 1];
            Voice? voice = measure.FindVoice(voiceNumber);
            if (voice is null)
            {
                if (tieOnset is not null)
                {
                    throw new InvalidOperationException($"A tie in voice {voiceNumber} of staff '{staff.Name}' runs into measure {number}, where that voice is absent.");
                }

                measureOnset += measure.Length;
                continue;
            }

            Duration onset = measureOnset;
            foreach (VoiceEntry entry in voice.Entries)
            {
                if (tieOnset is not null)
                {
                    if (entry.Pitch is null || !entry.Pitch.Equals(tiePitch))
                    {
                        throw new InvalidOperationException($"A tie on {tiePitch} in voice {voiceNumber} of staff '{staff.Name}' does not resolve onto the same pitch in measure {number}.");
                    }

                    tieLength += entry.Duration;
                    if (!entry.TiedToNext)
                    {
                        events.Add(CreateEvent(staff, voiceNumber, tiePitch, tieDynamic, tieOnset, tieLength));
                        tieOnset = null;
                        tiePitch = null;
                    }
                }
                else if (entry.Pitch is not null)
                {
                    if (entry.TiedToNext)
                    {
                        tieOnset = onset;
                        tieLength = entry.Duration;
                        tiePitch = entry.Pitch;
                        tieDynamic = measure.Dynamic;
                    }
                    else
                    {
                        events.Add(CreateEvent(staff, voiceNumber, entry.Pitch, measure.Dynamic, onset, entry.Duration));
                    }
                }

                onset += entry.Duration;
            }

            measureOnset += measure.Length;
        }

        if (tieOnset is not null)
        {
            throw new InvalidOperationException($"The last note on {tiePitch} in voice {voiceNumber} of staff '{staff.Name}' is tied to nothing.");
        }
    }

    private PerformedEvent CreateEvent(Staff staff, int voiceNumber, Pitch pitch, Dynamic dynamic, Duration onset, Duration length) =>
        new(staff.Name, voiceNumber, pitch, VelocityOf(dynamic), onset, length, Tempo.SecondsFor(onset), Tempo.SecondsFor(length));

    private static int VelocityOf(Dynamic dynamic) => dynamic switch
    {
        Dynamic.Pianissimo => 33,
        Dynamic.Piano => 49,
        Dynamic.MezzoPiano => 64,
        Dynamic.MezzoForte => 80,
        Dynamic.Forte => 96,
        Dynamic.Fortissimo => 112,
        _ => throw new ArgumentOutOfRangeException(nameof(dynamic), dynamic, "Unknown dynamic marking."),
    };
}
