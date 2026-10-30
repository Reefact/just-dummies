// Scaffolded by `any` (JustDummies). This file is yours: read it, edit it, commit it.
// `any generate Student --force` overwrites it. This type is partial, so members you add in a
// neighbouring file survive.

using System;

using JustDummies;

namespace Education.Domain;

/// <summary>
///     A generator of arbitrary <see cref="Student" /> values. It draws from the ambient random
///     context, so a reproducibility scope pins every draw it makes; outside a test,
///     <c>Any.UseSeed(...)</c> opens one.
/// </summary>
public sealed partial class AnyStudent : IAny<Student> {

    private readonly IAny<StudentNumber> _number;
    private readonly IAny<string>        _fullName;
    private readonly IAny<DateOnly>      _birthDate;

    /// <summary>Creates the generator with a default recipe for every constructor parameter.</summary>
    public AnyStudent()
        : this(number:    AnyValidNumber(),
               fullName:  AnyValidFullName(),
               birthDate: AnyValidBirthDate()) { }

    private static AnyStudentNumber AnyValidNumber() {
        return new AnyStudentNumber();
    }

    private static AnyString AnyValidFullName() {
        return Any.String().NotBlank().WithMaxLength(100);
    }

    private static AnyDateOnly AnyValidBirthDate() {
        return Any.DateOnly().AfterOrEqualTo(new DateOnly(1900, 1, 1));
    }

    private AnyStudent(IAny<StudentNumber> number,
                       IAny<string>        fullName,
                       IAny<DateOnly>      birthDate) {
        _number    = number;
        _fullName  = fullName;
        _birthDate = birthDate;
    }

    /// <summary>Pins <c>number</c> to a fixed value.</summary>
    public AnyStudent WithNumber(StudentNumber value) {
        return new AnyStudent(new FixedValue<StudentNumber>(value), _fullName, _birthDate);
    }

    /// <summary>Draws <c>number</c> from <c>AnyValidNumber()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStudent WithNumber(Func<AnyStudentNumber, AnyStudentNumber> configure) {
        return new AnyStudent(configure(AnyValidNumber()), _fullName, _birthDate);
    }

    /// <summary>Pins <c>fullName</c> to a fixed value.</summary>
    public AnyStudent WithFullName(string value) {
        return new AnyStudent(_number, new FixedValue<string>(value), _birthDate);
    }

    /// <summary>Draws <c>fullName</c> from <c>AnyValidFullName()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStudent WithFullName(Func<AnyString, AnyString> configure) {
        return new AnyStudent(_number, configure(AnyValidFullName()), _birthDate);
    }

    /// <summary>Pins <c>birthDate</c> to a fixed value.</summary>
    public AnyStudent WithBirthDate(DateOnly value) {
        return new AnyStudent(_number, _fullName, new FixedValue<DateOnly>(value));
    }

    /// <summary>Draws <c>birthDate</c> from <c>AnyValidBirthDate()</c>, refined by <paramref name="configure" />.</summary>
    public AnyStudent WithBirthDate(Func<AnyDateOnly, AnyDateOnly> configure) {
        return new AnyStudent(_number, _fullName, configure(AnyValidBirthDate()));
    }

    /// <summary>Produces one arbitrary <see cref="Student" />.</summary>
    public Student Generate() {
        return new Student(_number.Generate(),
                           _fullName.Generate(),
                           _birthDate.Generate());
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
