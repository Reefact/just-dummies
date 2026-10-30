// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Enrolment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;
using System.Collections.Generic;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Enrolment" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyEnrolment : IAny<Enrolment> {

    private readonly IAny<Student>               _student;
    private readonly IAny<Semester>              _semester;
    private readonly IAny<IReadOnlyList<Course>> _courses;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyEnrolment()
        : this(student:  AnyValidStudent(),
               semester: AnyValidSemester(),
               courses:  AnyValidCourses()) { }

    private static AnyStudent AnyValidStudent() {
        return new AnyStudent();
    }

    private static AnySemester AnyValidSemester() {
        return new AnySemester();
    }

    private static AnyList<Course> AnyValidCourses() {
        // 1 to 6 distinct courses of at most 6 credits each: never above the 36-credit cap.
        return Any.ListOf(new AnyCourse().WithCredits(credits => credits.LessThanOrEqualTo(6))).WithCountBetween(1, 6).Distinct();
    }

    private AnyEnrolment(IAny<Student>               student,
                         IAny<Semester>              semester,
                         IAny<IReadOnlyList<Course>> courses) {
        _student  = student;
        _semester = semester;
        _courses  = courses;
    }

    /// <summary>Pins <c>student</c> to a fixed value.</summary>
    public AnyEnrolment WithStudent(Student value) {
        return new AnyEnrolment(new FixedValue<Student>(value), _semester, _courses);
    }

    /// <summary>Draws <c>student</c> from <c>AnyValidStudent()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEnrolment WithStudent(Func<AnyStudent, AnyStudent> configure) {
        return new AnyEnrolment(configure(AnyValidStudent()), _semester, _courses);
    }

    /// <summary>Pins <c>semester</c> to a fixed value.</summary>
    public AnyEnrolment WithSemester(Semester value) {
        return new AnyEnrolment(_student, new FixedValue<Semester>(value), _courses);
    }

    /// <summary>Draws <c>semester</c> from <c>AnyValidSemester()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEnrolment WithSemester(Func<AnySemester, AnySemester> configure) {
        return new AnyEnrolment(_student, configure(AnyValidSemester()), _courses);
    }

    /// <summary>Pins <c>courses</c> to a fixed value.</summary>
    public AnyEnrolment WithCourses(IReadOnlyList<Course> value) {
        return new AnyEnrolment(_student, _semester, new FixedValue<IReadOnlyList<Course>>(value));
    }

    /// <summary>Draws <c>courses</c> from <c>AnyValidCourses()</c>, refined by <paramref name="configure" />.</summary>
    public AnyEnrolment WithCourses(Func<AnyList<Course>, AnyList<Course>> configure) {
        return new AnyEnrolment(_student, _semester, configure(AnyValidCourses()));
    }

    /// <summary>Produces one arbitrary <see cref="Enrolment" />.</summary>
    public Enrolment Generate() {
        return new Enrolment(_student.Generate(),
                             _semester.Generate(),
                             _courses.Generate());
    }

    private sealed class FixedValue<TValue> : IAny<TValue> {

        private readonly TValue _value;

        public FixedValue(TValue value) {
            _value = value;
        }

        public TValue Generate() {
            return _value;
        }

    }

}
