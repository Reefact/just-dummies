namespace ClinicalTrial.Domain;

/// <summary>The randomisation of a subject: number, date (study day 1) and allocated arm.</summary>
public sealed class Randomisation : IEquatable<Randomisation>
{
    public Randomisation(int number, DateOnly date, TreatmentArm arm)
    {
        if (number < 1 || number > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "A randomisation number is between 1 and 9999.");
        }

        if (!Enum.IsDefined(arm))
        {
            throw new ArgumentOutOfRangeException(nameof(arm), arm, "Unknown treatment arm.");
        }

        Number = number;
        Date = date;
        Arm = arm;
    }

    public int Number { get; }

    public DateOnly Date { get; }

    public TreatmentArm Arm { get; }

    /// <summary>
    /// The study day of a date: the randomisation date is day 1, the day after is day 2, the day before is day -1.
    /// There is no day 0.
    /// </summary>
    public int StudyDayOf(DateOnly date)
    {
        int offset = date.DayNumber - Date.DayNumber;
        return offset >= 0 ? offset + 1 : offset;
    }

    /// <summary>The calendar date of a study day (day 0 does not exist).</summary>
    public DateOnly DateOfStudyDay(int studyDay)
    {
        if (studyDay == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(studyDay), studyDay, "There is no study day 0.");
        }

        return Date.AddDays(studyDay > 0 ? studyDay - 1 : studyDay);
    }

    public bool Equals(Randomisation? other) =>
        other is not null && other.Number == Number && other.Date == Date && other.Arm == Arm;

    public override bool Equals(object? obj) => Equals(obj as Randomisation);

    public override int GetHashCode() => HashCode.Combine(Number, Date, Arm);
}
