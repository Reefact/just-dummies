namespace DocsMisc.Domain;

public sealed class Course {
    public Course(int credits) {
        if (credits < 1 || credits > 10) { throw new ArgumentOutOfRangeException(nameof(credits), credits, "Credits must be 1 to 10."); }
        Credits = credits;
    }

    public int Credits { get; }
}
