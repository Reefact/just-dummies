// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Student --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.
// under it is its best generator for the type. Verify it honours that guard, or replace it, then delete
// those two lines. What the scaffolder reads, and what it cannot:
// https://github.com/Reefact/just-dummies/blob/main/doc/handwritten/for-users/packages/justdummies-cli.en.md#what-the-last-column-means

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Student" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStudent : IAny<Student> {

    private IAny<StudentNumber> _number;
    private IAny<string>        _fullName;
    private IAny<DateOnly>      _birthDate;

    private AnyStudentNumber _numberRecipe;
    private AnyString        _fullNameRecipe;
    private AnyDateOnly      _birthDateRecipe;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStudent()
        : this(number:    AnyValidNumber(),
               fullName:  AnyValidFullName(),
               birthDate: AnyValidBirthDate()) { }

    private static AnyStudentNumber AnyValidNumber() {
        return new AnyStudentNumber();
    }

    private static AnyString AnyValidFullName() {
        return Any.String().Alpha().WithLengthBetween(1, 100);
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1));
    }

    /// <summary>Draws every parameter from the recipe given for it.</summary>
    private AnyStudent(AnyStudentNumber number,
                       AnyString        fullName,
                       AnyDateOnly      birthDate) {
        _number          = number;
        _numberRecipe    = number;
        _fullName        = fullName;
        _fullNameRecipe  = fullName;
        _birthDate       = birthDate;
        _birthDateRecipe = birthDate;
    }

    /// <summary>Copies <paramref name="from" />, for a <c>With…</c> method to change one parameter of before handing it out.</summary>
    private AnyStudent(AnyStudent from) {
        _number          = from._number;
        _numberRecipe    = from._numberRecipe;
        _fullName        = from._fullName;
        _fullNameRecipe  = from._fullNameRecipe;
        _birthDate       = from._birthDate;
        _birthDateRecipe = from._birthDateRecipe;
    }

    /// <summary>Pins <c>number</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStudent WithNumber(StudentNumber value) {
        AnyStudent copy = new AnyStudent(this);

        copy._number = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>number</c> is drawn from — <c>AnyValidNumber()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyStudentNumber, AnyStudentNumber&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStudent WithNumber(Func<AnyStudentNumber, AnyStudentNumber> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStudent copy = new AnyStudent(this);

        copy._numberRecipe = configure(_numberRecipe);
        copy._number       = copy._numberRecipe;

        return copy;
    }

    /// <summary>Pins <c>fullName</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStudent WithFullName(string value) {
        AnyStudent copy = new AnyStudent(this);

        copy._fullName = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>fullName</c> is drawn from — <c>AnyValidFullName()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyString, AnyString&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStudent WithFullName(Func<AnyString, AnyString> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStudent copy = new AnyStudent(this);

        copy._fullNameRecipe = configure(_fullNameRecipe);
        copy._fullName       = copy._fullNameRecipe;

        return copy;
    }

    /// <summary>Pins <c>birthDate</c> to <paramref name="value" />, which every draw hands over as it is — one shared instance for a reference type; its recipe is kept for a later refinement.</summary>
    public AnyStudent WithBirthDate(DateOnly value) {
        AnyStudent copy = new AnyStudent(this);

        copy._birthDate = Any.Fixed(value);

        return copy;
    }

    /// <summary>Refines the recipe <c>birthDate</c> is drawn from — <c>AnyValidBirthDate()</c>, then each earlier refinement — through <paramref name="configure" />.</summary>
    /// <remarks>Reuse a refinement as a <c>Func&lt;AnyDateOnly, AnyDateOnly&gt;</c> that builds on what it receives; one that ignores it, <c>_ =&gt; prepared</c>, compiles and replaces the recipe, every rule of the base with it.</remarks>
    public AnyStudent WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        if (configure is null) { throw new ArgumentNullException(nameof(configure)); }

        AnyStudent copy = new AnyStudent(this);

        copy._birthDateRecipe = configure(_birthDateRecipe);
        copy._birthDate       = copy._birthDateRecipe;

        return copy;
    }

    /// <summary>Produces one arbitrary <see cref="Student" />.</summary>
    public Student Generate() {
        return new Student(_number.Generate(),
                           _fullName.Generate(),
                           _birthDate.Generate());
    }

}
