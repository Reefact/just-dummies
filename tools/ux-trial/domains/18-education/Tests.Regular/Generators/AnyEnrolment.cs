// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Enrolment --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// A TODO(any) above a `_ = TODO_verify_…` line names a guard the scaffolder could not read; the recipe
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

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

    private IAny<Student>               _student;
    private IAny<Semester>              _semester;
    private IAny<IReadOnlyList<Course>> _courses;

    private AnyStudent      _studentRecipe;
    private AnySemester     _semesterRecipe;
    private AnyList<Course> _coursesRecipe;

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
        // Distinct codes are a rule of the enrolment. The credit ceiling (36 over at most 8 courses) is kept
        // by construction: 4 credits per course at most can never reach 36. Larger courses are pinned by the tests that need them.
        return Any.ListOf(new AnyCourse().WithCredits(c => c.Between(1, 4)))
                  .WithCountBetween(1, Enrolment.MaximumCourses)
                  .DistinctBy(course => course.Code);
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyEnrolment(AnyStudent      student,
                         AnySemester     semester,
                         AnyList<Course> courses) {
        _student        = student;
        _studentRecipe  = student;
        _semester       = semester;
        _semesterRecipe = semester;
        _courses        = courses;
        _coursesRecipe  = courses;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyEnrolment(AnyEnrolment from) {
        _student        = from._student;
        _studentRecipe  = from._studentRecipe;
        _semester       = from._semester;
        _semesterRecipe = from._semesterRecipe;
        _courses        = from._courses;
        _coursesRecipe  = from._coursesRecipe;
    }

    /// <summary>Pins <c>student</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnrolment WithStudent(Student value) {
        AnyEnrolment copy = new AnyEnrolment(this);

        copy._student = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>student</c> is drawn from — <c>AnyValidStudent()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudent, AnyStudent&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnrolment WithStudent(Func<AnyStudent, AnyStudent> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnrolment copy = new AnyEnrolment(this);

        copy._studentRecipe = configure(_studentRecipe);
        copy._student       = copy._studentRecipe;

        return copy;
    }

    /// <summary>Pins <c>semester</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnrolment WithSemester(Semester value) {
        AnyEnrolment copy = new AnyEnrolment(this);

        copy._semester = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>semester</c> is drawn from — <c>AnyValidSemester()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnySemester, AnySemester&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnrolment WithSemester(Func<AnySemester, AnySemester> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnrolment copy = new AnyEnrolment(this);

        copy._semesterRecipe = configure(_semesterRecipe);
        copy._semester       = copy._semesterRecipe;

        return copy;
    }

    /// <summary>Pins <c>courses</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyEnrolment WithCourses(IReadOnlyList<Course> value) {
        AnyEnrolment copy = new AnyEnrolment(this);

        copy._courses = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>courses</c> is drawn from — <c>AnyValidCourses()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyList&lt;Course&gt;, AnyList&lt;Course&gt;&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyEnrolment WithCourses(Func<AnyList<Course>, AnyList<Course>> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyEnrolment copy = new AnyEnrolment(this);

        copy._coursesRecipe = configure(_coursesRecipe);
        copy._courses       = copy._coursesRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Enrolment" />.</summary>
    public Enrolment Generate() {
        return new Enrolment(_student.Generate(),
                             _semester.Generate(),
                             _courses.Generate());
    }

}
