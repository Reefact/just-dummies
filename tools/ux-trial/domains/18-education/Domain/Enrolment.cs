namespace Education.Domain;

// A student's registration to a set of courses for one semester, and the grades it collects.
public sealed class Enrolment {

    public const int MaximumCourses = 8;
    public const int MaximumCredits = 36;
    public const decimal PassMark   = 10m;

    private readonly List<Course>               _courses;
    private readonly Dictionary<string, Grade> _grades = new Dictionary<string, Grade>();

    public Enrolment(Student student, Semester semester, IReadOnlyList<Course> courses) {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(semester);
        ArgumentNullException.ThrowIfNull(courses);
        if (courses.Count == 0 || courses.Count > MaximumCourses) {
            throw new ArgumentException($"An enrolment holds 1 to {MaximumCourses} courses, not {courses.Count}.", nameof(courses));
        }
        if (courses.Select(course => course.Code).Distinct().Count() != courses.Count) {
            throw new ArgumentException("A course appears twice in the enrolment.", nameof(courses));
        }
        if (courses.Sum(course => course.Credits) > MaximumCredits) {
            throw new ArgumentException($"An enrolment carries at most {MaximumCredits} credits.", nameof(courses));
        }

        Student   = student;
        Semester  = semester;
        _courses  = courses.ToList();
        Status    = EnrolmentStatus.Open;
    }

    public Student Student { get; }

    public Semester Semester { get; }

    public IReadOnlyList<Course> Courses => _courses;

    public EnrolmentStatus Status { get; private set; }

    public int Credits => _courses.Sum(course => course.Credits);

    public void RecordGrade(string courseCode, Grade grade) {
        ArgumentNullException.ThrowIfNull(grade);
        if (Status == EnrolmentStatus.Closed) {
            throw new InvalidOperationException("The enrolment is closed; grades are final.");
        }
        if (_courses.All(course => course.Code != courseCode)) {
            throw new InvalidOperationException($"The student is not enrolled in {courseCode}.");
        }

        _grades[courseCode] = grade;
    }

    public Grade? GradeFor(string courseCode) => _grades.TryGetValue(courseCode, out Grade? grade) ? grade : null;

    public void Close() {
        if (_grades.Count != _courses.Count) {
            throw new InvalidOperationException($"{_courses.Count - _grades.Count} course(s) still have no grade.");
        }

        Status = EnrolmentStatus.Closed;
    }

    // The credit-weighted average, rounded to two decimal places (half away from zero).
    public decimal Average() {
        if (Status != EnrolmentStatus.Closed) {
            throw new InvalidOperationException("The average is computed once the enrolment is closed.");
        }

        decimal weighted = _courses.Sum(course => _grades[course.Code].Value * course.Credits);

        return Math.Round(weighted / Credits, 2, MidpointRounding.AwayFromZero);
    }

    // Passed with an average of at least 10, and no eliminatory grade (below 5) anywhere.
    public bool IsPassed() => Average() >= PassMark && _grades.Values.All(grade => !grade.IsEliminatory);

    // The credits acquired: every course passed with 10 or more, or all of them when the semester is passed (compensation).
    public int AcquiredCredits() {
        if (IsPassed()) {
            return Credits;
        }

        return _courses.Where(course => _grades[course.Code].Value >= PassMark).Sum(course => course.Credits);
    }

    public Honours Honours() {
        if (!IsPassed()) {
            return Domain.Honours.None;
        }

        return Average() switch {
            >= 16m => Domain.Honours.VeryGood,
            >= 14m => Domain.Honours.Good,
            >= 12m => Domain.Honours.Fair,
            _      => Domain.Honours.Pass
        };
    }
}
